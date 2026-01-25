using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProceduresAndFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. FUNKCJA: Oblicza ile dni zwierzak jest w schronisku
            migrationBuilder.Sql(@"
                CREATE FUNCTION fn_DaysInShelter (@DateAdded DATETIME)
                RETURNS INT
                AS
                BEGIN
                    RETURN DATEDIFF(day, @DateAdded, GETDATE())
                END
            ");

            // 2. PROCEDURA: Zwraca proste statystyki
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetShelterStatistics
                AS
                BEGIN
                    SET NOCOUNT ON;
            
                    SELECT 
                        (SELECT COUNT(*) FROM Animals WHERE Status = 'Do adopcji' OR Status = 'Available') as AvailableCount,
                        (SELECT COUNT(*) FROM Animals WHERE Status = 'Zaadoptowany' OR Status = 'Adopted') as AdoptedCount,
                        -- TUTAJ JEST FIX: Uwzględniamy Oczekujący ORAZ Pending
                        (SELECT COUNT(*) FROM AdoptionRequests WHERE Status = 'Oczekujący' OR Status = 'Pending') as PendingRequests,
                         -- Dodajemy średni czas, o którym rozmawialiśmy wcześniej
                        (SELECT ISNULL(AVG(dbo.fn_DaysInShelter(Id)), 0) FROM Animals) as AverageTime
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_GetShelterStatistics");
            migrationBuilder.Sql("DROP FUNCTION fn_DaysInShelter");
        }
    }
}
