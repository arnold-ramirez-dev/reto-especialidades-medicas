using BonusTest.Application.Bonuses;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    public async Task<(IReadOnlyList<BonusListItemDto> Data, int TotalRecords)> GetPagedAsync(
        BonusQuery query,
        CancellationToken cancellationToken)
    {
        var bonuses = context.Bonuses
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.DepartmentCode is not null)
        {
            bonuses = bonuses.Where(x => x.DepartmentCode == query.DepartmentCode);
        }

        if (query.EmployeeNumber.HasValue)
        {
            bonuses = bonuses.Where(x => x.EmployeeNumber == query.EmployeeNumber.Value);
        }

        if (query.Period.HasValue)
        {
            bonuses = bonuses.Where(x => x.Period == query.Period.Value);
        }

        if (query.StateCode is not null)
        {
            bonuses = bonuses.Where(x => x.BonusStateCode == query.StateCode);
        }

        var totalRecords = await bonuses.CountAsync(cancellationToken);

        var data = await (
                from bonus in bonuses
                join employee in context.Employees.AsNoTracking()
                    on new { bonus.DepartmentCode, bonus.EmployeeNumber }
                    equals new { employee.DepartmentCode, employee.EmployeeNumber }
                join department in context.Departments.AsNoTracking()
                    on bonus.DepartmentCode equals department.DepartmentCode
                join state in context.States.AsNoTracking()
                    on bonus.BonusStateCode equals state.StateCode
                join bonusType in context.BonusTypes.AsNoTracking()
                    on bonus.BonusTypeId equals bonusType.BonusTypeId
                join reason in context.BonusReasons.AsNoTracking()
                    on bonus.ReasonCode equals reason.ReasonCode
                orderby bonus.Period descending, bonus.CreatedAt descending, bonus.BonusId descending
                select new BonusListItemDto(
                    bonus.BonusId,
                    bonus.BonusStateCode,
                    state.StateDescription,
                    bonus.Period,
                    bonus.DepartmentCode,
                    department.DepartmentName,
                    bonus.EmployeeNumber,
                    employee.FullName,
                    bonus.BonusTypeId,
                    bonusType.BonusTypeDescription,
                    bonus.FixedAmount ?? bonus.Percentage ?? 0m,
                    bonus.ReasonCode,
                    reason.ReasonDescription,
                    bonus.GrossAmount,
                    bonus.TaxAmount,
                    bonus.NetAmount,
                    bonus.CreatedAt))
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (data, totalRecords);
    }

    public Task<Employee?> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken) =>
        context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber,
                cancellationToken);

    public Task<BonusReason?> GetReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        context.BonusReasons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReasonCode == reasonCode, cancellationToken);

    public Task<bool> ActiveBonusExistsAsync(
        string departmentCode,
        int employeeNumber,
        DateOnly period,
        CancellationToken cancellationToken) =>
        context.Bonuses
            .AsNoTracking()
            .AnyAsync(
                x => x.DepartmentCode == departmentCode
                    && x.EmployeeNumber == employeeNumber
                    && x.Period == period
                    && !x.IsDeleted
                    && x.BonusStateCode != BonusStates.Cancelled,
                cancellationToken);

    public async Task<bool> TryAddAsync(Bonus bonus, CancellationToken cancellationToken)
    {
        context.Bonuses.Add(bonus);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            context.Entry(bonus).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Bonus?> GetForUpdateAsync(int bonusId, CancellationToken cancellationToken) =>
        context.Bonuses.SingleOrDefaultAsync(
            x => x.BonusId == bonusId && !x.IsDeleted,
            cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken) =>
        await context.EmployeeProjectHours
            .AsNoTracking()
            .Where(x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber)
            .OrderBy(x => x.EmployeeProjectHoursId)
            .ToListAsync(cancellationToken);

    public async Task<bool> TrySavePaymentAsync(
        IReadOnlyCollection<BonusProjectAllocation> allocations,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.BonusProjectAllocations.AddRange(allocations);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
