using FluentValidation;

namespace DMS.Application.Catalog;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Barcode).MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Barcode).MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
    }
}
