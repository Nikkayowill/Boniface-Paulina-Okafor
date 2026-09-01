using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Trigram indexes back the ILIKE '%term%' lookups in the public site search
        // (HomeController.Search) and the WhatsApp AI scheduling department match
        // (WhatsAppAppointmentSlotService), which would otherwise force a sequential
        // scan on every request.
        builder.HasIndex(d => d.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(d => d.Description).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
