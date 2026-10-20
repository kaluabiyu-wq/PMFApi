using System.Text.Json.Serialization;

namespace PmfApi.Domain.Entities;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountStatus
{
    Active,
    Suspended,
    Inactive,
    Deleted,
}