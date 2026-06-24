using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

[Trait("Category", "Integration")]
public class SalesApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly Faker Faker = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SalesApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static object BuildCreateSalePayload(string? saleNumber = null, int quantity = 1) => new
    {
        saleNumber = saleNumber ?? Faker.Random.AlphaNumeric(8).ToUpper(),
        saleDate = DateTime.UtcNow,
        customerId = Guid.NewGuid(),
        customerName = Faker.Company.CompanyName(),
        branchId = Guid.NewGuid(),
        branchName = Faker.Address.City(),
        items = new[]
        {
            new
            {
                productId = Guid.NewGuid(),
                productName = Faker.Commerce.ProductName(),
                unitPrice = 100.00m,
                quantity
            }
        }
    };

    [Fact(DisplayName = "POST /api/sales With valid payload Returns 201 Created")]
    public async Task CreateSale_ValidPayload_Returns201()
    {
        var payload = BuildCreateSalePayload();

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "POST /api/sales With empty payload Returns 400 BadRequest")]
    public async Task CreateSale_EmptyPayload_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/sales", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /api/sales Returns 200 OK with paginated list")]
    public async Task ListSales_Returns200WithPaginatedList()
    {
        var response = await _client.GetAsync("/api/sales?_page=1&_size=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact(DisplayName = "GET /api/sales/{id} With existing ID Returns 200 OK")]
    public async Task GetSale_ExistingId_Returns200()
    {
        // Arrange — create a sale first
        var payload = BuildCreateSalePayload();
        var createResponse = await _client.PostAsJsonAsync("/api/sales", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("data").GetProperty("id").GetString();

        // Act
        var response = await _client.GetAsync($"/api/sales/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /api/sales/{id} With non-existing ID Returns 404 NotFound")]
    public async Task GetSale_NonExistingId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /api/sales/{id} With existing ID Returns 200 and cancels sale")]
    public async Task CancelSale_ExistingId_Returns200()
    {
        // Arrange — create a sale
        var payload = BuildCreateSalePayload();
        var createResponse = await _client.PostAsJsonAsync("/api/sales", payload);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("data").GetProperty("id").GetString();

        // Act
        var response = await _client.DeleteAsync($"/api/sales/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "POST /api/sales With 10 items Returns 201 and applies 20% discount")]
    public async Task CreateSale_10Items_Applies20PercentDiscount()
    {
        var payload = BuildCreateSalePayload(quantity: 10);

        var response = await _client.PostAsJsonAsync("/api/sales", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var discount = json.GetProperty("data")
            .GetProperty("items")[0]
            .GetProperty("discount")
            .GetDecimal();

        discount.Should().Be(0.20m);
    }
}
