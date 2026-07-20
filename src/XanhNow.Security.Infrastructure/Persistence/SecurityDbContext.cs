using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Domain.Audit;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Domain.Profiles;
using XanhNow.Security.Domain.Recovery;
using XanhNow.Security.Domain.Sessions;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Infrastructure.Persistence.Models;

namespace XanhNow.Security.Infrastructure.Persistence;

public sealed class SecurityDbContext : DbContext
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    public DbSet<SecurityUser> SecurityUsers => Set<SecurityUser>();
    public DbSet<SecurityProfile> SecurityProfiles => Set<SecurityProfile>();
    public DbSet<SecuritySession> SecuritySessions => Set<SecuritySession>();
    public DbSet<SecurityGrant> SecurityGrants => Set<SecurityGrant>();
    public DbSet<SecurityPolicy> SecurityPolicies => Set<SecurityPolicy>();
    public DbSet<SecurityPolicyDecision> SecurityPolicyDecisions => Set<SecurityPolicyDecision>();
    public DbSet<SecurityRecoveryCase> SecurityRecoveryCases => Set<SecurityRecoveryCase>();
    public DbSet<SecurityOperationRequest> SecurityOperationRequests => Set<SecurityOperationRequest>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
    public DbSet<SecurityOutboxMessageRow> SecurityOutboxMessages => Set<SecurityOutboxMessageRow>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyRowVersionUpdates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyRowVersionUpdates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SecurityDatabaseConstants.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecurityDbContext).Assembly);
    }

    private void ApplyRowVersionUpdates()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified && x.Metadata.FindProperty("RowVersion") is not null))
        {
            var property = entry.Property("RowVersion");
            var original = property.OriginalValue is long value ? value : 0L;
            property.CurrentValue = original + 1;
        }
    }
}


