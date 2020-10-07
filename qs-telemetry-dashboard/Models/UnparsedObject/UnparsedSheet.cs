using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata.UnparsedObject
{
	internal class UnparsedSheet
	{
		internal Guid ID { get; private set; }
		internal Guid AppID { get; private set; }

		internal string EngineObjectID { get; private set; }

		internal string Name { get; private set; }

		internal Guid OwnerID { get; private set; }

		internal bool Published { get; private set; }

		internal bool Approved { get; private set; }

		internal UnparsedSheet(Guid id, Guid appID, string engineObjectId, string name, Guid ownerID, bool published, bool approved)
		{
			ID = id;
			AppID = appID;
			EngineObjectID = engineObjectId;
			Name = name;
			OwnerID = ownerID;
			Published = published;
			Approved = approved;
		}
	}
}
