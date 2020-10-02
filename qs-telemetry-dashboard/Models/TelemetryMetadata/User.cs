using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class User
	{
		internal Guid ID { get; set; }

		internal string UserID { get; set; }

		internal string UserDirectory { get; set; }

		internal User()
		{
		}
	}
}
