﻿using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class App
	{
		internal Guid ID { get; set; }

		internal string Name { get; set; }

		internal bool Published { get; set; }

		internal DateTime PublishedDateTime { get; set; }

		internal Guid StreamID { get; set; }

		internal string StreamName { get; set; }

		internal App()
		{
		}
	}
}