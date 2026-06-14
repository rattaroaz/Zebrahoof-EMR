using Microsoft.Extensions.DependencyInjection;

namespace Zebrahoof_EMR.IntegrationTests;

public class DatabaseMigrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DatabaseMigrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() =>
        await _factory.ExecuteScopeAsync(async services => await TestDataSeeder.ClearAllDataAsync(services));

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_CanBeCreated_WithMigrations() => Task.CompletedTask;

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_CanSeedData_WithMigrations() => Task.CompletedTask;

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_CanResetAndReseed() => Task.CompletedTask;

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_HandlesConcurrentOperations() => Task.CompletedTask;

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_Transactions_RollbackOnFailure() => Task.CompletedTask;

    [Fact(Skip = "Feature not fully implemented")]
    public Task Database_Transactions_CommitOnSuccess() => Task.CompletedTask;
}
