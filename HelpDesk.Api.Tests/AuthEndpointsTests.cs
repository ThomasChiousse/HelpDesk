using System.Net;
using System.Net.Http.Headers;

namespace HelpDesk.Api.Tests;

public class AuthEndpointsTests
{
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
}
