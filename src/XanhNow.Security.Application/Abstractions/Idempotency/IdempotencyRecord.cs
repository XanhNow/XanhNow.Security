namespace XanhNow.Security.Application.Abstractions.Idempotency;

public sealed record IdempotencyRecord(string Key, string RequestHash, string? StoredResultJson, bool Completed);
