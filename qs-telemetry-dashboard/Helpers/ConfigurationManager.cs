using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class ConfigurationManager
	{
		internal bool HasConfiguration { get; private set; }
		internal TelemetryConfiguration Configuration { get; private set; }

		internal void SetConfig(string hostname, X509Certificate2 creds)
		{
			// use credentials to run impersonation and set config. save config to file
		}
	}
}
