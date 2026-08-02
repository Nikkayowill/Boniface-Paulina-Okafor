using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasOne(n => n.TeleconsultationRequest)
            .WithMany()
            .HasForeignKey(n => n.TeleconsultationRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.TeleconsultationRequestId);

        builder.HasIndex(n => n.ExternalMessageId)
            .HasFilter("\"ExternalMessageId\" IS NOT NULL");
    }
}
