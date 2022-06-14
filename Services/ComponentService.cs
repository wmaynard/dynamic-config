using Rumble.Platform.Config.Models;
using Rumble.Platform.Common.Services;

namespace Rumble.Platform.Config.Services;

public class ComponentService : PlatformMongoService<Component>
{
	public ComponentService() : base("components") { }
	
	
}