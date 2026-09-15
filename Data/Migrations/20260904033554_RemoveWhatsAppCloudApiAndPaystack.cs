using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWhatsAppCloudApiAndPaystack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppSchedulingSessions");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "TeleconsultationRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "TeleconsultationRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WhatsAppSchedulingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentRequestId = table.Column<int>(type: "integer", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PatientPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SelectedOptionNumber = table.Column<int>(type: "integer", nullable: true),
                    SlotOptionsJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppSchedulingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppSchedulingSessions_AppointmentRequests_AppointmentR~",
                        column: x => x.AppointmentRequestId,
                        principalTable: "AppointmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppSchedulingSessions_AppointmentRequestId",
                table: "WhatsAppSchedulingSessions",
                column: "AppointmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppSchedulingSessions_PatientPhone_Status_ExpiresAt",
                table: "WhatsAppSchedulingSessions",
                columns: new[] { "PatientPhone", "Status", "ExpiresAt" });
        }
    }
}
