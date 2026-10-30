using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Services;

public readonly record struct BonusAmounts(decimal Gross, decimal Tax, decimal Net);

/// <summary>
/// Reglas de valor y cálculo del bono. Todos los montos se redondean a 2 decimales con el medio hacia arriba.
/// </summary>
public static class BonusCalculator
{
    public static void ValidateValue(int bonusTypeId, decimal value, decimal monthlySalary)
    {
        switch (bonusTypeId)
        {
            case BonusTypes.FixedAmount:
                if (value <= 0 || value > monthlySalary)
                    throw new BusinessException("INVALID_AMOUNT",
                        $"El monto fijo debe ser mayor a 0 y no mayor al sueldo mensual ({monthlySalary:N2}).");
                break;
            case BonusTypes.SalaryPercentage:
                if (value <= 0 || value > 100)
                    throw new BusinessException("INVALID_PERCENTAGE", "El porcentaje debe ser mayor a 0 y hasta 100.");
                break;
            default:
                throw new BusinessException("INVALID_BONUS_TYPE", "El tipo de bono no es válido.");
        }
    }

    public static BonusAmounts Calculate(int bonusTypeId, decimal value, decimal monthlySalary, decimal taxRate)
    {
        var gross = Round(bonusTypeId == BonusTypes.FixedAmount ? value : monthlySalary * value / 100m);
        var tax = Round(gross * taxRate / 100m);
        return new BonusAmounts(gross, tax, gross - tax);
    }

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
