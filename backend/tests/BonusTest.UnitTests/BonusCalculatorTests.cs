using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.UnitTests;

public class BonusCalculatorTests
{
    [Fact]
    public void PercentageBonus_CalculatesGrossTaxAndNet()
    {
        var result = BonusCalculator.Calculate(5_000m, 8m, BonusTypes.SalaryPercentage, 10m);

        Assert.Equal(500m, result.GrossAmount);
        Assert.Equal(40m, result.TaxAmount);
        Assert.Equal(460m, result.NetAmount);
    }

    [Fact]
    public void FixedBonus_CalculatesUsingEnteredAmount()
    {
        var result = BonusCalculator.Calculate(5_000m, 8m, BonusTypes.FixedAmount, 1_000m);

        Assert.Equal(1_000m, result.GrossAmount);
        Assert.Equal(80m, result.TaxAmount);
        Assert.Equal(920m, result.NetAmount);
    }

    [Fact]
    public void Calculation_RoundsMidpointAwayFromZero()
    {
        var result = BonusCalculator.Calculate(1.25m, 0m, BonusTypes.SalaryPercentage, 10m);

        Assert.Equal(0.13m, result.GrossAmount);
        Assert.Equal(0.13m, result.NetAmount);
    }

    [Fact]
    public void EmployeeWithoutTax_ReceivesGrossAsNet()
    {
        var result = BonusCalculator.Calculate(2_500m, 0m, BonusTypes.FixedAmount, 500m);

        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(500m, result.NetAmount);
    }

    [Fact]
    public void FixedBonusAboveSalary_IsRejected()
    {
        var exception = Assert.Throws<BusinessException>(() =>
            BonusCalculator.Calculate(2_000m, 8m, BonusTypes.FixedAmount, 2_000.01m));

        Assert.Equal("INVALID_FIXED_AMOUNT", exception.Code);
    }

    [Fact]
    public void PercentageAboveOneHundred_IsRejected()
    {
        var exception = Assert.Throws<BusinessException>(() =>
            BonusCalculator.Calculate(2_000m, 8m, BonusTypes.SalaryPercentage, 100.01m));

        Assert.Equal("INVALID_PERCENTAGE", exception.Code);
    }
}
