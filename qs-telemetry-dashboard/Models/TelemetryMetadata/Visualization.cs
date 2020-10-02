using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class Visualization
	{
		internal string SheetObjectID { get; set; }

		internal string ObjectName { get; set; }

		internal string ObjectType { get; set; }

		internal Visualization()
		{
		}
	}
}
