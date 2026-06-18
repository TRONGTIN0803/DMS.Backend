using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DMS.IntegrationTests;

public sealed class SmokeTests
{
    [Fact(Skip = "Requires PostgreSQL test container/runtime.")]
    public async Task Health_endpoint_returns_success()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
