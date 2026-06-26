using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DMS.Application.Auth;
using DMS.Application.Catalog;
using DMS.Application.Orders;
using DMS.Domain.Enums;
using DMS.Infrastructure.Persistence;
using DMS.Infrastructure.Persistence.Seed;
using DMS.Shared;
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
    public async Task Versioned_v2_system_endpoint_returns_success()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v2/system/version");
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue(body);
        body.Should().Contain("\"apiVersion\":\"v2\"");
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

    [Fact]
    public async Task Authenticated_admin_can_submit_and_approve_sales_order()
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
            Note: "Integration approved order",
            Lines: [new CreateSalesOrderLineRequest(ItemId: 1, Quantity: 3m)]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/sales-orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdOrder.Should().NotBeNull();

        var submitResponse = await client.PostAsync($"/api/v1/sales-orders/{createdOrder!.Id}/submit", null);
        var submittedOrder = await submitResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        var submitBody = await submitResponse.Content.ReadAsStringAsync();

        submitResponse.IsSuccessStatusCode.Should().BeTrue(submitBody);
        submittedOrder.Should().NotBeNull();
        submittedOrder!.Status.Should().Be(SalesOrderStatus.Submitted);

        using var submitScope = _factory!.Services.CreateScope();
        var submitDbContext = submitScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reservedInventory = await submitDbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        reservedInventory.Quantity.Should().Be(100m);
        reservedInventory.ReservedQuantity.Should().Be(3m);

        var approveResponse = await client.PostAsync($"/api/v1/sales-orders/{createdOrder.Id}/approve", null);
        var approvedOrder = await approveResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        var approveBody = await approveResponse.Content.ReadAsStringAsync();

        approveResponse.IsSuccessStatusCode.Should().BeTrue(approveBody);
        approvedOrder.Should().NotBeNull();
        approvedOrder!.Status.Should().Be(SalesOrderStatus.Approved);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var invoice = await dbContext.Invoices.SingleAsync(x => x.SalesOrderId == createdOrder.Id);
        var batch = await dbContext.Batches.Include(x => x.Details).SingleAsync(x => x.RefType == "SalesOrder" && x.RefId == createdOrder.Id);
        var stockTransaction = await dbContext.StockTransactions.SingleAsync(x => x.RefType == "Batch" && x.RefId == batch.Id);
        var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
        var auditLogs = await dbContext.AuditLogs.Where(x => x.EntityName == "SalesOrder" && x.EntityId == createdOrder.Id.ToString()).ToListAsync();

        inventory.Quantity.Should().Be(97m);
        inventory.ReservedQuantity.Should().Be(0m);
        invoice.InvoiceNo.Should().Be($"INV-{createdOrder.OrderNo}");
        invoice.CustomerId.Should().Be(createdOrder.CustomerId);
        invoice.SubTotal.Should().Be(createdOrder.SubTotal);
        invoice.VatAmount.Should().Be(createdOrder.VatAmount);
        invoice.GrandTotal.Should().Be(createdOrder.GrandTotal);
        batch.BatchNo.Should().Be($"OM-{createdOrder.OrderNo}");
        batch.Type.Should().Be(BatchType.Out);
        batch.Status.Should().Be(BatchStatus.Approved);
        batch.Details.Should().ContainSingle();
        batch.Details.Single().ItemId.Should().Be(1);
        batch.Details.Single().Quantity.Should().Be(3m);
        stockTransaction.TransType.Should().Be(StockTransactionType.Out);
        stockTransaction.SiteId.Should().Be(1);
        stockTransaction.ItemId.Should().Be(1);
        stockTransaction.Quantity.Should().Be(-3m);
        stockTransaction.BalanceAfter.Should().Be(97m);
        outboxMessages.Should().Contain(x => x.Type.EndsWith("SalesOrderApprovedEvent"));
        outboxMessages.Should().Contain(x => x.Type.EndsWith("InvoiceCreatedEvent"));
        auditLogs.Should().Contain(x => x.Action == "Modified");

        var replayApproveResponse = await client.PostAsync($"/api/v1/sales-orders/{createdOrder.Id}/approve", null);
        var replayApproveBody = await replayApproveResponse.Content.ReadAsStringAsync();
        replayApproveResponse.IsSuccessStatusCode.Should().BeTrue(replayApproveBody);

        var inventoryAfterReplay = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var invoiceCount = await dbContext.Invoices.CountAsync(x => x.SalesOrderId == createdOrder.Id);
        var batchCount = await dbContext.Batches.CountAsync(x => x.RefType == "SalesOrder" && x.RefId == createdOrder.Id);
        var stockTransactionCount = await dbContext.StockTransactions.CountAsync(x => x.RefType == "Batch" && x.RefId == batch.Id);

        inventoryAfterReplay.Quantity.Should().Be(97m);
        invoiceCount.Should().Be(1);
        batchCount.Should().Be(1);
        stockTransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task Approve_sales_order_with_insufficient_stock_does_not_change_inventory_or_create_invoice()
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
            Note: "Integration insufficient stock order",
            Lines: [new CreateSalesOrderLineRequest(ItemId: 1, Quantity: 101m)]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/sales-orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdOrder.Should().NotBeNull();

        var submitResponse = await client.PostAsync($"/api/v1/sales-orders/{createdOrder!.Id}/submit", null);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var order = await dbContext.SalesOrders.SingleAsync(x => x.Id == createdOrder.Id);
        var invoiceExists = await dbContext.Invoices.AnyAsync(x => x.SalesOrderId == createdOrder.Id);
        var batchExists = await dbContext.Batches.AnyAsync(x => x.RefType == "SalesOrder" && x.RefId == createdOrder.Id);
        var stockTransactionCount = await dbContext.StockTransactions.CountAsync();
        var outboxCount = await dbContext.OutboxMessages.CountAsync();

        inventory.Quantity.Should().Be(100m);
        inventory.ReservedQuantity.Should().Be(0m);
        order.Status.Should().Be(SalesOrderStatus.Draft);
        invoiceExists.Should().BeFalse();
        batchExists.Should().BeFalse();
        stockTransactionCount.Should().Be(1);
        outboxCount.Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_submit_requests_do_not_overreserve_inventory()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var firstOrder = await CreateDraftOrderAsync(client, 80m, "Concurrent order 1");
        var secondOrder = await CreateDraftOrderAsync(client, 80m, "Concurrent order 2");

        var submitResponses = await Task.WhenAll(
            client.PostAsync($"/api/v1/sales-orders/{firstOrder.Id}/submit", null),
            client.PostAsync($"/api/v1/sales-orders/{secondOrder.Id}/submit", null));

        submitResponses.Count(x => x.IsSuccessStatusCode).Should().Be(1);
        submitResponses.Count(x => x.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);

        using var scope = _factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var submittedOrderCount = await dbContext.SalesOrders.CountAsync(x =>
            (x.Id == firstOrder.Id || x.Id == secondOrder.Id) &&
            x.Status == SalesOrderStatus.Submitted);
        var draftOrderCount = await dbContext.SalesOrders.CountAsync(x =>
            (x.Id == firstOrder.Id || x.Id == secondOrder.Id) &&
            x.Status == SalesOrderStatus.Draft);
        var outboundTransactions = await dbContext.StockTransactions
            .Where(x => x.TransType == StockTransactionType.Out)
            .ToListAsync();

        inventory.Quantity.Should().Be(100m);
        inventory.ReservedQuantity.Should().Be(80m);
        submittedOrderCount.Should().Be(1);
        draftOrderCount.Should().Be(1);
        outboundTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_submitted_sales_order_releases_reserved_inventory()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var order = await CreateSubmittedOrderAsync(client, 25m, "Cancel reservation order");

        using var submitScope = _factory!.Services.CreateScope();
        var submitDbContext = submitScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reservedInventory = await submitDbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        reservedInventory.Quantity.Should().Be(100m);
        reservedInventory.ReservedQuantity.Should().Be(25m);

        var cancelResponse = await client.PostAsync($"/api/v1/sales-orders/{order.Id}/cancel", null);
        var cancelledOrder = await cancelResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        var cancelBody = await cancelResponse.Content.ReadAsStringAsync();

        cancelResponse.IsSuccessStatusCode.Should().BeTrue(cancelBody);
        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(SalesOrderStatus.Cancelled);

        using var cancelScope = _factory.Services.CreateScope();
        var cancelDbContext = cancelScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await cancelDbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var stockTransactionCount = await cancelDbContext.StockTransactions.CountAsync();

        inventory.Quantity.Should().Be(100m);
        inventory.ReservedQuantity.Should().Be(0m);
        stockTransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task Authenticated_admin_can_create_inbound_batch_and_view_stock_transactions()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var request = new CreateInventoryBatchRequest(
            SiteId: 1,
            Lines: [new CreateInventoryBatchLineRequest(ItemId: 1, Quantity: 5m)]);

        var response = await client.PostAsJsonAsync("/api/v1/inventory-batches/inbound", request);
        var batchResponse = await response.Content.ReadFromJsonAsync<InventoryBatchResponse>();
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        batchResponse.Should().NotBeNull();
        batchResponse!.BatchNo.Should().StartWith("IN-");
        batchResponse.Type.Should().Be(BatchType.In);
        batchResponse.Status.Should().Be(BatchStatus.Approved);
        batchResponse.Lines.Should().ContainSingle();
        batchResponse.Lines[0].ItemId.Should().Be(1);
        batchResponse.Lines[0].Quantity.Should().Be(5m);

        using var scope = _factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        var stockTransaction = await dbContext.StockTransactions.SingleAsync(x => x.RefType == "Batch" && x.RefId == batchResponse.Id);

        inventory.Quantity.Should().Be(105m);
        stockTransaction.TransType.Should().Be(StockTransactionType.In);
        stockTransaction.Quantity.Should().Be(5m);
        stockTransaction.BalanceAfter.Should().Be(105m);

        var historyResponse = await client.GetAsync("/api/v1/inventories/site/1/item/1/transactions");
        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResult<StockTransactionResponse>>();

        historyResponse.IsSuccessStatusCode.Should().BeTrue();
        history.Should().NotBeNull();
        history!.Items.Should().HaveCount(2);
        history.Items[0].Quantity.Should().Be(5m);
        history.Items[0].BalanceAfter.Should().Be(105m);

        var reconciliationResponse = await client.GetAsync("/api/v1/inventories/reconciliation");
        var differences = await reconciliationResponse.Content.ReadFromJsonAsync<IReadOnlyList<InventoryReconciliationResponse>>();

        reconciliationResponse.IsSuccessStatusCode.Should().BeTrue();
        differences.Should().NotBeNull();
        differences.Should().BeEmpty();
    }

    [Fact]
    public async Task Inventory_reconciliation_reports_quantity_mismatch()
    {
        var client = CreateClient();
        var login = await LoginAsync(client);

        using var scope = _factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await dbContext.Inventories.SingleAsync(x => x.SiteId == 1 && x.ItemId == 1);
        inventory.Quantity = 99m;
        await dbContext.SaveChangesAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync("/api/v1/inventories/reconciliation");
        var differences = await response.Content.ReadFromJsonAsync<IReadOnlyList<InventoryReconciliationResponse>>();

        response.IsSuccessStatusCode.Should().BeTrue();
        differences.Should().NotBeNull();
        differences.Should().ContainSingle();
        differences![0].InventoryQuantity.Should().Be(99m);
        differences[0].LedgerQuantity.Should().Be(100m);
        differences[0].Difference.Should().Be(-1m);
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

    private static async Task<SalesOrderResponse> CreateDraftOrderAsync(HttpClient client, decimal quantity, string note)
    {
        var request = new CreateSalesOrderRequest(
            CompanyId: 1,
            CustomerId: 1,
            SalesPersonId: 1,
            SiteId: 1,
            OrderDate: null,
            Note: note,
            Lines: [new CreateSalesOrderLineRequest(ItemId: 1, Quantity: quantity)]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/sales-orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdOrder.Should().NotBeNull();

        return createdOrder!;
    }

    private static async Task<SalesOrderResponse> CreateSubmittedOrderAsync(HttpClient client, decimal quantity, string note)
    {
        var createdOrder = await CreateDraftOrderAsync(client, quantity, note);

        var submitResponse = await client.PostAsync($"/api/v1/sales-orders/{createdOrder!.Id}/submit", null);
        submitResponse.IsSuccessStatusCode.Should().BeTrue();

        return createdOrder;
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
                    ["BackgroundJobs:Enabled"] = "false",
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
