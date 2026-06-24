using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _eventRepository = Substitute.For<ISaleEventRepository>();
        _handler = new CancelSaleHandler(_saleRepository, _eventRepository);
    }

    [Fact(DisplayName = "Given active sale When cancelling Then sale is cancelled and event is published")]
    public async Task Handle_ActiveSale_CancelsSaleAndPublishesEvent()
    {
        // Given
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(command);

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var result = await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        // Then
        result.Success.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Cancelled);
        await _saleRepository.Received(1).UpdateAsync(
            Arg.Is<DomainSale>(s => s.Status == SaleStatus.Cancelled),
            Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).SaveEventAsync(
            "SaleCancelledEvent", Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existing sale ID When cancelling Then throws KeyNotFoundException")]
    public async Task Handle_NonExistingSaleId_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);

        // When
        var act = () => _handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }
}
