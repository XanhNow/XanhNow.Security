using System.Text.Json;
using System.Text.Json.Serialization;

namespace XanhNow.Security.Infrastructure.Persistence.Converters;

internal static class PersistenceJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException($"Cannot deserialize {typeof(T).Name} from persistence JSON.");
}
