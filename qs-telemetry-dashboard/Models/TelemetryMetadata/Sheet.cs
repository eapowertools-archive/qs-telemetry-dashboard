using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class Sheet
	{
		internal Guid AppID { get; set; }

		internal string EngineObjectID { get; set; }

		internal string Name { get; set; }

		internal Guid OwnerID { get; set; }

		internal bool Published { get; set; }

		internal bool Approved { get; set; }

		internal Sheet()
		{
		}
	}
}
