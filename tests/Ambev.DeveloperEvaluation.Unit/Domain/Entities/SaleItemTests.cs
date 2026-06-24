using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    [Theory(DisplayName = "Given quantity below 4 When calculating discount Then returns 0%")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Given_QuantityBelow4_When_CalculateDiscount_Then_ReturnsZero(int quantity)
    {
        var discount = SaleItem.CalculateDiscount(quantity);
        discount.Should().Be(0m);
    }

    [Theory(DisplayName = "Given quantity between 4 and 9 When calculating discount Then returns 10%")]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(9)]
    public void Given_QuantityBetween4And9_When_CalculateDiscount_Then_Returns10Percent(int quantity)
    {
        var discount = SaleItem.CalculateDiscount(quantity);
        discount.Should().Be(0.10m);
    }

    [Theory(DisplayName = "Given quantity between 10 and 20 When calculating discount Then returns 20%")]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void Given_QuantityBetween10And20_When_CalculateDiscount_Then_Returns20Percent(int quantity)
    {
        var discount = SaleItem.CalculateDiscount(quantity);
        discount.Should().Be(0.20m);
    }

    [Fact(DisplayName = "Given quantity above 20 When creating SaleItem Then throws DomainException")]
    public void Given_QuantityAbove20_When_CreateSaleItem_Then_ThrowsDomainException()
    {
        var act = () => new SaleItem(Guid.NewGuid(), "Product", 10m, 21);
        act.Should().Throw<DomainException>()
            .WithMessage("*20*");
    }

    [Fact(DisplayName = "Given valid SaleItem When computing TotalAmount Then applies discount correctly")]
    public void Given_ValidSaleItem_When_ComputingTotalAmount_Then_AppliesDiscountCorrectly()
    {
        // 10 items at $100 with 20% discount = $800
        var item = new SaleItem(Guid.NewGuid(), "Product", 100m, 10);

        item.TotalAmount.Should().Be(800m);
        item.Discount.Should().Be(0.20m);
    }

    [Fact(DisplayName = "Given SaleItem When cancelled Then IsCancelled is true")]
    public void Given_SaleItem_When_Cancelled_Then_IsCancelledIsTrue()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 10m, 1);

        item.Cancel();

        item.IsCancelled.Should().BeTrue();
    }
}
