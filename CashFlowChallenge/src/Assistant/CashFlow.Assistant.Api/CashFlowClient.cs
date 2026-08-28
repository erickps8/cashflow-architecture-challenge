using System.Net.Http.Headers;
using System.Text.Json;

namespace CashFlow.Assistant.Api;

public sealed class CashFlowClient(IHttpClientFactory factory, IHttpContextAccessor http)
{
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient("CashFlowLaunch");
        var authorization = http.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var header))
            client.DefaultRequestHeaders.Authorization = header;
        return client;
    }

    public async Task<JsonElement> GetAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return (await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)).RootElement.Clone();
    }
}
