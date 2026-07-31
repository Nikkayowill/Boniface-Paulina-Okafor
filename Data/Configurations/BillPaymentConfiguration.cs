using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasConversion<string>();

        builder.HasIndex(p => p.InvoiceNumber).IsUnique();

        builder.HasIndex(p => p.ProviderReference)
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        builder.HasIndex(p => new { p.Status, p.CreatedAt });

        builder.HasOne(p => p.ApplicationUser)
            .WithMany()
            .HasForeignKey(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
