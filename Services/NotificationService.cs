using RCL.Logging;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Services;

public class NotificationService : QueueService<NotificationService.Data>
{
    private readonly ApiService _apiService;
    private readonly DC2Service _dc2Service;
    private readonly SectionService _sectionService;
    private string AdminToken => _dc2Service?.ProjectValues.Optional<string>(Section.FRIENDLY_KEY_ADMIN_TOKEN);

    public NotificationService(ApiService apiService, DC2Service dc2Service, SectionService sectionService) : base("notifications", intervalMs: 5_000, primaryNodeTaskCount: 50, secondaryNodeTaskCount: 0)
    {
        _apiService = apiService;
        _dc2Service = dc2Service;
        _sectionService = sectionService;
    }

    protected override void PrimaryNodeWork()
    {
        
    }

    protected override void ProcessTask(Data data)
    {
        Log.Local(Owner.Will, $"Sending an update out to {data.Url}");
        _apiService
            .Request(PlatformEnvironment.Url(data.Url + "/refresh"))
            .OnFailure(response =>
            {
                Log.Local(Owner.Will, $"Couldn't refresh dynamic config: {response.RequestUrl}.", emphasis: Log.LogType.WARN);
            })
            .AddAuthorization(AdminToken)
            .Patch();
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

