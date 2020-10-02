using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models.TelemetryMetadata;

namespace qs_telemetry_dashboard.MetadataFetch
{
	internal class MetadataFetchRunner
	{
		internal static int Run()
		{
			// check to see if metadata binary exists
			string telemetryMetadataFile = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_METADATA_BINARY);
			TelemetryDashboardMain.Logger.Log("Trying to get metadata binary file: " + telemetryMetadataFile, LogLevel.Debug);
			TelemetryMetadata oldMeta;
			if (File.Exists(telemetryMetadataFile))
			{
				TelemetryDashboardMain.Logger.Log("Found metadata file, will load contents.", LogLevel.Info);
				Stream openFileStream = File.OpenRead(telemetryMetadataFile);
				BinaryFormatter deserializer = new BinaryFormatter();
				oldMeta = (TelemetryMetadata)deserializer.Deserialize(openFileStream);
				openFileStream.Close();
			}
			else
			{
				oldMeta = new TelemetryMetadata();
			}

			string outputPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER);
			throw new NotImplementedException();
		}

		internal static IList<App> GetApps()
		{


			return new List<App>();
		}
	}
}
