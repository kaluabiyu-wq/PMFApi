using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Migrations
{
    /// <inheritdoc />
    public partial class RefinePmfModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Medicines_MedicineId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Pharmacies_PharmacyId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_Locations_LocationId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_MedicineId",
                table: "Inventories");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LicenceNumber",
                table: "Pharmacies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "GenericName",
                table: "Medicines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Locations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Locations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_LicenceNumber",
                table: "Pharmacies",
                column: "LicenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Latitude_Longitude",
                table: "Locations",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_MedicineId_PharmacyId",
                table: "Inventories",
                columns: new[] { "MedicineId", "PharmacyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Medicines_MedicineId",
                table: "Inventories",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Pharmacies_PharmacyId",
                table: "Inventories",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_Locations_LocationId",
                table: "Pharmacies",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Medicines_MedicineId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Pharmacies_PharmacyId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_Locations_LocationId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_LicenceNumber",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Locations_Latitude_Longitude",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_MedicineId_PharmacyId",
                table: "Inventories");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "LicenceNumber",
                table: "Pharmacies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "GenericName",
                table: "Medicines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Locations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Locations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_MedicineId",
                table: "Inventories",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Medicines_MedicineId",
                table: "Inventories",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Pharmacies_PharmacyId",
                table: "Inventories",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_Locations_LocationId",
                table: "Pharmacies",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
