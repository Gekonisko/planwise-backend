using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanWise.Modules.IdentityAccess.Domain.Roles;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Roles;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Name);

        builder.Property(r => r.Name).HasMaxLength(200);

        builder
            .HasMany<User>()
            .WithMany(u => u.Roles)
            .UsingEntity(joinBuider =>
            {
                joinBuider.ToTable("user_roles");
                joinBuider.Property("RolesName").HasColumnName("role_name");
            });

        builder.HasData(
            Role.User,
            Role.Admin
        );
    }
}
