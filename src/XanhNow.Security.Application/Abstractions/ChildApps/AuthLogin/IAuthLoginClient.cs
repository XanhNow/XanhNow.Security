using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;

public sealed record AuthLoginRegisterRequest(string PhoneNumber, SensitiveString Password, string DisplayName);
public sealed record AuthLoginRegisterResult(Guid UserId);
public sealed record AuthLoginPasswordRequest(string PhoneNumber, SensitiveString Password);
public sealed record AuthLoginPasswordResult(Guid UserId, string AssuranceLevel);

public interface IAuthLoginClient
{
    ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken);
}
