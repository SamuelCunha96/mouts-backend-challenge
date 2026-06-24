namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ISaleEventRepository
{
    Task SaveEventAsync(string eventType, Guid saleId, string saleNumber, object payload, CancellationToken cancellationToken = default);
}
