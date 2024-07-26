using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.Config;

public class Startup : PlatformStartup
{
  protected override PlatformOptions ConfigureOptions(PlatformOptions options) => options
      .SetProjectOwner(Owner.Will)
      .SetPerformanceThresholds(warnMS: 30_000, errorMS: 60_000, criticalMS: 90_000)
      .SetRegistrationName("Dynamic Config")
      .SetTokenAudience(Audience.DynamicConfigService)
      .SetLogglyThrottleThreshold(suppressAfter: 10, period: 1_800)
      .DisableServices(CommonService.Config)
      .DisableFeatures(CommonFeature.ConsoleObjectPrinting);
}