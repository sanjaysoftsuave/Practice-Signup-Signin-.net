using EmployeeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeApp.Infrastructure.Data.Configurations;

/// <summary>
/// 9.2 Fluent API: how the Employee class hierarchy maps to ONE "employees" table.
/// Core's model classes have no database attributes; all the mapping lives here, in Infrastructure.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        // TPH (Table Per Hierarchy): FullTimeEmployee, Manager and Intern share one table.
        // An extra "employee_type" column says which class to create when a row is read back.
        builder.HasDiscriminator<string>("EmployeeType")
            .HasValue<FullTimeEmployee>("FullTime")
            .HasValue<Manager>("Manager")
            .HasValue<Intern>("Intern");
        builder.Property<string>("EmployeeType").HasMaxLength(20)
            // EF normally forbids changing the type of a saved row. PUT may change the role
            // (FullTime → Manager), so allow the column to be updated. EfRepository.Update sets it.
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

        builder.Ignore(e => e.Role);   // calculated from the class, not stored

        builder.Property(e => e.Name).HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.MonthlySalary).HasPrecision(12, 2);   // numeric(12,2): exact money (1.3)

        // A join date has no time. Without this, PostgreSQL uses "timestamp with time zone", and Npgsql
        // refuses DateTime.Today because it isn't UTC ("Cannot write DateTime with Kind=Local").
        builder.Property(e => e.JoinedOn).HasColumnType("date");

        // Value conversion: the IWorkLocation object is stored as its name ("Remote"), and turned back
        // into a Remote object when read.
        builder.Property(e => e.Location)
            .HasConversion(location => location.Name, name => WorkLocations.FromName(name))
            .HasMaxLength(20);

        // The Address record (2.5) is a complex type: its fields become columns of the employees table
        // (address_city, address_pincode), not a separate table. Null address = both columns null.
        builder.ComplexProperty(e => e.Address, address =>
        {
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.Pincode).HasMaxLength(10);
        });

        // Relationship 1: Department (1) → Employees (many), joined on Department.Code.
        // Restrict: the database refuses to delete a department that still has employees.
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentCode)
            .HasPrincipalKey(d => d.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship 2: an employee's manager is another employee (self-reference).
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
