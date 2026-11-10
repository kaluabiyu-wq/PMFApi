using Microsoft.EntityFrameworkCore;
using Npgsql;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Tests.Support;


public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public const string EnvVar = "PMF_TEST_PG";

    public RequiresPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVar)))
            Skip = $"Set {EnvVar} to a PostgreSQL admin connection string (see PostgresFixture) to run this test.";
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}


public sealed class PostgresFixture : IAsyncLifetime
{
    private string? _adminConnectionString;
    private string? _databaseName;

    public bool Enabled { get; private set; }
    public string ConnectionString { get; private set; } = PmfApiFactory.UnusedConnectionString;
    public PmfApiFactory Factory { get; private set; } = null!;
    public TestWorld World { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable(RequiresPostgresFactAttribute.EnvVar);
        Enabled = !string.IsNullOrWhiteSpace(_adminConnectionString);

        if (Enabled)
        {
            _databaseName = $"pmf_test_{Guid.NewGuid():N}";

            await using (var admin = new NpgsqlConnection(_adminConnectionString))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
            {
                Database = _databaseName,
            }.ConnectionString;

            await using var db = NewDbContext();
            await db.Database.MigrateAsync();
            World = await WorldSeeder.SeedAsync(db);
        }

        Factory = new PmfApiFactory(ConnectionString);
    }

    public PmfDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<PmfDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        Factory?.Dispose();

        if (!Enabled || _databaseName is null) return;

        
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
