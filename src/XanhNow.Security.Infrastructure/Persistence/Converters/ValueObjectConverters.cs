using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Infrastructure.Persistence.Converters;

internal static class ValueObjectConverters
{
    public static ValueConverter<AuditAction, string> AuditAction() => new(v => v.Value, v => Domain.ValueObjects.AuditAction.From(v));
    public static ValueConverter<GrantAudience, string> GrantAudience() => new(v => v.Value, v => Domain.ValueObjects.GrantAudience.From(v));
    public static ValueConverter<GrantPurpose, string> GrantPurpose() => new(v => v.Value, v => Domain.ValueObjects.GrantPurpose.From(v));
    public static ValueConverter<IdempotencyKey, string> IdempotencyKey() => new(v => v.Value, v => Domain.ValueObjects.IdempotencyKey.From(v));
    public static ValueConverter<JtiHash, string> JtiHash() => new(v => v.Value, v => Domain.ValueObjects.JtiHash.From(v));
    public static ValueConverter<OperationTypeCode, string> OperationTypeCode() => new(v => v.Value, v => Domain.ValueObjects.OperationTypeCode.From(v));
    public static ValueConverter<PolicyCode, string> PolicyCode() => new(v => v.Value, v => Domain.ValueObjects.PolicyCode.From(v));
    public static ValueConverter<ReasonCode, string> ReasonCode() => new(v => v.Value, v => Domain.ValueObjects.ReasonCode.From(v));
    public static ValueConverter<ReasonCode?, string?> NullableReasonCode() => new(v => v == null ? null : v.Value, v => v == null ? null : Domain.ValueObjects.ReasonCode.From(v));
    public static ValueConverter<TraceId, string> TraceId() => new(v => v.Value, v => Domain.ValueObjects.TraceId.From(v));
}
