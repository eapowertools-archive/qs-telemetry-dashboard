using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class Sheet
	{
		internal string EngineObjectID { get; set; }

		internal string Name { get; set; }

		internal Guid OwnerID { get; set; }

		internal bool Published { get; set; }

		internal bool Approved { get; set; }

		internal IList<Visualization> Visualizations { get; set; }

		internal Sheet()
		{
		}
	}
}
