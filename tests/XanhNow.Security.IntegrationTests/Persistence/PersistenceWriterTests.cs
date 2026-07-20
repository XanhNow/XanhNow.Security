using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Infrastructure.Persistence.Writers;

namespace XanhNow.Security.IntegrationTests.Persistence;

public sealed class PersistenceWriterTests
{
    [Fact]
    public async Task Audit_and_outbox_writers_add_rows_without_saving_individually()
    {
        await using var db = PersistenceTestFactory.CreateContext();
        var audit = new AuditIntentWriter(db);
        var outbox = new OutboxIntentWriter(db);

        await audit.AppendAsync(new AuditIntent(Guid.NewGuid(), "LOGIN", "SUCCESS", "test", "trace-1", DateTimeOffset.UtcNow), CancellationToken.None);
        await outbox.AppendAsync(new OutboxIntent(Guid.NewGuid(), "Security.LoginSucceeded", "security_operation", Guid.NewGuid(), "{\"ok\":true}", DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Empty(db.SecurityAuditLogs);
        Assert.Empty(db.SecurityOutboxMessages);

        await db.SaveChangesAsync();

        Assert.Single(db.SecurityAuditLogs);
        Assert.Single(db.SecurityOutboxMessages);
    }
}
