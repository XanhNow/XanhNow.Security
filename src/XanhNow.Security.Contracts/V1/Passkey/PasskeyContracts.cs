using System.Text.Json;
using XanhNow.Security.Contracts.Common.Attributes;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Contracts.V1.Passkey;

public sealed record BeginPasskeyRegistrationRequest(string DisplayName, DeviceContextRequest? DeviceContext);
public sealed record BeginPasskeyRegistrationResponse(string CeremonyId, [property: SensitiveData("webauthn-public-key-options")] JsonElement PublicKeyOptions, DateTimeOffset ExpiresAtUtc);
public sealed record FinishPasskeyRegistrationRequest(string CeremonyId, [property: SensitiveData("webauthn-credential")] JsonElement Credential, DeviceContextRequest? DeviceContext);
public sealed record PasskeySummaryResponse(string PasskeyId, string DisplayName, string DeviceName, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);
public sealed record RenamePasskeyRequest(string DisplayName);
public sealed record RevokePasskeyRequest(string ReasonCode);
public sealed record PasskeyStateResponse(string PasskeyId, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
