using MongoDB.Driver;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Config.Models;

namespace Rumble.Platform.Config.Services;

public class SettingsService : PlatformMongoService<Settings>
{
	public SettingsService() : base("settings")
	{
		if (!Exists(DC2Service.GLOBAL_SETTING_NAME))
			Create(new Settings(DC2Service.GLOBAL_SETTING_NAME, DC2Service.GLOBAL_SETTING_FRIENDLY_NAME));
	}

	public void RemoveInactiveServices(long thresholdSeconds)
	{
		string[] ids = _collection
			.Find(filter: settings => true)
			.Project(Builders<Settings>.Projection.Expression(settings => settings.ActiveClients))
			.ToList()
			.Where(list => list != null)
			.SelectMany(list => list)
			.Where(info => Timestamp.UnixTime - info.LastActivity > thresholdSeconds)
			.Select(info => info.ClientID)
			.ToArray();

		Settings[] all = Find(settings => true);

		foreach (Settings setting in all)
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
		Settings settings = FindByName(info.ServiceName);
		
		info.LastActivity = Timestamp.UnixTime;

		if (!settings.ActiveClients.Any(client => client.ClientID == info.ClientID))
			settings.ActiveClients.Add(info);
		else
			settings.ActiveClients.First(client => client.ClientID == info.ClientID).LastActivity = info.LastActivity;
		Update(settings);
	}

	public Settings FindByName(string name) => _collection
		.Find(filter: settings => settings.Name == name)
		.FirstOrDefault()
		??	throw new PlatformException("Setting does not exist.", code: ErrorCode.NotSpecified); // TODO: Error code

	public bool Exists(string name) => _collection
		.CountDocuments(settings => settings.Name == name) > 0;

	public bool RemoveValue(string name, string key) => _collection
		.UpdateOne(
			filter: settings => settings.Name == name,
			update: Builders<Settings>.Update.Unset($"{Settings.DB_KEY_VALUES}.{key}")
		).ModifiedCount > 0;

	public string[] GetUpdateListeners() => _collection
		.Find(filter: settings => true)
		.Project(Builders<Settings>.Projection.Expression(settings => settings.Services))
		.ToList()
		.SelectMany(list => list)
		.Select(service => service.RootIngress) // TODO: This needs to be updated with the container url
		.Where(value => !string.IsNullOrWhiteSpace(value))
		.ToArray();
}