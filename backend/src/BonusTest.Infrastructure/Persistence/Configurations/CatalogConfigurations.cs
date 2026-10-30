using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("State").HasKey(x => x.StateCode);
        builder.Property(x => x.StateCode).HasColumnType("char(4)");
        builder.Property(x => x.StateDescription).HasColumnType("varchar(50)");
        builder.Property(x => x.MasterStateCode).HasColumnType("char(2)");
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Department").HasKey(x => x.DepartmentCode);
        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.DepartmentName).HasColumnType("varchar(100)");
    }
}

public class BonusTypeConfiguration : IEntityTypeConfiguration<BonusType>
{
    public void Configure(EntityTypeBuilder<BonusType> builder)
    {
        builder.ToTable("BonusType").HasKey(x => x.BonusTypeId);
        builder.Property(x => x.BonusTypeId).ValueGeneratedNever();
        builder.Property(x => x.BonusTypeDescription).HasColumnType("varchar(30)");
    }
}

public class BonusReasonConfiguration : IEntityTypeConfiguration<BonusReason>
{
    public void Configure(EntityTypeBuilder<BonusReason> builder)
    {
        builder.ToTable("BonusReason").HasKey(x => x.ReasonCode);
        builder.Property(x => x.ReasonCode).HasColumnType("char(6)");
        builder.Property(x => x.ReasonDescription).HasColumnType("varchar(100)");
    }
}
