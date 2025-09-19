using Microsoft.AspNetCore.Mvc;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(IRedisService redisService, ILogger<CacheController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    [HttpPost("{key}")]
    public async Task<IActionResult> Set(string key, [FromBody] CacheRequest request)
    {
        try
        {
            var expiry = request.ExpiryMinutes.HasValue ? (TimeSpan?)TimeSpan.FromMinutes(request.ExpiryMinutes.Value) : null;
            var success = await _redisService.SetAsync(key, request.Value, expiry);
            
            if (success)
            {
                _logger.LogInformation("Successfully set cache key: {Key}", key);
                return Ok(new { Message = "Value set successfully", Key = key });
            }
            
            return BadRequest("Failed to set value");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        try
        {
            var value = await _redisService.GetAsync(key);
            
            if (value != null)
            {
                _logger.LogInformation("Successfully retrieved cache key: {Key}", key);
                return Ok(new { Key = key, Value = value });
            }
            
            return NotFound($"Key '{key}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        try
        {
            var success = await _redisService.DeleteAsync(key);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted cache key: {Key}", key);
                return Ok(new { Message = "Key deleted successfully", Key = key });
            }
            
            return NotFound($"Key '{key}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache key: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{key}/exists")]
    public async Task<IActionResult> Exists(string key)
    {
        try
        {
            var exists = await _redisService.ExistsAsync(key);
            return Ok(new { Key = key, Exists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{key}/increment")]
    public async Task<IActionResult> Increment(string key)
    {
        try
        {
            var value = await _redisService.IncrementAsync(key);
            _logger.LogInformation("Successfully incremented cache key: {Key}, new value: {Value}", key, value);
            return Ok(new { Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing cache key: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{key}/decrement")]
    public async Task<IActionResult> Decrement(string key)
    {
        try
        {
            var value = await _redisService.DecrementAsync(key);
            _logger.LogInformation("Successfully decremented cache key: {Key}, new value: {Value}", key, value);
            return Ok(new { Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing cache key: {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }
}

public record CacheRequest(string Value, int? ExpiryMinutes = null);
