using System.Text.Json.Serialization;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Models;

public class KeyValueComment : PlatformDataModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("value")]
    public string Value { get; set; }
    
    [JsonPropertyName("comment")]
    public string Comment { get; set; }
}