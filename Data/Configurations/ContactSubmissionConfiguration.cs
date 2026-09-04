using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class ContactSubmissionConfiguration : IEntityTypeConfiguration<ContactSubmission>
{
    public void Configure(EntityTypeBuilder<ContactSubmission> builder)
    {
        // The admin list and dashboard "recent submissions" widget both sort by SubmittedAt
        // descending; without this the table is fully scanned and sorted on every load.
        builder.HasIndex(c => c.SubmittedAt);
    }
}
