using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

public static class SaleTestData
{
    private static readonly Faker Faker = new();

    public static Sale GenerateValidSale(int itemCount = 1)
    {
        var sale = new Sale
        {
            SaleNumber = Faker.Random.AlphaNumeric(8).ToUpper(),
            SaleDate = Faker.Date.Recent(),
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City()
        };

        for (var i = 0; i < itemCount; i++)
            sale.AddItem(Guid.NewGuid(), Faker.Commerce.ProductName(), Faker.Random.Decimal(1, 500), 1);

        return sale;
    }

    public static (Guid ProductId, string ProductName, decimal UnitPrice) GenerateValidItemData(int quantity = 1)
        => (Guid.NewGuid(), Faker.Commerce.ProductName(), Faker.Random.Decimal(1, 500));
}
