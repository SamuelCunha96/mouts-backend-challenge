using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, CancelSaleItemResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;

    public CancelSaleItemHandler(ISaleRepository saleRepository, ISaleEventRepository eventRepository)
    {
        _saleRepository = saleRepository;
        _eventRepository = eventRepository;
    }

    public async Task<CancelSaleItemResult> Handle(CancelSaleItemCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID '{command.SaleId}' not found.");

        var item = sale.Items.FirstOrDefault(i => i.ProductId == command.ProductId && !i.IsCancelled)
            ?? throw new KeyNotFoundException($"Active item with product ID '{command.ProductId}' not found in this sale.");

        sale.CancelItem(command.ProductId);

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _eventRepository.SaveEventAsync(
            nameof(SaleItemCancelledEvent),
            sale.Id,
            sale.SaleNumber,
            new SaleItemCancelledEvent(sale, item),
            cancellationToken);

        return new CancelSaleItemResult { Success = true, Message = $"Item '{item.ProductName}' cancelled successfully." };
    }
}
