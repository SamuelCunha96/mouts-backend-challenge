using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(r => r.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(r => r.SaleDate).NotEmpty();
        RuleFor(r => r.CustomerId).NotEmpty();
        RuleFor(r => r.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(r => r.BranchId).NotEmpty();
        RuleFor(r => r.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Items).NotEmpty();
        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(20);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
        });
    }
}
