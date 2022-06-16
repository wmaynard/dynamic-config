using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Models;

// TODO: Snapshots?  Restore?
public class Settings : PlatformCollectionDocument
{
	internal const string DB_KEY_SERVICES = "svc";
	internal const string DB_KEY_VALUES = "v";
	internal const string DB_KEY_ADMIN_TOKEN = "token";
	internal const string DB_KEY_NAME = "service";
	internal const string DB_KEY_FRIENDLY_NAME = "name";

	public const string FRIENDLY_KEY_SERVICES = "registeredServices";
	public const string FRIENDLY_KEY_VALUES = "values";
	public const string FRIENDLY_KEY_ADMIN_TOKEN = "adminToken";
	public const string FRIENDLY_KEY_NAME = "serviceName";
	public const string FRIENDLY_KEY_FRIENDLY_NAME = "friendlyName";

	[BsonElement(DB_KEY_SERVICES)] // TODO: Allow SimpleIndex to enforce unique constraints?
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SERVICES), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<RegisteredService> Services { get; init; }
	
	[BsonElement(DB_KEY_VALUES)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_VALUES), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public GenericData Values { get; init; }
	
	[BsonElement(DB_KEY_ADMIN_TOKEN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ADMIN_TOKEN), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AdminToken { get; init; }
	
	[BsonElement(DB_KEY_NAME)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NAME)]
	public string Name { get; set; }
	
	[BsonElement(DB_KEY_FRIENDLY_NAME)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FRIENDLY_NAME)]
	public string FriendlyName { get; set; }
	
	public List<DC2Service.DC2ClientInformation> ActiveClients { get; set; }

	[JsonConstructor]
	public Settings(){}

	public Settings(string name, string friendlyName)
	{
		Name = name;
		FriendlyName = friendlyName;
		Values = new GenericData();
		Services = new List<RegisteredService>();
		AdminToken = null;
		ActiveClients = new List<DC2Service.DC2ClientInformation>();
	}
}