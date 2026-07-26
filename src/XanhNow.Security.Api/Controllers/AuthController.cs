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

    public AuthController(
        ApplicationExecutor<RegisterCommand, RegisterResult> register,
        ApplicationExecutor<PasswordLoginCommand, PasswordLoginResult> passwordLogin,
        ApplicationExecutor<BeginPasskeyLoginCommand, BeginPasskeyLoginResult> beginPasskeyLogin,
        ApplicationExecutor<FinishPasskeyLoginCommand, PasswordLoginResult> finishPasskeyLogin)
    {
        _register = register;
        _passwordLogin = passwordLogin;
        _beginPasskeyLogin = beginPasskeyLogin;
        _finishPasskeyLogin = finishPasskeyLogin;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EndpointMaturity("Current", "auth.register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _register.ExecuteAsync(new RegisterCommand(request.PhoneNumber, request.Password, MapDevice(request.DeviceContext)), cancellationToken);
        return FromApplicationResult(result, x => new RegisterResponse(x.UserId, SecurityStatusContract.Active, x.RegisteredAtUtc));
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

    private static DeviceContext? MapDevice(DeviceContextRequest? device)
        => device is null ? null : new DeviceContext(device.DeviceId, device.DeviceName, device.Platform, device.IpAddress, device.UserAgent);

    private static PasswordLoginResponse MapLogin(PasswordLoginResult result)
        => new(
            Enum.TryParse<AuthenticationState>(result.State, ignoreCase: true, out var state) ? state : AuthenticationState.Completed,
            result.UserId,
            result.Tokens is null ? null : new TokenPairResponse(result.Tokens.AccessToken, result.Tokens.RefreshToken, result.Tokens.AccessTokenExpiresAtUtc, result.Tokens.RefreshTokenExpiresAtUtc, result.Tokens.TokenType),
            result.Mfa is null ? null : new MfaChallengeResponse(result.Mfa.ChallengeId, result.Mfa.Method, result.Mfa.ExpiresAtUtc),
            result.ReasonCode);
}
