using qs_telemetry_dashboard.Models.TelemetryMetadata.UnparsedObject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class TelemetryMetadata
	{
		internal DateTime ReloadTime { get; set; }

		internal IList<EngineInfo> EngineInfos { get; set; }

		internal IList<User> Users { get; set; }

		internal IDictionary<Guid, QRSApp> Apps { get; set; }

		internal TelemetryMetadata()
		{
			if (ReloadTime == null)
			{
				ReloadTime = DateTime.MinValue;
			}
			EngineInfos = new List<EngineInfo>();
			Users = new List<User>();
			Apps = new Dictionary<Guid, QRSApp>();
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
			unparsedSheets.ToList().ForEach(sheet =>
			{
				this.Apps[sheet.AppID].Sheets.Add(sheet.ID, new QRSSheet(sheet.EngineObjectID, sheet.Name, sheet.ModifiedDateTime, sheet.OwnerID, sheet.Published, sheet.Approved));
			});
		}

		internal void PopulateFromCachedMetadata(TelemetryMetadata oldMeta)
		{
			if (oldMeta.Apps.Count == 0)
			{
				return;
			}

			IList<Guid> newAppKeys = this.Apps.Keys.ToList();

			foreach(Guid key in newAppKeys)
			{
				QRSApp oldApp;
				if (oldMeta.Apps.TryGetValue(key, out oldApp))
				{
					if (this.Apps[key] == oldApp)
					{
						oldApp.VisualizationUpdateNeeded = false;
						this.Apps[key] = oldApp;
					}
				}
			}
		}
	}
}
