using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Services;

public record BonusCalculation(decimal GrossAmount, decimal TaxAmount, decimal NetAmount);

public static class BonusCalculator
{
    public static BonusCalculation Calculate(
        decimal monthlySalary,
        decimal taxRate,
        int bonusTypeId,
        decimal enteredValue)
    {
        if (monthlySalary <= 0)
        {
            throw new BusinessException("INVALID_SALARY", "El sueldo mensual del empleado no es válido.");
        }

        if (taxRate is < 0 or > 100)
        {
            throw new BusinessException("INVALID_TAX_RATE", "El porcentaje de retención del empleado no es válido.");
        }

        var normalizedValue = Round(enteredValue);
        var grossAmount = bonusTypeId switch
        {
            BonusTypes.FixedAmount when enteredValue > 0 && enteredValue <= monthlySalary && normalizedValue > 0 =>
                normalizedValue,
            BonusTypes.FixedAmount => throw new BusinessException(
                "INVALID_FIXED_AMOUNT",
                "El monto fijo debe ser mayor a cero y no puede superar el sueldo mensual."),
            BonusTypes.SalaryPercentage when enteredValue > 0 && enteredValue <= 100 && normalizedValue > 0 =>
                monthlySalary * normalizedValue / 100m,
            BonusTypes.SalaryPercentage => throw new BusinessException(
                "INVALID_PERCENTAGE",
                "El porcentaje debe ser mayor a cero y no puede superar el 100 %."),
            _ => throw new BusinessException("INVALID_BONUS_TYPE", "El tipo de bono seleccionado no es válido.")
        };

        var roundedGross = Round(grossAmount);
        var taxAmount = Round(roundedGross * taxRate / 100m);
        var netAmount = Round(roundedGross - taxAmount);

        return new BonusCalculation(roundedGross, taxAmount, netAmount);
    }

    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
