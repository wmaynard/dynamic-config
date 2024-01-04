using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Config.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Controllers;

[Route("config")] //  RequireAuth(AuthType.RUMBLE_KEYS)]
public class TopController : PlatformController
{
    public const string  SECRET     = "1a8ae7a7-228c-4c1e-b5d1-3e41941f6910";
    private const string KEY_SECRET = "key";
    
#pragma warning disable
    private readonly ApiService     _apiService;
    private readonly ScopeService _scopes;
#pragma warning restore

    /// <summary>
    /// Calculates a diff between other DC environments.
    /// As Platform has no knowledge of other valid URLs, the environments to compare must be passed in.  Any number
    /// of environments are supported.
    /// </summary>
    /// <returns></returns>
    [HttpPatch, Route("diff"), NoAuth]
    public ActionResult ShowDiff()
    {
        string[] urls = Optional<string[]>("environments") ?? Array.Empty<string>();
        string filter = Optional<string>("filter");

        Section[] locals = _scopes.List().ToArray();

        if (!urls.Any())
        {
            EnforceSecretUsed();
            return Ok(new RumbleJson
            {
                { "sections", locals }
            });
        }

        if (Token is not { IsAdmin: true })
            throw new PlatformException("Admin token required.");

        Dictionary<string, Section[]> dict = new Dictionary<string, Section[]>
        {
            { PlatformEnvironment.ClusterUrl, locals }
        };

        List<string> warnings = new List<string>();
        foreach (string url in urls.Distinct())
            _apiService
                .Request(PlatformEnvironment.Url(url, "config/diff"))
                .SetPayload(new RumbleJson
                {
                    { KEY_SECRET, SECRET },
                    { "environments", Array.Empty<string>() }
                })
                .OnSuccess(response => dict[url] = response.Require<Section[]>("sections"))
                .OnFailure(response => warnings.Add($"Unable to retrieve config at '{url}'."))
                .Patch();

        return Ok(new RumbleJson
        {
            { "diff", Section.GetDiff(dict, filter) },
            { "warnings", warnings }
        });
}

    [HttpPost, Route("import"), NoAuth]
    public ActionResult MergeEnvironment()
    {
        EnforceSecretUsed();

        Section[] fromRequest = Require<Section[]>("sections");
        int deployment = Require<int>("deployment");
        int sections = 0;
        int values = 0;

        Section[] all = _scopes.List().ToArray();
        foreach (Section incoming in fromRequest.Where(req => req != null))
        {
            Section local = all.FirstOrDefault(a => a.Name == incoming.Name);
            if (local == null)
            {
                incoming.ResetId();

                foreach (KeyValuePair<string, SettingsValue> pair in incoming.Data)
                    pair.Value.Comment = $"[Imported from {deployment}] {pair.Value.Comment}";

                _scopes.Insert(incoming);
                sections++;
                values += incoming.Data.Count;

                continue;
            }

            local.Data ??= new Dictionary<string, SettingsValue>();

            bool changed = false;
            foreach (KeyValuePair<string, SettingsValue> pair in incoming.Data)
            {
                if (local.Data.ContainsKey(pair.Key) && local.Data[pair.Key]?.Value != null)
                    continue;

                SettingsValue value = pair.Value;
                value.Comment = $"[Imported from {deployment}] {value.Comment}";

                local.Data[pair.Key] = value;
                changed = true;
                values++;
            }

            if (!changed)
                continue;

            sections++;
            _scopes.Update(local);
        }

        return Ok(new RumbleJson
        {
            { "sectionsAffected", sections },
            { "valuesAdded", values }
        });
    }

    [HttpPost, Route("export"), RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult Export()
    {
        string url = Require<string>("envUrl");

        _apiService
            .Request(PlatformEnvironment.Url(url, "/config/import"))
            .SetPayload(new RumbleJson
            {
                { KEY_SECRET, SECRET },
                { "deployment", PlatformEnvironment.Deployment },
                { "sections", _scopes.List().Select(section => section.PrepareForExport()) }
            })
            .OnFailure(_ => Log.Local(Owner.Will, "Failed to merge dynamic config environments."))
            .Post(out RumbleJson json, out int code);

        return code.Between(200, 299)
            ? Ok(json)
            : Problem(json);
    }

    [HttpPatch, Route("register"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult Register()
    {
        string name = Require<string>(PlatformEnvironment.KEY_COMPONENT);
        string friendlyName = Require<string>(PlatformEnvironment.KEY_REGISTRATION_NAME);
        RegisteredService service = Require<RegisteredService>("service");
        service.LastUpdated = Timestamp.Now;

        if (string.IsNullOrWhiteSpace(name))
            throw new PlatformException($"{PlatformEnvironment.KEY_COMPONENT} not provided.");
        if (string.IsNullOrWhiteSpace(friendlyName))
            throw new PlatformException($"{PlatformEnvironment.KEY_REGISTRATION_NAME} not provided.");
        
        Section dynamicConfigSection = _scopes.UpsertByName(name, friendlyName);

        // dynamicConfigSection.Services ??= new List<RegisteredService>();
        // dynamicConfigSection.Services.Add(service);
        _scopes.Update(dynamicConfigSection);

        // TODO: Ping other versions of service to see if they're still up; if not, remove them.

        return Ok(new
        {
            Settings = dynamicConfigSection
        });
    }

    private void EnforceSecretUsed()
    {
        if (Require<string>(KEY_SECRET) != SECRET)
            throw new PlatformException("Unauthorized.");
    }
}