using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.HasOne(s => s.Doctor)
            .WithMany()
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.DoctorId, s.SlotDateTime }).IsUnique();

        builder.HasOne(s => s.AppointmentRequest)
            .WithMany()
            .HasForeignKey(s => s.AppointmentRequestId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
