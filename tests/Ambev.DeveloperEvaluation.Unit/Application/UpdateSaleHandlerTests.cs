using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly UpdateSaleHandler _handler;

    public UpdateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _eventRepository = Substitute.For<ISaleEventRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new UpdateSaleHandler(_saleRepository, _eventRepository, _mapper);
    }

    [Fact(DisplayName = "Given existing active sale When updating Then returns updated SaleResult")]
    public async Task Handle_ExistingActiveSale_ReturnsUpdatedResult()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        var expectedResult = SaleHandlerTestData.GenerateSaleResult(sale);

        var updateCommand = new UpdateSaleCommand
        {
            Id = sale.Id,
            SaleDate = DateTime.UtcNow,
            CustomerName = "Updated Customer",
            BranchName = "Updated Branch",
            Items = createCommand.Items
        };

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<SaleResult>(Arg.Any<DomainSale>()).Returns(expectedResult);

        // When
        var result = await _handler.Handle(updateCommand, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).SaveEventAsync(
            "SaleModifiedEvent", Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existing sale ID When updating Then throws KeyNotFoundException")]
    public async Task Handle_NonExistingSaleId_ThrowsKeyNotFoundException()
    {
        // Given
        var command = new UpdateSaleCommand
        {
            Id = Guid.NewGuid(),
            SaleDate = DateTime.UtcNow,
            CustomerName = "Customer",
            BranchName = "Branch",
            Items = SaleHandlerTestData.GenerateValidCreateCommand().Items
        };

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{command.Id}*");
    }

    [Fact(DisplayName = "Given cancelled sale When updating Then throws InvalidOperationException")]
    public async Task Handle_CancelledSale_ThrowsInvalidOperationException()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        sale.Cancel();

        var command = new UpdateSaleCommand
        {
            Id = sale.Id,
            SaleDate = DateTime.UtcNow,
            CustomerName = "Customer",
            BranchName = "Branch",
            Items = createCommand.Items
        };

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact(DisplayName = "Given invalid command When updating Then throws ValidationException")]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        // Given
        var command = new UpdateSaleCommand(); // empty — will fail validation

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact(DisplayName = "Given update command When handling Then previous items are cancelled and new ones added")]
    public async Task Handle_ValidCommand_ReplacesItemsViaCancel()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        var originalProductId = sale.Items.First().ProductId;

        var newItem = new SaleItemDto
        {
            ProductId = Guid.NewGuid(),
            ProductName = "New Product",
            UnitPrice = 50m,
            Quantity = 2
        };

        var command = new UpdateSaleCommand
        {
            Id = sale.Id,
            SaleDate = DateTime.UtcNow,
            CustomerName = "Customer",
            BranchName = "Branch",
            Items = [newItem]
        };

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<SaleResult>(Arg.Any<DomainSale>()).Returns(new SaleResult());

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then — original item was cancelled, new item added
        sale.Items.First(i => i.ProductId == originalProductId).IsCancelled.Should().BeTrue();
        sale.Items.Should().Contain(i => i.ProductId == newItem.ProductId);
    }
}
