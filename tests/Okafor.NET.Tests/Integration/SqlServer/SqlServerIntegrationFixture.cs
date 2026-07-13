using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Okafor_.NET.Data;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace Okafor_.NET.Tests.Integration.SqlServer;

public sealed class SqlServerIntegrationFixture : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image)
        .WithCleanUp(true)
        .Build();
    private readonly SemaphoreSlim _resetLock = new(1, 1);

    private SqlConnection? _connection;
    private Respawner? _respawner;

    public string ConnectionString => _container.GetConnectionString();

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .EnableDetailedErrors()
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = CreateDbContext())
        {
            EnsureNoPendingModelChanges(context);
            await context.Database.MigrateAsync();
        }

        _connection = new SqlConnection(ConnectionString);
        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new Table("dbo", "__EFMigrationsHistory")]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_connection is null || _respawner is null)
        {
            throw new InvalidOperationException("The SQL Server integration fixture has not been initialized.");
        }

        await _resetLock.WaitAsync();
        try
        {
            await _respawner.ResetAsync(_connection);
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _resetLock.Dispose();
        await _container.DisposeAsync();
    }

    private static void EnsureNoPendingModelChanges(ApplicationDbContext context)
    {
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot;
        if (snapshot is null)
        {
            throw new InvalidOperationException("No EF Core migration snapshot was found.");
        }

        var modelInitializer = context.GetService<IModelRuntimeInitializer>();
        var snapshotModel = modelInitializer.Initialize(snapshot.Model, designTime: true, validationLogger: null);
        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var modelDiffer = context.GetService<IMigrationsModelDiffer>();
        var differences = modelDiffer.GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        if (differences.Count > 0)
        {
            var operationNames = string.Join(", ", differences.Select(DescribeOperation));
            throw new InvalidOperationException(
                $"The EF Core model differs from the migration snapshot. Operations: {operationNames}.");
        }
    }

    private static string DescribeOperation(MigrationOperation operation) => operation switch
    {
        AlterColumnOperation alter =>
            $"AlterColumn({alter.Schema ?? "dbo"}.{alter.Table}.{alter.Name}: {alter.OldColumn.ColumnType ?? alter.OldColumn.ClrType.Name} -> {alter.ColumnType ?? alter.ClrType.Name})",
        _ => operation.GetType().Name
    };
}
