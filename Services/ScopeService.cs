using Rumble.Platform.Common.Enums;
using Rumble.Platform.Config;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Config.Services;

public class ScopeService : MinqService<Section>
{
    private readonly ApiService _api;
    
    public ScopeService(ApiService api) : base("settings")
    {
        _api = api;
        
        EnsureExists(DynamicConfig.GLOBAL_SETTING_NAME);
        EnsureExists(DynamicConfig.COMMON_SETTING_NAME);
        EnsureExists(DynamicConfig.CLIENT_SETTING_NAME);
        EnsureExists(DynamicConfig.SERVER_SETTING_NAME);
        
        mongo.DefineIndex(builder => builder
            .Add(section => section.Name)
            .EnforceUniqueConstraint()
        );
    }

    private void EnsureExists(string sectionName) => mongo
        .Where(query => query.EqualTo(section => section.Name, sectionName))
        .Upsert(update => update
            .Set(section => section.UpdatedOn, Timestamp.Now)
            .SetOnInsert(section => section.Data, new Dictionary<string, SettingsValue>())
        );

    public Section[] List() => mongo
        .All()
        .ToArray();

    public Section FindByName(string scope) => mongo.FirstOrDefault(query => query.EqualTo(section => section.Name, scope))
        ?? throw new SectionMissingException(scope);

    public Section UpsertByName(string scope, string friendlyName) => mongo
        .Where(query => query.EqualTo(section => section.Name, scope))
        .Upsert(update => update
            .Set(section => section.UpdatedOn, Timestamp.Now)
            .SetOnInsert(section => section.Data, new Dictionary<string, SettingsValue>())
            .SetOnInsert(section => section.FriendlyName, friendlyName)
        );

    public bool Exists(string name) => mongo.Count(query => query.EqualTo(section => section.Name, name)) > 0;

    public bool RemoveValue(string scope, string key)
    {
        Section section = FindByName(scope);
        if (!section.Data.Remove(key))
            throw new PlatformException("No values modified.");
        
        Update(section);
        return true;
    }

    public string GenerateAdminToken(string scope) => _api
        .Request(PlatformEnvironment.Url("/secured/token/generate"))
        .SetPayload(new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, scope },
            { TokenInfo.FRIENDLY_KEY_SCREENNAME, $"{scope} ({PlatformEnvironment.Deployment})" },
            { TokenInfo.FRIENDLY_KEY_DISCRIMINATOR, 10_000 },
            { TokenInfo.FRIENDLY_KEY_EMAIL_ADDRESS, "william.maynard@rumbleentertainment.com" },
            { "days", 5_000 },
            { "key", PlatformEnvironment.RumbleSecret },
            { "origin", $"{PlatformEnvironment.Name} ({PlatformEnvironment.Deployment})" }
        })
        .OnSuccess(_ => Log.Local(Owner.Will, "Admin token successfully generated.", emphasis: Log.LogType.WARN))
        .OnFailure(response => Log.Error(Owner.Will, "Admin token failed to generate.", data: new
        {
            Response = response
        }))
        .Post()
        ?.Optional<RumbleJson>("authorization")
        ?.Optional<string>("token");
}