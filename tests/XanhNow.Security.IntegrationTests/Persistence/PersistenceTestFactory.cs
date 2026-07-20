using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.IntegrationTests.Persistence;

internal static class PersistenceTestFactory
{
    public static SecurityDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new SecurityDbContext(options);
    }

    public static SecurityDbContext CreateRelationalModelContext()
    {
        var options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=5432;Database=xanhnow_security_model_only;Username=model_only;Password=model_only")
            .Options;

        return new SecurityDbContext(options);
    }
}
