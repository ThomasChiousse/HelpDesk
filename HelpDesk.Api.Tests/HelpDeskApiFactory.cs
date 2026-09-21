using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelpDesk.Api.Tests;

public class HelpDeskApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("DataSource=:memory:");

    public HelpDeskApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(
       IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<HelpDeskDbContext>(
                options =>
                    options.UseSqlite(_connection));
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] =
                        "this-is-a-fake-integration-test-key-that-is-long-enough-123",
                    ["Jwt:Issuer"] = "HelpDesk.Api",
                    ["Jwt:Audience"] = "HelpDesk.Client"
                });
        });
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<HelpDeskDbContext>();

        context.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}