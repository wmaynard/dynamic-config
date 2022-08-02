using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Config.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.Config.Controllers;

[Route("config")] //  RequireAuth(AuthType.RUMBLE_KEYS)]
public class TopController : PlatformController
{
#pragma warning disable
	private readonly ApiService _apiService;
	private readonly SectionService _sectionService;
#pragma warning restore

	[HttpPatch, Route("register"), RequireAuth(AuthType.RUMBLE_KEYS)]
	public ActionResult Register()
	{
		string name = Require<string>(PlatformEnvironment.KEY_COMPONENT);
		string friendlyName = Require<string>(PlatformEnvironment.KEY_REGISTRATION_NAME);
		RegisteredService service = Require<RegisteredService>("service");
		service.LastUpdated = Timestamp.UnixTime;
		
		Section dynamicConfigSection = _sectionService.Exists(name)
			? _sectionService.FindByName(name)
			: _sectionService.Create(new Section(name, friendlyName));

		dynamicConfigSection.Services.Add(service);
		_sectionService.Update(dynamicConfigSection);
		
		// TODO: Ping other versions of service to see if they're still up; if not, remove them.

		return Ok(new
		{ 
			Settings = dynamicConfigSection
		});
	}

	protected override GenericData AdditionalHealthData => new GenericData
	{
		{ "AllDC2", _dc2Service.AllValues },
		{ "ProjectDC2", _dc2Service.ProjectValues },
		{ "GlobalDC2", _dc2Service.GlobalValues }
	};
}