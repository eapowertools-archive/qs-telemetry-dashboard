using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class ConfigurationManager
	{
		private readonly string WORKING_DIRECTORY;
		internal bool HasConfiguration { get; private set; }
		internal TelemetryConfiguration Configuration { get; private set; }

		internal ConfigurationManager(string pwd)
		{
			WORKING_DIRECTORY = pwd;

			//try to get config, if you do, set config and set has config to true, else, false
		}

		internal void SetConfig(string hostname, X509Certificate2 creds)
		{
			// use credentials to run impersonation and set config. save config to file
		}
	}
}
