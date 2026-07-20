namespace XanhNow.Security.Application.Abstractions.Idempotency;

public interface IRequestFingerprint
{
    string Compute(object request);
}
