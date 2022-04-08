using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Config;

public class Startup : PlatformStartup
{
	public void ConfigureServices(IServiceCollection services) => base.ConfigureServices(services, Owner.Will, 30_000, 60_000, 90_000);
}