using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository, TimeProvider timeProvider)
{
    public async Task<PagedResponse<BonusDto>> GetPagedAsync(BonusFilter filter, CancellationToken cancellationToken)
    {
        var period = string.IsNullOrWhiteSpace(filter.Period) ? (DateOnly?)null : BonusPeriod.Parse(filter.Period);
        return await repository.GetPagedAsync(filter, period, cancellationToken);
    }

    public async Task<EmployeeDto> GetActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await FindActiveEmployeeAsync(departmentCode, employeeNumber, cancellationToken);
        return new EmployeeDto(employee.DepartmentCode, employee.EmployeeNumber, employee.FullName,
            DateOnly.FromDateTime(employee.HireDate), employee.MonthlySalary, employee.TaxRate);
    }

    public async Task<BonusDto> RegisterAsync(CreateBonusRequest request, string? userCode, CancellationToken cancellationToken)
    {
        var user = RequireUser(userCode);
        var employee = await FindActiveEmployeeAsync(request.DepartmentCode, request.EmployeeNumber, cancellationToken);

        var period = BonusPeriod.Parse(request.Period);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (period > new DateOnly(today.Year, today.Month, 1))
            throw new BusinessException("PERIOD_IN_FUTURE", "El periodo no puede ser posterior al mes actual.");

        if (!await repository.BonusTypeExistsAsync(request.BonusTypeId, cancellationToken))
            throw new BusinessException("INVALID_BONUS_TYPE", "El tipo de bono no es válido.");

        if (!await repository.IsActiveReasonAsync(request.ReasonCode, cancellationToken))
            throw new BusinessException("INVALID_REASON", "El motivo no existe o está inactivo.");

        if (await repository.HasActiveBonusAsync(employee.DepartmentCode, employee.EmployeeNumber, period, cancellationToken))
            throw DuplicatePeriod();

        var bonus = Bonus.Register(employee, period, request.BonusTypeId, request.BonusValue,
            request.ReasonCode, user, Now());

        await repository.AddAsync(bonus, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return await repository.GetDtoByIdAsync(bonus.BonusId, cancellationToken);
    }

    public async Task CancelAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var bonus = await FindBonusAsync(bonusId, cancellationToken);
        bonus.Cancel(RequireUser(userCode), Now());
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var bonus = await FindBonusAsync(bonusId, cancellationToken);
        bonus.Delete(RequireUser(userCode), Now());
        await repository.SaveChangesAsync(cancellationToken);
    }

    public static BusinessException DuplicatePeriod() =>
        new("BONUS_ALREADY_EXISTS", "El empleado ya tiene un bono activo en ese periodo.");

    private async Task<Employee> FindActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeAsync(departmentCode, employeeNumber, cancellationToken)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", "No existe un empleado con ese área y número.");

        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_NOT_ACTIVE", "Solo se pueden registrar bonos a empleados activos.");

        return employee;
    }

    private async Task<Bonus> FindBonusAsync(int bonusId, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(bonusId, cancellationToken)
            ?? throw new BusinessException("BONUS_NOT_FOUND", "El bono no existe.");

    private static string RequireUser(string? userCode) =>
        string.IsNullOrWhiteSpace(userCode)
            ? throw new BusinessException("USER_REQUIRED", "Falta el header UserCode.")
            : userCode.Trim();

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}
