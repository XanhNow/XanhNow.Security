using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.IntegrationTests.Persistence;

public sealed class SecurityDbContextModelTests
{
    [Fact]
    public void Model_contains_exactly_ten_security_tables()
    {
        using var db = PersistenceTestFactory.CreateContext();

        var tables = db.Model.GetEntityTypes()
            .Select(x => (Schema: x.GetSchema(), Table: x.GetTableName()))
            .Where(x => x.Table is not null)
            .Distinct()
            .OrderBy(x => x.Table)
            .ToArray();

        Assert.Equal(10, tables.Length);
        Assert.All(tables, x => Assert.Equal(SecurityDatabaseConstants.Schema, x.Schema));
        Assert.DoesNotContain(tables, x => x.Table == "security_operation_steps");
        Assert.Equal(SecurityDatabaseConstants.ExpectedTables.OrderBy(x => x), tables.Select(x => x.Table!).OrderBy(x => x));
    }

    [Fact]
    public void Mutable_security_tables_have_row_version_concurrency_token()
    {
        using var db = PersistenceTestFactory.CreateContext();
        var mutableTables = new[]
        {
            SecurityDatabaseConstants.UsersTable,
            SecurityDatabaseConstants.ProfilesTable,
            SecurityDatabaseConstants.SessionsTable,
            SecurityDatabaseConstants.GrantsTable,
            SecurityDatabaseConstants.PoliciesTable,
            SecurityDatabaseConstants.RecoveryCasesTable,
            SecurityDatabaseConstants.OperationRequestsTable
        };

        foreach (var table in mutableTables)
        {
            var entity = db.Model.GetEntityTypes().Single(x => x.GetTableName() == table);
            var property = entity.FindProperty("RowVersion");
            Assert.NotNull(property);
            Assert.True(property!.IsConcurrencyToken);
            Assert.Equal("row_version", property.GetColumnName());
        }
    }

    [Fact]
    public void Operation_steps_are_jsonb_column_not_separate_table()
    {
        using var db = PersistenceTestFactory.CreateRelationalModelContext();
        var entity = db.Model.GetEntityTypes().Single(x => x.GetTableName() == SecurityDatabaseConstants.OperationRequestsTable);
        var property = entity.FindProperty("_steps");

        Assert.NotNull(property);
        Assert.Equal("step_states", property!.GetColumnName());
        Assert.Equal("jsonb", property.GetColumnType());
    }
}
