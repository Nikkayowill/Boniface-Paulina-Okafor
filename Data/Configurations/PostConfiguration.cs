using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasIndex(p => p.Slug).IsUnique();

        // Trigram index backs the ILIKE '%term%' title lookup in the public site search
        // (HomeController.Search). Content is left un-indexed here deliberately: a proper
        // full-text (tsvector) index is a better fit for long-form body text and is worth
        // a dedicated follow-up rather than folding into this pass.
        builder.HasIndex(p => p.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
