using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class TeleconsultationRequestConfiguration : IEntityTypeConfiguration<TeleconsultationRequest>
{
    public void Configure(EntityTypeBuilder<TeleconsultationRequest> builder)
    {
        builder.Property(t => t.Status).HasConversion<string>();
        builder.Property(t => t.ConsultationType).HasConversion<string>();
        builder.Property(t => t.PreferredDate).HasColumnType("date");

        builder.HasIndex(t => new { t.Status, t.CreatedAt });

        builder.HasOne(t => t.Department)
            .WithMany()
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Doctor)
            .WithMany()
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.ApplicationUser)
            .WithMany()
            .HasForeignKey(t => t.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.PatientProfile)
            .WithMany()
            .HasForeignKey(t => t.PatientProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
