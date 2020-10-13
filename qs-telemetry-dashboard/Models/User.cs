using System;
namespace qs_telemetry_dashboard.Models.TelemetryMetadata
{
	[Serializable]
	internal class User
	{
		internal Guid ID { get; set; }

		internal string UserID { get; set; }

		internal string UserDirectory { get; set; }

		internal string Username { get; set; }

		internal User(Guid id, string userId, string userDirectory, string username)
		{
			this.ID = id;
			this.UserID = userId;
			this.UserDirectory = userDirectory;
			this.Username = username;
		}
	}
}
