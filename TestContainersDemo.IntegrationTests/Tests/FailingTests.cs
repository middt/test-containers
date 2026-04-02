using System.Net;
using TestContainersDemo.IntegrationTests.Infrastructure;

namespace TestContainersDemo.IntegrationTests.Tests;

public class FailingTests : IntegrationTestBase
{
    public FailingTests(TestContainersWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetNonExistentEndpoint_ShouldReturnOk_ButWillFail()
    {
        var response = await HttpClient.GetAsync("/api/this-does-not-exist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
