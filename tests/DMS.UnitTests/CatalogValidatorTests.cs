using DMS.Application.Catalog;
using DMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DMS.UnitTests;

public sealed class CatalogValidatorTests
{
    [Fact]
    public void Create_company_validator_accepts_valid_request()
    {
        var validator = new CreateCompanyRequestValidator();
        var request = new CreateCompanyRequest("NPP001", "Default Distributor", "0000000000", "Ho Chi Minh City", "0900000000", "ops@example.com");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_company_validator_rejects_invalid_email()
    {
        var validator = new CreateCompanyRequestValidator();
        var request = new CreateCompanyRequest("NPP001", "Default Distributor", null, null, null, "invalid-email");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCompanyRequest.Email));
    }

    [Fact]
    public void Create_sales_person_validator_requires_positive_company_id()
    {
        var validator = new CreateSalesPersonRequestValidator();
        var request = new CreateSalesPersonRequest("SP001", "Default Sales", 0, "0900000001", null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateSalesPersonRequest.CompanyId));
    }

    [Fact]
    public void Create_customer_validator_rejects_unknown_customer_type()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest("CUS001", "Default Customer", 1, null, null, null, (CustomerType)99);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCustomerRequest.CustomerType));
    }

    [Fact]
    public void Create_site_validator_rejects_empty_code()
    {
        var validator = new CreateSiteRequestValidator();
        var request = new CreateSiteRequest("", "Main Warehouse", 1, "Ho Chi Minh City");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateSiteRequest.Code));
    }

    [Fact]
    public void Create_item_validator_accepts_valid_request()
    {
        var validator = new CreateItemRequestValidator();
        var request = new CreateItemRequest("ITEM001", "Sample Item", "case", "893000000001", 100000m, 8m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_item_validator_rejects_vat_rate_over_100()
    {
        var validator = new CreateItemRequestValidator();
        var request = new CreateItemRequest("ITEM001", "Sample Item", "case", null, 100000m, 101m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateItemRequest.VatRate));
    }

    [Fact]
    public void Create_item_validator_rejects_negative_price()
    {
        var validator = new CreateItemRequestValidator();
        var request = new CreateItemRequest("ITEM001", "Sample Item", "case", null, -1m, 8m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateItemRequest.Price));
    }

    [Fact]
    public void Create_inventory_validator_accepts_available_stock()
    {
        var validator = new CreateInventoryRequestValidator();
        var request = new CreateInventoryRequest(1, 1, 100m, 20m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_inventory_validator_rejects_reserved_quantity_over_quantity()
    {
        var validator = new CreateInventoryRequestValidator();
        var request = new CreateInventoryRequest(1, 1, 10m, 11m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateInventoryRequest.ReservedQuantity));
    }
}
