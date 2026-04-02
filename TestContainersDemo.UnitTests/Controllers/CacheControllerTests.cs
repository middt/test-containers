using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TestContainersDemo.Api.Controllers;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.UnitTests.Controllers;

public class CacheControllerTests
{
    private readonly Mock<IRedisService> _redisService;
    private readonly CacheController _controller;

    public CacheControllerTests()
    {
        _redisService = new Mock<IRedisService>();
        var logger = new Mock<ILogger<CacheController>>();
        _controller = new CacheController(_redisService.Object, logger.Object);
    }

    [Fact]
    public async Task Set_ReturnsOk_WhenValueSetSuccessfully()
    {
        _redisService.Setup(r => r.SetAsync("key1", "value1", null)).ReturnsAsync(true);

        var result = await _controller.Set("key1", new CacheRequest("value1"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Set_ReturnsBadRequest_WhenSetFails()
    {
        _redisService.Setup(r => r.SetAsync("key1", "value1", null)).ReturnsAsync(false);

        var result = await _controller.Set("key1", new CacheRequest("value1"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Set_Returns500_WhenExceptionThrown()
    {
        _redisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("Redis down"));

        var result = await _controller.Set("key1", new CacheRequest("value1"));

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenKeyExists()
    {
        _redisService.Setup(r => r.GetAsync("key1")).ReturnsAsync("value1");

        var result = await _controller.Get("key1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenKeyMissing()
    {
        _redisService.Setup(r => r.GetAsync("missing")).ReturnsAsync((string?)null);

        var result = await _controller.Get("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenKeyDeleted()
    {
        _redisService.Setup(r => r.DeleteAsync("key1")).ReturnsAsync(true);

        var result = await _controller.Delete("key1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenKeyMissing()
    {
        _redisService.Setup(r => r.DeleteAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Delete("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Exists_ReturnsOk_WithExistsFlag()
    {
        _redisService.Setup(r => r.ExistsAsync("key1")).ReturnsAsync(true);

        var result = await _controller.Exists("key1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Increment_ReturnsOk_WithNewValue()
    {
        _redisService.Setup(r => r.IncrementAsync("counter")).ReturnsAsync(5L);

        var result = await _controller.Increment("counter");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Decrement_ReturnsOk_WithNewValue()
    {
        _redisService.Setup(r => r.DecrementAsync("counter")).ReturnsAsync(3L);

        var result = await _controller.Decrement("counter");

        Assert.IsType<OkObjectResult>(result);
    }
}
