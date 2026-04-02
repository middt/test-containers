using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TestContainersDemo.Api.Controllers;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Models;

namespace TestContainersDemo.UnitTests.Controllers;

public class DaprControllerTests
{
    private readonly Mock<IDaprStateService> _stateService;
    private readonly DaprController _controller;

    public DaprControllerTests()
    {
        _stateService = new Mock<IDaprStateService>();
        var logger = new Mock<ILogger<DaprController>>();
        _controller = new DaprController(_stateService.Object, logger.Object);
    }

    [Fact]
    public async Task SaveState_ReturnsOk()
    {
        _stateService.Setup(s => s.SaveStateAsync("k1", It.IsAny<object>())).Returns(Task.CompletedTask);

        var result = await _controller.SaveState("k1", new { Value = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SaveState_Returns500_OnException()
    {
        _stateService.Setup(s => s.SaveStateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Dapr down"));

        var result = await _controller.SaveState("k1", new { });

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetState_ReturnsOk_WhenKeyExists()
    {
        _stateService.Setup(s => s.GetStateAsync<object>("k1"))
            .ReturnsAsync(new { Name = "Alice" });

        var result = await _controller.GetState("k1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetState_ReturnsNotFound_WhenKeyMissing()
    {
        _stateService.Setup(s => s.GetStateAsync<object>("missing")).ReturnsAsync((object?)null);

        var result = await _controller.GetState("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteState_ReturnsOk()
    {
        _stateService.Setup(s => s.DeleteStateAsync("k1")).Returns(Task.CompletedTask);

        var result = await _controller.DeleteState("k1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteState_Returns500_OnException()
    {
        _stateService.Setup(s => s.DeleteStateAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteState("k1");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task PublishOrder_ReturnsAccepted()
    {
        var order = new OrderEvent { OrderId = "ORD-1", Product = "Widget", Quantity = 1, Price = 9.99m };
        _stateService.Setup(s => s.PublishEventAsync("orders", It.IsAny<OrderEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.PublishOrder(order);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task PublishOrder_ReturnsBadRequest_WhenOrderIdEmpty()
    {
        var order = new OrderEvent { OrderId = "", Product = "Widget" };

        var result = await _controller.PublishOrder(order);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PublishOrder_ReturnsBadRequest_WhenProductEmpty()
    {
        var order = new OrderEvent { OrderId = "ORD-1", Product = "" };

        var result = await _controller.PublishOrder(order);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PublishOrder_Returns500_OnException()
    {
        var order = new OrderEvent { OrderId = "ORD-1", Product = "Widget" };
        _stateService.Setup(s => s.PublishEventAsync(It.IsAny<string>(), It.IsAny<OrderEvent>()))
            .ThrowsAsync(new Exception("pubsub fail"));

        var result = await _controller.PublishOrder(order);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
