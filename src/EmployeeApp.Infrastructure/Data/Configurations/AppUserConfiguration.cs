using EmployeeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeApp.Infrastructure.Data.Configurations;

/// <summary>10.1 The "users" table: login accounts. Holds the password HASH only.</summary>
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();   // the app chooses Ids, like employees

        builder.Property(u => u.Email).HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();            // the database also refuses two accounts per email

        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.Role).HasMaxLength(20);
        builder.Property(u => u.CreatedOn).HasColumnType("timestamp with time zone");   // a UTC moment
    }
}
