using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddComputedColumnDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs");

            migrationBuilder.AddColumn<int>(
                name: "DaysInShelter",
                table: "Animals",
                type: "int",
                nullable: false,
                computedColumnSql: "dbo.fn_DaysInShelter(DateAdded)");

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DaysInShelter",
                table: "Animals");

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
