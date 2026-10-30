using System.Globalization;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository, TimeProvider timeProvider)
{
    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(
        BonusFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new BonusQuery(
            NormalizeOptionalCode(request.DepartmentCode),
            request.EmployeeNumber,
            string.IsNullOrWhiteSpace(request.Period) ? null : ParsePeriod(request.Period),
            NormalizeOptionalCode(request.StateCode),
            request.PageIndex,
            request.PageSize);

        var (data, totalRecords) = await repository.GetPagedAsync(query, cancellationToken);
        return PagedResponse<BonusListItemDto>.Create(
            data,
            request.PageIndex,
            request.PageSize,
            totalRecords);
    }

    public async Task<EmployeeBonusDto> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        var normalizedDepartment = NormalizeRequiredCode(
            departmentCode,
            "DEPARTMENT_REQUIRED",
            "Debe seleccionar un área.");

        if (employeeNumber <= 0)
        {
            throw new BusinessException("EMPLOYEE_NUMBER_REQUIRED", "Debe ingresar un número de empleado válido.");
        }

        var employee = await repository.GetEmployeeAsync(
            normalizedDepartment,
            employeeNumber,
            cancellationToken);

        EnsureEmployeeAvailable(employee);

        return new EmployeeBonusDto(
            employee!.DepartmentCode,
            employee.EmployeeNumber,
            employee.FullName,
            employee.HireDate,
            employee.MonthlySalary,
            employee.TaxRate);
    }

    public async Task<BonusMutationDto> CreateAsync(
        CreateBonusRequest request,
        string? userCode,
        CancellationToken cancellationToken)
    {
        var actor = NormalizeUserCode(userCode);
        var departmentCode = NormalizeRequiredCode(
            request.DepartmentCode,
            "DEPARTMENT_REQUIRED",
            "Debe seleccionar un área.");

        if (request.EmployeeNumber <= 0)
        {
            throw new BusinessException("EMPLOYEE_NUMBER_REQUIRED", "Debe ingresar un número de empleado válido.");
        }

        var period = ParsePeriod(request.Period);
        EnsurePeriodIsNotFuture(period);

        var employee = await repository.GetEmployeeAsync(
            departmentCode,
            request.EmployeeNumber,
            cancellationToken);
        EnsureEmployeeAvailable(employee);

        var reasonCode = NormalizeRequiredCode(
            request.ReasonCode,
            "REASON_REQUIRED",
            "Debe seleccionar un motivo.");
        var reason = await repository.GetReasonAsync(reasonCode, cancellationToken);
        if (reason is null || !reason.State)
        {
            throw new BusinessException("INVALID_REASON", "El motivo seleccionado no existe o se encuentra inactivo.");
        }

        if (await repository.ActiveBonusExistsAsync(
                departmentCode,
                request.EmployeeNumber,
                period,
                cancellationToken))
        {
            throw DuplicateBonus();
        }

        var bonus = Bonus.Create(
            employee!,
            period,
            request.BonusTypeId,
            request.Value,
            reasonCode,
            actor,
            UtcNow());

        if (!await repository.TryAddAsync(bonus, cancellationToken))
        {
            throw DuplicateBonus();
        }

        return new BonusMutationDto(bonus.BonusId, bonus.BonusStateCode);
    }

    public async Task<BonusMutationDto> CancelAsync(
        int bonusId,
        string? userCode,
        CancellationToken cancellationToken)
    {
        var bonus = await GetBonusForUpdateAsync(bonusId, cancellationToken);
        bonus.Cancel(NormalizeUserCode(userCode), UtcNow());
        await SaveMutationAsync(cancellationToken);

        return new BonusMutationDto(bonus.BonusId, bonus.BonusStateCode);
    }

    public async Task DeleteAsync(
        int bonusId,
        string? userCode,
        CancellationToken cancellationToken)
    {
        var bonus = await GetBonusForUpdateAsync(bonusId, cancellationToken);
        bonus.SoftDelete(NormalizeUserCode(userCode), UtcNow());
        await SaveMutationAsync(cancellationToken);
    }

    public async Task<PayBonusResultDto> PayAsync(
        int bonusId,
        string? userCode,
        CancellationToken cancellationToken)
    {
        var actor = NormalizeUserCode(userCode);
        var bonus = await GetBonusForUpdateAsync(bonusId, cancellationToken);
        var projectHours = await repository.GetProjectHoursAsync(
            bonus.DepartmentCode,
            bonus.EmployeeNumber,
            cancellationToken);

        var distribution = BonusAllocationCalculator.Distribute(bonus.GrossAmount, projectHours);
        var now = UtcNow();
        var allocations = distribution
            .Select(x => BonusProjectAllocation.Create(
                bonus.BonusId,
                x.Project,
                x.Amount,
                actor,
                now))
            .ToList();

        bonus.MarkAsPaid(actor, now);

        if (!await repository.TrySavePaymentAsync(allocations, cancellationToken))
        {
            throw new BusinessException(
                "BONUS_CONCURRENT_UPDATE",
                "El bono fue actualizado por otro usuario. Actualiza el listado e intenta nuevamente.");
        }

        return new PayBonusResultDto(
            bonus.BonusId,
            bonus.BonusStateCode,
            bonus.GrossAmount,
            distribution.Select(x => new ProjectAllocationDto(
                x.Project.EmployeeProjectHoursId,
                x.Project.ProjectName,
                x.Project.Hours,
                x.Amount)).ToList());
    }

    private async Task<Bonus> GetBonusForUpdateAsync(int bonusId, CancellationToken cancellationToken)
    {
        if (bonusId <= 0)
        {
            throw BonusNotFound();
        }

        return await repository.GetForUpdateAsync(bonusId, cancellationToken) ?? throw BonusNotFound();
    }

    private async Task SaveMutationAsync(CancellationToken cancellationToken)
    {
        if (!await repository.TrySaveChangesAsync(cancellationToken))
        {
            throw new BusinessException(
                "BONUS_CONCURRENT_UPDATE",
                "El bono fue actualizado por otro usuario. Actualiza el listado e intenta nuevamente.");
        }
    }

    private void EnsurePeriodIsNotFuture(DateOnly period)
    {
        var localNow = timeProvider.GetLocalNow();
        var currentPeriod = new DateOnly(localNow.Year, localNow.Month, 1);
        if (period > currentPeriod)
        {
            throw new BusinessException("FUTURE_PERIOD", "El periodo no puede ser posterior al mes actual.");
        }
    }

    private static DateOnly ParsePeriod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !DateOnly.TryParseExact(
                $"{value.Trim()}-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var period))
        {
            throw new BusinessException("INVALID_PERIOD", "El periodo debe tener el formato YYYY-MM.");
        }

        return period;
    }

    private static void EnsureEmployeeAvailable(Employee? employee)
    {
        if (employee is null)
        {
            throw new BusinessException("EMPLOYEE_NOT_FOUND", "No se encontró al empleado indicado.");
        }

        if (employee.EmployeeStateCode != EmployeeStates.Active)
        {
            throw new BusinessException("EMPLOYEE_INACTIVE", "El empleado se encuentra cesado y no puede recibir bonos.");
        }
    }

    private static string NormalizeUserCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException("USER_CODE_REQUIRED", "El header UserCode es obligatorio.");
        }

        var result = value.Trim();
        if (result.Length > 100)
        {
            throw new BusinessException("INVALID_USER_CODE", "El header UserCode no puede superar los 100 caracteres.");
        }

        return result;
    }

    private static string NormalizeRequiredCode(string? value, string errorCode, string description)
    {
        var result = NormalizeOptionalCode(value);
        return result ?? throw new BusinessException(errorCode, description);
    }

    private static string? NormalizeOptionalCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static BusinessException DuplicateBonus() => new(
        "DUPLICATE_ACTIVE_BONUS",
        "El empleado ya tiene un bono activo para el periodo seleccionado.");

    private static BusinessException BonusNotFound() => new(
        "BONUS_NOT_FOUND",
        "No se encontró el bono solicitado.");
}
