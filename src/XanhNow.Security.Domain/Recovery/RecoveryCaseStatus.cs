namespace XanhNow.Security.Domain.Recovery;

public enum RecoveryCaseStatus
{
    Pending,
    VerifyingProof,
    ProtectingAccount,
    RevokingSessions,
    DisablingAuthenticators,
    RestoringAccess,
    Completed,
    Cancelled
}
