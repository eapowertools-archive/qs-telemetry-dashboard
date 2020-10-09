﻿using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class QRSApp
	{
		internal string Name { get; set; }

		internal Guid AppOwnerID { get; set; }

		internal bool Published { get; set; }

		internal DateTime PublishedDateTime { get; set; }

		internal Guid StreamID { get; set; }

		internal string StreamName { get; set; }

		internal IDictionary<Guid, QRSSheet> Sheets { get; set; }

		[field: NonSerializedAttribute()]
		internal bool VisualizationUpdateNeeded { get; set; }

		internal QRSApp(string name, Guid appOwnerId, bool published)
		{
			this.Name = name;
			this.AppOwnerID = appOwnerId;
			this.Published = published;
			this.Sheets = new Dictionary<Guid, QRSSheet>();
			VisualizationUpdateNeeded = true;
		}

		internal QRSApp(string name, Guid appOwnerId, bool published, DateTime publishedDate, Guid streamID, string streamName) : this(name, appOwnerId, published)
		{
			this.PublishedDateTime = publishedDate;
			this.StreamID = streamID;
			this.StreamName = streamName;
		}

		public override bool Equals(object obj)
		{
			QRSApp other = obj as QRSApp; //avoid double casting
			if (other is null)
			{
				return false;
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