using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Tests;

public class AuthServiceLoginTests
{
    private const string Email = "user@test.com";
    private const string Password = "Right-Pass@123";

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtService _jwt = Substitute.For<IJwtService>();
    private readonly AuthService _service;

    public AuthServiceLoginTests()
    {
        _identity.GetUserByEmailAsync(Email).Returns(new ApplicationUserDto
        {
            Id = "u-1",
            Email = Email,
            FullName = "Test User"
        });
        _identity.IsActiveAsync("u-1").Returns(true);
        _identity.GetUserRolesAsync("u-1").Returns(new List<string> { "Client" });
        _jwt.GenerateTokenAsync(Arg.Any<ApplicationUserDto>(), Arg.Any<IList<string>>())
            .Returns(new TokenDto { Token = "jwt-token", Expiration = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc) });

        _service = new AuthService(
            _identity,
            _jwt,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IFreelancerProfileRepository>());
    }

    private static LoginRequestDto Login(string email = Email, string password = Password) =>
        new() { Email = email, Password = password };

    [Fact]
    public async Task Login_ReturnsAToken_WhenTheCredentialsAreValid()
    {
        _identity.CheckCredentialsAsync(Email, Password).Returns(CredentialCheckResult.Success);

        var result = await _service.LoginAsync(Login());

        Assert.True(result.Succeeded);
        Assert.Equal("jwt-token", result.Data!.Token);
        Assert.Equal("u-1", result.Data.User.UserId);
        Assert.Contains("Client", result.Data.User.Roles);
    }

    [Fact]
    public async Task Login_Fails_WhenTheEmailIsUnknown()
    {
        _identity.GetUserByEmailAsync("nobody@test.com").Returns((ApplicationUserDto?)null);

        var result = await _service.LoginAsync(Login("nobody@test.com"));

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { AuthService.InvalidCredentialsMessage }, result.Errors);
        await _identity.DidNotReceiveWithAnyArgs().CheckCredentialsAsync(default!, default!);
    }

    [Fact]
    public async Task Login_Fails_WithTheGenericMessage_WhenThePasswordIsWrong()
    {
        _identity.CheckCredentialsAsync(Email, "wrong").Returns(CredentialCheckResult.InvalidCredentials);

        var result = await _service.LoginAsync(Login(password: "wrong"));

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { AuthService.InvalidCredentialsMessage }, result.Errors);
        await _jwt.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default!, default!);
    }

    [Fact]
    public async Task Login_IsRefused_WhenTheAccountIsLockedOut_EvenIfThePasswordIsRight()
    {
        _identity.CheckCredentialsAsync(Email, Password).Returns(CredentialCheckResult.LockedOut);

        var result = await _service.LoginAsync(Login());

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { AuthService.LockedOutMessage }, result.Errors);
        await _jwt.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default!, default!);
    }

    [Fact]
    public async Task Login_DoesNotRevealThatAnAccountIsInactive_ToSomeoneWithTheWrongPassword()
    {
        _identity.IsActiveAsync("u-1").Returns(false);
        _identity.CheckCredentialsAsync(Email, "wrong").Returns(CredentialCheckResult.InvalidCredentials);

        var result = await _service.LoginAsync(Login(password: "wrong"));

        Assert.Equal(new[] { AuthService.InvalidCredentialsMessage }, result.Errors);
        await _identity.DidNotReceiveWithAnyArgs().IsActiveAsync(default!);
    }

    [Fact]
    public async Task Login_Fails_WhenTheAccountIsInactive_AndThePasswordIsRight()
    {
        _identity.IsActiveAsync("u-1").Returns(false);
        _identity.CheckCredentialsAsync(Email, Password).Returns(CredentialCheckResult.Success);

        var result = await _service.LoginAsync(Login());

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { "Your account is inactive." }, result.Errors);
        await _jwt.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default!, default!);
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("", "pass")]
    [InlineData("   ", "pass")]
    [InlineData(Email, null)]
    [InlineData(Email, "")]
    public async Task Login_Fails_WithoutCallingIdentity_WhenAFieldIsMissing(string? email, string? password)
    {
        var result = await _service.LoginAsync(new LoginRequestDto { Email = email!, Password = password! });

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { AuthService.InvalidCredentialsMessage }, result.Errors);
        await _identity.DidNotReceiveWithAnyArgs().GetUserByEmailAsync(default!);
    }
}
