using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Config.Models;
using Rumble.Platform.Config.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.Config.Controllers;

[Route("config")] //  RequireAuth(AuthType.RUMBLE_KEYS)]
public class TopController : PlatformController
{
#pragma warning disable
	private readonly ComponentService _componentService;
	private readonly ApiService _apiService;
#pragma warning restore

	[HttpPatch, Route("register"), RequireAuth(AuthType.RUMBLE_KEYS)]
	public ActionResult Register()
	{
		GenericData data = Require<GenericData>("component");
		
		ControllerInfo[] info = data.Require<ControllerInfo[]>("controllers");
		Component c = Require<Component>("component");
		c.LastUpdated = Timestamp.UnixTime;

		// _componentService.Update(c, createIfNotFound: true);
		
		
		
		return Ok(new
		{
			Component = c
		});
	}
}