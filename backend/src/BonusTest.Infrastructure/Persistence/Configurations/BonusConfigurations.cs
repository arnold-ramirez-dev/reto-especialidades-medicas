using BonusTest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus").HasKey(x => x.BonusId);

        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.Period).HasColumnType("date");
        builder.Property(x => x.FixedAmount).HasPrecision(16, 2);
        builder.Property(x => x.Percentage).HasPrecision(5, 2);
        builder.Property(x => x.ReasonCode).HasColumnType("char(6)");
        builder.Property(x => x.MonthlySalarySnapshot).HasPrecision(16, 2);
        builder.Property(x => x.TaxRateSnapshot).HasPrecision(5, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);
        builder.Property(x => x.BonusStateCode).HasColumnType("char(4)");
        builder.Property(x => x.CreatedBy).HasColumnType("varchar(100)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.ModifiedBy).HasColumnType("varchar(100)");
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber });

        builder.HasOne<BonusType>()
            .WithMany()
            .HasForeignKey(x => x.BonusTypeId);

        builder.HasOne<BonusReason>()
            .WithMany()
            .HasForeignKey(x => x.ReasonCode);

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(x => x.BonusStateCode);
    }
}

public class BonusProjectAllocationConfiguration : IEntityTypeConfiguration<BonusProjectAllocation>
{
    public void Configure(EntityTypeBuilder<BonusProjectAllocation> builder)
    {
        builder.ToTable("BonusProjectAllocation").HasKey(x => x.BonusProjectAllocationId);
        builder.Property(x => x.ProjectName).HasColumnType("varchar(100)");
        builder.Property(x => x.Hours).HasPrecision(8, 2);
        builder.Property(x => x.AllocatedAmount).HasPrecision(16, 2);
        builder.Property(x => x.CreatedBy).HasColumnType("varchar(100)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        builder.HasOne<Bonus>()
            .WithMany()
            .HasForeignKey(x => x.BonusId);

        builder.HasOne<EmployeeProjectHours>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeProjectHoursId);
    }
}
