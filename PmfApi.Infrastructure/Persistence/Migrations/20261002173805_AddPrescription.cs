using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerifiedByUserId = table.Column<int>(type: "integer", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.CheckConstraint("CK_Prescription_Status", "\"VerificationStatus\" IN ('Pending', 'Approved', 'Rejected')");
                    table.CheckConstraint("CK_Prescription_Verifier", "(\"VerificationStatus\" = 'Pending' AND \"VerifiedByUserId\" IS NULL AND \"VerifiedAt\" IS NULL) OR (\"VerificationStatus\" <> 'Pending' AND \"VerifiedByUserId\" IS NOT NULL AND \"VerifiedAt\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Prescriptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrderId",
                table: "Prescriptions",
                column: "OrderId",
                unique: true,
                filter: "\"VerificationStatus\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_VerifiedByUserId",
                table: "Prescriptions",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prescriptions");
        }
    }
}
