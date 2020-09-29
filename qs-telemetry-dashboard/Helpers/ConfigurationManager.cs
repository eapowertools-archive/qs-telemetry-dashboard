using System;
using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class ConfigurationManager
	{
		internal static bool HasConfiguration
		{
			get
			{
				return true;
			}
		}

		internal static TelemetryConfiguration GetConfiguration(bool isInShare)
		{

			return new TelemetryConfiguration();
		}

		internal static void SaveConfiguration(TelemetryConfiguration tConfig)
		{
			string configPath = FileLocationManager.GetTelemetrySharePath();

			// todo save tconfig object to config path
			// overwrite file in place
		}
	}
}
