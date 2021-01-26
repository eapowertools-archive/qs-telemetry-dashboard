using System;
using System.Collections.Generic;
using System.Linq;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models.UnparsedObject;

namespace qs_telemetry_dashboard.Models
{
	[Serializable]
	internal class TelemetryMetadata
	{
		internal DateTime ReloadTime { get; set; }

		internal IDictionary<Guid, QRSApp> Apps { get; set; }

		[field: NonSerialized()]
		internal string Version { get; set; }

		[field: NonSerialized()]
		internal string ReleaseLabel { get; set; }

		[field: NonSerialized()]
		internal IList<EngineInfo> EngineInfos { get; set; }

		[field: NonSerialized()]
		internal IList<User> Users { get; set; }

		[field: NonSerialized()]
		internal IList<ExtensionSchema> ExtensionSchemas { get; set; }

		[field: NonSerialized()]
		internal IList<Extension> Extensions { get; set; }

		internal TelemetryMetadata()
		{
			if (ReloadTime == null)
			{
				this.ReloadTime = DateTime.MinValue;
			}
			this.Apps = new Dictionary<Guid, QRSApp>();

			this.Version = "";
			this.ReleaseLabel = "";
			this.EngineInfos = new List<EngineInfo>();
			this.Users = new List<User>();
			this.ExtensionSchemas = new List<ExtensionSchema>();
			this.Extensions = new List<Extension>();
		}

		internal TelemetryMetadata(bool updateTime) : this()
		{
			if (updateTime)
			{
				this.ReloadTime = DateTime.Now;
			}
		}

		internal void ParseSheets(IList<UnparsedSheet> unparsedSheets)
		{
			unparsedSheets.ToList().ForEach(sheet =>
			{
				try
				{
					this.Apps[sheet.AppID].Sheets.Add(sheet.ID, new QRSSheet(sheet.EngineObjectID, sheet.Name, sheet.ModifiedDateTime, sheet.OwnerID, sheet.Published, sheet.Approved));
				}
				catch (KeyNotFoundException e)
				{
					TelemetryDashboardMain.Logger.Log(string.Format("Failed referencing app with ID '{0}' to parse sheet with ID '{1}' and engine ID '{2}'.", sheet.AppID, sheet.ID, sheet.EngineObjectID), LogLevel.Error);
				}
			});
		}

		internal void PopulateFromCachedMetadata(TelemetryMetadata oldMeta)
		{
			if (oldMeta.Apps.Count == 0)
			{
				return;
			}

			IList<Guid> newAppKeys = this.Apps.Keys.ToList();

			foreach (Guid key in newAppKeys)
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
