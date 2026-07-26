using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Infrastructure.Integration.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.AuthLogin;

internal sealed class AuthLoginRestClient : ChildAppJsonClient, IAuthLoginClient
{
    public AuthLoginRestClient(HttpClient http, SecurityIntegrationOptions options)
        : base(http, options.AuthLogin, options.ContractVersion)
    {
    }

    public ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginRegisterRequest, AuthLoginRegisterResult>("/api/v1/auth/register", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordRequest, AuthLoginPasswordResult>("/api/v1/auth/login/password", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> ChangePasswordAsync(AuthLoginChangePasswordRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginChangePasswordRequest, AuthLoginOperationResult>("/internal/v1/password/change", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPasswordResetAsync(AuthLoginPasswordResetStartRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordResetStartRequest, AuthLoginOperationResult>("/internal/v1/password/reset/start", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> CompletePasswordResetAsync(AuthLoginPasswordResetCompleteRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordResetCompleteRequest, AuthLoginOperationResult>("/internal/v1/password/reset/complete", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ForcePasswordChangeAsync(AuthLoginForcePasswordChangeRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginForcePasswordChangeRequest, AuthLoginAccountStateChangeResult>("/internal/v1/password/force-change", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPhoneChangeAsync(AuthLoginPhoneChangeStartRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeStartRequest, AuthLoginOperationResult>("/internal/v1/phone/change/start", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> ConfirmPhoneChangeAsync(AuthLoginPhoneChangeConfirmRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeConfirmRequest, AuthLoginOperationResult>("/internal/v1/phone/change/confirm", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> CancelPhoneChangeAsync(AuthLoginPhoneChangeCancelRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeCancelRequest, AuthLoginOperationResult>("/internal/v1/phone/change/cancel", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStatusResult>> GetAccountStatusAsync(Guid userId, CancellationToken cancellationToken)
        => GetAsync<AuthLoginAccountStatusResult>($"/internal/v1/accounts/{userId}/status", cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ChangeAccountStateAsync(AuthLoginAccountStateChangeRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginAccountStateChangeRequest, AuthLoginAccountStateChangeResult>($"/internal/v1/accounts/{request.UserId}/state", request, cancellationToken);
}
