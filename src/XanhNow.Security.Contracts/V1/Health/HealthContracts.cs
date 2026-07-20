using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.V1.Health;

public sealed record LiveHealthResponse(string Service, string Status, DateTimeOffset CheckedAtUtc);
public sealed record ReadyHealthResponse(string Service, DependencyStatusContract Status, IReadOnlyCollection<DependencyHealthResponse> Dependencies, DateTimeOffset CheckedAtUtc);
public sealed record DependencyHealthResponse(string Name, DependencyStatusContract Status, string Message);
