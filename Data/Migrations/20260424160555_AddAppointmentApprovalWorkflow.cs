using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentRequestId",
                table: "PatientAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "AppointmentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "AppointmentRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContactConfirmed",
                table: "AppointmentRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContactConfirmedAt",
                table: "AppointmentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactMethod",
                table: "AppointmentRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNotes",
                table: "AppointmentRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_AppointmentRequestId",
                table: "PatientAppointments",
                column: "AppointmentRequestId",
                unique: true,
                filter: "[AppointmentRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAppointments_AppointmentRequests_AppointmentRequestId",
                table: "PatientAppointments",
                column: "AppointmentRequestId",
                principalTable: "AppointmentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientAppointments_AppointmentRequests_AppointmentRequestId",
                table: "PatientAppointments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAppointments_AppointmentRequestId",
                table: "PatientAppointments");

            migrationBuilder.DropColumn(
                name: "AppointmentRequestId",
                table: "PatientAppointments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ContactConfirmed",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ContactConfirmedAt",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ContactMethod",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ContactNotes",
                table: "AppointmentRequests");
        }
    }
}
