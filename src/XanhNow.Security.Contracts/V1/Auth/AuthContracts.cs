using System.Text.Json;
using XanhNow.Security.Contracts.Common.Attributes;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Contracts.V1.Auth;

public sealed record RegisterRequest(string PhoneNumber, [property: SensitiveData("password")] string Password, DeviceContextRequest? DeviceContext);
public sealed record RegisterResponse(Guid UserId, SecurityStatusContract Status, DateTimeOffset RegisteredAtUtc);
public sealed record PasswordLoginRequest(string PhoneNumber, [property: SensitiveData("password")] string Password, DeviceContextRequest? DeviceContext);
public sealed record PasswordLoginResponse(AuthenticationState State, Guid? UserId, TokenPairResponse? Tokens, MfaChallengeResponse? Mfa, string? ReasonCode);
public sealed record MfaChallengeResponse(string ChallengeId, string Method, DateTimeOffset ExpiresAtUtc);
public sealed record BeginMfaLoginRequest(Guid UserId, string LoginOperationId, string TransactionDigest);
public sealed record BeginMfaLoginResponse(Guid UserId, string ChallengeId, string Method, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record CompleteMfaLoginRequest(Guid UserId, string ChallengeId, [property: SensitiveData("otp")] string Otp, string Audience, DeviceContextRequest? DeviceContext);
public sealed record ProtectedGrantResponse(Guid GrantId, [property: SensitiveData("protected-grant")] string Grant, string GrantType, string Audience, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record PasskeyLoginBeginRequest(string? LoginIdentifier, DeviceContextRequest? DeviceContext);
public sealed record PasskeyLoginBeginResponse(string CeremonyId, [property: SensitiveData("webauthn-public-key-options")] JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record PasskeyLoginFinishRequest(string CeremonyId, [property: SensitiveData("webauthn-credential")] JsonElement Credential, DeviceContextRequest? DeviceContext);
public sealed record PasskeyLoginFinishGrantRequest(string CeremonyId, [property: SensitiveData("webauthn-credential")] JsonElement Credential, string Audience, DeviceContextRequest? DeviceContext);
