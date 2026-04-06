namespace TestContainersDemo.Api.Interfaces;

public interface IDaprStateService
{
    Task SaveStateAsync<T>(string key, T value);
    Task<T?> GetStateAsync<T>(string key);
    Task DeleteStateAsync(string key);
    Task PublishEventAsync<T>(string topicName, T data);
}
