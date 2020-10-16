namespace qs_telemetry_dashboard.Models
{
	internal class ExtensionSchema
	{
		internal string ID { get; set; }

		internal string Name { get; set; }

		internal string Type { get; set; }

		internal ExtensionSchema(string id, string name, string type)
		{
			this.ID = id;
			this.Name = name;
			this.Type = type;
		}
	}
}
