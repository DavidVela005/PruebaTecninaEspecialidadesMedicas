using System.Globalization;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

/// <summary>Convierte el periodo YYYY-MM de la API en el primer día de ese mes.</summary>
public static class BonusPeriod
{
    public static DateOnly Parse(string? value)
    {
        if (!DateOnly.TryParseExact($"{value}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var period))
            throw new BusinessException("INVALID_PERIOD", "El periodo debe tener el formato YYYY-MM.");
        return period;
    }

    public static string ToText(DateOnly period) => period.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}
