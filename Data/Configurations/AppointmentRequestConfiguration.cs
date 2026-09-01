using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class AppointmentRequestConfiguration : IEntityTypeConfiguration<AppointmentRequest>
{
    public void Configure(EntityTypeBuilder<AppointmentRequest> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>();
        builder.Property(a => a.PreferredDate).HasColumnType("date");

        // Backs the dashboard's pending-count and "longest waiting" queries, and the admin
        // list's status filter + created-at sort, which otherwise force a full table scan.
        builder.HasIndex(a => new { a.Status, a.CreatedAt });
    }
}
