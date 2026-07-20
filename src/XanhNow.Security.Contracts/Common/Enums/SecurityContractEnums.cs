namespace XanhNow.Security.Contracts.Common.Enums;

public enum ContractMaturity { Current, Planned, Composite }
public enum AuthenticationState { Completed, MfaRequired, PasskeyRequired, RecoveryRequired }
public enum SecurityStatusContract { Active, Locked, Disabled, RecoveryRequired }
public enum OperationStatusContract { Accepted, Running, Completed, Partial, Failed, FailedSafe, Cancelled }
public enum SessionStatusContract { Active, Revoked, Expired }
public enum DeviceTrustLevelContract { Unknown, New, Known, Trusted }
public enum GrantPurposeContract { Login, RefreshToken, PasswordChange, PhoneChange, PasskeyManagement, SmartOtpManagement, Recovery, TransactionStepUp }
public enum PolicyDecisionContract { Allow, Deny, StepUpRequired, ReviewRequired }
public enum DependencyStatusContract { Healthy, Degraded, Unhealthy }
