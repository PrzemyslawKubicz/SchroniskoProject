using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial_NewStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================================
            // Funkcja
            // =========================================================================
            migrationBuilder.Sql(@"
                CREATE OR ALTER FUNCTION dbo.fn_DaysInShelter (@DateAdded DATETIME)
                RETURNS INT
                AS
                BEGIN
                    RETURN DATEDIFF(day, @DateAdded, GETDATE())
                END
            ");

            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Species = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysInShelter = table.Column<int>(type: "int", nullable: false, computedColumnSql: "dbo.fn_DaysInShelter(DateAdded)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdoptionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: true), // Ważne: Nullable!
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdoptionRequests_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull); // Ważne: SetNull!
                    table.ForeignKey(
                        name: "FK_AdoptionRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_AnimalId",
                table: "AdoptionRequests",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_UserId",
                table: "AdoptionRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserId",
                table: "Logs",
                column: "UserId");


            // A) Procedura Statystyk
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetShelterStatistics
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        (SELECT COUNT(*) FROM Animals WHERE Status = 'Do adopcji') as AvailableCount,
                        (SELECT COUNT(*) FROM AdoptionRequests WHERE Status = 'Zatwierdzony') as AdoptedCount,
                        (SELECT COUNT(*) FROM AdoptionRequests WHERE Status = 'Oczekujący') as PendingRequests,
                        (SELECT ISNULL(AVG(dbo.fn_DaysInShelter(DateAdded)), 0) FROM Animals) as AverageTime
                END
            ");

            // B) Trigger
            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER trg_ApproveAdoption
                ON AdoptionRequests
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted WHERE Status = 'Zatwierdzony')
                    BEGIN
                        -- 1. Zmień status psa na 'Zaadoptowany'
                        UPDATE Animals
                        SET Status = 'Zaadoptowany'
                        FROM Animals a
                        JOIN inserted i ON a.Id = i.AnimalId
                        WHERE i.Status = 'Zatwierdzony';

                        -- 2. Odrzuć inne oczekujące wnioski na tego samego psa
                        UPDATE AdoptionRequests
                        SET Status = 'Odrzucony'
                        FROM AdoptionRequests ar
                        JOIN inserted i ON ar.AnimalId = i.AnimalId
                        WHERE ar.Id != i.Id AND ar.Status = 'Oczekujący';
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Przy usuwaniu bazy (Revert) najpierw usuwamy nasze dodatki
            migrationBuilder.Sql("DROP PROCEDURE sp_GetShelterStatistics");
            migrationBuilder.Sql("DROP TRIGGER trg_ApproveAdoption");
            migrationBuilder.Sql("DROP FUNCTION fn_DaysInShelter");

            migrationBuilder.DropTable(name: "AdoptionRequests");
            migrationBuilder.DropTable(name: "Logs");
            migrationBuilder.DropTable(name: "Animals");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}