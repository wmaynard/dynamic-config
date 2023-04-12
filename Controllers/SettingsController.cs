using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
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
#pragma warning disable
    private readonly SectionService      _sectionService;
    private readonly NotificationService _notificationService;
    #pragma warning restore

    [HttpGet, Route("all"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult GetPortalData() => Ok(new RumbleJson
    {
        { DynamicConfig.API_KEY_SECTIONS, _sectionService.List() }
    });

    [HttpGet, RequireAuth(AuthType.RUMBLE_KEYS), HealthMonitor(weight: 1)]
    public ActionResult Get()
    {
        string name = Optional<string>("name");

        DynamicConfig.DC2ClientInformation info = Optional<DynamicConfig.DC2ClientInformation>("client");

        RumbleJson output = new RumbleJson();

        Section[] settings = name == null
            ? _sectionService.List().ToArray()
            : new[] { _sectionService.FindByName(name) };

        foreach (Section s in settings)
        {
            output[s.Name] = s.ClientData;

            if (s.Data.ContainsKey(Section.FRIENDLY_KEY_ADMIN_TOKEN) || s.Name == Audience.GameClient.GetDisplayName())
                continue;

            string token = s.AdminToken ?? _sectionService.GenerateAdminToken(s.Name);

            s.Data[Section.FRIENDLY_KEY_ADMIN_TOKEN] = new SettingsValue(token, "Auto-generated admin token");
            _sectionService.Update(s);
        }

        // if (info != null)
        //     _sectionService.LogActivity(info);

        return Ok(output);
    }

    [HttpPost, Route("new"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult Create()
    {
        string name = Require<string>("name");
        string friendlyName = Require<string>("friendlyName");

        if (_sectionService.Exists(name))
            return Ok("Section already exists", ErrorCode.Unnecessary);
        // throw new PlatformException("Project already exists.", code: ErrorCode.Unnecessary);

        _sectionService.Create(new Section(name, friendlyName));
        Log.Info(Owner.Default, "New dynamic-config project created", data: new
        {
            Name = name
        });

        return Ok();
    }

    [HttpPatch, Route("update"), RequireAuth(AuthType.ADMIN_TOKEN), HealthMonitor(weight: 5)]
    public async Task<ActionResult> Update()
    {
        string name = Optional<string>("name");
        string key = Require<string>("key");
        string value = Require<string>("value");
        string comment = Require<string>("comment");

        if (value is JsonElement)
            throw new PlatformException("'value' cannot be a JSON object.  It must be a primitive type.", code: ErrorCode.InvalidDataType);

        Section dynamicConfigSection = _sectionService.FindByName(name);
        dynamicConfigSection.Data[key] = new SettingsValue(value, comment);

        _sectionService.Update(dynamicConfigSection);

        string adminToken = DynamicConfig.ProjectValues.Optional<string>(Section.FRIENDLY_KEY_ADMIN_TOKEN); // TODO: Make DynamicConfig accessor properties for admin token

        _notificationService.QueueNotifications();
        return Ok();
    }

    [HttpDelete, Route("value"), RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult DeleteKey()
    {
        string name = Require<string>("name");
        string key = Require<string>("key");

        string message = _sectionService.RemoveValue(name, key)
            ? $"'{name}.{key}' removed."
            : "No records were modified.";

        return Ok(new
        {
            Message = message
        });
    }

    [HttpGet, Route("validate"), RequireAuth(AuthType.RUMBLE_KEYS)]
    public ActionResult ValidateSections()
    {
        RumbleJson errors = new RumbleJson();
        foreach (string id in _sectionService.GetAllIds())
            try
            {
                _sectionService.Get(id);
            }
            catch (Exception e)
            {
                errors[id] = e.Message;
            }

        return errors.Any()
            ? Problem(errors)
            : Ok();
    }
}