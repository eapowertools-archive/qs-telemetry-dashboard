using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class QRSApp
	{
		internal Guid ID { get; set; }

		internal string Name { get; set; }

		internal DateTime ModifiedDateTime { get; set; }

		internal Guid AppOwnerID { get; set; }

		internal bool Published { get; set; }

		internal DateTime PublishedDateTime { get; set; }

		internal Guid StreamID { get; set; }

		internal string StreamName { get; set; }

		internal IDictionary<Guid, QRSSheet> Sheets { get; set; }

		[field: NonSerializedAttribute()]
		internal bool VisualizationUpdateNeeded { get; set; }

		internal QRSApp(Guid id, string name, DateTime modifiedDateTime, Guid appOwnerId, bool published)
		{
			this.ID = id;
			this.Name = name;
			this.ModifiedDateTime = modifiedDateTime;
			this.AppOwnerID = appOwnerId;
			this.Published = published;
			this.Sheets = new Dictionary<Guid, QRSSheet>();
			VisualizationUpdateNeeded = true;
		}

		internal QRSApp(Guid id, string name, DateTime modifiedDateTime, Guid appOwnerId, bool published, DateTime publishedDate, Guid streamID, string streamName) : this(id, name, modifiedDateTime, appOwnerId, published)
		{
			this.PublishedDateTime = publishedDate;
			this.StreamID = streamID;
			this.StreamName = streamName;
		}

		public override int GetHashCode()
		{
			char[] c = this.ID.ToString().ToCharArray();
			return (int)c[0];
		}

		public override bool Equals(object obj)
		{
			QRSApp other = obj as QRSApp; //avoid double casting
			if (other is null)
			{
				return false;
			}
			if (this.ID != other.ID)
			{
				return false;
			}
			if (this.ModifiedDateTime != other.ModifiedDateTime)
			{
				return false;
			}
			if (this.Sheets.Count != other.Sheets.Count)
			{
				return false;
			}
			foreach(KeyValuePair<Guid, QRSSheet> sheet in this.Sheets)
			{
				QRSSheet comparedSheet;
				if (!other.Sheets.TryGetValue(sheet.Key, out comparedSheet))
				{
					return false;
				}
				if (sheet.Value.ModifiedDateTime != comparedSheet.ModifiedDateTime)
				{
					return false;
				}
			}
			return true;
		}

		public static bool operator ==(QRSApp left, QRSApp right)
		{
			if (left is null)
			{
				return right is null;
			}
			return left.Equals(right);
		}

		public static bool operator !=(QRSApp left, QRSApp right)
		{
			return !(left == right);
		}
	}
}
