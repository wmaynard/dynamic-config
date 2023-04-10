using RCL.Logging;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Services;

public class DeregisterService : PlatformTimerService
{
    private readonly SectionService _sectionService;

    public DeregisterService(SectionService sectionService) : base(intervalMS: 259_200_000, startImmediately: true) // 3 days, in ms
        => _sectionService = sectionService;

    protected override void OnElapsed()
    {
        Log.Local(Owner.Will, "Removing old services");
        _sectionService.RemoveInactiveServices(thresholdSeconds: 5);
    }
}