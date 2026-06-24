using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }

    // External Identity pattern — denormalized product info
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public bool IsCancelled { get; set; }

    public decimal TotalAmount => Quantity * UnitPrice * (1 - Discount);

    public SaleItem() { }

    public SaleItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity > 20)
            throw new DomainException("Cannot sell more than 20 identical items.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Discount = CalculateDiscount(quantity);
        IsCancelled = false;
    }

    public static decimal CalculateDiscount(int quantity)
    {
        if (quantity >= 10 && quantity <= 20)
            return 0.20m;

        if (quantity >= 4)
            return 0.10m;

        return 0m;
    }

    public void Cancel()
    {
        IsCancelled = true;
    }
}
