using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, CancelSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;

    public CancelSaleHandler(ISaleRepository saleRepository, ISaleEventRepository eventRepository)
    {
        _saleRepository = saleRepository;
        _eventRepository = eventRepository;
    }

    public async Task<CancelSaleResult> Handle(CancelSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID '{command.Id}' not found.");

        sale.Cancel();

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _eventRepository.SaveEventAsync(
            nameof(SaleCancelledEvent),
            sale.Id,
            sale.SaleNumber,
            new SaleCancelledEvent(sale),
            cancellationToken);

        return new CancelSaleResult { Success = true, Message = "Sale cancelled successfully." };
    }
}
