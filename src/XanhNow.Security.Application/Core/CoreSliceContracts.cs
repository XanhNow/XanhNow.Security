using System.Text.Json;
using XanhNow.Security.Application.Common.Requests;

namespace XanhNow.Security.Application.Core;

public sealed record DeviceContext(string? DeviceId, string? DeviceName, string? Platform, string? IpAddress, string? UserAgent);

public sealed record RegisterCommand(string PhoneNumber, string Password, DeviceContext? DeviceContext) : ICommand<RegisterResult>;
public sealed record RegisterResult(Guid UserId, string Status, DateTimeOffset RegisteredAtUtc);

public sealed record PasswordLoginCommand(string PhoneNumber, string Password, DeviceContext? DeviceContext) : ICommand<PasswordLoginResult>;
public sealed record PasswordLoginResult(string State, Guid? UserId, TokenPairResult? Tokens, MfaChallengeResult? Mfa, string? ReasonCode);
public sealed record TokenPairResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc, string TokenType = "Bearer");
public sealed record MfaChallengeResult(string ChallengeId, string Method, DateTimeOffset ExpiresAtUtc);

public sealed record RefreshSessionCommand(Guid UserId, string RefreshTokenReference, string? SessionId) : ICommand<TokenPairResult>;
public sealed record LogoutSessionCommand(Guid UserId, string SessionId, string ReasonCode) : ICommand<LogoutSessionResult>;
public sealed record LogoutSessionResult(string SessionId, string Status, DateTimeOffset RevokedAtUtc);

public sealed record BeginPasskeyRegistrationCommand(Guid UserId, string DisplayName) : ICommand<BeginPasskeyRegistrationResult>;
public sealed record BeginPasskeyRegistrationResult(string CeremonyId, JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishPasskeyRegistrationCommand(string CeremonyId, JsonElement Credential, string DeviceName) : ICommand<PasskeyStateResult>;
public sealed record PasskeyStateResult(string PasskeyId, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
public sealed record ListPasskeysQuery(Guid UserId) : IQuery<IReadOnlyCollection<PasskeySummaryResult>>;
public sealed record PasskeySummaryResult(string PasskeyId, string DisplayName, string DeviceName, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);
public sealed record RevokePasskeyCommand(Guid UserId, string PasskeyId, string ReasonCode) : ICommand<PasskeyStateResult>;
public sealed record BeginPasskeyLoginCommand(string? LoginIdentifier, DeviceContext? DeviceContext) : ICommand<BeginPasskeyLoginResult>;
public sealed record BeginPasskeyLoginResult(string CeremonyId, JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishPasskeyLoginCommand(string CeremonyId, JsonElement Credential, DeviceContext? DeviceContext) : ICommand<PasswordLoginResult>;

public sealed record BeginSmartOtpEnrollmentCommand(Guid UserId, string DeviceName) : ICommand<BeginSmartOtpEnrollmentResult>;
public sealed record BeginSmartOtpEnrollmentResult(string EnrollmentId, string ProvisioningUri, string ManualEntryKey, DateTimeOffset ExpiresAtUtc);
public sealed record ConfirmSmartOtpEnrollmentCommand(string EnrollmentId, string Otp) : ICommand<SmartOtpDeviceStateResult>;
public sealed record SmartOtpDeviceStateResult(string DeviceId, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
public sealed record StartStepUpCommand(Guid UserId, string Purpose, string TransactionDigest, DateTimeOffset ExpiresAtUtc) : ICommand<StepUpChallengeResult>;
public sealed record StepUpChallengeResult(string ChallengeId, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyStepUpCommand(string ChallengeId, string Otp) : ICommand<StepUpGrantResult>;
public sealed record StepUpGrantResult(string ChallengeId, string StepUpGrant, string Purpose, DateTimeOffset ExpiresAtUtc);
