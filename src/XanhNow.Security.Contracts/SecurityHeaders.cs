namespace XanhNow.Security.Contracts;

public static class SecurityHeaders
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string RequestId = "X-Request-Id";
    public const string ClientId = "X-Client-Id";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string StepUpGrant = "X-Step-Up-Grant";
    public const string ContractVersion = "X-Contract-Version";
}
