using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.UnitTests;

public class BonusAllocationCalculatorTests
{
    [Fact]
    public void ThreeProjects_LastProjectReceivesRoundingDifference()
    {
        var projects = new[]
        {
            Project(1, "Proyecto A", 1m),
            Project(2, "Proyecto B", 1m),
            Project(3, "Proyecto C", 1m)
        };

        var result = BonusAllocationCalculator.Distribute(100m, projects);

        Assert.Collection(
            result,
            item => Assert.Equal(33.33m, item.Amount),
            item => Assert.Equal(33.33m, item.Amount),
            item => Assert.Equal(33.34m, item.Amount));
        Assert.Equal(100m, result.Sum(x => x.Amount));
    }

    [Fact]
    public void SingleProject_ReceivesFullGrossAmount()
    {
        var result = BonusAllocationCalculator.Distribute(
            987.65m,
            new[] { Project(1, "Proyecto único", 160m) });

        Assert.Single(result);
        Assert.Equal(987.65m, result[0].Amount);
    }

    [Fact]
    public void ProjectsWithoutPositiveHours_AreRejected()
    {
        var exception = Assert.Throws<BusinessException>(() =>
            BonusAllocationCalculator.Distribute(
                500m,
                new[] { Project(1, "Sin horas", 0m) }));

        Assert.Equal("PROJECT_HOURS_NOT_FOUND", exception.Code);
    }

    private static EmployeeProjectHours Project(int id, string name, decimal hours) => new()
    {
        EmployeeProjectHoursId = id,
        DepartmentCode = "TECH1",
        EmployeeNumber = 101,
        ProjectName = name,
        Hours = hours
    };
}
