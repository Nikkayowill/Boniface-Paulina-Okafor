using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurposeCode",
                table: "Donations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "general-hospital-support");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurposeCode",
                table: "Donations");
        }
    }
}
