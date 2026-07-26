using XanhNow.Security.Application.Common.Requests;

namespace XanhNow.Security.Application.Core;

public sealed record PolicyDecisionResultDto(Guid DecisionId, string PolicyCode, string Decision, string ReasonCode, int PolicyVersion, DateTimeOffset EvaluatedAtUtc);
public sealed record ProtectedGrantResult(Guid GrantId, string Grant, string GrantType, string Audience, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record RecoveryWorkflowResult(Guid RecoveryCaseId, Guid UserId, string Status, string CurrentStep, DateTimeOffset UpdatedAtUtc);
public sealed record LoginMfaChallengeResult(Guid UserId, string ChallengeId, string Method, string Purpose, DateTimeOffset ExpiresAtUtc);

public sealed record EvaluateSecurityPolicyCommand(Guid? UserId, string PolicyCode, string Purpose, string AssuranceLevel, IReadOnlyDictionary<string, string> Context) : ICommand<PolicyDecisionResultDto>;
public sealed record IssueAuthGrantCommand(Guid UserId, string Audience, string Purpose, string AssuranceLevel, TimeSpan Lifetime) : ICommand<ProtectedGrantResult>;
public sealed record BeginLoginMfaCommand(Guid UserId, string LoginOperationId, string TransactionDigest) : ICommand<LoginMfaChallengeResult>;
public sealed record CompleteLoginMfaCommand(Guid UserId, string ChallengeId, string TotpCode, string Audience) : ICommand<ProtectedGrantResult>;
public sealed record CompletePasskeyLoginWithGrantCommand(string CeremonyId, string CredentialJson, string Audience) : ICommand<ProtectedGrantResult>;
public sealed record IssueTransactionStepUpGrantCommand(Guid UserId, string Audience, string Purpose, string TransactionId, string TransactionDigest, string CanonicalizationVersion, string ChallengeId, string TotpCode) : ICommand<ProtectedGrantResult>;
public sealed record ReportLostPhoneCommand(Guid UserId, string DeviceReference, string ReasonCode) : ICommand<RecoveryWorkflowResult>;
public sealed record StartAccountRecoveryCommand(Guid UserId, string ReasonCode) : ICommand<RecoveryWorkflowResult>;
public sealed record CompleteAccountRecoveryCommand(Guid UserId, Guid RecoveryCaseId, string ReasonCode) : ICommand<RecoveryWorkflowResult>;
public sealed record ProtectAccountFromTakeoverCommand(Guid UserId, string ReasonCode) : ICommand<AccountStateResult>;
public sealed record CompositeLockUserCommand(Guid UserId, string ReasonCode, string? Comment) : ICommand<AccountStateResult>;
public sealed record CompositeUnlockUserCommand(Guid UserId, string ReasonCode, string? Comment) : ICommand<AccountStateResult>;
public sealed record CompositeLogoutAllCommand(Guid UserId, string ReasonCode) : ICommand<LogoutAllSessionsResult>;
public sealed record ResumeRecoveryOperationsCommand(string WorkerInstanceId, int BatchSize) : ICommand<RecoveryWorkerResult>;
public sealed record RecoveryWorkerResult(string Status, int ResumedCount, DateTimeOffset CheckedAtUtc);
