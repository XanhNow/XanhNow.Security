using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Users.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Users;

public sealed class SecurityUser : AggregateRoot<Guid>
{
    private SecurityUser()
    {
    }

    private SecurityUser(Guid userId, DateTimeOffset createdAt) : base(Guard.NotEmpty(userId, nameof(userId)))
    {
        Status = UserSecurityStatus.Active;
        RegistrationStatus = UserRegistrationStatus.Completed;
        RiskLevel = RiskLevel.Low;
        PasswordRegisteredAt = createdAt;
        PasskeyRegisteredAt = createdAt;
        RegistrationCompletedAt = createdAt;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public UserSecurityStatus Status { get; private set; }
    public UserRegistrationStatus RegistrationStatus { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public DateTimeOffset? PasswordRegisteredAt { get; private set; }
    public DateTimeOffset? PasskeyRegisteredAt { get; private set; }
    public DateTimeOffset? RegistrationCompletedAt { get; private set; }
    public string? RegistrationDeviceId { get; private set; }
    public string? RegistrationPhoneNumberHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public ReasonCode? LastReason { get; private set; }

    public static SecurityUser Create(Guid userId, DateTimeOffset createdAt) => new(userId, createdAt);

    public static SecurityUser CreatePendingPasskey(Guid userId, DateTimeOffset passwordRegisteredAt, string? registrationDeviceId = null, string? registrationPhoneNumberHash = null)
    {
        var user = new SecurityUser(userId, passwordRegisteredAt)
        {
            RegistrationStatus = UserRegistrationStatus.PendingPasskey,
            PasswordRegisteredAt = passwordRegisteredAt,
            PasskeyRegisteredAt = null,
            RegistrationCompletedAt = null,
            RegistrationDeviceId = NormalizeOptional(registrationDeviceId),
            RegistrationPhoneNumberHash = NormalizeOptional(registrationPhoneNumberHash)
        };

        return user;
    }

    public bool IsRegistrationCompleted => RegistrationStatus == UserRegistrationStatus.Completed;

    public void CompletePasskeyRegistration(DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        if (RegistrationStatus == UserRegistrationStatus.Completed)
        {
            return;
        }

        Guard.True(PasswordRegisteredAt is not null, "password_registration_required", "Password registration must be completed before passkey registration.");
        PasskeyRegisteredAt = occurredAt;
        RegistrationCompletedAt = occurredAt;
        RegistrationStatus = UserRegistrationStatus.Completed;
        UpdatedAt = occurredAt;
    }

    public void BindRegistrationDevice(string deviceId, string phoneNumberHash, DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        var normalizedDeviceId = Guard.NotBlank(deviceId, nameof(deviceId), 128);
        var normalizedPhoneNumberHash = Guard.NotBlank(phoneNumberHash, nameof(phoneNumberHash), 128);

        if (RegistrationDeviceId is not null && !string.Equals(RegistrationDeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            throw new DomainException("registration_device_conflict", "Security user is already bound to another registration device.");
        }

        if (RegistrationPhoneNumberHash is not null && !string.Equals(RegistrationPhoneNumberHash, normalizedPhoneNumberHash, StringComparison.Ordinal))
        {
            throw new DomainException("registration_phone_conflict", "Security user is already bound to another registration phone number.");
        }

        RegistrationDeviceId = normalizedDeviceId;
        RegistrationPhoneNumberHash = normalizedPhoneNumberHash;
        UpdatedAt = occurredAt;
    }

    public void Lock(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Locked, reason, occurredAt);

    public void RequireRecovery(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.RecoveryRequired, reason, occurredAt);

    public void MarkCompromised(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Compromised, reason, occurredAt);

    public void Disable(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Disabled, reason, occurredAt);

    public void SetRisk(RiskLevel riskLevel, ReasonCode reason, DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        RiskLevel = riskLevel;
        LastReason = reason;
        UpdatedAt = occurredAt;
    }

    private void ChangeStatus(UserSecurityStatus target, ReasonCode reason, DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        if (Status == target)
        {
            return;
        }

        var previous = Status;
        Status = target;
        LastReason = reason;
        UpdatedAt = occurredAt;
        Raise(new SecurityUserStatusChangedDomainEvent(Id, previous, target, reason, occurredAt));
    }

    private void EnsureNotDisabled()
    {
        if (Status == UserSecurityStatus.Disabled)
        {
            throw new DomainException("security_user_disabled_terminal", "Disabled security user is terminal.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
