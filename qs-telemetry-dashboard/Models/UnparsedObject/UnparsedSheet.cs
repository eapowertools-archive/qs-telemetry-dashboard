using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata.UnparsedObject
{
	internal class UnparsedSheet
	{
		internal Guid ID { get; private set; }
		internal Guid AppID { get; private set; }
		internal string EngineObjectID { get; private set; }
		internal string Name { get; private set; }
		internal DateTime ModifiedDateTime { get; private set; }
		internal Guid OwnerID { get; private set; }
		internal bool Published { get; private set; }
		internal bool Approved { get; private set; }

		internal UnparsedSheet(Guid id, Guid appID, string engineObjectId, string name, DateTime modifiedDateTime, Guid ownerID, bool published, bool approved)
		{
			this.ID = id;
			this.AppID = appID;
			this.EngineObjectID = engineObjectId;
			this.Name = name;
			this.ModifiedDateTime = modifiedDateTime;
			this.OwnerID = ownerID;
			this.Published = published;
			this.Approved = approved;
		}
	}
}
