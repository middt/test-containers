namespace TestContainersDemo.IntegrationTests.Dapr.Infrastructure;

public abstract class DaprTestBase : IClassFixture<DaprWebApplicationFactory>
{
    protected readonly DaprWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;

    protected DaprTestBase(DaprWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
    }
}
