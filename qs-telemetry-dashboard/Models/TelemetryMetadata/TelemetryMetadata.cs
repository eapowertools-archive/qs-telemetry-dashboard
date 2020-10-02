using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class TelemetryMetadata
	{
		internal DateTime ReloadTime { get; set; }

		internal List<User> Users { get; set; }

		internal List<App> Apps { get; set; }

		public TelemetryMetadata()
		{
		}

		internal bool WriteToDisk()
		{
			// write all the data to disk.
			return true;
		}
	}
}
