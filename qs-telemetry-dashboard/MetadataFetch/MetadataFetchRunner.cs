using System;
using System.IO;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;

namespace qs_telemetry_dashboard.MetadataFetch
{
	internal class MetadataFetchRunner
	{
		internal static int Run()
		{
			string outputPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER);
			throw new NotImplementedException();
		}
	}
}
