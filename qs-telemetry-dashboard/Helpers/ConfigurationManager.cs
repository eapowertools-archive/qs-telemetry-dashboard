using System;
using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class ConfigurationManager
	{
		internal static void SaveConfiguration(TelemetryConfiguration tConfig)
		{
			string configPath = FileLocationManager.GetConfigurationPath();

			// todo save tconfig object to config path
		}
	}
}
