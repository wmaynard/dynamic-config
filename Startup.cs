using RCL.Logging;
using Rumble.Platform.Common;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Config;

public class Startup : PlatformStartup
{
	protected override PlatformOptions Configure(PlatformOptions options) => options
		.SetProjectOwner(Owner.Will)
		.SetPerformanceThresholds(warnMS: 30_000, errorMS: 60_000, criticalMS: 90_000)
		.SetRegistrationName("Dynamic Config")
		.SetLogglyThrottleThreshold(suppressAfter: 10, period: 1_800)
		.DisableServices(CommonService.Config)
		// .DisableFeatures(CommonFeature.ConsoleColorPrinting)
		.DisableFeatures(CommonFeature.ConsoleObjectPrinting)
	;
}