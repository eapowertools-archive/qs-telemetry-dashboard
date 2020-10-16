using System;

namespace qs_telemetry_dashboard.Models
{
	[Serializable]
	internal class Visualization
	{
		internal string ObjectName { get; set; }

		internal string ObjectType { get; set; }

		internal Visualization(string objectName, string objectType)
		{
			ObjectName = objectName;
			ObjectType = objectType;
		}
	}
}
