using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class SaleItemCancelledEvent
{
    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public Guid ProductId { get; }
    public string ProductName { get; }
    public DateTime OccurredAt { get; }

    public SaleItemCancelledEvent(Sale sale, SaleItem item)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        ProductId = item.ProductId;
        ProductName = item.ProductName;
        OccurredAt = DateTime.UtcNow;
    }
}
