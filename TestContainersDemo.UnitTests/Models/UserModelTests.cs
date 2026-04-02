using TestContainersDemo.Api.Models;

namespace TestContainersDemo.UnitTests.Models;

public class UserModelTests
{
    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        var user = new User();

        Assert.Equal(0, user.Id);
        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(string.Empty, user.Email);
        Assert.Null(user.CreatedAt);
    }

    [Fact]
    public void User_Properties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = 42,
            Name = "Alice",
            Email = "alice@test.com",
            CreatedAt = now
        };

        Assert.Equal(42, user.Id);
        Assert.Equal("Alice", user.Name);
        Assert.Equal("alice@test.com", user.Email);
        Assert.Equal(now, user.CreatedAt);
    }

    [Fact]
    public void CreateUserRequest_DefaultValues_AreCorrect()
    {
        var request = new CreateUserRequest();

        Assert.Equal(string.Empty, request.Name);
        Assert.Equal(string.Empty, request.Email);
    }
}
