using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HelpDesk.Api.Tests;

public class HelpDeskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public HelpDeskApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
       IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<HelpDeskDbContext>>();

            services.RemoveAll<HelpDeskDbContext>();

            services.AddDbContext<HelpDeskDbContext>(
                options =>
                    options.UseSqlServer(_connectionString));
        });
    }

    public const string TestJwtKey =
        "this-is-a-fake-integration-test-key-that-is-long-enough-123";
    public const string TestJwtIssuer = "HelpDesk.Api";
    public const string TestJwtAudience = "HelpDesk.Client";

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = TestJwtKey,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience
                });
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<HelpDeskDbContext>();

        context.Database.Migrate();

        return host;
    }
}