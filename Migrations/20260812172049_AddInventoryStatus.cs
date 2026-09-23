using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Inventories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Inventories");
        }
    }
}
