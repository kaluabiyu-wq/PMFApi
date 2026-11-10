using System.Text.Json;
using System.Text.Json.Serialization;

namespace PmfApi.Tests.Support;

public static class Json
{
    
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
