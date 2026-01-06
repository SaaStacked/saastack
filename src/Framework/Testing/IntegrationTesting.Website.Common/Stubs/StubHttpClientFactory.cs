namespace IntegrationTesting.Website.Common.Stubs;

/// <summary>
///     We need to stub this <see cref="IHttpClientFactory" /> so that we set the BaseUrl
///     for testing the WebsiteHost against the Backend API.
/// </summary>
public class StubHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        client.BaseAddress = new Uri("https://localhost:5001");
        return client;
    }
}