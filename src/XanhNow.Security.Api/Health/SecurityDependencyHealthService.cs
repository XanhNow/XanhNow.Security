using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.V1.Health;

namespace XanhNow.Security.Api.Health;

public sealed class SecurityDependencyHealthService
{
    public Task<ReadyHealthResponse> CheckReadyAsync(string serviceName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dependencies = new[]
        {
            new DependencyHealthResponse("postgres", DependencyStatusContract.Healthy, "configured"),
            new DependencyHealthResponse("vault", DependencyStatusContract.Healthy, "configured"),
            new DependencyHealthResponse("redis", DependencyStatusContract.Healthy, "configured"),
            new DependencyHealthResponse("kafka", DependencyStatusContract.Healthy, "configured"),
            new DependencyHealthResponse("child-app-contracts", DependencyStatusContract.Healthy, "registered")
        };

        return Task.FromResult(new ReadyHealthResponse(serviceName, DependencyStatusContract.Healthy, dependencies, DateTimeOffset.UtcNow));
    }
}
