using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationInterestCaptureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContactConsent",
                table: "Donations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DonorMessage",
                table: "Donations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonorPhone",
                table: "Donations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredMethod",
                table: "Donations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "hospital-contact");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "Donations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffNotes",
                table: "Donations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactConsent",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonorMessage",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonorPhone",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PreferredMethod",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "StaffNotes",
                table: "Donations");
        }
    }
}
