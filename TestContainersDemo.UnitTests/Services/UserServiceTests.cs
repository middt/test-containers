using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TestContainersDemo.Api.Models;
using TestContainersDemo.Api.Services;

namespace TestContainersDemo.UnitTests.Services;

public class UserServiceTests
{
    private readonly UserService _service;
    private readonly Mock<HttpMessageHandler> _httpHandler;

    public UserServiceTests()
    {
        _httpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:3000/")
        };
        _service = new UserService(httpClient);
    }

    private void SetupResponse(HttpStatusCode status, object? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body != null)
            response.Content = new StringContent(JsonSerializer.Serialize(body));

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsUsers()
    {
        var users = new[] { new User { Id = 1, Name = "Alice", Email = "a@b.com" } };
        SetupResponse(HttpStatusCode.OK, users);

        var result = await _service.GetUsersAsync();

        Assert.Single(result);
        Assert.Equal("Alice", result.First().Name);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsEmpty_WhenNoUsers()
    {
        SetupResponse(HttpStatusCode.OK, Array.Empty<User>());

        var result = await _service.GetUsersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersAsync_Throws_On500()
    {
        SetupResponse(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetUsersAsync());
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenFound()
    {
        var user = new User { Id = 1, Name = "Alice", Email = "a@b.com" };
        SetupResponse(HttpStatusCode.OK, user);

        var result = await _service.GetUserByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_OnHttpError()
    {
        SetupResponse(HttpStatusCode.NotFound);

        var result = await _service.GetUserByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsCreatedUser()
    {
        var created = new User { Id = 10, Name = "Bob", Email = "bob@test.com" };
        SetupResponse(HttpStatusCode.Created, created);

        var result = await _service.CreateUserAsync(
            new CreateUserRequest { Name = "Bob", Email = "bob@test.com" });

        Assert.Equal(10, result.Id);
        Assert.Equal("Bob", result.Name);
    }

    [Fact]
    public async Task CreateUserAsync_Throws_OnServerError()
    {
        SetupResponse(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.CreateUserAsync(new CreateUserRequest { Name = "X", Email = "x@y.com" }));
    }
}
