using EmployeeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeApp.Infrastructure.Data.Configurations;

/// <summary>9.2 Fluent API: how Department maps to the "departments" table.</summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();   // the app chooses Ids, not the database

        // Alternate key = UNIQUE constraint. Employees point at a department by its Code ("ENG"),
        // so Code must be unique for it to be the target of a foreign key.
        builder.HasAlternateKey(d => d.Code);
        builder.Property(d => d.Code).HasMaxLength(10);
        builder.Property(d => d.Name).HasMaxLength(100);
    }
}
