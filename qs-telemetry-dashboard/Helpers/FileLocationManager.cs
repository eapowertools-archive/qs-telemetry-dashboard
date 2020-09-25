using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qs_telemetry_dashboard.Helpers
{
	internal class FileLocationManager
	{
		internal static string TELEMETRY_FOLDER = "TelemetryDashboard";
		internal static string WorkingDirectory
		{
			get
			{
				return AppDomain.CurrentDomain.BaseDirectory;
			}
		}

		internal static string GetConfigurationPath()
		{

			return "";
		}
		//internal static bool IsInQlikShare()
		//{
		//	DirectoryInfo di = Directory.GetParent(WorkingDirectory);
		//	if (di.Name.ToLowerInvariant() == TELEMETRY_FOLDER.ToLowerInvariant())
		//	{
		//		string[] shareFolders = Directory.GetDirectories(di.Parent.FullName);
		//		if ((shareFolders.Contains("Apps") && shareFolders.Contains("ArchivedLogs") && shareFolders.Contains("CustomData") && shareFolders.Contains("StaticContent")))
		//		{
		//			return true;
		//		}

		//	}
		//	return false;
		//}
	}
}
