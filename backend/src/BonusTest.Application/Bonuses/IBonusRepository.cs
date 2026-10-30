using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<Employee?> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task<bool> BonusTypeExistsAsync(int bonusTypeId, CancellationToken cancellationToken);
    Task<bool> IsActiveReasonAsync(string reasonCode, CancellationToken cancellationToken);
    Task<bool> HasActiveBonusAsync(string departmentCode, int employeeNumber, DateOnly period, CancellationToken cancellationToken);
    Task<Bonus?> GetByIdAsync(int bonusId, CancellationToken cancellationToken);
    Task<BonusDto> GetDtoByIdAsync(int bonusId, CancellationToken cancellationToken);
    Task<PagedResponse<BonusDto>> GetPagedAsync(BonusFilter filter, DateOnly? period, CancellationToken cancellationToken);
    Task AddAsync(Bonus bonus, CancellationToken cancellationToken);
    /// <summary>Guarda los cambios. Lanza BusinessException si choca con el índice único de bono activo por periodo.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
