using System;
using System.Collections.Generic;

namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class App
	{
		internal string Name { get; set; }

		internal Guid AppOwner { get; set; }

		internal bool Published { get; set; }

		internal DateTime PublishedDateTime { get; set; }

		internal Guid StreamID { get; set; }

		internal string StreamName { get; set; }

		internal IDictionary<Guid, Sheet> Sheets { get; set; }

		internal App(string name, Guid appOwner, bool published)
		{
			this.Name = name;
			this.Published = published;
		}

		internal App(string name, Guid appOwner, bool published, DateTime publishedDate, Guid streamID, string streamName) : this(name, appOwner, published)
		{
			this.PublishedDateTime = publishedDate;
			this.StreamID = streamID;
			this.StreamName = streamName;
		}
	}
}
