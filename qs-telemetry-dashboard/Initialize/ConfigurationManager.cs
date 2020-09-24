using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace qs_telemetry_dashboard.Initialize
{
	internal class ConfigurationManager
	{
		private readonly Logger _logger;
		private readonly string WORKING_DIRECTORY;
		internal bool HasConfiguration { get; private set; }
		internal TelemetryConfiguration Configuration { get; private set; }

		internal ConfigurationManager(Logger logger, string pwd)
		{
			_logger = logger;
			WORKING_DIRECTORY = pwd;

			//try to get config, if you do, set config and set has config to true, else, false
		}

		internal void SetConfig(string hostname, X509Certificate2 creds)
		{
			// use credentials to run impersonation and set config. save config to file
		}
	}
}
