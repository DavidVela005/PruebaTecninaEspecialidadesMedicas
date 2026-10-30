using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.Domain.Entities;

public class Bonus
{
    public int BonusId { get; private set; }
    public string DepartmentCode { get; private set; } = null!;
    public int EmployeeNumber { get; private set; }
    /// <summary>Primer día del mes del periodo.</summary>
    public DateOnly Period { get; private set; }
    public int BonusTypeId { get; private set; }
    /// <summary>Monto fijo o porcentaje del sueldo, según el tipo de bono.</summary>
    public decimal BonusValue { get; private set; }
    public string ReasonCode { get; private set; } = null!;
    public decimal TaxRate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string BonusStateCode { get; private set; } = BonusStates.Registered;
    public bool IsDeleted { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;
    public BonusType BonusType { get; private set; } = null!;
    public BonusReason Reason { get; private set; } = null!;
    public State State { get; private set; } = null!;

    private Bonus() { }

    public static Bonus Register(Employee employee, DateOnly period, int bonusTypeId, decimal bonusValue,
        string reasonCode, string userCode, DateTime now)
    {
        BonusCalculator.ValidateValue(bonusTypeId, bonusValue, employee.MonthlySalary);
        var amounts = BonusCalculator.Calculate(bonusTypeId, bonusValue, employee.MonthlySalary, employee.TaxRate);

        return new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = period,
            BonusTypeId = bonusTypeId,
            BonusValue = bonusValue,
            ReasonCode = reasonCode,
            TaxRate = employee.TaxRate,
            GrossAmount = amounts.Gross,
            TaxAmount = amounts.Tax,
            NetAmount = amounts.Net,
            BonusStateCode = BonusStates.Registered,
            CreatedBy = userCode,
            CreatedAt = now
        };
    }

    public void Cancel(string userCode, DateTime now)
    {
        EnsureRegistered("anular");
        BonusStateCode = BonusStates.Cancelled;
        Touch(userCode, now);
    }

    public void Delete(string userCode, DateTime now)
    {
        EnsureRegistered("eliminar");
        IsDeleted = true;
        Touch(userCode, now);
    }

    private void EnsureRegistered(string action)
    {
        if (BonusStateCode != BonusStates.Registered)
            throw new BusinessException("INVALID_BONUS_STATE", $"Solo se puede {action} un bono en estado Registrado.");
    }

    private void Touch(string userCode, DateTime now)
    {
        ModifiedBy = userCode;
        ModifiedAt = now;
    }
}
