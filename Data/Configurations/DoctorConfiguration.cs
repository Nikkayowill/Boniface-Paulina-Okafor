using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasIndex(d => d.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");

        // Trigram indexes back the ILIKE '%term%' lookups in the public site search
        // (HomeController.Search), which would otherwise force a sequential
        // scan on every request.
        builder.HasIndex(d => d.FullName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(d => d.Specialty).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
