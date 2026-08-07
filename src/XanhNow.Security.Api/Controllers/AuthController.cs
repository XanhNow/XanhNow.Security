using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Auth;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly ApplicationExecutor<RegisterCommand, RegisterResult> _register;
    private readonly ApplicationExecutor<PasswordLoginCommand, PasswordLoginResult> _passwordLogin;
    private readonly ApplicationExecutor<BeginPasskeyLoginCommand, BeginPasskeyLoginResult> _beginPasskeyLogin;
    private readonly ApplicationExecutor<FinishPasskeyLoginCommand, PasswordLoginResult> _finishPasskeyLogin;
    private readonly ApplicationExecutor<BeginRegistrationPasskeyCommand, BeginRegistrationPasskeyResult> _beginRegistrationPasskey;
    private readonly ApplicationExecutor<FinishRegistrationPasskeyCommand, FinishRegistrationPasskeyResult> _finishRegistrationPasskey;
    private readonly ApplicationExecutor<BeginLoginMfaCommand, LoginMfaChallengeResult> _beginLoginMfa;
    private readonly ApplicationExecutor<CompleteLoginMfaCommand, ProtectedGrantResult> _completeLoginMfa;
    private readonly ApplicationExecutor<CompletePasskeyLoginWithGrantCommand, ProtectedGrantResult> _completePasskeyLoginWithGrant;

    public AuthController(
        ApplicationExecutor<RegisterCommand, RegisterResult> register,
        ApplicationExecutor<PasswordLoginCommand, PasswordLoginResult> passwordLogin,
        ApplicationExecutor<BeginPasskeyLoginCommand, BeginPasskeyLoginResult> beginPasskeyLogin,
        ApplicationExecutor<FinishPasskeyLoginCommand, PasswordLoginResult> finishPasskeyLogin,
        ApplicationExecutor<BeginRegistrationPasskeyCommand, BeginRegistrationPasskeyResult> beginRegistrationPasskey,
        ApplicationExecutor<FinishRegistrationPasskeyCommand, FinishRegistrationPasskeyResult> finishRegistrationPasskey,
        ApplicationExecutor<BeginLoginMfaCommand, LoginMfaChallengeResult> beginLoginMfa,
        ApplicationExecutor<CompleteLoginMfaCommand, ProtectedGrantResult> completeLoginMfa,
        ApplicationExecutor<CompletePasskeyLoginWithGrantCommand, ProtectedGrantResult> completePasskeyLoginWithGrant)
    {
        _register = register;
        _passwordLogin = passwordLogin;
        _beginPasskeyLogin = beginPasskeyLogin;
        _finishPasskeyLogin = finishPasskeyLogin;
        _beginRegistrationPasskey = beginRegistrationPasskey;
        _finishRegistrationPasskey = finishRegistrationPasskey;
        _beginLoginMfa = beginLoginMfa;
        _completeLoginMfa = completeLoginMfa;
        _completePasskeyLoginWithGrant = completePasskeyLoginWithGrant;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EndpointMaturity("Current", "auth.register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _register.ExecuteAsync(new RegisterCommand(request.PhoneNumber, request.Password, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, x => new RegisterResponse(x.UserId, SecurityStatusContract.Active, MapRegistrationStatus(x.RegistrationStatus), x.RegisteredAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("register/passkey/begin")]
    [EndpointMaturity("Current", "auth.register.passkey.begin")]
    public async Task<ActionResult<ApiResponse<BeginRegistrationPasskeyResponse>>> BeginRegistrationPasskeyAsync(BeginRegistrationPasskeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _beginRegistrationPasskey.ExecuteAsync(new BeginRegistrationPasskeyCommand(request.UserId, request.DisplayName, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, x => new BeginRegistrationPasskeyResponse(x.UserId, x.CeremonyId, x.PublicKeyOptions, x.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("register/passkey/finish")]
    [EndpointMaturity("Current", "auth.register.passkey.finish")]
    public async Task<ActionResult<ApiResponse<FinishRegistrationPasskeyResponse>>> FinishRegistrationPasskeyAsync(FinishRegistrationPasskeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _finishRegistrationPasskey.ExecuteAsync(new FinishRegistrationPasskeyCommand(request.UserId, request.CeremonyId, request.Credential, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, x => new FinishRegistrationPasskeyResponse(x.UserId, MapRegistrationStatus(x.RegistrationStatus), x.CompletedAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("login/password")]
    [EndpointMaturity("Current", "auth.login.password")]
    public async Task<ActionResult<ApiResponse<PasswordLoginResponse>>> LoginWithPasswordAsync(PasswordLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordLogin.ExecuteAsync(new PasswordLoginCommand(request.PhoneNumber, request.Password, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, MapLogin);
    }

    [AllowAnonymous]
    [HttpPost("login/mfa/begin")]
    [EndpointMaturity("Current", "auth.login.mfa.begin")]
    public async Task<ActionResult<ApiResponse<BeginMfaLoginResponse>>> BeginLoginMfaAsync(BeginMfaLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _beginLoginMfa.ExecuteAsync(new BeginLoginMfaCommand(request.UserId, request.LoginOperationId, request.TransactionDigest), cancellationToken);
        return FromApplicationResult(result, x => new BeginMfaLoginResponse(x.UserId, x.ChallengeId, x.Method, x.Purpose, x.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("login/mfa/complete")]
    [EndpointMaturity("Current", "auth.login.mfa.complete")]
    public async Task<ActionResult<ApiResponse<ProtectedGrantResponse>>> CompleteLoginMfaAsync(CompleteMfaLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _completeLoginMfa.ExecuteAsync(new CompleteLoginMfaCommand(request.UserId, request.ChallengeId, request.Otp, request.Audience), cancellationToken);
        return FromApplicationResult(result, MapGrant);
    }

    [AllowAnonymous]
    [HttpPost("login/passkey/begin")]
    [EndpointMaturity("Current", "auth.login.passkey.begin")]
    public async Task<ActionResult<ApiResponse<PasskeyLoginBeginResponse>>> BeginPasskeyLoginAsync(PasskeyLoginBeginRequest request, CancellationToken cancellationToken)
    {
        var result = await _beginPasskeyLogin.ExecuteAsync(new BeginPasskeyLoginCommand(request.LoginIdentifier, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, x => new PasskeyLoginBeginResponse(x.CeremonyId, x.PublicKeyOptions, x.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("login/passkey/finish")]
    [EndpointMaturity("Current", "auth.login.passkey.finish")]
    public async Task<ActionResult<ApiResponse<PasswordLoginResponse>>> FinishPasskeyLoginAsync(PasskeyLoginFinishRequest request, CancellationToken cancellationToken)
    {
        var result = await _finishPasskeyLogin.ExecuteAsync(new FinishPasskeyLoginCommand(request.CeremonyId, request.Credential, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, MapLogin);
    }

    [AllowAnonymous]
    [HttpPost("login/passkey/finish-grant")]
    [EndpointMaturity("Current", "auth.login.passkey.finish_grant")]
    public async Task<ActionResult<ApiResponse<ProtectedGrantResponse>>> FinishPasskeyLoginWithGrantAsync(PasskeyLoginFinishGrantRequest request, CancellationToken cancellationToken)
    {
        var result = await _completePasskeyLoginWithGrant.ExecuteAsync(new CompletePasskeyLoginWithGrantCommand(request.CeremonyId, request.Credential.GetRawText(), request.Audience), cancellationToken);
        return FromApplicationResult(result, MapGrant);
    }

    private static DeviceContext? MapDevice(DeviceContextRequest? device)
        => device is null ? null : new DeviceContext(device.DeviceId, device.DeviceName, device.Platform, device.IpAddress, device.UserAgent);

    private static PasswordLoginResponse MapLogin(PasswordLoginResult result)
        => new(
            Enum.TryParse<AuthenticationState>(result.State, ignoreCase: true, out var state) ? state : AuthenticationState.Completed,
            result.UserId,
            result.Tokens is null ? null : new TokenPairResponse(result.Tokens.AccessToken, result.Tokens.RefreshToken, result.Tokens.AccessTokenExpiresAtUtc, result.Tokens.RefreshTokenExpiresAtUtc, result.Tokens.TokenType),
            result.Mfa is null ? null : new MfaChallengeResponse(result.Mfa.ChallengeId, result.Mfa.Method, result.Mfa.ExpiresAtUtc),
            result.ReasonCode);

    private static ProtectedGrantResponse MapGrant(ProtectedGrantResult result)
        => new(result.GrantId, result.Grant, result.GrantType, result.Audience, result.Purpose, result.ExpiresAtUtc);

    private static RegistrationStatusContract MapRegistrationStatus(string status)
        => Enum.TryParse<RegistrationStatusContract>(status, ignoreCase: true, out var parsed) ? parsed : RegistrationStatusContract.PendingPasskey;
}
