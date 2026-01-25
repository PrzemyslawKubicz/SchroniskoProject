using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TWORZENIE TRIGGERA
            // Kiedy status wniosku zmieni się na 'Zatwierdzony',
            // ustaw status powiązanego zwierzaka na 'Zaadoptowany'.

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_ApproveAdoption
                ON AdoptionRequests
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Sprawdź czy zaktualizowano status na 'Zatwierdzony'
                    IF EXISTS (SELECT * FROM inserted WHERE Status = 'Zatwierdzony')
                    BEGIN
                        UPDATE Animals
                        SET Status = 'Zaadoptowany'
                        FROM Animals a
                        JOIN inserted i ON a.Id = i.AnimalId
                        WHERE i.Status = 'Zatwierdzony';
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER trg_ApproveAdoption");
        }
    }
}
