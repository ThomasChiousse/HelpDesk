using Testcontainers.MsSql;

namespace HelpDesk.Api.Tests;

public sealed class MsSqlFixture : IAsyncLifetime
{
    private const string Image =
        "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    public MsSqlContainer Container { get; } =
        new MsSqlBuilder(Image)
            .Build();

    public string ConnectionString =>
        Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}