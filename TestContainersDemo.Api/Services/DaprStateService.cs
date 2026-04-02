using Dapr.Client;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.Api.Services;

public class DaprStateService : IDaprStateService
{
    private readonly DaprClient _daprClient;
    private const string StateStoreName = "statestore";
    private const string PubSubName = "pubsub";

    public DaprStateService(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task SaveStateAsync<T>(string key, T value)
    {
        await _daprClient.SaveStateAsync(StateStoreName, key, value);
    }

    public async Task<T?> GetStateAsync<T>(string key)
    {
        return await _daprClient.GetStateAsync<T>(StateStoreName, key);
    }

    public async Task DeleteStateAsync(string key)
    {
        await _daprClient.DeleteStateAsync(StateStoreName, key);
    }

    public async Task PublishEventAsync<T>(string topicName, T data)
    {
        await _daprClient.PublishEventAsync(PubSubName, topicName, data);
    }
}
