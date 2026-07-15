namespace Okafor_.NET.E2E;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class E2eCollection : ICollectionFixture<E2eFixture>
{
    public const string Name = "End-to-end browser tests";
}
