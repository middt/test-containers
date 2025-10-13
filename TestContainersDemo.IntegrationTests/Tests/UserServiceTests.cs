using Microsoft.Extensions.DependencyInjection;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Models;
using TestContainersDemo.IntegrationTests.Infrastructure;

namespace TestContainersDemo.IntegrationTests.Tests;

public class UserServiceTests : IntegrationTestBase
{
    public UserServiceTests(TestContainersWebApplicationFactory factory) : base(factory)
    {
    }

    private IUserService GetUserService()
    {
        return Factory.Services.GetRequiredService<IUserService>();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnUsersList()
    {
        // Arrange
        var userService = GetUserService();

        // Act
        var users = await userService.GetUsersAsync();

        // Assert
        Assert.NotNull(users);
        var userList = users.ToList();
        Assert.Equal(2, userList.Count);
        
        var firstUser = userList[0];
        Assert.Equal(1, firstUser.Id);
        Assert.Equal("John Doe", firstUser.Name);
        Assert.Equal("john@example.com", firstUser.Email);

        var secondUser = userList[1];
        Assert.Equal(2, secondUser.Id);
        Assert.Equal("Jane Smith", secondUser.Name);
        Assert.Equal("jane@example.com", secondUser.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var userService = GetUserService();
        const int userId = 1;

        // Act
        var user = await userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
        Assert.Equal($"User {userId}", user.Name);
        Assert.Equal($"user{userId}@example.com", user.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_DifferentId_ShouldReturnCorrectUser()
    {
        // Arrange
        var userService = GetUserService();
        const int userId = 42;

        // Act
        var user = await userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
        Assert.Equal($"User {userId}", user.Name);
        Assert.Equal($"user{userId}@example.com", user.Email);
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ShouldReturnCreatedUser()
    {
        // Arrange
        var userService = GetUserService();
        var createRequest = new CreateUserRequest
        {
            Name = "Integration Test User",
            Email = "integration@test.com"
        };

        // Act
        var createdUser = await userService.CreateUserAsync(createRequest);

        // Assert
        Assert.NotNull(createdUser);
        Assert.True(createdUser.Id > 0);
        Assert.False(string.IsNullOrEmpty(createdUser.Name));  // Mockoon generates random name
        Assert.False(string.IsNullOrEmpty(createdUser.Email)); // Mockoon generates random email
        Assert.NotNull(createdUser.CreatedAt);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldGenerateUniqueIds()
    {
        // Arrange
        var userService = GetUserService();
        var createRequest1 = new CreateUserRequest
        {
            Name = "User One",
            Email = "user1@test.com"
        };
        var createRequest2 = new CreateUserRequest
        {
            Name = "User Two",
            Email = "user2@test.com"
        };

        // Act
        var user1 = await userService.CreateUserAsync(createRequest1);
        var user2 = await userService.CreateUserAsync(createRequest2);

        // Assert
        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotEqual(user1.Id, user2.Id);
        Assert.True(user1.Id > 0);
        Assert.True(user2.Id > 0);
    }

    [Fact]
    public async Task UserService_ShouldHandleMultipleOperations()
    {
        // Arrange
        var userService = GetUserService();

        // Act & Assert - Test that we can perform multiple operations
        var users = await userService.GetUsersAsync();
        Assert.NotNull(users);
        Assert.Equal(2, users.Count());

        var userById = await userService.GetUserByIdAsync(1);
        Assert.NotNull(userById);
        Assert.Equal(1, userById.Id);

        var newUser = await userService.CreateUserAsync(new CreateUserRequest
        {
            Name = "Multi Op User",
            Email = "multiop@test.com"
        });
        Assert.NotNull(newUser);
        Assert.False(string.IsNullOrEmpty(newUser.Name)); // Mockoon generates random name
    }

    [Fact]
    public async Task UserService_HttpClient_ShouldBeConfiguredCorrectly()
    {
        // This test verifies that the HttpClient is properly configured
        // and can communicate with the external API
        
        // Arrange
        var userService = GetUserService();

        // Act
        var users = await userService.GetUsersAsync();

        // Assert
        Assert.NotNull(users);
        // If we get here without exceptions, HttpClient is properly configured
        // and the external API is responding
        Assert.True(users.Any(), "Should have received users from the API");
    }

    [Fact]
    public async Task UserService_JsonDeserialization_ShouldWorkCorrectly()
    {
        // This test verifies that JSON deserialization is working properly
        
        // Arrange
        var userService = GetUserService();

        // Act
        var user = await userService.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(user);
        
        // Verify all properties are properly deserialized
        Assert.True(user.Id > 0, "Id should be properly deserialized");
        Assert.False(string.IsNullOrEmpty(user.Name), "Name should be properly deserialized");
        Assert.False(string.IsNullOrEmpty(user.Email), "Email should be properly deserialized");
        
        // Verify email format (basic check)
        Assert.Contains("@", user.Email);
    }
}
