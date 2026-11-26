using System.Net;
using alealamia.Application.DTOs.Auth;
using alealamia.Integration.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace alealamia.Integration.Tests.Api;

/// <summary>Security headers, CORS and HSTS of the real HTTP pipeline.</summary>
[Collection(ApiCollection.Name)]
public class SecurityEndToEndTests
{
    private const string Allowed = "http://localhost:5173";
    private const string Foreign = "https://evil.example";

    private readonly ApiFixture _api;

    public SecurityEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task EveryResponse_CarriesTheSecurityHeaders_EvenErrors()
    {
        using var client = _api.CreateAnonymousClient();

        var unauthorized = await client.GetAsync("api/FreelancerProfile/profile");
        var badLogin = await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = "nobody@e2e.test", Password = "x" });
        var notFound = await client.GetAsync("api/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        foreach (var response in new[] { unauthorized, badLogin, notFound })
        {
            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
            Assert.Equal("DENY", Header(response, "X-Frame-Options"));
            Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
            Assert.Contains("frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
            Assert.False(string.IsNullOrEmpty(Header(response, "Permissions-Policy")));
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task ApiResponses_AreNotCached()
    {
        using var client = _api.CreateAnonymousClient();

        var response = await client.GetAsync("api/admin/tasks");

        Assert.Equal("no-store", Header(response, "Cache-Control"));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Cors_AllowsTheConfiguredOrigin_AndNoOtherOrigin()
    {
        using var factory = _api.CreateFactory(("Cors:AllowedOrigins:0", Allowed));
        using var client = factory.CreateClient();

        var allowed = await client.SendAsync(WithOrigin(HttpMethod.Get, "api/Track/active", Allowed));
        var foreign = await client.SendAsync(WithOrigin(HttpMethod.Get, "api/Track/active", Foreign));

        Assert.Equal(Allowed, Header(allowed, "Access-Control-Allow-Origin"));
        Assert.Equal(string.Empty, Header(foreign, "Access-Control-Allow-Origin"));
        Assert.Equal(string.Empty, Header(allowed, "Access-Control-Allow-Credentials"));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Cors_AnswersThePreflight_ForTheConfiguredOriginOnly()
    {
        using var factory = _api.CreateFactory(("Cors:AllowedOrigins:0", Allowed));
        using var client = factory.CreateClient();

        var allowed = await client.SendAsync(Preflight("api/Auth/login", Allowed));
        var foreign = await client.SendAsync(Preflight("api/Auth/login", Foreign));

        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.Equal(Allowed, Header(allowed, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Header(allowed, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", Header(allowed, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(string.Empty, Header(foreign, "Access-Control-Allow-Origin"));
    }

    [SqlServerFact]
    public void Cors_RejectsAWildcardOrMalformedOrigin_AtStartup()
    {
        foreach (var invalid in new[] { "*", "localhost:5173", "https://app.example.com/path" })
        {
            using var factory = _api.CreateFactory(("Cors:AllowedOrigins:0", invalid));
            Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Production_AllowsNoOriginByDefault_AndSendsHsts()
    {
        using var factory = _api.CreateFactory().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.test")
        });

        var response = await client.SendAsync(WithOrigin(HttpMethod.Get, "api/Track/active", Allowed));

        Assert.Equal(string.Empty, Header(response, "Access-Control-Allow-Origin"));

        var hsts = Header(response, "Strict-Transport-Security");
        Assert.Contains("max-age=31536000", hsts);
        Assert.Contains("includeSubDomains", hsts);
    }

    private static HttpRequestMessage WithOrigin(HttpMethod method, string url, string origin)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Origin", origin);
        return request;
    }

    private static HttpRequestMessage Preflight(string url, string origin)
    {
        var request = WithOrigin(HttpMethod.Options, url, origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : string.Empty;
}
