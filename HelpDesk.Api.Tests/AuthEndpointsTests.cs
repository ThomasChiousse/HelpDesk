using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace HelpDesk.Api.Tests;

public class AuthEndpointsTests
{
    private static string CreateValidJwt()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                "this-is-a-fake-integration-test-key-that-is-long-enough-123"));

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
            issuer: "HelpDesk.Api",
            audience: "HelpDesk.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
    }
}
