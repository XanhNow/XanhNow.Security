using System.Text.Json;
using XanhNow.Security.Application.Common.Requests;

namespace XanhNow.Security.Application.Core;

public sealed record DeviceContext(string? DeviceId, string? DeviceName, string? Platform, string? IpAddress, string? UserAgent);

public sealed record RegisterCommand(string PhoneNumber, string Password, DeviceContext? DeviceContext) : ICommand<RegisterResult>;
public sealed record AuthenticatedUserContextResult(Guid UserId, string? PhoneNumber, string? MaskedPhoneNumber);
public sealed record RegisterResult(Guid UserId, string Status, string RegistrationStatus, DateTimeOffset RegisteredAtUtc, AuthenticatedUserContextResult? Identity);

public sealed record PasswordLoginCommand(string PhoneNumber, string Password, DeviceContext? DeviceContext) : ICommand<PasswordLoginResult>;
public sealed record PasswordLoginResult(string State, Guid? UserId, TokenPairResult? Tokens, MfaChallengeResult? Mfa, string? ReasonCode, AuthenticatedUserContextResult? Identity);
public sealed record TokenPairResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc, string? SessionId, string TokenType = "Bearer");
public sealed record MfaChallengeResult(string ChallengeId, string Method, DateTimeOffset ExpiresAtUtc);

public sealed record RefreshSessionCommand(Guid UserId, string RefreshTokenReference, string? SessionId) : ICommand<TokenPairResult>;
public sealed record LogoutSessionCommand(Guid UserId, string SessionId, string ReasonCode) : ICommand<LogoutSessionResult>;
public sealed record LogoutSessionResult(string SessionId, string Status, DateTimeOffset RevokedAtUtc);

public sealed record BeginPasskeyRegistrationCommand(Guid UserId, string DisplayName, DeviceContext? DeviceContext) : ICommand<BeginPasskeyRegistrationResult>;
public sealed record BeginPasskeyRegistrationResult(string CeremonyId, JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishPasskeyRegistrationCommand(Guid UserId, string CeremonyId, JsonElement Credential, DeviceContext? DeviceContext) : ICommand<PasskeyStateResult>;
public sealed record PasskeyStateResult(string PasskeyId, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
public sealed record ListPasskeysQuery(Guid UserId) : IQuery<IReadOnlyCollection<PasskeySummaryResult>>;
public sealed record PasskeySummaryResult(string PasskeyId, string DisplayName, string DeviceName, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);
public sealed record RevokePasskeyCommand(Guid UserId, string PasskeyId, string ReasonCode) : ICommand<PasskeyStateResult>;
public sealed record BeginPasskeyLoginCommand(string? LoginIdentifier, DeviceContext? DeviceContext) : ICommand<BeginPasskeyLoginResult>;
public sealed record BeginPasskeyLoginResult(string CeremonyId, JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishPasskeyLoginCommand(string CeremonyId, JsonElement Credential, DeviceContext? DeviceContext) : ICommand<PasswordLoginResult>;
public sealed record BeginRegistrationPasskeyCommand(Guid UserId, string DisplayName, DeviceContext? DeviceContext) : ICommand<BeginRegistrationPasskeyResult>;
public sealed record BeginRegistrationPasskeyResult(Guid UserId, string CeremonyId, JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishRegistrationPasskeyCommand(Guid UserId, string CeremonyId, JsonElement Credential, DeviceContext? DeviceContext) : ICommand<FinishRegistrationPasskeyResult>;
public sealed record FinishRegistrationPasskeyResult(Guid UserId, string RegistrationStatus, DateTimeOffset CompletedAtUtc);

public sealed record BeginSmartOtpEnrollmentCommand(Guid UserId, string DeviceName, string Platform, string AppInstanceIdHash, string KeyAlgorithm, string CandidatePublicKeySpki, string CandidatePublicKeyThumbprint) : ICommand<BeginSmartOtpEnrollmentResult>;
public sealed record BeginSmartOtpEnrollmentResult(string EnrollmentId, string ServerChallenge, int ChallengeFormatVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, string Status);
public sealed record ConfirmSmartOtpEnrollmentCommand(Guid UserId, string EnrollmentId, string ClientNonce, string DeviceSignature) : ICommand<SmartOtpDeviceStateResult>;
public sealed record SmartOtpDeviceStateResult(string DeviceId, string DeviceKeyId, string Status, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
public sealed record StartStepUpCommand(Guid UserId, string DeviceId, string Purpose, string ExternalTransactionId, string TransactionDigest, DateTimeOffset ExpiresAtUtc) : ICommand<StepUpChallengeResult>;
public sealed record StepUpChallengeResult(string ChallengeId, string ExternalUserId, string DeviceId, string DeviceKeyId, string Purpose, string ExternalTransactionId, string TransactionDigest, DateTimeOffset ExpiresAtUtc, int CodeLength, int MaxAttempts);
public sealed record RevealStepUpCommand(Guid UserId, string ChallengeId, string DeviceId, string DeviceKeyId, string Purpose, string ExternalTransactionId, string TransactionDigest, string RevealRequestId, DateTimeOffset IssuedAtUtc, DateTimeOffset ProofExpiresAtUtc, string DeviceSignature) : ICommand<StepUpRevealResult>;
public sealed record StepUpRevealResult(string ChallengeId, string OtpCode, DateTimeOffset ExpiresAtUtc, int RevealCount, DateTimeOffset ReleasedAtUtc);
public sealed record VerifyStepUpCommand(Guid UserId, string ChallengeId, string DeviceId, string Purpose, string ExternalTransactionId, string TransactionDigest, string Otp) : ICommand<StepUpGrantResult>;
public sealed record StepUpGrantResult(string ChallengeId, string StepUpGrant, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record CompleteLoginSmartOtpCommand(Guid UserId, string ChallengeId, string DeviceId, string Purpose, string ExternalTransactionId, string TransactionDigest, string Otp, DeviceContext? DeviceContext) : ICommand<PasswordLoginResult>;
