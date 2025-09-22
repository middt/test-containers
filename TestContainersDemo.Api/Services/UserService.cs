using System.Text.Json;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Models;

namespace TestContainersDemo.Api.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        // Simulate users list using httpbin.org as demonstration
        // In a real scenario, this would call the actual mock API
        var response = await _httpClient.GetAsync("get");
        response.EnsureSuccessStatusCode();
        
        // Return mock data for demo purposes
        return new[]
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
        };
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            // Simulate API call to httpbin.org for demo
            var response = await _httpClient.GetAsync($"get?id={id}");
            response.EnsureSuccessStatusCode();
            
            // Return mock user for demo purposes  
            if (id == 1)
                return new User { Id = 1, Name = "User 1", Email = "user1@example.com" };
            if (id == 2)
                return new User { Id = 2, Name = "User 2", Email = "user2@example.com" };
                
            return null; // Simulate user not found
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        // Simulate POST to httpbin.org for demo
        var response = await _httpClient.PostAsync("post", content);
        response.EnsureSuccessStatusCode();
        
        // Return mock created user for demo purposes
        var random = new Random();
        return new User 
        { 
            Id = random.Next(100, 999), 
            Name = request.Name, 
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
    }
}
