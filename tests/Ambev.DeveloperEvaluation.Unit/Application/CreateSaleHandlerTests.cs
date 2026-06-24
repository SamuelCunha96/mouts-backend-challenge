using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DomainSale = Ambev.DeveloperEvaluation.Domain.Entities.Sale;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _eventRepository = Substitute.For<ISaleEventRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new CreateSaleHandler(_saleRepository, _eventRepository, _mapper);
    }

    [Fact(DisplayName = "Given valid command When creating sale Then returns SaleResult")]
    public async Task Handle_ValidCommand_ReturnsSaleResult()
    {
        // Given
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(command);
        var expectedResult = SaleHandlerTestData.GenerateSaleResult(sale);

        _saleRepository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);
        _saleRepository.CreateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<SaleResult>(Arg.Any<DomainSale>()).Returns(expectedResult);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.SaleNumber.Should().Be(command.SaleNumber);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).SaveEventAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given duplicate sale number When creating sale Then throws InvalidOperationException")]
    public async Task Handle_DuplicateSaleNumber_ThrowsInvalidOperationException()
    {
        // Given
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var existingSale = SaleHandlerTestData.GenerateSaleFromCommand(command);

        _saleRepository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns(existingSale);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{command.SaleNumber}*");
    }

    [Fact(DisplayName = "Given invalid command When creating sale Then throws ValidationException")]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        // Given
        var command = new CreateSaleCommand(); // empty — will fail validation

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact(DisplayName = "Given valid command When creating sale Then domain events are published")]
    public async Task Handle_ValidCommand_PublishesDomainEvent()
    {
        // Given
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var sale = SaleHandlerTestData.GenerateSaleFromCommand(command);

        _saleRepository.GetBySaleNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DomainSale?)null);
        _saleRepository.CreateAsync(Arg.Any<DomainSale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<SaleResult>(Arg.Any<DomainSale>()).Returns(new SaleResult());

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _eventRepository.Received(1).SaveEventAsync(
            "SaleCreatedEvent",
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
