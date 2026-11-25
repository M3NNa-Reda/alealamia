using System.Net;
using alealamia.Application.DTOs.Auth;
using alealamia.Domain.Constants;
using alealamia.Integration.Tests.Support;

namespace alealamia.Integration.Tests.Api;

/// <summary>Registration, login, token handling and role checks of the real API.</summary>
[Collection(ApiCollection.Name)]
public class AuthEndToEndTests
{
    private readonly ApiFixture _api;

    public AuthEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Register_ReturnsAToken_WithTheChosenRole()
    {
        foreach (var role in new[] { AppRoles.Client, AppRoles.Freelancer })
        {
            using var client = _api.CreateAnonymousClient();
            var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@e2e.test";

            var response = await client.PostJsonAsync("api/Auth/register", NewRegistration(email, role));
            await response.ExpectAsync(HttpStatusCode.OK);

            var result = await response.ReadAsync<AuthResultDto>();
            Assert.True(result.Succeeded);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));
            Assert.Contains(role, result.Data.User.Roles);
            Assert.Equal(email, result.Data.User.Email, ignoreCase: true);
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Register_CannotCreateAnAdmin_OrAnUnknownRole()
    {
        using var client = _api.CreateAnonymousClient();

        foreach (var role in new[] { AppRoles.Admin, "SuperUser", "" })
        {
            var response = await client.PostJsonAsync("api/Auth/register", NewRegistration($"x.{Guid.NewGuid():N}@e2e.test", role));
            await response.ExpectAsync(HttpStatusCode.BadRequest);
            Assert.False((await response.ReadAsync<AuthResultDto>()).Succeeded);
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Register_RejectsADuplicateEmail_AndAWeakPassword()
    {
        using var client = _api.CreateAnonymousClient();
        var email = $"dup.{Guid.NewGuid():N}@e2e.test";

        await (await client.PostJsonAsync("api/Auth/register", NewRegistration(email, AppRoles.Freelancer)))
            .ExpectAsync(HttpStatusCode.OK);
        await (await client.PostJsonAsync("api/Auth/register", NewRegistration(email, AppRoles.Freelancer)))
            .ExpectAsync(HttpStatusCode.BadRequest);

        foreach (var weak in new[] { "short1A", "alllowercase1", "ALLUPPERCASE1", "NoDigitsHere" })
        {
            var registration = NewRegistration($"weak.{Guid.NewGuid():N}@e2e.test", AppRoles.Client);
            registration.Password = weak;
            await (await client.PostJsonAsync("api/Auth/register", registration)).ExpectAsync(HttpStatusCode.BadRequest);
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Login_FailsWithTheSameMessage_ForAWrongPasswordAndAnUnknownEmail()
    {
        var (_, _, email) = await _api.RegisterAsync(AppRoles.Client);
        using var client = _api.CreateAnonymousClient();

        var wrongPassword = await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = email, Password = "Wrong-Pass@123" });
        var unknownEmail = await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = $"nobody.{Guid.NewGuid():N}@e2e.test", Password = "Wrong-Pass@123" });

        await wrongPassword.ExpectAsync(HttpStatusCode.Unauthorized);
        await unknownEmail.ExpectAsync(HttpStatusCode.Unauthorized);

        var first = await wrongPassword.ReadAsync<AuthResultDto>();
        var second = await unknownEmail.ReadAsync<AuthResultDto>();
        Assert.Equal(first.Errors, second.Errors);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Login_WithTheRightPassword_ReturnsATokenThatTheApiAccepts()
    {
        var (_, _, email) = await _api.RegisterAsync(AppRoles.Freelancer);

        using var signedIn = await _api.LoginAsync(email, ApiFixture.UserPassword);

        // A freelancer without a completed profile is authenticated (404, not 401) on a freelancer endpoint.
        var response = await signedIn.GetAsync("api/FreelancerProfile/profile");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task ProtectedEndpoints_RejectMissingAndTamperedTokens()
    {
        using var anonymous = _api.CreateAnonymousClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/FreelancerProfile/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/admin/tasks")).StatusCode);

        var (_, _, email) = await _api.RegisterAsync(AppRoles.Freelancer);
        var login = await anonymous.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = email, Password = ApiFixture.UserPassword });
        var token = (await login.ReadAsync<AuthResultDto>()).Data!.Token;

        var tampered = token[..^3] + (token.EndsWith("AAA") ? "BBB" : "AAA");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/FreelancerProfile/profile");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tampered);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(request)).StatusCode);

        using var garbage = new HttpRequestMessage(HttpMethod.Get, "api/FreelancerProfile/profile");
        garbage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(garbage)).StatusCode);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task RoleBoundaries_AreEnforcedAcrossProfileAndAdminEndpoints()
    {
        var (client, _, _) = await _api.RegisterAsync(AppRoles.Client);
        var (freelancer, _, _) = await _api.RegisterAsync(AppRoles.Freelancer);

        // A client cannot use freelancer or admin endpoints.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/FreelancerProfile/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/admin/freelancers/pending")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/Track/admin")).StatusCode);

        // A freelancer cannot use client or admin endpoints.
        Assert.Equal(HttpStatusCode.Forbidden, (await freelancer.GetAsync("api/client-profile/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await freelancer.GetAsync("api/admin/freelancers/pending")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await freelancer.PostJsonAsync("api/Track", new { name = "Hack", description = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await freelancer.PostJsonAsync("api/Skill", new { name = "Hack", description = "x" })).StatusCode);

        // The admin can reach the admin endpoints.
        Assert.Equal(HttpStatusCode.OK, (await _api.Admin.GetAsync("api/admin/freelancers/pending")).StatusCode);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task FreelancerApproval_CanHappenOnlyOnce()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world, approve: false);

        await (await _api.Admin.PatchEmptyAsync($"api/admin/freelancers/{freelancer.ProfileId}/approve")).ExpectAsync(HttpStatusCode.OK);
        await (await _api.Admin.PatchEmptyAsync($"api/admin/freelancers/{freelancer.ProfileId}/approve")).ExpectAsync(HttpStatusCode.BadRequest);
        await (await _api.Admin.PatchEmptyAsync($"api/admin/freelancers/{freelancer.ProfileId}/reject")).ExpectAsync(HttpStatusCode.BadRequest);
        await (await _api.Admin.PatchEmptyAsync("api/admin/freelancers/999999/approve")).ExpectAsync(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Known security gap: login uses UserManager.CheckPasswordAsync, which never counts failed attempts, so the lockout configured in DependencyInjection is not enforced. Enable this test together with the lockout fix.")]
    public async System.Threading.Tasks.Task Login_LocksTheAccount_AfterFiveWrongPasswords()
    {
        var (_, _, email) = await _api.RegisterAsync(AppRoles.Client);
        using var client = _api.CreateAnonymousClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = email, Password = "Wrong-Pass@123" });
        }

        // The correct password must now be refused because the account is locked.
        var response = await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = email, Password = ApiFixture.UserPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static RegisterRequestDto NewRegistration(string email, string role) => new()
    {
        FirstName = "E2E",
        LastName = "User",
        Email = email,
        Password = ApiFixture.UserPassword,
        Role = role
    };
}
