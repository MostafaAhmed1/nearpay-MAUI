using System.Net.Http.Headers;
using System.Text;

namespace NearpayPosMauiDemo.App.Services;

public interface INearpayApiClient
{
    Task<(int StatusCode, string Body)> GetAsync(string baseUrl, string path, string apiKey, CancellationToken ct);
}

public sealed class NearpayApiClient : INearpayApiClient
{
    private readonly HttpClient _http;

    public NearpayApiClient(HttpClient http) => _http = http;

    public async Task<(int StatusCode, string Body)> GetAsync(string baseUrl, string path, string apiKey, CancellationToken ct)
    {
        // NOTE: official docs say "api key is needed in all requests as a header" but don't always name it.
        // We'll try common header names in order.
        var headersToTry = new[] { "api-key", "x-api-key", "Api-Key", "X-API-Key" };

        foreach (var headerName in headersToTry)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, Combine(baseUrl, path));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.TryAddWithoutValidation(headerName, apiKey);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            // If unauthorized, try next header name
            if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                continue;

            return ((int)resp.StatusCode, body);
        }

        // Last attempt result (401/403) with first header for body/debug
        using var lastReq = new HttpRequestMessage(HttpMethod.Get, Combine(baseUrl, path));
        lastReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        lastReq.Headers.TryAddWithoutValidation("api-key", apiKey);
        using var lastResp = await _http.SendAsync(lastReq, ct);
        var lastBody = await lastResp.Content.ReadAsStringAsync(ct);
        return ((int)lastResp.StatusCode, lastBody);
    }

    private static string Combine(string baseUrl, string path)
    {
        baseUrl = baseUrl.TrimEnd('/');
        path = path.StartsWith('/') ? path : "/" + path;
        return baseUrl + path;
    }
}

