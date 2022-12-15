using MongoDB.Driver;
using RCL.Logging;
using Rumble.Config;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models.Config;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace Rumble.Platform.Config.Services;

public class SectionService : PlatformMongoService<Section>
{
	private readonly ApiService _apiService;
	public SectionService(ApiService apiService) : base("settings")
	{
		if (!Exists(DynamicConfig.GLOBAL_SETTING_NAME))
			Create(new Section(DynamicConfig.GLOBAL_SETTING_NAME, DynamicConfig.GLOBAL_SETTING_FRIENDLY_NAME));
		if (!Exists(DynamicConfig.COMMON_SETTING_NAME))
			Create(new Section(DynamicConfig.COMMON_SETTING_NAME, DynamicConfig.COMMON_SETTING_FRIENDLY_NAME));
		if (!Exists(DynamicConfig.CLIENT_SETTING_NAME))
			Create(new Section(DynamicConfig.CLIENT_SETTING_NAME, DynamicConfig.CLIENT_SETTING_FRIENDLY_NAME));
		if (!Exists(DynamicConfig.SERVER_SETTING_NAME))
			Create(new Section(DynamicConfig.SERVER_SETTING_NAME, DynamicConfig.SERVER_SETTING_FRIENDLY_NAME));
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

	public void LogActivity(DynamicConfig.DC2ClientInformation info)
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
		??	throw new SectionMissingException(name);

	public bool Exists(string name) => _collection
		.CountDocuments(settings => settings.Name == name) > 0;

	public bool RemoveValue(string name, string key)
	{
		// Tried to do this with a single Mongo query; couldn't get it to cooperate within two hours, so now doing it in
		// 3 stages of Find -> Update locally -> Update mongo
		Section section = _collection
			.Find(Builders<Section>.Filter.Eq(section => section.Name, name))
			.FirstOrDefault()
			?? throw new PlatformException("Section not found.");

		if (!section.Data.Remove(key))
			throw new PlatformException("No values modified.");
		
		Update(section);
		return true;
	}

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
			.SetPayload(new RumbleJson
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
			.AsRumbleJson
			?.Optional<RumbleJson>("authorization")
			?.Optional<string>("token");

	public string[] GetAllIds() => _collection
		.Find(_ => true)
		.Project(Builders<Section>.Projection.Expression(section => section.Id))
		.ToList()
		.ToArray();

	public long DeleteAllExcept(string id) => _collection
		.DeleteMany(section => section.Id != id)
		.DeletedCount;
}