using System.Text.Json.Serialization;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Models;

public class KeyValueComment : PlatformDataModel
{
    public const string FRIENDLY_KEY_COMMENT = "comment";
    public const string FRIENDLY_KEY_KEY = "key";
    public const string FRIENDLY_KEY_VALUE = "value";
    
    [JsonPropertyName(FRIENDLY_KEY_KEY)]
    public string Key { get; set; }
    
    [JsonPropertyName(FRIENDLY_KEY_VALUE)]
    public string Value { get; set; }
    
    [JsonPropertyName(FRIENDLY_KEY_COMMENT)]
    public string Comment { get; set; }
}