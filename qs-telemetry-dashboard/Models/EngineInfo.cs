using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	internal enum EngineLogLevel
	{
		Off,
		Fatal,
		Error,
		Warning,
		Info,
		Debug
	}

	[Serializable]
	internal class EngineInfo
	{
		internal string Hostname { get; set; }
		internal int WorkingSetMin { get; set; }
		internal int WorkingSetMax { get; set; }

		internal EngineLogLevel PerformanceLogLevel { get; set; }
		internal EngineLogLevel QIXPerformanceLogLevel { get; set; }
		internal EngineLogLevel SessionLogLevel { get; set; }

		internal EngineInfo(string hostname, int workingSetMin, int workingSetMax, EngineLogLevel performanceLogLevel, EngineLogLevel qixPerformanceLogLevel, EngineLogLevel sessionLogLevel)
		{
			this.Hostname = hostname;
			this.WorkingSetMin = workingSetMin;
			this.WorkingSetMax = workingSetMax;

			this.PerformanceLogLevel = performanceLogLevel;
			this.QIXPerformanceLogLevel = qixPerformanceLogLevel;
			this.SessionLogLevel = sessionLogLevel;
		}
	}
}
