using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace HelpDesk.Api.Tests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    private Respawner _respawner = null!;

    public HelpDeskApiFactory Factory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder =
            new SqlConnectionStringBuilder(
                _container.GetConnectionString())
            {
                InitialCatalog = "HelpDeskTests"
            };

        ConnectionString = builder.ConnectionString;

        Factory = new HelpDeskApiFactory(ConnectionString);

        // Force le démarrage du host + Migrate()
        _ = Factory.CreateClient();

        await using var connection = new SqlConnection(ConnectionString);

        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                TablesToIgnore =
                [
                    new Table("__EFMigrationsHistory")
                ]
            });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}