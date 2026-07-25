using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Okafor_.NET.Data.Configurations;

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> entity)
    {
        entity.Property(token => token.LoginProvider).HasMaxLength(128);
        entity.Property(token => token.Name).HasMaxLength(128);
    }
}
