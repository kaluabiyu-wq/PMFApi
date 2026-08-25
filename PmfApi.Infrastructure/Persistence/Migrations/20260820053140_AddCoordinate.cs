using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations

{
    /// <inheritdoc />
    public partial class AddCoordinate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "medicineSearch",
                table: "Searches",
                newName: "MedicineSearch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MedicineSearch",
                table: "Searches",
                newName: "medicineSearch");
        }
    }
}
