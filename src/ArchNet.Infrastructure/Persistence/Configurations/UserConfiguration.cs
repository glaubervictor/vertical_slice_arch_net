using ArchNet.Common.Primitives;
using ArchNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchNet.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
               .HasColumnName("id")
               .HasMaxLength(EntityBase.IdMaxLength)
               .ValueGeneratedNever();

        builder.Property(u => u.Name)
               .HasColumnName("name")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(u => u.Login)
               .HasColumnName("login")
               .HasMaxLength(100)
               .IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();

        builder.Property(u => u.PasswordHash)
               .HasColumnName("password_hash")
               .HasMaxLength(256)
               .IsRequired();

        builder.Property(u => u.Salt)
               .HasColumnName("salt")
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(u => u.Role)
               .HasColumnName("role")
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();
    }
}
