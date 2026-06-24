using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class GetSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly GetSaleHandler _handler;

    public GetSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetSaleHandler(_saleRepository, _mapper);
    }

    [Fact(DisplayName = "Given existing sale ID When getting sale Then returns SaleResult")]
    public async Task Handle_ExistingSaleId_ReturnsSaleResult()
    {
        // Given
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(command);
        var expectedResult = SaleHandlerTestData.GenerateSaleResult(sale);

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<SaleResult>(sale).Returns(expectedResult);

        // When
        var result = await _handler.Handle(new GetSaleQuery(sale.Id), CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedResult.Id);
        result.SaleNumber.Should().Be(expectedResult.SaleNumber);
    }

    [Fact(DisplayName = "Given non-existing sale ID When getting sale Then throws KeyNotFoundException")]
    public async Task Handle_NonExistingSaleId_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);

        // When
        var act = () => _handler.Handle(new GetSaleQuery(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }
}
