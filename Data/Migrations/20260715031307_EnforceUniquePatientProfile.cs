using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniquePatientProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE appointments
                SET PatientProfileId = profiles.KeepId
                FROM PatientAppointments AS appointments
                INNER JOIN (
                    SELECT Id, MIN(Id) OVER (PARTITION BY ApplicationUserId) AS KeepId
                    FROM PatientProfiles
                ) AS profiles ON appointments.PatientProfileId = profiles.Id
                WHERE profiles.Id <> profiles.KeepId;

                UPDATE documents
                SET PatientProfileId = profiles.KeepId
                FROM PatientDocuments AS documents
                INNER JOIN (
                    SELECT Id, MIN(Id) OVER (PARTITION BY ApplicationUserId) AS KeepId
                    FROM PatientProfiles
                ) AS profiles ON documents.PatientProfileId = profiles.Id
                WHERE profiles.Id <> profiles.KeepId;

                UPDATE messages
                SET PatientProfileId = profiles.KeepId
                FROM PatientMessages AS messages
                INNER JOIN (
                    SELECT Id, MIN(Id) OVER (PARTITION BY ApplicationUserId) AS KeepId
                    FROM PatientProfiles
                ) AS profiles ON messages.PatientProfileId = profiles.Id
                WHERE profiles.Id <> profiles.KeepId;

                UPDATE consultations
                SET PatientProfileId = profiles.KeepId
                FROM TeleconsultationRequests AS consultations
                INNER JOIN (
                    SELECT Id, MIN(Id) OVER (PARTITION BY ApplicationUserId) AS KeepId
                    FROM PatientProfiles
                ) AS profiles ON consultations.PatientProfileId = profiles.Id
                WHERE profiles.Id <> profiles.KeepId;

                DELETE profiles
                FROM PatientProfiles AS profiles
                INNER JOIN (
                    SELECT Id, MIN(Id) OVER (PARTITION BY ApplicationUserId) AS KeepId
                    FROM PatientProfiles
                ) AS duplicates ON profiles.Id = duplicates.Id
                WHERE duplicates.Id <> duplicates.KeepId;
                """);

            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_ApplicationUserId",
                table: "PatientProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_ApplicationUserId",
                table: "PatientProfiles",
                column: "ApplicationUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_ApplicationUserId",
                table: "PatientProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_ApplicationUserId",
                table: "PatientProfiles",
                column: "ApplicationUserId");
        }
    }
}
