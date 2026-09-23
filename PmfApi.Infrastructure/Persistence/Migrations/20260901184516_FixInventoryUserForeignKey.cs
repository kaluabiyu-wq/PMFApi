using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixInventoryUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Users_UserId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_UserId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Inventories");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_UpdatebyUserId",
                table: "Inventories",
                column: "UpdatebyUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Users_UpdatebyUserId",
                table: "Inventories",
                column: "UpdatebyUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Users_UpdatebyUserId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_UpdatebyUserId",
                table: "Inventories");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_UserId",
                table: "Inventories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Users_UserId",
                table: "Inventories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
