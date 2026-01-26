using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Animals_AnimalId",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests");

            migrationBuilder.AlterColumn<int>(
                name: "AnimalId",
                table: "AdoptionRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Animals_AnimalId",
                table: "AdoptionRequests",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_AdoptionRequests_Animals_AnimalId",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_UserId",
                table: "AdoptionRequests");

            migrationBuilder.AlterColumn<int>(
                name: "AnimalId",
                table: "AdoptionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Animals_AnimalId",
                table: "AdoptionRequests",
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
                onDelete: ReferentialAction.Restrict);
        }
    }
}
