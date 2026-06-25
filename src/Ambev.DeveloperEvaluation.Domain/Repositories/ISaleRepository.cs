using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(
        int page,
        int size,
        string? order = null,
        Guid? customerId = null,
        Guid? branchId = null,
        string? status = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        string? customerName = null,
        string? branchName = null,
        CancellationToken cancellationToken = default);
}
