using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Config.Models;
using Rumble.Platform.Config.Services;

namespace Rumble.Platform.Config.Controllers;

[Route("config/settings")]
public class SettingsController : PlatformController
{
#pragma warning disable
	private readonly SettingsService _settingsService;
#pragma warning restore
	
	[HttpGet]
	public ActionResult Get()
	{
		string name = Optional<string>("name");

		DC2Service.DC2ClientInformation info = Optional<DC2Service.DC2ClientInformation>("client");
		
		GenericData output = new GenericData();

		Settings[] settings = name == null
			? _settingsService.List().ToArray()
			: new[] { _settingsService.FindByName(name) };

		foreach (Settings s in settings)
		{
			output[s.Name] = s.Data;
			s.Data[Settings.FRIENDLY_KEY_ADMIN_TOKEN] ??= new SettingsValue(s.AdminToken, "Admin token");
		}

		if (info != null)
			_settingsService.LogActivity(info);
		
		return Ok(output);
	}

	[HttpPost, Route("new")]
	public ActionResult Create()
	{
		string name = Require<string>("name");
		string friendlyName = Require<string>("friendlyName");

		if (_settingsService.Exists(name))
			throw new PlatformException("Project already exists.", code: ErrorCode.NotSpecified); // TODO: Error code

		_settingsService.Create(new Settings(name, friendlyName));
		
		return Ok();
	}

	[HttpPatch, Route("update")]
	public async Task<ActionResult> Update()
	{
		string name = Optional<string>("name");
		string key = Require<string>("key");
		object value = Require("value");
		string comment = Require<string>("comment");
		
		Settings settings = _settingsService.FindByName(name);
		settings.Data[key] = new SettingsValue(value, comment);
		
		_settingsService.Update(settings);

		string[] urls = _settingsService.GetUpdateListeners();
		string adminToken = _dc2Service.ProjectValues.Optional<string>(Settings.FRIENDLY_KEY_ADMIN_TOKEN); // TODO: Make DC2Service accessor properties for admin token
		
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

	[HttpDelete, Route("value")]
	public ActionResult DeleteKey()
	{
		string name = Require<string>("name");
		string key = Require<string>("key");

		string message = _settingsService.RemoveValue(name, key)
			? $"'{name}.{key}' removed."
			: "No records were modified.";

		return Ok(new
		{
			Message = message
		});
	}
}