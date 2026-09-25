using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace HelpDesk.Api.Tests;

public class AuthEndpointsTests
{
    #region Prework/Helpers
    private static string CreateValidJwt()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                HelpDeskApiFactory.TestJwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Email, "john@example.com"),
            new Claim(ClaimTypes.Role, "Technician")
        };

        var token = new JwtSecurityToken(
            issuer: HelpDeskApiFactory.TestJwtIssuer,
            audience: HelpDeskApiFactory.TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<int> SeedUserWithPasswordAsync(
    HelpDeskApiFactory factory,
    string password = "correct-password")
    {
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<HelpDeskDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var user = new User(
            "John",
            "Doe",
            "john@example.com",
            UserRole.Technician);

        var hash = passwordHasher.Hash(
            user,
            password);

        user.SetPasswordHash(hash);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }
    #endregion

    #region GetMe
    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturnUnauthorized()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_ShouldReturnUnauthorized()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturnOk()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var token = CreateValidJwt();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(currentUser);
        Assert.Equal("42", currentUser.UserId);
        Assert.Equal("john@example.com", currentUser.Email);
        Assert.Equal("Technician", currentUser.Role);
    }
    #endregion

    #region Login

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        using var factory = new HelpDeskApiFactory();

        var userId = await SeedUserWithPasswordAsync(factory);

        var client = factory.CreateClient();

        var request = new LoginRequest(
            "john@example.com",
            "correct-password");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loginResponse =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResponse);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResponse.Token));

        client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(
        "Bearer",
        loginResponse.Token);

        var meResponse = await client.GetAsync(
            "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(currentUser);

        Assert.Equal(
            userId.ToString(),
            currentUser.UserId);

        Assert.Equal(
            "john@example.com",
            currentUser.Email);

        Assert.Equal(
            "Technician",
            currentUser.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        using var factory = new HelpDeskApiFactory();
        await SeedUserWithPasswordAsync(factory);

        var client = factory.CreateClient();
        var request = new LoginRequest("john@example.com", "wrongpassworddummy");

        var response = await client.PostAsJsonAsync("/api/auth/login", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnUnauthorized()
    {
        using var factory = new HelpDeskApiFactory();
        await SeedUserWithPasswordAsync(factory);

        var client = factory.CreateClient();
        var request = new LoginRequest("not-john@example.com", "correct-password");

        var response = await client.PostAsJsonAsync("/api/auth/login", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    #endregion
}
