using System.ComponentModel;
using System.Text.Json.Serialization;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Models;

[TypeDescriptionProvider(typeof(FooTypeDescriptionProvider))]
public class Component : PlatformCollectionDocument
{
	public const string FRIENDLY_KEY_VERSION = "version";
	public const string FRIENDLY_KEY_COMMON_VERSION = "commonVersion";
	public const string FRIENDLY_KEY_LAST_UPDATED = "updated";
	public const string FRIENDLY_KEY_OWNER = "owner";
	public const string FRIENDLY_KEY_ENDPOINTS = "endpoints";
	public const string FRIENDLY_KEY_CONTROLLER_INFO = "controllers";
	
	[JsonInclude, JsonPropertyName(PlatformEnvironment.KEY_COMPONENT)]
	public string Name { get; set; }
	
	[JsonInclude, JsonPropertyName(PlatformEnvironment.KEY_REGISTRATION_NAME)]
	public string FriendlyName { get; set; }
	
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_VERSION)]
	public string Version { get; set; }
	
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_COMMON_VERSION)]
	public string CommonVersion { get; set; }
	
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LAST_UPDATED)]
	public long LastUpdated { get; set; }
	
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_OWNER)]
	public string Owner { get; set; }
	
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENDPOINTS)]
	public string[] Endpoints { get; set; }
	
	public ControllerInfo[] Controllers { get; set; }

	public GenericData Config { get; set; }

	public Component()
	{
		Config = new GenericData()
		{
			{ "AdminToken", null }
		};
	}

	protected override void Validate(out List<string> errors)
	{
		errors = new List<string>();
		
		if (string.IsNullOrWhiteSpace(Name))
			errors.Add("Missing ...");
		if (string.IsNullOrWhiteSpace(FriendlyName))
			errors.Add("Missing ...");
		if (string.IsNullOrWhiteSpace(Version))
			errors.Add("Missing ...");
		if (string.IsNullOrWhiteSpace(Owner))
			errors.Add("Missing ...");
	}
}