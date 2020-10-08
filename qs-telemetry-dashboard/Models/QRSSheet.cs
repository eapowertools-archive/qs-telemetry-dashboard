using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class QRSSheet
	{
		internal string EngineObjectID { get; set; }

		internal string Name { get; set; }

		internal Guid OwnerID { get; set; }

		internal bool Published { get; set; }

		internal bool Approved { get; set; }

		internal IList<Visualization> Visualizations { get; set; }

		internal QRSSheet(string engineObjectId, string name, Guid ownerId, bool published, bool approved)
		{
			this.EngineObjectID = engineObjectId;
			this.Name = name;
			this.OwnerID = ownerId;
			this.Published = published;
			this.Approved = approved;

			Visualizations = new List<Visualization>();
		}

		internal void SetSheetsList(IList<Visualization> visualizations)
		{
			Visualizations = visualizations;
		}
	}
}
