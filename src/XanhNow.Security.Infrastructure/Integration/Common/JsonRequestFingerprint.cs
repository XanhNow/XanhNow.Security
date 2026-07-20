using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XanhNow.Security.Application.Abstractions.Idempotency;

namespace XanhNow.Security.Infrastructure.Integration.Common;

internal sealed class JsonRequestFingerprint : IRequestFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Compute(object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var json = JsonSerializer.Serialize(request, request.GetType(), JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
