namespace XanhNow.Security.Domain.Users;

public enum UserSecurityStatus
{
    Active,
    Locked,
    RecoveryRequired,
    Disabled,
    Compromised
}
