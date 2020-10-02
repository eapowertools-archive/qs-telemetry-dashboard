using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class EngineInfo
	{
		internal string Hostname { get; set; }

		internal int WorkingSetMin { get; set; }

		internal int WorkingSetMax { get; set; }

		internal IDictionary<string, string> LogLevels { get; set; }

		internal EngineInfo()
		{
		}
	}
}
