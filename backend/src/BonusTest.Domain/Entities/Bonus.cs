using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.Domain.Entities;

public class Bonus
{
    private Bonus() { }

    public int BonusId { get; private set; }
    public string DepartmentCode { get; private set; } = null!;
    public int EmployeeNumber { get; private set; }
    public DateOnly Period { get; private set; }
    public int BonusTypeId { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }
    public string ReasonCode { get; private set; } = null!;
    public decimal MonthlySalarySnapshot { get; private set; }
    public decimal TaxRateSnapshot { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string BonusStateCode { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Bonus Create(
        Employee employee,
        DateOnly period,
        int bonusTypeId,
        decimal enteredValue,
        string reasonCode,
        string userCode,
        DateTime createdAt)
    {
        if (employee.EmployeeStateCode != EmployeeStates.Active)
        {
            throw new BusinessException("EMPLOYEE_INACTIVE", "El empleado se encuentra cesado y no puede recibir bonos.");
        }

        var calculation = BonusCalculator.Calculate(
            employee.MonthlySalary,
            employee.TaxRate,
            bonusTypeId,
            enteredValue);

        return new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = period,
            BonusTypeId = bonusTypeId,
            FixedAmount = bonusTypeId == BonusTypes.FixedAmount ? BonusCalculator.Round(enteredValue) : null,
            Percentage = bonusTypeId == BonusTypes.SalaryPercentage ? BonusCalculator.Round(enteredValue) : null,
            ReasonCode = reasonCode,
            MonthlySalarySnapshot = employee.MonthlySalary,
            TaxRateSnapshot = employee.TaxRate,
            GrossAmount = calculation.GrossAmount,
            TaxAmount = calculation.TaxAmount,
            NetAmount = calculation.NetAmount,
            BonusStateCode = BonusStates.Registered,
            IsDeleted = false,
            CreatedBy = userCode,
            CreatedAt = createdAt
        };
    }

    public void Cancel(string userCode, DateTime modifiedAt)
    {
        EnsureRegistered();
        BonusStateCode = BonusStates.Cancelled;
        SetAudit(userCode, modifiedAt);
    }

    public void SoftDelete(string userCode, DateTime modifiedAt)
    {
        EnsureRegistered();
        IsDeleted = true;
        SetAudit(userCode, modifiedAt);
    }

    public void MarkAsPaid(string userCode, DateTime modifiedAt)
    {
        EnsureRegistered();
        BonusStateCode = BonusStates.Paid;
        SetAudit(userCode, modifiedAt);
    }

    private void EnsureRegistered()
    {
        if (IsDeleted || BonusStateCode != BonusStates.Registered)
        {
            throw new BusinessException(
                "INVALID_BONUS_STATE",
                "La operación solo está permitida para bonos en estado Registrado.");
        }
    }

    private void SetAudit(string userCode, DateTime modifiedAt)
    {
        ModifiedBy = userCode;
        ModifiedAt = modifiedAt;
    }
}
