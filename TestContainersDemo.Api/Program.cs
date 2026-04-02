using Dapr.Client;
using StackExchange.Redis;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);






// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // Allow reconnection attempts
    return ConnectionMultiplexer.Connect(configuration);
});

// Register Redis service
builder.Services.AddScoped<IRedisService, RedisService>();

// Register Dapr client and state service
builder.Services.AddDaprClient();
builder.Services.AddScoped<IDaprStateService, DaprStateService>();

// Register HTTP client and User service
builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    var mockApiUrl = builder.Configuration.GetConnectionString("MockApi") ?? "http://localhost:3000";
    client.BaseAddress = new Uri(mockApiUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make the Program class testable
public partial class Program { }
