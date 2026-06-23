using FluentValidation;

namespace DMS.Application.Orders;

public sealed class CreateSalesOrderRequestValidator : AbstractValidator<CreateSalesOrderRequest>
{
    public CreateSalesOrderRequestValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.SiteId).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new CreateSalesOrderLineRequestValidator());
    }
}

public sealed class CreateSalesOrderLineRequestValidator : AbstractValidator<CreateSalesOrderLineRequest>
{
    public CreateSalesOrderLineRequestValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
