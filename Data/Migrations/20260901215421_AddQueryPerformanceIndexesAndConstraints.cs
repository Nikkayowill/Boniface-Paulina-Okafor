using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientMessages_PatientProfileId",
                table: "PatientMessages");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PatientAppointments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Title",
                table: "Posts",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMessages_IsRead",
                table: "PatientMessages",
                column: "IsRead",
                filter: "\"IsRead\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMessages_PatientProfileId_SentAt",
                table: "PatientMessages",
                columns: new[] { "PatientProfileId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_DoctorId_AppointmentDate",
                table: "PatientAppointments",
                columns: new[] { "DoctorId", "AppointmentDate" },
                unique: true,
                filter: "\"DoctorId\" IS NOT NULL AND \"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_Status_AppointmentDate",
                table: "PatientAppointments",
                columns: new[] { "Status", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_SentAt",
                table: "NotificationLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_FullName",
                table: "Doctors",
                column: "FullName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Specialty",
                table: "Doctors",
                column: "Specialty")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Description",
                table: "Departments",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactSubmissions_SubmittedAt",
                table: "ContactSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_InvoiceNumber_Trgm",
                table: "BillPayments",
                column: "InvoiceNumber")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_PatientEmail",
                table: "BillPayments",
                column: "PatientEmail")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_PatientName",
                table: "BillPayments",
                column: "PatientName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_Status_CreatedAt",
                table: "AppointmentRequests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_Title",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_PatientMessages_IsRead",
                table: "PatientMessages");

            migrationBuilder.DropIndex(
                name: "IX_PatientMessages_PatientProfileId_SentAt",
                table: "PatientMessages");

            migrationBuilder.DropIndex(
                name: "IX_PatientAppointments_DoctorId_AppointmentDate",
                table: "PatientAppointments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAppointments_Status_AppointmentDate",
                table: "PatientAppointments");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_SentAt",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_FullName",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_Specialty",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Description",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Name",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_ContactSubmissions_SubmittedAt",
                table: "ContactSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BillPayments_InvoiceNumber_Trgm",
                table: "BillPayments");

            migrationBuilder.DropIndex(
                name: "IX_BillPayments_PatientEmail",
                table: "BillPayments");

            migrationBuilder.DropIndex(
                name: "IX_BillPayments_PatientName",
                table: "BillPayments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_Status_CreatedAt",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PatientAppointments");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMessages_PatientProfileId",
                table: "PatientMessages",
                column: "PatientProfileId");
        }
    }
}
