using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace Rumble.Platform.Config.Services;

public class SectionService : PlatformMongoService<Section>
{
	private readonly ApiService _apiService;
	public SectionService(ApiService apiService) : base("settings")
	{
		if (!Exists(DC2Service.GLOBAL_SETTING_NAME))
			Create(new Section(DC2Service.GLOBAL_SETTING_NAME, DC2Service.GLOBAL_SETTING_FRIENDLY_NAME));

		_apiService = apiService;
	}

	public void RemoveInactiveServices(long thresholdSeconds)
	{
		string[] ids = _collection
			.Find(filter: settings => true)
			.Project(Builders<Section>.Projection.Expression(settings => settings.ActiveClients))
			.ToList()
			.Where(list => list != null)
			.SelectMany(list => list)
			.Where(info => Timestamp.UnixTime - info.LastActivity > thresholdSeconds)
			.Select(info => info.ClientID)
			.ToArray();

		Section[] all = Find(settings => true);

		foreach (Section setting in all)
		{
			if (setting.ActiveClients == null)
				continue;

			string[] union = setting.ActiveClients.Select(client => client.ClientID).Union(ids).ToArray();
			foreach (string id in union)
				setting.Services.RemoveAll(service => service.DynamicConfigClientId == id);
			setting.ActiveClients?.RemoveAll(client => Timestamp.UnixTime - client.LastActivity > thresholdSeconds);

			Update(setting);
		}
	}

	public void LogActivity(DC2Service.DC2ClientInformation info)
	{
		Section dynamicConfigSection = FindByName(info.ServiceName);
		
		info.LastActivity = Timestamp.UnixTime;

		if (!dynamicConfigSection.ActiveClients.Any(client => client.ClientID == info.ClientID))
			dynamicConfigSection.ActiveClients.Add(info);
		else
			dynamicConfigSection.ActiveClients.First(client => client.ClientID == info.ClientID).LastActivity = info.LastActivity;
		Update(dynamicConfigSection);
	}

	public Section FindByName(string name) => _collection
		.Find(filter: settings => settings.Name == name)
		.FirstOrDefault()
		??	throw new PlatformException("Setting does not exist.", code: ErrorCode.MongoRecordNotFound); // TODO: Error code

	public bool Exists(string name) => _collection
		.CountDocuments(settings => settings.Name == name) > 0;

	public bool RemoveValue(string name, string key) => _collection
		.UpdateOne(
			filter: settings => settings.Name == name,
			update: Builders<Section>.Update.Unset($"{Section.DB_KEY_VALUES}.{key}")
		).ModifiedCount > 0;

	public string[] GetUpdateListeners() => _collection
		.Find(filter: settings => true)
		.Project(Builders<Section>.Projection.Expression(settings => settings.Services))
		.ToList()
		.SelectMany(list => list)
		.Select(service => service.RootIngress) // TODO: This needs to be updated with the container url
		.Where(value => !string.IsNullOrWhiteSpace(value))
		.ToArray();

	public string GenerateAdminToken(string serviceName) => PlatformEnvironment.IsLocal
		? "(local token generation unavailable)"
		: _apiService
			.Request(PlatformEnvironment.Url("/secured/token/generate"))
			.SetPayload(new GenericData
			{
				{ "aid", serviceName },
				{ "screenname", $"{serviceName} ({PlatformEnvironment.Deployment})" },
				{ "discriminator", 10_000 },
				{ "origin", $"{PlatformEnvironment.Name} ({PlatformEnvironment.Deployment})" },
				{ "email", "william.maynard@rumbleentertainment.com" },
				{ "days", 5_000 },
				{ "key", PlatformEnvironment.RumbleSecret }
			})
			.OnSuccess((_, _) =>
			{
				Log.Local(Owner.Will, "Admin token successfully generated.");
			})
			.OnFailure((_, response) =>
			{
				Log.Error(Owner.Will, "Admin token failed to generate.", data: new
				{
					Response = response
				});
			}).Post()
			.AsGenericData
			?.Optional<GenericData>("authorization")
			?.Optional<string>("token");
}