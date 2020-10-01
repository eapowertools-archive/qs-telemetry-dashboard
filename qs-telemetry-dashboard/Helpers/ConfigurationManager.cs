using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
			string configPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_CONFIG_FILENAME);

			if (File.Exists(configPath))
			{
				File.Delete(configPath);
			}

			Stream SaveFileStream = File.Create(configPath);
			BinaryFormatter serializer = new BinaryFormatter();
			serializer.Serialize(SaveFileStream, tConfig);
			SaveFileStream.Close();
		}
	}
}
