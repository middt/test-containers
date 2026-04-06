using Microsoft.AspNetCore.Mvc;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Models;

namespace TestContainersDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DaprController : ControllerBase
{
    private readonly IDaprStateService _stateService;
    private readonly ILogger<DaprController> _logger;

    public DaprController(IDaprStateService stateService, ILogger<DaprController> logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    [HttpPost("state/{key}")]
    public async Task<IActionResult> SaveState(string key, [FromBody] object value)
    {
        try
        {
            await _stateService.SaveStateAsync(key, value);
            _logger.LogInformation("State saved for key: {Key}", key);
            return Ok(new { Message = "State saved", Key = key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving state for key: {Key}", key);
            return StatusCode(500, "Failed to save state");
        }
    }

    [HttpGet("state/{key}")]
    public async Task<IActionResult> GetState(string key)
    {
        try
        {
            var value = await _stateService.GetStateAsync<object>(key);
            if (value == null)
                return NotFound(new { Message = "Key not found", Key = key });

            _logger.LogInformation("State retrieved for key: {Key}", key);
            return Ok(new StateEntry<object> { Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving state for key: {Key}", key);
            return StatusCode(500, "Failed to retrieve state");
        }
    }

    [HttpDelete("state/{key}")]
    public async Task<IActionResult> DeleteState(string key)
    {
        try
        {
            await _stateService.DeleteStateAsync(key);
            _logger.LogInformation("State deleted for key: {Key}", key);
            return Ok(new { Message = "State deleted", Key = key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting state for key: {Key}", key);
            return StatusCode(500, "Failed to delete state");
        }
    }

    [HttpPost("publish/orders")]
    public async Task<IActionResult> PublishOrder([FromBody] OrderEvent order)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(order.OrderId) || string.IsNullOrWhiteSpace(order.Product))
                return BadRequest("OrderId and Product are required");

            await _stateService.PublishEventAsync("orders", order);
            _logger.LogInformation("Order event published: {OrderId}", order.OrderId);
            return Accepted(new { Message = "Order event published", OrderId = order.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing order event: {OrderId}", order.OrderId);
            return StatusCode(500, "Failed to publish event");
        }
    }
}
