using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public UpdateSaleHandler(ISaleRepository saleRepository, ISaleEventRepository eventRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<SaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID '{command.Id}' not found.");

        if (sale.Status == Domain.Enums.SaleStatus.Cancelled)
            throw new InvalidOperationException("Cannot update a cancelled sale.");

        sale.SaleDate = command.SaleDate;
        sale.CustomerName = command.CustomerName;
        sale.BranchName = command.BranchName;
        sale.UpdatedAt = DateTime.UtcNow;

        // Cancel all active items and re-add from command
        foreach (var item in sale.Items.Where(i => !i.IsCancelled).ToList())
            sale.CancelItem(item.ProductId);

        foreach (var item in command.Items)
            sale.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

        var updated = await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _eventRepository.SaveEventAsync(
            nameof(SaleModifiedEvent),
            updated.Id,
            updated.SaleNumber,
            new SaleModifiedEvent(updated),
            cancellationToken);

        return _mapper.Map<SaleResult>(updated);
    }
}
