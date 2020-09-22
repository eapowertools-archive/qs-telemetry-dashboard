using System;
using System.Security;

namespace qs_telemetry_dashboard
{
	[Serializable]
	internal class TelemetryConfiguration
	{
		internal string UserDirectory { get; set; }
		internal string UserName { get; set; }
		internal SecureString Password { get; set; }
		internal string Hostname { get; set; }
	}
}
