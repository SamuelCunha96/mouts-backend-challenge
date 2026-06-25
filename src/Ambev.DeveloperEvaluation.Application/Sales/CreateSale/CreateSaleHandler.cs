using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public CreateSaleHandler(ISaleRepository saleRepository, ISaleEventRepository eventRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var existing = await _saleRepository.GetBySaleNumberAsync(command.SaleNumber, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Sale with number '{command.SaleNumber}' already exists.");

        var sale = new Sale
        {
            SaleNumber = command.SaleNumber,
            SaleDate = DateTime.SpecifyKind(command.SaleDate, DateTimeKind.Utc),
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName
        };

        foreach (var item in command.Items)
            sale.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

        var created = await _saleRepository.CreateAsync(sale, cancellationToken);

        await _eventRepository.SaveEventAsync(
            nameof(SaleCreatedEvent),
            created.Id,
            created.SaleNumber,
            new SaleCreatedEvent(created),
            cancellationToken);

        return _mapper.Map<SaleResult>(created);
    }
}
