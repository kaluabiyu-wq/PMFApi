using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmfApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Inventories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Inventories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Inventories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "Inventories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ApprovalStatus",
                table: "Inventories",
                column: "ApprovalStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_ApprovalStatus",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Inventories");
        }
    }
}
