using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee").HasKey(x => new { x.DepartmentCode, x.EmployeeNumber });
        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.EmployeeNumber).ValueGeneratedNever();
        builder.Property(x => x.FullName).HasColumnType("varchar(150)");
        builder.Property(x => x.HireDate).HasColumnType("date");
        builder.Property(x => x.MonthlySalary).HasPrecision(16, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.EmployeeStateCode).HasColumnType("char(4)");

        builder.HasMany(x => x.ProjectHours)
            .WithOne()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber });
    }
}

public class EmployeeProjectHoursConfiguration : IEntityTypeConfiguration<EmployeeProjectHours>
{
    public void Configure(EntityTypeBuilder<EmployeeProjectHours> builder)
    {
        builder.ToTable("EmployeeProjectHours").HasKey(x => x.EmployeeProjectHoursId);
        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.ProjectName).HasColumnType("varchar(100)");
        builder.Property(x => x.Hours).HasPrecision(8, 2);
    }
}
