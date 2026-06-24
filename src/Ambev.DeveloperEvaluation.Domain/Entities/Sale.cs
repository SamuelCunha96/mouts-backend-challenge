using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }

    // External Identity pattern — Customer (from Users domain)
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // External Identity pattern — Branch (from Branches domain)
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public SaleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private readonly List<SaleItem> _items = new();
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);

    public Sale()
    {
        CreatedAt = DateTime.UtcNow;
        Status = SaleStatus.Active;
        SaleDate = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Cannot add items to a cancelled sale.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId && !i.IsCancelled);
        if (existing != null)
            throw new DomainException($"Product '{productName}' is already in the sale. Update the existing item instead.");

        var item = new SaleItem(productId, productName, unitPrice, quantity);
        item.SaleId = Id;
        _items.Add(item);

        UpdatedAt = DateTime.UtcNow;
    }

    public void CancelItem(Guid productId)
    {
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Sale is already cancelled.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId && !i.IsCancelled)
            ?? throw new DomainException($"Active item with product ID '{productId}' not found in this sale.");

        item.Cancel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Sale is already cancelled.");

        Status = SaleStatus.Cancelled;
        foreach (var item in _items.Where(i => !i.IsCancelled))
            item.Cancel();

        UpdatedAt = DateTime.UtcNow;
    }

    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}
