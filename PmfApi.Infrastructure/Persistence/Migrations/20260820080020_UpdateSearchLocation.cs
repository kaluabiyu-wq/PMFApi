using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations

{
    /// <inheritdoc />
    public partial class UpdateSearchLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserLatitude",
                table: "Searches");

            migrationBuilder.DropColumn(
                name: "UserLongitude",
                table: "Searches");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Searches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Searches_LocationId",
                table: "Searches",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Searches_Locations_LocationId",
                table: "Searches",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Searches_Locations_LocationId",
                table: "Searches");

            migrationBuilder.DropIndex(
                name: "IX_Searches_LocationId",
                table: "Searches");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Searches");

            migrationBuilder.AddColumn<decimal>(
                name: "UserLatitude",
                table: "Searches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UserLongitude",
                table: "Searches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
