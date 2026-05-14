using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationPaymentProviderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Donations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSandbox",
                table: "Donations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Donations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessage",
                table: "Donations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderReference",
                table: "Donations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Donations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Paid");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ProviderReference",
                table: "Donations",
                column: "ProviderReference",
                unique: true,
                filter: "[ProviderReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_Status_CreatedAt",
                table: "Donations",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donations_ProviderReference",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_Status_CreatedAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "IsSandbox",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ProviderMessage",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ProviderReference",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Donations");
        }
    }
}
