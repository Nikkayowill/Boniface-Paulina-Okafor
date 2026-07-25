using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class WhatsAppSchedulingSessionConfiguration : IEntityTypeConfiguration<WhatsAppSchedulingSession>
{
    public void Configure(EntityTypeBuilder<WhatsAppSchedulingSession> builder)
    {
        builder.HasIndex(s => new { s.PatientPhone, s.Status, s.ExpiresAt });

        builder.HasOne(s => s.AppointmentRequest)
            .WithMany()
            .HasForeignKey(s => s.AppointmentRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
