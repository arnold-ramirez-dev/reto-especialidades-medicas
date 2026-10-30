using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Services;

public record ProjectAllocation(EmployeeProjectHours Project, decimal Amount);

public static class BonusAllocationCalculator
{
    public static IReadOnlyList<ProjectAllocation> Distribute(
        decimal grossAmount,
        IReadOnlyList<EmployeeProjectHours> projects)
    {
        var eligibleProjects = projects
            .Where(x => x.Hours > 0)
            .OrderBy(x => x.EmployeeProjectHoursId)
            .ToList();

        if (eligibleProjects.Count == 0)
        {
            throw new BusinessException(
                "PROJECT_HOURS_NOT_FOUND",
                "El empleado no tiene horas de proyecto válidas para distribuir el bono.");
        }

        var totalHours = eligibleProjects.Sum(x => x.Hours);
        var result = new List<ProjectAllocation>(eligibleProjects.Count);
        var allocated = 0m;

        for (var index = 0; index < eligibleProjects.Count; index++)
        {
            var project = eligibleProjects[index];
            var isLast = index == eligibleProjects.Count - 1;
            var amount = isLast
                ? BonusCalculator.Round(grossAmount - allocated)
                : BonusCalculator.Round(grossAmount * project.Hours / totalHours);

            result.Add(new ProjectAllocation(project, amount));
            allocated += amount;
        }

        return result;
    }
}
