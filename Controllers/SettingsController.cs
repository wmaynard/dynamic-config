using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;
using RCL.Logging;
using Rumble.Platform.Config.Models;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Config.Services;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Controllers;

[Route("config/settings")]
public class SettingsController : PlatformController
{
    public const string FRIENDLY_KEY_SCOPE = "scope";
    public const string FRIENDLY_KEY_SCOPE_BACKCOMPAT = "name";
    
    #pragma warning disable
    private readonly ScopeService _sections;
    #pragma warning restore

    [HttpGet, Route("all"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult GetPortalData() => Ok(new RumbleJson
    {
        { DynamicConfig.API_KEY_SECTIONS, _sections.List() }
    });

    [HttpGet, RequireAuth(AuthType.RUMBLE_KEYS), HealthMonitor(weight: 1)]
    public ActionResult Get()
    {
        string name = GetScope(false);

        // DynamicConfig.DC2ClientInformation info = Optional<DynamicConfig.DC2ClientInformation>("client");

        RumbleJson output = new();

        Section[] settings = string.IsNullOrWhiteSpace(name)
            ? _sections.List().ToArray()
            : new[] { _sections.FindByName(name) };

        foreach (Section s in settings)
        {
            // The game client can't have an admin token.  If other sections don't have one, generate it now.
            if (!s.Data.ContainsKey(Section.FRIENDLY_KEY_ADMIN_TOKEN) && s.Name != Audience.GameClient.GetDisplayName())
            {
                string token = _sections.GenerateAdminToken(s.Name);
                s.Data[Section.FRIENDLY_KEY_ADMIN_TOKEN] = new SettingsValue(token, "Auto-generated admin token");
                _sections.Update(s);
            }
            
            output[s.Name] = s.ClientData;
        }

        return Ok(output);
    }

    [HttpPost, Route("new"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult Create()
    {
        string name = GetScope();
        string friendlyName = Require<string>("friendlyName");

        if (_sections.Exists(name))
            return Ok("Section already exists", ErrorCode.Unnecessary);
        // throw new PlatformException("Project already exists.", code: ErrorCode.Unnecessary);

        _sections.Insert(new Section(name, friendlyName));
        Log.Info(Owner.Default, "New dynamic-config project created", data: new
        {
            Name = name
        });

        return Ok();
    }

    [HttpPatch, Route("ensure"), RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult EnsureExists()
    {
        string name = GetScope();
        string key = Require<string>("key");
        string value = Optional<string>("value") ?? "";
        
        Section dynamicConfigSection = _sections.FindByName(name);
        if (dynamicConfigSection.Data.ContainsKey(key))
            return Ok();

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            throw new PlatformException("Key or name not provided.");
        
        dynamicConfigSection.Data[key] = new SettingsValue(value, "(newly added; needs comment)");
        _sections.Update(dynamicConfigSection);
        
        Log.Info(Owner.Will, "Created default value for DC variable.", data: new
        {
            Project = name,
            Key = key,
            DefaultValue = value,
            Author = Token?.AccountId
        });

        return Ok();
    }

    [HttpPatch, Route("update"), RequireAuth(AuthType.ADMIN_TOKEN), HealthMonitor(weight: 5)]
    public ActionResult Update()
    {
        string scope = GetScope();
        KeyValueComment[] updates = Require<KeyValueComment[]>("updates");

        if (!updates.Any())
            throw new PlatformException("No updates provided and config cannot be changed");
        
        if (updates.Any(trio => string.IsNullOrWhiteSpace(trio.Key)))
            throw new PlatformException("Unable to update dynamic config; at least one null or empty key was provided.");

        if (updates.DistinctBy(trio => trio.Key).Count() < updates.Length)
            throw new PlatformException("Unable to update dynamic config; at least one updated key was repeated.");

        Section section = _sections.FindByName(scope);

        int unchanged = 0;
        foreach (KeyValueComment trio in updates)
        {
            string value = trio.Value ?? "";
            string comment = trio.Comment ?? "";

            section.Data.TryGetValue(trio.Key, out SettingsValue db);

            if (db == null || db.Value != value || db.Comment != comment)
                section.Data[trio.Key] = new SettingsValue(value, comment);
            else
                unchanged++;
        }

        if (updates.Length == unchanged)
            throw new PlatformException("Update request had the same values as the database; no change was made.");

        _sections.Update(section);
        return Ok(section);
    }

    [HttpDelete, Route("value"), RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult DeleteKey()
    {
        string name = GetScope();
        string key = Require<string>("key");

        string message = _sections.RemoveValue(name, key)
            ? $"'{name}.{key}' removed."
            : "No records were modified.";

        return Ok(new
        {
            Message = message
        });
    }
    
    private string GetScope(bool required = true)
    {
        string output = Optional<string>(FRIENDLY_KEY_SCOPE) ?? Optional<string>(FRIENDLY_KEY_SCOPE_BACKCOMPAT);

        return required && string.IsNullOrWhiteSpace(output)
            ? throw new PlatformException($"'{FRIENDLY_KEY_SCOPE}' or '{FRIENDLY_KEY_SCOPE_BACKCOMPAT}' not provided.")
            : output;
    }
}