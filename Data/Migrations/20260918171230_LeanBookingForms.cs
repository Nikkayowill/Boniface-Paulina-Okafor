using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class LeanBookingForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentAccepted",
                table: "TeleconsultationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "TeleconsultationRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TeleconsultationRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppointmentRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restoring NOT NULL needs the rows booked without an email or reason filled in first.
            migrationBuilder.Sql("UPDATE \"TeleconsultationRequests\" SET \"Reason\" = '' WHERE \"Reason\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"TeleconsultationRequests\" SET \"Email\" = '' WHERE \"Email\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"AppointmentRequests\" SET \"Email\" = '' WHERE \"Email\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "TeleconsultationRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TeleconsultationRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentAccepted",
                table: "TeleconsultationRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppointmentRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
