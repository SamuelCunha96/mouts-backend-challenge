using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Given valid sale When adding item Then item is added to collection")]
    public void Given_ValidSale_When_AddItem_Then_ItemIsAddedToCollection()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        var (productId, name, price) = SaleTestData.GenerateValidItemData();

        sale.AddItem(productId, name, price, 1);

        sale.Items.Should().HaveCount(1);
        sale.Items.First().ProductId.Should().Be(productId);
    }

    [Fact(DisplayName = "Given 4 identical items When adding Then applies 10% discount")]
    public void Given_4Items_When_AddItem_Then_Applies10PercentDiscount()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        var productId = Guid.NewGuid();

        sale.AddItem(productId, "Product", 100m, 4);

        sale.Items.First().Discount.Should().Be(0.10m);
        sale.Items.First().TotalAmount.Should().Be(360m); // 4 * 100 * 0.90
    }

    [Fact(DisplayName = "Given 10 identical items When adding Then applies 20% discount")]
    public void Given_10Items_When_AddItem_Then_Applies20PercentDiscount()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        var productId = Guid.NewGuid();

        sale.AddItem(productId, "Product", 100m, 10);

        sale.Items.First().Discount.Should().Be(0.20m);
        sale.Items.First().TotalAmount.Should().Be(800m); // 10 * 100 * 0.80
    }

    [Fact(DisplayName = "Given 3 items When adding Then no discount applied")]
    public void Given_3Items_When_AddItem_Then_NoDiscountApplied()
    {
        var sale = SaleTestData.GenerateValidSale(0);

        sale.AddItem(Guid.NewGuid(), "Product", 100m, 3);

        sale.Items.First().Discount.Should().Be(0m);
        sale.Items.First().TotalAmount.Should().Be(300m);
    }

    [Fact(DisplayName = "Given 21 items When adding Then throws DomainException")]
    public void Given_21Items_When_AddItem_Then_ThrowsDomainException()
    {
        var sale = SaleTestData.GenerateValidSale(0);

        var act = () => sale.AddItem(Guid.NewGuid(), "Product", 100m, 21);

        act.Should().Throw<DomainException>().WithMessage("*20*");
    }

    [Fact(DisplayName = "Given active sale When cancelling item Then item is marked as cancelled")]
    public void Given_ActiveSale_When_CancelItem_Then_ItemIsMarkedCancelled()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Product", 100m, 1);

        sale.CancelItem(productId);

        sale.Items.First(i => i.ProductId == productId).IsCancelled.Should().BeTrue();
    }

    [Fact(DisplayName = "Given cancelled sale When cancelling item Then throws DomainException")]
    public void Given_CancelledSale_When_CancelItem_Then_ThrowsDomainException()
    {
        var sale = SaleTestData.GenerateValidSale(1);
        sale.Cancel();

        var act = () => sale.CancelItem(sale.Items.First().ProductId);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given active sale When cancelling Then status becomes Cancelled")]
    public void Given_ActiveSale_When_Cancel_Then_StatusIsCancelled()
    {
        var sale = SaleTestData.GenerateValidSale(1);

        sale.Cancel();

        sale.Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact(DisplayName = "Given cancelled sale When cancelling again Then throws DomainException")]
    public void Given_CancelledSale_When_CancelAgain_Then_ThrowsDomainException()
    {
        var sale = SaleTestData.GenerateValidSale(1);
        sale.Cancel();

        var act = () => sale.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given sale with cancelled item When computing TotalAmount Then excludes cancelled items")]
    public void Given_SaleWithCancelledItem_When_ComputingTotalAmount_Then_ExcludesCancelledItems()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        var product1 = Guid.NewGuid();
        var product2 = Guid.NewGuid();
        sale.AddItem(product1, "Product 1", 100m, 1);
        sale.AddItem(product2, "Product 2", 200m, 1);

        sale.CancelItem(product1);

        sale.TotalAmount.Should().Be(200m);
    }

    [Fact(DisplayName = "Given cancelled sale When cancelling Then all active items are cancelled")]
    public void Given_ActiveSale_When_Cancel_Then_AllItemsAreCancelled()
    {
        var sale = SaleTestData.GenerateValidSale(0);
        sale.AddItem(Guid.NewGuid(), "P1", 10m, 1);
        sale.AddItem(Guid.NewGuid(), "P2", 20m, 1);

        sale.Cancel();

        sale.Items.Should().AllSatisfy(i => i.IsCancelled.Should().BeTrue());
    }
}
