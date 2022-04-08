using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Config.Models;

public class Environment : PlatformCollectionDocument
{
	public int DeploymentNumber { get; init; }
	public string GameKey { get; init; }
	public string RumbleKey { get; init; }
	public string Name { get; init; }
	public List<Component> Components { get; init; }
	
	public Environment()
	{
		DeploymentNumber = -1;
	}
}

// Corresponds to RUMBLE_COMPONENT
public class Component : PlatformDataModel
{
	public string Name { get; init; }
	public string FriendlyName { get; init; }
	public GenericData Variables { get; init; }

	public Component()
	{
	}
}