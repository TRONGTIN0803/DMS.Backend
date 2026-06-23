using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DMS.Application.Auth;
using DMS.Application.Orders;
using DMS.Domain.Enums;
using DMS.Infrastructure.Persistence;
using DMS.Infrastructure.Persistence.Seed;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DMS.IntegrationTests;

public sealed class ApiIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DMS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=dms_test;Username=dms_app;Password=dms_password";
    private DmsApiFactory? _factory;
    private string? _previousDefaultConnection;

    [Fact]
    public async Task Health_endpoint_returns_success()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Protected_items_endpoint_requires_bearer_token()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/items");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_token_can_call_protected_items_endpoint()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync("/api/v1/items");
        var body = await response.Content.ReadAsStringAsync();
        var challenge = string.Join(", ", response.Headers.WwwAuthenticate.Select(x => x.ToString()));

        response.IsSuccessStatusCode.Should().BeTrue($"response was {(int)response.StatusCode}: {body}; challenge: {challenge}");
    }

    [Fact]
    public async Task Refresh_token_rotates_and_returns_new_access_token()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.IsSuccessStatusCode.Should().BeTrue();
        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task Authenticated_admin_can_create_sales_order_draft()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var request = new CreateSalesOrderRequest(
            CompanyId: 1,
            CustomerId: 1,
            SalesPersonId: 1,
            SiteId: 1,
            OrderDate: null,
            Note: "Integration draft order",
            Lines: [new CreateSalesOrderLineRequest(ItemId: 1, Quantity: 2m)]);

        var response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);
        var order = await response.Content.ReadFromJsonAsync<SalesOrderResponse>();
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        order.Should().NotBeNull();
        order!.OrderNo.Should().StartWith($"SO-{DateTimeOffset.UtcNow:yyyy}-");
        order.Status.Should().Be(SalesOrderStatus.Draft);
        order.SubTotal.Should().Be(200000m);
        order.VatAmount.Should().Be(16000m);
        order.GrandTotal.Should().Be(216000m);
        order.Lines.Should().ContainSingle();
        order.Lines[0].UnitPrice.Should().Be(100000m);
        order.Lines[0].VatRate.Should().Be(8m);
    }

    public async Task InitializeAsync()
    {
        _previousDefaultConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);

        _factory = new DmsApiFactory(_connectionString);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DROP SCHEMA IF EXISTS public CASCADE;
            CREATE SCHEMA public;
            """);
        await dbContext.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _previousDefaultConnection);
    }

    private HttpClient CreateClient()
    {
        _factory.Should().NotBeNull();
        return _factory!.CreateClient();
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin@12345"));
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.IsSuccessStatusCode.Should().BeTrue();
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshToken.Should().NotBeNullOrWhiteSpace();

        return login;
    }

    private sealed class DmsApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Database:SeedOnStartup"] = "false",
                    ["Jwt:Issuer"] = "DMS.Api",
                    ["Jwt:Audience"] = "DMS.Api",
                    ["Jwt:Secret"] = "dev-only-change-this-secret-before-production",
                    ["Jwt:AccessTokenMinutes"] = "60",
                    ["Jwt:RefreshTokenDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString));
            });
        }
    }
}
