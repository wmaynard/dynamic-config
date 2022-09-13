using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Config.Services;

namespace Rumble.Platform.Config.Controllers;

[Route("config/settings")]
public class SettingsController : PlatformController
{
#pragma warning disable
	private readonly SectionService _sectionService;
#pragma warning restore

	[HttpGet, Route("all"), RequireAuth(AuthType.RUMBLE_KEYS)]
	public ActionResult GetPortalData()
	{
		return Ok(new GenericData
		{
			{ DC2Service.API_KEY_SECTIONS, _sectionService.List() }
		});
	}
	
	[HttpGet, RequireAuth(AuthType.RUMBLE_KEYS)]
	public ActionResult Get()
	{
		string name = Optional<string>("name");

		DC2Service.DC2ClientInformation info = Optional<DC2Service.DC2ClientInformation>("client");
		
		GenericData output = new GenericData();

		Section[] settings = name == null
			? _sectionService.List().ToArray()
			: new[] { _sectionService.FindByName(name) };

		foreach (Section s in settings)
		{
			output[s.Name] = s.ClientData;
			
			if (s.Data.ContainsKey(Section.FRIENDLY_KEY_ADMIN_TOKEN))
				continue;
		
			string token = s.AdminToken ?? _sectionService.GenerateAdminToken(s.Name);
			
			s.Data[Section.FRIENDLY_KEY_ADMIN_TOKEN] = new SettingsValue(token, "Auto-generated admin token");
			_sectionService.Update(s);
		}

		if (info != null)
			_sectionService.LogActivity(info);

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

	[HttpPatch, Route("update"), RequireAuth(AuthType.ADMIN_TOKEN)]
	public async Task<ActionResult> Update()
	{
		string name = Optional<string>("name");
		string key = Require<string>("key");
		object value = Require("value");
		string comment = Require<string>("comment");

		if (value is JsonElement)
			throw new PlatformException("'value' cannot be a JSON object.  It must be a primitive type.", code: ErrorCode.InvalidDataType);
		
		Section dynamicConfigSection = _sectionService.FindByName(name);
		dynamicConfigSection.Data[key] = new SettingsValue(value, comment);

		_sectionService.Update(dynamicConfigSection);

		string[] urls = _sectionService.GetUpdateListeners();
		string adminToken = _dc2Service.ProjectValues.Optional<string>(Section.FRIENDLY_KEY_ADMIN_TOKEN); // TODO: Make DC2Service accessor properties for admin token
		
		// Try to get all subscribers to refresh their variables
		foreach (string url in urls)
			await _apiService
				.Request(PlatformEnvironment.Url(url + "/refresh"))
				.OnFailure((_, response) =>
				{
					Log.Info(Owner.Will, $"Couldn't refresh dynamic config.", data: new
					{
						Url = response.RequestUrl
					});
					// TODO: Deregister the service after enough failures.  This is likely a sign the service is no longer active.  Only do this if other services are active, though.
				})
				.AddAuthorization(adminToken)
				.PatchAsync();
		
		// TODO: remove conditional after testing
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
}