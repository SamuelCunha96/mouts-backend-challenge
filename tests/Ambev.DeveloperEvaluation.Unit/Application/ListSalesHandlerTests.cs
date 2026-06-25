using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ListSalesHandler _handler;

    public ListSalesHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListSalesHandler(_saleRepository, _mapper);
    }

    [Fact(DisplayName = "Given default query When listing sales Then returns paginated result")]
    public async Task Handle_DefaultQuery_ReturnsPaginatedResult()
    {
        // Given
        var sales = Enumerable.Range(1, 3)
            .Select(_ => SaleHandlerTestData.GenerateSaleFromCommand(SaleHandlerTestData.GenerateValidCreateCommand()))
            .ToList();

        _saleRepository.GetAllAsync(1, 10, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((sales.AsEnumerable(), sales.Count));

        _mapper.Map<IEnumerable<SaleResult>>(Arg.Any<IEnumerable<DomainSale>>())
            .Returns(sales.Select(s => SaleHandlerTestData.GenerateSaleResult(s)));

        var query = new ListSalesQuery { Page = 1, Size = 10 };

        // When
        var result = await _handler.Handle(query, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(3);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Data.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Given query with filters When listing sales Then passes filters to repository")]
    public async Task Handle_QueryWithFilters_PassesFiltersToRepository()
    {
        // Given
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var minDate = DateTime.UtcNow.AddDays(-30);
        var maxDate = DateTime.UtcNow;

        _saleRepository.GetAllAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(),
            customerId, branchId, "Active", minDate, maxDate,
            Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<DomainSale>(), 0));

        _mapper.Map<IEnumerable<SaleResult>>(Arg.Any<IEnumerable<DomainSale>>())
            .Returns(Enumerable.Empty<SaleResult>());

        var query = new ListSalesQuery
        {
            Page = 1, Size = 10,
            CustomerId = customerId,
            BranchId = branchId,
            Status = "Active",
            MinDate = minDate,
            MaxDate = maxDate
        };

        // When
        await _handler.Handle(query, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).GetAllAsync(
            1, 10, null,
            customerId, branchId, "Active", minDate, maxDate,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given empty result When listing sales Then returns zero totals")]
    public async Task Handle_EmptyRepository_ReturnsZeroTotals()
    {
        // Given
        _saleRepository.GetAllAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<DomainSale>(), 0));

        _mapper.Map<IEnumerable<SaleResult>>(Arg.Any<IEnumerable<DomainSale>>())
            .Returns(Enumerable.Empty<SaleResult>());

        // When
        var result = await _handler.Handle(new ListSalesQuery(), CancellationToken.None);

        // Then
        result.TotalItems.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given 25 items with page size 10 When listing sales Then computes total pages correctly")]
    public async Task Handle_25Items_ComputesTotalPagesCorrectly()
    {
        // Given
        _saleRepository.GetAllAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<DomainSale>(), 25));

        _mapper.Map<IEnumerable<SaleResult>>(Arg.Any<IEnumerable<DomainSale>>())
            .Returns(Enumerable.Empty<SaleResult>());

        // When
        var result = await _handler.Handle(new ListSalesQuery { Page = 1, Size = 10 }, CancellationToken.None);

        // Then
        result.TotalPages.Should().Be(3); // ceil(25/10)
    }
}
