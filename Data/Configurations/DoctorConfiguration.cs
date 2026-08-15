using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        // Only confirmed clinicians may appear through any application query.
        builder.HasQueryFilter(doctor =>
            doctor.Slug == "rev-fr-dr-toochukwu-bartholomew-okafor" ||
            doctor.Slug == "dr-opie-thomas-n");

        builder.HasIndex(d => d.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");
    }
}
