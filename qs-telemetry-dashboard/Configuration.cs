using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace qs_telemetry_dashboard
{
	[Serializable]
	internal class Configuration
	{
		internal string UserDirectory { get; set; }
		internal string UserName { get; set; }
		internal SecureString Password { get; set; }
	}
}
