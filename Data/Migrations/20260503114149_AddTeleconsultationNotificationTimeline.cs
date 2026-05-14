using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeleconsultationNotificationTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "NotificationLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "NotificationLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalMessageId",
                table: "NotificationLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "NotificationLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeleconsultationRequestId",
                table: "NotificationLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_ExternalMessageId",
                table: "NotificationLogs",
                column: "ExternalMessageId",
                filter: "[ExternalMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TeleconsultationRequestId",
                table: "NotificationLogs",
                column: "TeleconsultationRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_TeleconsultationRequests_TeleconsultationRequestId",
                table: "NotificationLogs",
                column: "TeleconsultationRequestId",
                principalTable: "TeleconsultationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationLogs_TeleconsultationRequests_TeleconsultationRequestId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_ExternalMessageId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_TeleconsultationRequestId",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "TeleconsultationRequestId",
                table: "NotificationLogs");
        }
    }
}
