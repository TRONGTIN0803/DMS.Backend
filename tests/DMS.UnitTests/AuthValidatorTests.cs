using DMS.Application.Auth;
using FluentAssertions;
using Xunit;

namespace DMS.UnitTests;

public sealed class AuthValidatorTests
{
    [Fact]
    public void Login_validator_accepts_valid_request()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("admin", "Admin@12345");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Login_validator_rejects_empty_user_name()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("", "Admin@12345");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(LoginRequest.UserName));
    }

    [Fact]
    public void Login_validator_rejects_empty_password()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("admin", "");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Refresh_token_validator_rejects_empty_refresh_token()
    {
        var validator = new RefreshTokenRequestValidator();
        var request = new RefreshTokenRequest("");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
    }
}
