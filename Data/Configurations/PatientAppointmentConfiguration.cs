using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class PatientAppointmentConfiguration : IEntityTypeConfiguration<PatientAppointment>
{
    public void Configure(EntityTypeBuilder<PatientAppointment> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>();

        builder.HasOne(p => p.AppointmentRequest)
            .WithMany()
            .HasForeignKey(p => p.AppointmentRequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.AppointmentRequestId)
            .IsUnique()
            .HasFilter("[AppointmentRequestId] IS NOT NULL");
    }
}
