using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

public static class SaleHandlerTestData
{
    private static readonly Faker Faker = new();

    public static CreateSaleCommand GenerateValidCreateCommand()
    {
        return new CreateSaleCommand
        {
            SaleNumber = Faker.Random.AlphaNumeric(8).ToUpper(),
            SaleDate = Faker.Date.Recent(),
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City(),
            Items =
            [
                new SaleItemDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = Faker.Commerce.ProductName(),
                    UnitPrice = Faker.Random.Decimal(1, 500),
                    Quantity = Faker.Random.Int(1, 3)
                }
            ]
        };
    }

    public static Sale GenerateSaleFromCommand(CreateSaleCommand command)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = command.SaleNumber,
            SaleDate = command.SaleDate,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
            Status = SaleStatus.Active
        };

        foreach (var item in command.Items)
            sale.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

        return sale;
    }

    public static SaleResult GenerateSaleResult(Sale sale)
    {
        return new SaleResult
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerName,
            BranchId = sale.BranchId,
            BranchName = sale.BranchName,
            Status = sale.Status,
            TotalAmount = sale.TotalAmount,
            Items = sale.Items.Select(i => new SaleItemResult
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                TotalAmount = i.TotalAmount,
                IsCancelled = i.IsCancelled
            })
        };
    }
}
