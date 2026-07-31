using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Okafor_.NET.Data.Configurations;

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> entity)
    {
        entity.Property(login => login.LoginProvider).HasMaxLength(128);
        entity.Property(login => login.ProviderKey).HasMaxLength(128);
    }
}
