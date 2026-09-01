using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class PatientMessageConfiguration : IEntityTypeConfiguration<PatientMessage>
{
    public void Configure(EntityTypeBuilder<PatientMessage> builder)
    {
        // Backs the patient portal's per-patient message thread (sorted by SentAt) and the
        // admin inbox's listing, both of which otherwise scan every message in the table.
        builder.HasIndex(m => new { m.PatientProfileId, m.SentAt });

        // Backs the unread-count badges (admin dashboard, patient portal) with a small,
        // cheap-to-maintain index instead of scanning all messages to count unread ones.
        builder.HasIndex(m => m.IsRead)
            .HasFilter("\"IsRead\" = false");
    }
}
