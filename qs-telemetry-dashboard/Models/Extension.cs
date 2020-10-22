using System;

namespace qs_telemetry_dashboard.Models
{
	internal class Extension
	{
		internal Guid ID { get; set; }

		internal DateTime CreatedDate { get; set; }

		internal string Name { get; set; }

		internal Guid OwnerID { get; set; }

		internal bool DashboardBundle { get; set; }

		internal bool VisualizationBundle { get; set; }

		internal Extension(Guid id, DateTime createdDate, string name, Guid ownerID, bool dashboardBundle, bool visualizationBundle)
		{
			this.ID = id;
			this.CreatedDate = createdDate;
			this.Name = name;
			this.OwnerID = ownerID;
			this.DashboardBundle = dashboardBundle;
			this.VisualizationBundle = visualizationBundle;
		}
	}
}