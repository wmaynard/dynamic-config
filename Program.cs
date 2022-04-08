using System;
using System.Linq;
using System.Reflection;
using Rumble.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Rumble.Config;

public class Program
{
	public static void Main(string[] args)
	{
		if (args.Contains("-version"))
		{
			AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
			Console.WriteLine($"{assembly.Name}:{assembly.Version}");
			return;
		}
		CreateHostBuilder(args).Build().Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}

/*
	ENVIRONMENT
		Url

*/