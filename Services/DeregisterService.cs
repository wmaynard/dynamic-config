using RCL.Logging;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Services;

public class DeregisterService : PlatformTimerService
{
	private readonly SettingsService _settingsService;

	public DeregisterService(SettingsService settingsService) : base(intervalMS: 259_200_000, startImmediately: true) // 3 days, in ms
		=> _settingsService = settingsService; 

	protected override void OnElapsed()
	{
		Log.Local(Owner.Will, "Removing old services");
		_settingsService.RemoveInactiveServices(thresholdSeconds: 5);
	}
}