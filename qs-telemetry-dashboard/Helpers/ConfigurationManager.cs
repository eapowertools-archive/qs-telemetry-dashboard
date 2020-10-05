using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using qs_telemetry_dashboard.Logging;
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

		internal static bool TryGetConfiguration(out TelemetryConfiguration telemetryDashboardConfig, string overridePath = null)
		{
			string configPath;
			if (string.IsNullOrEmpty(overridePath))
			{
				configPath = Path.Combine(FileLocationManager.WorkingDirectory, FileLocationManager.TELEMETRY_CONFIG_FILENAME);
			}
			else
			{
				configPath = overridePath;
			}
			TelemetryDashboardMain.Logger.Log("Trying to get config file: " + configPath, LogLevel.Debug);
			if (!File.Exists(configPath))
			{
				TelemetryDashboardMain.Logger.Log("Failed to find file: " + configPath, LogLevel.Debug);

				telemetryDashboardConfig = null;
				return false;
			}
			TelemetryDashboardMain.Logger.Log("File found. Loading config.", LogLevel.Debug);

			Stream openFileStream = File.OpenRead(configPath);
			BinaryFormatter deserializer = new BinaryFormatter();
			telemetryDashboardConfig = (TelemetryConfiguration)deserializer.Deserialize(openFileStream);
			openFileStream.Close();
			return true;
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
