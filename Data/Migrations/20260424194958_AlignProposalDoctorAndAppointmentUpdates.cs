using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignProposalDoctorAndAppointmentUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsultationHours",
                table: "Doctors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Qualifications",
                table: "Doctors",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Doctors",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredTime",
                table: "AppointmentRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Slug",
                table: "Doctors",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctors_Slug",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "ConsultationHours",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Qualifications",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "PreferredTime",
                table: "AppointmentRequests");
        }
    }
}
