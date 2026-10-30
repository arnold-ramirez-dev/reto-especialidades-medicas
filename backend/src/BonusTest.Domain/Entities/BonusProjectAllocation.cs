namespace BonusTest.Domain.Entities;

public class BonusProjectAllocation
{
    private BonusProjectAllocation() { }

    public int BonusProjectAllocationId { get; private set; }
    public int BonusId { get; private set; }
    public int EmployeeProjectHoursId { get; private set; }
    public string ProjectName { get; private set; } = null!;
    public decimal Hours { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static BonusProjectAllocation Create(
        int bonusId,
        EmployeeProjectHours project,
        decimal allocatedAmount,
        string userCode,
        DateTime createdAt) => new()
    {
        BonusId = bonusId,
        EmployeeProjectHoursId = project.EmployeeProjectHoursId,
        ProjectName = project.ProjectName,
        Hours = project.Hours,
        AllocatedAmount = allocatedAmount,
        CreatedBy = userCode,
        CreatedAt = createdAt
    };
}
