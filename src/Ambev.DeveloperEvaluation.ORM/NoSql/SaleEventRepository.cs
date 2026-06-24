using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.ORM.NoSql;

public class SaleEventRepository : ISaleEventRepository
{
    private readonly IMongoCollection<SaleEventDocument> _collection;
    private readonly ILogger<SaleEventRepository> _logger;

    public SaleEventRepository(IMongoDatabase database, ILogger<SaleEventRepository> logger)
    {
        _collection = database.GetCollection<SaleEventDocument>("SaleEvents");
        _logger = logger;
    }

    public async Task SaveEventAsync(string eventType, Guid saleId, string saleNumber, object payload, CancellationToken cancellationToken = default)
    {
        var document = new SaleEventDocument
        {
            EventType = eventType,
            SaleId = saleId,
            SaleNumber = saleNumber,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);

        _logger.LogInformation("Sale event '{EventType}' saved for sale {SaleNumber} ({SaleId})",
            eventType, saleNumber, saleId);
    }
}
