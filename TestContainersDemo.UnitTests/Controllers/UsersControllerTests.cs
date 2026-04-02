using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TestContainersDemo.Api.Controllers;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Models;

namespace TestContainersDemo.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_userService.Object, logger.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithUserList()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "alice@test.com" },
            new() { Id = 2, Name = "Bob", Email = "bob@test.com" }
        };
        _userService.Setup(s => s.GetUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<User>>(okResult.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetUsers_Returns500_WhenServiceThrows()
    {
        _userService.Setup(s => s.GetUsersAsync()).ThrowsAsync(new Exception("Service down"));

        var result = await _controller.GetUsers();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetUser_ReturnsOk_WhenUserExists()
    {
        var user = new User { Id = 1, Name = "Alice", Email = "alice@test.com" };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        var result = await _controller.GetUser(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<User>(okResult.Value);
        Assert.Equal("Alice", returned.Name);
    }

    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenUserMissing()
    {
        _userService.Setup(s => s.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _controller.GetUser(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WithValidRequest()
    {
        var request = new CreateUserRequest { Name = "Alice", Email = "alice@test.com" };
        var created = new User { Id = 1, Name = "Alice", Email = "alice@test.com" };
        _userService.Setup(s => s.CreateUserAsync(request)).ReturnsAsync(created);

        var result = await _controller.CreateUser(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenNameEmpty()
    {
        var request = new CreateUserRequest { Name = "", Email = "alice@test.com" };

        var result = await _controller.CreateUser(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailEmpty()
    {
        var request = new CreateUserRequest { Name = "Alice", Email = "" };

        var result = await _controller.CreateUser(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_Returns500_WhenServiceThrows()
    {
        var request = new CreateUserRequest { Name = "Alice", Email = "alice@test.com" };
        _userService.Setup(s => s.CreateUserAsync(request)).ThrowsAsync(new Exception("fail"));

        var result = await _controller.CreateUser(request);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
