using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace home_budget_app.Migrations
{
    /// <inheritdoc />
    public partial class EntityFrameworkDalszaPraca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_AspNetUsers_ApplicationUserId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_AspNetUsers_ApplicationUserId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_ApplicationUserId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ApplicationUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Expenses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Incomes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_ApplicationUserId",
                table: "Incomes",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ApplicationUserId",
                table: "Expenses",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_AspNetUsers_ApplicationUserId",
                table: "Expenses",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_AspNetUsers_ApplicationUserId",
                table: "Incomes",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
