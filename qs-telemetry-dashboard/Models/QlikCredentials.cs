using System.Security;

namespace qs_telemetry_dashboard.Models
{
	internal class QlikCredentials
	{
		internal string UserDirectory { get; set; }

		internal string UserName { get; set; }

		internal SecureString Password { get; set; }
	}
}
