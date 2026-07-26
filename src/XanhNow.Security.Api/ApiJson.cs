using System.Text.Json;
using System.Text.Json.Serialization;

namespace XanhNow.Security.Api;

public static class ApiJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
