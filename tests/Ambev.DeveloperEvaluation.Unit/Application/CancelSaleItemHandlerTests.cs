using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly CancelSaleItemHandler _handler;

    public CancelSaleItemHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _eventRepository = Substitute.For<ISaleEventRepository>();
        _handler = new CancelSaleItemHandler(_saleRepository, _eventRepository);
    }

    [Fact(DisplayName = "Given active item When cancelling item Then item is cancelled and event is published")]
    public async Task Handle_ActiveItem_CancelsItemAndPublishesEvent()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        var productId = sale.Items.First().ProductId;

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, productId), CancellationToken.None);

        // Then
        result.Success.Should().BeTrue();
        sale.Items.First(i => i.ProductId == productId).IsCancelled.Should().BeTrue();
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).SaveEventAsync(
            "SaleItemCancelledEvent", Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existing sale ID When cancelling item Then throws KeyNotFoundException")]
    public async Task Handle_NonExistingSaleId_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(saleId, Guid.NewGuid()), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{saleId}*");
    }

    [Fact(DisplayName = "Given non-existing product ID When cancelling item Then throws KeyNotFoundException")]
    public async Task Handle_NonExistingProductId_ThrowsKeyNotFoundException()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        var nonExistingProductId = Guid.NewGuid();

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, nonExistingProductId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{nonExistingProductId}*");
    }

    [Fact(DisplayName = "Given already cancelled item When cancelling again Then throws KeyNotFoundException")]
    public async Task Handle_AlreadyCancelledItem_ThrowsKeyNotFoundException()
    {
        // Given
        var createCommand = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(createCommand);
        var productId = sale.Items.First().ProductId;

        sale.CancelItem(productId); // pre-cancel the item

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, productId), CancellationToken.None);

        // Then — handler queries for active items only, so cancelled item is not found
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
