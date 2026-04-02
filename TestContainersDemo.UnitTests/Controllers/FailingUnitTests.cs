using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TestContainersDemo.Api.Controllers;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.UnitTests.Controllers;

public class FailingUnitTests
{
    [Fact]
    public async Task Increment_ShouldReturnIncrementedValue()
    {
        var redisService = new Mock<IRedisService>();
        redisService.Setup(r => r.IncrementAsync("counter")).ReturnsAsync(5L);
        var controller = new CacheController(redisService.Object, Mock.Of<ILogger<CacheController>>());

        var result = await controller.Increment("counter");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(5L, okResult.Value?.GetType().GetProperty("Value")?.GetValue(okResult.Value));
    }
}
