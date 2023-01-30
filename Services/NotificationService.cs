using RCL.Logging;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Services;

public class NotificationService : QueueService<NotificationService.Data>
{
    private readonly ApiService _apiService;
    private readonly DynamicConfig _dynamicConfig;
    private readonly SectionService _sectionService;
    private string AdminToken => _dynamicConfig?.ProjectValues.Optional<string>(Section.FRIENDLY_KEY_ADMIN_TOKEN);

    public NotificationService(ApiService apiService, DynamicConfig dynamicConfig, SectionService sectionService) : base("notifications", intervalMs: 5_000, primaryNodeTaskCount: 50, secondaryNodeTaskCount: 0)
    {
        _apiService = apiService;
        _dynamicConfig = dynamicConfig;
        _sectionService = sectionService;
    }

    protected override void OnTasksCompleted(Data[] data)
    {
    }

    protected override void PrimaryNodeWork()
    {
        
    }

    protected override void ProcessTask(Data data)
    {
        // TODO: Replace this intended functionality with MINQ
        // Log.Local(Owner.Will, $"Sending an update out to {data.Url}");
        // _apiService
        //     .Request(PlatformEnvironment.Url(data.Url + "/refresh"))
        //     .OnFailure(response =>
        //     {
        //         Log.Local(Owner.Will, $"Couldn't refresh dynamic config: {response.RequestUrl}.", emphasis: Log.LogType.WARN);
        //     })
        //     .AddAuthorization(AdminToken)
        //     .Patch();
    }

    public void QueueNotifications()
    {
        string[] urls = _sectionService.GetUpdateListeners();
        
        Log.Local(Owner.Will, $"Enqueueing {urls.Length} notifications");
        foreach (string url in urls)
            CreateTask(new Data
            {
                Url = url
            });
    }
    
    public class Data : PlatformCollectionDocument
    {
        public string Url { get; set; }
    }
}

