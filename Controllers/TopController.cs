using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RCL.Logging;
using Rumble.Platform.Config.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Controllers;

[Route("config")] //  RequireAuth(AuthType.RUMBLE_KEYS)]
public class TopController : PlatformController
{
	public const string SECRET = "1a8ae7a7-228c-4c1e-b5d1-3e41941f6910";
	private const string KEY_SECRET = "key";
	
	
#pragma warning disable
	private readonly ApiService _apiService;
	private readonly SectionService _sectionService;
#pragma warning restore
	
	[HttpPatch, Route("diff"), NoAuth]
	public ActionResult ShowDiff()
	{
		EnforceSecretUsed();
		
		string[] urls = Optional<string[]>("environments") ?? Array.Empty<string>();

		if (!urls.Any())
			return Ok(new RumbleJson
			{
				{ "sections", _sectionService.List() }
			});

		Dictionary<string, Section[]> environments = new Dictionary<string, Section[]>();
		
		foreach (string url in urls)
			_apiService
				.Request(PlatformEnvironment.Url(url, "config/diff"))
				.SetPayload(new RumbleJson
				{
					{ KEY_SECRET, SECRET },
					{ "environments", Array.Empty<string>() }
				})
				.OnSuccess(response =>
				{
					environments[url] = response.Require<Section[]>("sections");
				})
				.OnFailure(response => { })
				.Patch();
		
		

		return Ok();
	}
	
	[HttpPost, Route("import"), NoAuth]
	public ActionResult MergeEnvironment()
	{
		EnforceSecretUsed();
		
		Section[] fromRequest = Require<Section[]>("sections");
		int deployment = Require<int>("deployment");
		int sections = 0;
		int values = 0;
		
		Section[] all = _sectionService.List().ToArray();
		foreach (Section incoming in fromRequest.Where(req => req != null))
		{
			Section local = all.FirstOrDefault(a => a.Name == incoming.Name);
			if (local == null)
			{
				incoming.ResetId();
				
				foreach (KeyValuePair<string, SettingsValue> pair in incoming.Data)
					pair.Value.Comment = $"[Imported from {deployment}] {pair.Value.Comment}";
				
				_sectionService.Create(incoming);
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
			_sectionService.Update(local);
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
		
		RumbleJson payload = new RumbleJson
		{
			{ KEY_SECRET, SECRET },
			{ "deployment", PlatformEnvironment.Deployment },
			{ "sections", _sectionService.List().Select(section => section.PrepareForExport()) }
		};
		_apiService
			.Request(PlatformEnvironment.Url(url, "/config/import"))
			.SetPayload(payload)
			.OnFailure(response =>
			{
				Log.Local(Owner.Will, "Failed to merge dynamic config environments.");
			})
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
		service.LastUpdated = Timestamp.UnixTime;

		if (string.IsNullOrWhiteSpace(name))
			throw new PlatformException($"{PlatformEnvironment.KEY_COMPONENT} not provided.");
		if (string.IsNullOrWhiteSpace(friendlyName))
			throw new PlatformException($"{PlatformEnvironment.KEY_REGISTRATION_NAME} not provided.");

		Section dynamicConfigSection = _sectionService.Exists(name)
			? _sectionService.FindByName(name) ?? _sectionService.Create(new Section(name, friendlyName))
			: _sectionService.Create(new Section(name, friendlyName));

		dynamicConfigSection.Services ??= new List<RegisteredService>();
		dynamicConfigSection.Services.Add(service);
		_sectionService.Update(dynamicConfigSection);
		
		// TODO: Ping other versions of service to see if they're still up; if not, remove them.

		return Ok(new
		{ 
			Settings = dynamicConfigSection
		});
	}

	[HttpPost, Route("test")]
	public ActionResult Forward()
	{
		string url = Require<string>("url");

		_apiService
			.Request(url)
			.AddAuthorization(Token.Authorization)
			.AddRumbleKeys()
			.SetPayload(Body)
			.Get(out RumbleJson response);

		return Ok(response);
	}

	protected override RumbleJson AdditionalHealthData => new RumbleJson
	{
		{ "AllDC2", DynamicConfig.AllValues },
		{ "ProjectDC2", DynamicConfig.ProjectValues },
		{ "GlobalDC2", DynamicConfig.GlobalValues }
	};

	private void EnforceSecretUsed()
	{
		if (Require<string>(KEY_SECRET) != SECRET)
			throw new PlatformException("Unauthorized.");
	}
}