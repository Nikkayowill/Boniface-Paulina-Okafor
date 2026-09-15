using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class PatientAppointmentConfiguration : IEntityTypeConfiguration<PatientAppointment>
{
    public void Configure(EntityTypeBuilder<PatientAppointment> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>();
        builder.Property(a => a.AppointmentDate).HasColumnType("timestamp without time zone");

        // Optimistic concurrency token: guards against two staff members editing the same
        // appointment (e.g. rescheduling) at once and silently overwriting each other. Uses
        // Postgres's own xmin system column (auto-updated by Postgres on every row change)
        // rather than a mapped byte[] property — a plain [Timestamp] byte[] column has no
        // database-side mechanism to update itself on write and would silently provide no
        // actual protection (verified empirically against Npgsql.EntityFrameworkCore.PostgreSQL
        // 8.0.8: a [Timestamp] byte[] property just generates an inert bytea column).
        // UseXminAsConcurrencyToken() is marked obsolete in favor of IsRowVersion()/[Timestamp],
        // but that guidance doesn't hold for this provider version — xmin is the only mechanism
        // here with a real database-side auto-update behind it, so the obsolete call is intentional.
#pragma warning disable CS0618
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        builder.HasOne(p => p.AppointmentRequest)
            .WithMany()
            .HasForeignKey(p => p.AppointmentRequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.AppointmentRequestId)
            .IsUnique()
            .HasFilter("\"AppointmentRequestId\" IS NOT NULL");

        // Supports the admin list's status filter + date sort/pagination without a full table scan.
        builder.HasIndex(p => new { p.Status, p.AppointmentDate });

        // Closes the double-booking race: two concurrent writes for the same doctor/slot can no
        // longer both commit. Cancelled appointments are excluded so a doctor's freed-up slot can
        // be rebooked, and unassigned appointments (DoctorId is null while awaiting triage) aren't
        // constrained by this index.
        builder.HasIndex(p => new { p.DoctorId, p.AppointmentDate })
            .IsUnique()
            .HasFilter("\"DoctorId\" IS NOT NULL AND \"Status\" <> 'Cancelled'");

        // Kept explicitly: without this, EF's convention drops the plain FK index on DoctorId as
        // "redundant" once the composite index above exists, reasoning only about column order and
        // not about the fact that the composite index is partial (excludes Cancelled rows). A
        // doctor-scoped lookup across every status — including Cancelled — needs a non-partial index
        // to stay index-backed, and so does the FK relationship itself.
        builder.HasIndex(p => p.DoctorId);
    }
}
