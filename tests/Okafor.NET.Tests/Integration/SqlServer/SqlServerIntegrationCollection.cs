namespace Okafor_.NET.Tests.Integration.SqlServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection : ICollectionFixture<SqlServerIntegrationFixture>
{
    public const string Name = "SQL Server integration";
}
