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
        => PostAsync<AuthLoginRegisterWireRequest, AuthLoginRegisterResult>("/api/auth/register", new AuthLoginRegisterWireRequest(request.PhoneNumber, request.Password.Value, request.DisplayName), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordWireRequest, AuthLoginPasswordResult>("/api/auth/login", new AuthLoginPasswordWireRequest(request.PhoneNumber, request.Password.Value), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> ChangePasswordAsync(AuthLoginChangePasswordRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginChangePasswordWireRequest, AuthLoginOperationResult>("/internal/v1/password/change", new AuthLoginChangePasswordWireRequest(request.UserId, request.CurrentPassword.Value, request.NewPassword.Value, request.ReasonCode), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPasswordResetAsync(AuthLoginPasswordResetStartRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordResetStartRequest, AuthLoginOperationResult>("/internal/v1/password/reset/start", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> CompletePasswordResetAsync(AuthLoginPasswordResetCompleteRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordResetCompleteWireRequest, AuthLoginOperationResult>("/internal/v1/password/reset/complete", new AuthLoginPasswordResetCompleteWireRequest(request.ResetOperationId, request.NewPassword.Value), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ForcePasswordChangeAsync(AuthLoginForcePasswordChangeRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginForcePasswordChangeWireRequest, AuthLoginAccountStateChangeResult>("/internal/v1/password/force-change", new AuthLoginForcePasswordChangeWireRequest(request.UserId, request.NewPassword.Value, request.ReasonCode), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPhoneChangeAsync(AuthLoginPhoneChangeStartRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeStartRequest, AuthLoginOperationResult>("/internal/v1/phone/change/start", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> ConfirmPhoneChangeAsync(AuthLoginPhoneChangeConfirmRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeConfirmWireRequest, AuthLoginOperationResult>("/internal/v1/phone/change/confirm", new AuthLoginPhoneChangeConfirmWireRequest(request.UserId, request.OperationId, request.Otp.Value), cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginOperationResult>> CancelPhoneChangeAsync(AuthLoginPhoneChangeCancelRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPhoneChangeCancelRequest, AuthLoginOperationResult>("/internal/v1/phone/change/cancel", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStatusResult>> GetAccountStatusAsync(Guid userId, CancellationToken cancellationToken)
        => GetAsync<AuthLoginAccountStatusResult>($"/internal/v1/accounts/{userId}/status", cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ChangeAccountStateAsync(AuthLoginAccountStateChangeRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginAccountStateChangeRequest, AuthLoginAccountStateChangeResult>($"/internal/v1/accounts/{request.UserId}/state", request, cancellationToken);
}

internal sealed record AuthLoginRegisterWireRequest(string PhoneNumber, string Password, string DisplayName);
internal sealed record AuthLoginPasswordWireRequest(string PhoneNumber, string Password);
internal sealed record AuthLoginChangePasswordWireRequest(Guid UserId, string CurrentPassword, string NewPassword, string ReasonCode);
internal sealed record AuthLoginPasswordResetCompleteWireRequest(string ResetOperationId, string NewPassword);
internal sealed record AuthLoginForcePasswordChangeWireRequest(Guid UserId, string NewPassword, string ReasonCode);
internal sealed record AuthLoginPhoneChangeConfirmWireRequest(Guid UserId, Guid OperationId, string Otp);
