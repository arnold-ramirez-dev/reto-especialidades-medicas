using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

public sealed class BonusFilterRequest : PaginationRequest
{
    public string? DepartmentCode { get; init; }
    public int? EmployeeNumber { get; init; }
    public string? Period { get; init; }
    public string? StateCode { get; init; }
}

public sealed record BonusQuery(
    string? DepartmentCode,
    int? EmployeeNumber,
    DateOnly? Period,
    string? StateCode,
    int PageIndex,
    int PageSize);

public sealed record BonusListItemDto(
    int BonusId,
    string StateCode,
    string StateDescription,
    DateOnly Period,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeName,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal EnteredValue,
    string ReasonCode,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount,
    DateTime CreatedAt);

public sealed record EmployeeBonusDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate);

public sealed class CreateBonusRequest
{
    public string DepartmentCode { get; init; } = string.Empty;
    public int EmployeeNumber { get; init; }
    public string Period { get; init; } = string.Empty;
    public int BonusTypeId { get; init; }
    public decimal Value { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}

public sealed record BonusMutationDto(int BonusId, string StateCode);

public sealed record ProjectAllocationDto(
    int EmployeeProjectHoursId,
    string ProjectName,
    decimal Hours,
    decimal AllocatedAmount);

public sealed record PayBonusResultDto(
    int BonusId,
    string StateCode,
    decimal GrossAmount,
    IReadOnlyList<ProjectAllocationDto> Allocations);
