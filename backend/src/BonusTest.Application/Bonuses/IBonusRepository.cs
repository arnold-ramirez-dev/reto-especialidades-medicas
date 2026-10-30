using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<(IReadOnlyList<BonusListItemDto> Data, int TotalRecords)> GetPagedAsync(
        BonusQuery query,
        CancellationToken cancellationToken);

    Task<Employee?> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken);

    Task<BonusReason?> GetReasonAsync(string reasonCode, CancellationToken cancellationToken);

    Task<bool> ActiveBonusExistsAsync(
        string departmentCode,
        int employeeNumber,
        DateOnly period,
        CancellationToken cancellationToken);

    Task<bool> TryAddAsync(Bonus bonus, CancellationToken cancellationToken);
    Task<Bonus?> GetForUpdateAsync(int bonusId, CancellationToken cancellationToken);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken);

    Task<bool> TrySavePaymentAsync(
        IReadOnlyCollection<BonusProjectAllocation> allocations,
        CancellationToken cancellationToken);
}
