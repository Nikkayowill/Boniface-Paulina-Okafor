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
            .HasFilter("\"ProviderReference\" IS NOT NULL");

        builder.HasIndex(p => new { p.Status, p.CreatedAt });

        // Trigram indexes back the admin bill-payment search (invoice number/patient
        // name/email Contains() filter), which otherwise forces a sequential scan as
        // the table grows.
        builder.HasIndex(p => p.PatientName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(p => p.PatientEmail).HasMethod("gin").HasOperators("gin_trgm_ops");
        // InvoiceNumber already has a unique btree index above (for exact lookups). Using the
        // named-index overload here (rather than another bare HasIndex(p => p.InvoiceNumber))
        // is required: EF Core matches indexes by property list by default, so a second bare
        // call on the same single property would reconfigure that same unique btree index in
        // place — silently turning it into a "unique" GIN trigram index instead of adding a
        // second, non-unique one for partial-match search.
        builder.HasIndex(p => p.InvoiceNumber, "IX_BillPayments_InvoiceNumber_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasOne(p => p.ApplicationUser)
            .WithMany()
            .HasForeignKey(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
