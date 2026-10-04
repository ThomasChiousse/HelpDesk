namespace HelpDesk.Api.Tests
{
    [CollectionDefinition("Api integration tests",
        DisableParallelization = true)]
    public class ApiTestCollection : ICollectionFixture<ApiTestFixture>
    {
    }
}
