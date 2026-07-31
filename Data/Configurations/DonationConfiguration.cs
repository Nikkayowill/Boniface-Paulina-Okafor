using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.Property(d => d.Amount).HasPrecision(18, 2);

        builder.HasIndex(d => d.PaymentReference).IsUnique();

        builder.Property(d => d.Status).HasConversion<string>();

        builder.HasIndex(d => d.ProviderReference)
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        builder.HasIndex(d => new { d.Status, d.CreatedAt });
    }
}
