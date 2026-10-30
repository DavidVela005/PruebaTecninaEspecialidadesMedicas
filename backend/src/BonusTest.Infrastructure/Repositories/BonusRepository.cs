using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public Task<Employee?> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber, cancellationToken);

    public Task<bool> BonusTypeExistsAsync(int bonusTypeId, CancellationToken cancellationToken) =>
        context.BonusTypes.AnyAsync(x => x.BonusTypeId == bonusTypeId, cancellationToken);

    public Task<bool> IsActiveReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        context.BonusReasons.AnyAsync(x => x.ReasonCode == reasonCode && x.State, cancellationToken);

    public Task<bool> HasActiveBonusAsync(string departmentCode, int employeeNumber, DateOnly period, CancellationToken cancellationToken) =>
        context.Bonuses.AnyAsync(x => x.DepartmentCode == departmentCode
                                      && x.EmployeeNumber == employeeNumber
                                      && x.Period == period
                                      && x.BonusStateCode != BonusStates.Cancelled, cancellationToken);

    public Task<Bonus?> GetByIdAsync(int bonusId, CancellationToken cancellationToken) =>
        context.Bonuses.FirstOrDefaultAsync(x => x.BonusId == bonusId, cancellationToken);

    public async Task<BonusDto> GetDtoByIdAsync(int bonusId, CancellationToken cancellationToken)
    {
        var row = await Project(context.Bonuses.AsNoTracking().Where(x => x.BonusId == bonusId))
            .SingleAsync(cancellationToken);
        return row.ToDto();
    }

    public async Task<PagedResponse<BonusDto>> GetPagedAsync(BonusFilter filter, DateOnly? period, CancellationToken cancellationToken)
    {
        var query = context.Bonuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            query = query.Where(x => x.DepartmentCode == filter.DepartmentCode);
        if (filter.EmployeeNumber is { } employeeNumber)
            query = query.Where(x => x.EmployeeNumber == employeeNumber);
        if (period is { } p)
            query = query.Where(x => x.Period == p);
        if (!string.IsNullOrWhiteSpace(filter.StateCode))
            query = query.Where(x => x.BonusStateCode == filter.StateCode);

        var totalRecords = await query.CountAsync(cancellationToken);

        // Paginación en la base de datos (OFFSET/FETCH); el orden incluye BonusId para que sea determinista.
        var rows = await Project(query
                .OrderByDescending(x => x.Period)
                .ThenByDescending(x => x.BonusId)
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize))
            .ToListAsync(cancellationToken);

        return PagedResponse<BonusDto>.Create(rows.Select(x => x.ToDto()).ToList(), filter.PageIndex, filter.PageSize, totalRecords);
    }

    public async Task AddAsync(Bonus bonus, CancellationToken cancellationToken) =>
        await context.Bonuses.AddAsync(bonus, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            // Otra solicitud registró el mismo bono entre la validación y el guardado.
            throw BonusService.DuplicatePeriod();
        }
    }

    private IQueryable<BonusRow> Project(IQueryable<Bonus> query) =>
        from b in query
        join d in context.Departments on b.DepartmentCode equals d.DepartmentCode
        select new BonusRow(
            b.BonusId, b.BonusStateCode, b.State.StateDescription, b.Period,
            b.DepartmentCode, d.DepartmentName, b.EmployeeNumber, b.Employee.FullName,
            b.BonusTypeId, b.BonusType.BonusTypeDescription, b.BonusValue,
            b.ReasonCode, b.Reason.ReasonDescription,
            b.GrossAmount, b.TaxAmount, b.NetAmount);

    /// <summary>Fila traída de la BD; el periodo se formatea como texto fuera de la consulta.</summary>
    private sealed record BonusRow(
        int BonusId, string StateCode, string StateDescription, DateOnly Period,
        string DepartmentCode, string DepartmentName, int EmployeeNumber, string FullName,
        int BonusTypeId, string BonusTypeDescription, decimal BonusValue,
        string ReasonCode, string ReasonDescription,
        decimal GrossAmount, decimal TaxAmount, decimal NetAmount)
    {
        public BonusDto ToDto() => new(
            BonusId, StateCode, StateDescription, BonusPeriod.ToText(Period),
            DepartmentCode, DepartmentName, EmployeeNumber, FullName,
            BonusTypeId, BonusTypeDescription, BonusValue,
            ReasonCode, ReasonDescription,
            GrossAmount, TaxAmount, NetAmount);
    }
}
