using FluentValidation;

namespace DMS.Application.Catalog;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateSalesPersonRequestValidator : AbstractValidator<CreateSalesPersonRequest>
{
    public CreateSalesPersonRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateSalesPersonRequestValidator : AbstractValidator<UpdateSalesPersonRequest>
{
    public UpdateSalesPersonRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.CustomerType).IsInEnum();
    }
}

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.CustomerType).IsInEnum();
    }
}

public sealed class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public sealed class UpdateSiteRequestValidator : AbstractValidator<UpdateSiteRequest>
{
    public UpdateSiteRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public sealed class CreateInventoryRequestValidator : AbstractValidator<CreateInventoryRequest>
{
    public CreateInventoryRequestValidator()
    {
        RuleFor(x => x.SiteId).GreaterThan(0);
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservedQuantity).LessThanOrEqualTo(x => x.Quantity);
    }
}

public sealed class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservedQuantity).LessThanOrEqualTo(x => x.Quantity);
    }
}
