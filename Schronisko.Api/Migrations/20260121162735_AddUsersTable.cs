using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adoptions_Animals_AnimalId",
                table: "Adoptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Adoptions",
                table: "Adoptions");

            migrationBuilder.RenameTable(
                name: "Adoptions",
                newName: "Adoption");

            migrationBuilder.RenameIndex(
                name: "IX_Adoptions_AnimalId",
                table: "Adoption",
                newName: "IX_Adoption_AnimalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Adoption",
                table: "Adoption",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_UserId",
                table: "AdoptionRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Adoption_Animals_AnimalId",
                table: "Adoption",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adoption_Animals_AnimalId",
                table: "Adoption");

            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_UserId",
                table: "AdoptionRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Adoption",
                table: "Adoption");

            migrationBuilder.RenameTable(
                name: "Adoption",
                newName: "Adoptions");

            migrationBuilder.RenameIndex(
                name: "IX_Adoption_AnimalId",
                table: "Adoptions",
                newName: "IX_Adoptions_AnimalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Adoptions",
                table: "Adoptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Adoptions_Animals_AnimalId",
                table: "Adoptions",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
