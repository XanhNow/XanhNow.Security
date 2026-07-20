namespace XanhNow.Security.Application.Abstractions.Context;

public sealed record CorrelationContext(string CorrelationId, string TraceId);
