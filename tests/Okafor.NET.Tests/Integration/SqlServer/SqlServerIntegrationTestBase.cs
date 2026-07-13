namespace Okafor_.NET.Tests.Integration.SqlServer;

public abstract class SqlServerIntegrationTestBase : IAsyncLifetime
{
    protected SqlServerIntegrationTestBase(SqlServerIntegrationFixture fixture)
    {
        Fixture = fixture;
    }

    protected SqlServerIntegrationFixture Fixture { get; }

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
