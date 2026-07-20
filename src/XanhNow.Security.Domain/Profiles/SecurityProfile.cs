using XanhNow.Security.Domain.Common;

namespace XanhNow.Security.Domain.Profiles;

public sealed class SecurityProfile : Entity<Guid>
{
    private SecurityProfile()
    {
    }

    private SecurityProfile(Guid userId, int passkeyCount, int smartOtpDeviceCount, bool passwordLoginEnabled, bool isStale, DateTimeOffset snapshotAt) : base(Guard.NotEmpty(userId, nameof(userId)))
    {
        Guard.True(passkeyCount >= 0, "passkey_count_invalid", "Passkey count cannot be negative.");
        Guard.True(smartOtpDeviceCount >= 0, "smart_otp_count_invalid", "Smart OTP device count cannot be negative.");
        PasskeyCount = passkeyCount;
        SmartOtpDeviceCount = smartOtpDeviceCount;
        PasswordLoginEnabled = passwordLoginEnabled;
        IsStale = isStale;
        SnapshotAt = snapshotAt;
    }

    public int PasskeyCount { get; private set; }
    public int SmartOtpDeviceCount { get; private set; }
    public bool PasswordLoginEnabled { get; private set; }
    public bool IsStale { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }

    public static SecurityProfile Create(Guid userId, int passkeyCount, int smartOtpDeviceCount, bool passwordLoginEnabled, DateTimeOffset snapshotAt)
        => new(userId, passkeyCount, smartOtpDeviceCount, passwordLoginEnabled, false, snapshotAt);

    public void ApplySnapshot(int passkeyCount, int smartOtpDeviceCount, bool passwordLoginEnabled, DateTimeOffset snapshotAt)
    {
        Guard.True(snapshotAt >= SnapshotAt, "profile_snapshot_stale", "Cannot apply older security profile snapshot.");
        Guard.True(passkeyCount >= 0, "passkey_count_invalid", "Passkey count cannot be negative.");
        Guard.True(smartOtpDeviceCount >= 0, "smart_otp_count_invalid", "Smart OTP device count cannot be negative.");
        PasskeyCount = passkeyCount;
        SmartOtpDeviceCount = smartOtpDeviceCount;
        PasswordLoginEnabled = passwordLoginEnabled;
        SnapshotAt = snapshotAt;
        IsStale = false;
    }

    public void MarkStale() => IsStale = true;
}
