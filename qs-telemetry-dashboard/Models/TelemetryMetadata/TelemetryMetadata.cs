using qs_telemetry_dashboard.Models.TelemetryMetadata.UnparsedObject;
using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class TelemetryMetadata
	{
		internal DateTime ReloadTime { get; set; }

		internal IList<EngineInfo> EngineInfos { get; set; }

		internal IList<User> Users { get; set; }

		internal IDictionary<Guid, App> Apps { get; set; }

		internal TelemetryMetadata()
		{
			if (ReloadTime == null)
			{
				ReloadTime = DateTime.MinValue;
			}
			EngineInfos = new List<EngineInfo>();
			Users = new List<User>();
			Apps = new Dictionary<Guid, App>();
		}

		internal TelemetryMetadata(bool updateTime) : this()
		{
			if (updateTime)
			{
				ReloadTime = DateTime.Now;
			}
		}

		internal void ParseSheets(IList<UnparsedSheet> unparsedSheets)
		{
			//throw new NotImplementedException();
		}
	}
}
