using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace alealamia.Integration.Tests.Support;

internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object? body)
        => client.PostAsJsonAsync(url, body, Options);

    public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, object? body)
        => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, options: Options)
        });

    public static Task<HttpResponseMessage> PatchEmptyAsync(this HttpClient client, string url)
        => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, url));

    /// <summary>Fails the test with the status code and the response body when the status is not the expected one.</summary>
    public static async Task<HttpResponseMessage> ExpectAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail(
                $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode} " +
                $"for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}. Body: {body}");
        }

        return response;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);
        return value ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");
    }

    public static async Task<T> GetAsAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        await response.ExpectAsync(HttpStatusCode.OK);
        return await response.ReadAsync<T>();
    }

    /// <summary>Reads a list that is either a bare JSON array or wrapped in "data" / "items".</summary>
    public static async Task<List<T>> ReadListAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            foreach (var name in new[] { "data", "items" })
            {
                if (root.TryGetProperty(name, out var inner))
                {
                    root = inner;
                    break;
                }
            }
        }

        return root.Deserialize<List<T>>(Options) ?? new List<T>();
    }
}
