using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace qs_telemetry_dashboard.Models
{
	[Serializable]
	internal class TelemetryConfiguration
	{
		internal string Hostname { get; set; }

		internal X509Certificate2 QlikClientCertificate { get; set; }
	}
}
