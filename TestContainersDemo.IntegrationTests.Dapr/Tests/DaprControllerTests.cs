using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TestContainersDemo.Api.Models;
using TestContainersDemo.IntegrationTests.Dapr.Infrastructure;

namespace TestContainersDemo.IntegrationTests.Dapr.Tests;

public class DaprControllerTests : DaprTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DaprControllerTests(DaprWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SaveAndGetState_ShouldRoundTrip()
    {
        var key = $"test-{Guid.NewGuid():N}";
        var payload = new { Name = "Alice", Score = 42 };

        var saveResponse = await HttpClient.PostAsJsonAsync($"/api/dapr/state/{key}", payload);
        saveResponse.EnsureSuccessStatusCode();

        var getResponse = await HttpClient.GetAsync($"/api/dapr/state/{key}");
        getResponse.EnsureSuccessStatusCode();

        var body = await getResponse.Content.ReadAsStringAsync();
        var entry = JsonSerializer.Deserialize<StateEntry<JsonElement>>(body, JsonOptions);

        Assert.NotNull(entry);
        Assert.Equal(key, entry.Key);
        Assert.Equal("Alice", entry.Value.GetProperty("name").GetString());
        Assert.Equal(42, entry.Value.GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task GetState_NonExistentKey_ReturnsNotFound()
    {
        var response = await HttpClient.GetAsync($"/api/dapr/state/nonexistent-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteState_ShouldRemoveKey()
    {
        var key = $"delete-{Guid.NewGuid():N}";
        await HttpClient.PostAsJsonAsync($"/api/dapr/state/{key}", new { Value = "temp" });

        var deleteResponse = await HttpClient.DeleteAsync($"/api/dapr/state/{key}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await HttpClient.GetAsync($"/api/dapr/state/{key}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SaveState_OverwritesExistingKey()
    {
        var key = $"overwrite-{Guid.NewGuid():N}";

        await HttpClient.PostAsJsonAsync($"/api/dapr/state/{key}", new { Version = 1 });
        await HttpClient.PostAsJsonAsync($"/api/dapr/state/{key}", new { Version = 2 });

        var getResponse = await HttpClient.GetAsync($"/api/dapr/state/{key}");
        getResponse.EnsureSuccessStatusCode();

        var body = await getResponse.Content.ReadAsStringAsync();
        var entry = JsonSerializer.Deserialize<StateEntry<JsonElement>>(body, JsonOptions);

        Assert.Equal(2, entry!.Value.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task PublishOrder_ReturnsAccepted()
    {
        var order = new OrderEvent
        {
            OrderId = $"ORD-{Guid.NewGuid():N}",
            Product = "Widget",
            Quantity = 5,
            Price = 19.99m
        };

        var response = await HttpClient.PostAsJsonAsync("/api/dapr/publish/orders", order);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PublishOrder_EmptyOrderId_ReturnsBadRequest()
    {
        var order = new OrderEvent { OrderId = "", Product = "Widget", Quantity = 1, Price = 9.99m };

        var response = await HttpClient.PostAsJsonAsync("/api/dapr/publish/orders", order);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublishOrder_EmptyProduct_ReturnsBadRequest()
    {
        var order = new OrderEvent { OrderId = "ORD-1", Product = "", Quantity = 1, Price = 9.99m };

        var response = await HttpClient.PostAsJsonAsync("/api/dapr/publish/orders", order);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
