using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations

{
    /// <inheritdoc />
    public partial class UpdateUsernameininventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UpdatebyUserId",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatebyUserId",
                table: "Inventories");
        }
    }
}
