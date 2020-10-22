using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using qs_telemetry_dashboard.Exceptions;

namespace qs_telemetry_dashboard.Helpers
{
	internal class FileLocationManager
	{
		internal static string HOST_CONFIG_PATH = @"C:\ProgramData\Qlik\Sense\Host.cfg";

		internal static string TELEMETRY_FOLDER_NAME = "TelemetryDashboard";
		internal static string TELEMETRY_OUTPUT_FOLDER_NAME = "output";
		internal static string TELEMETRY_EXE_FILE_NAME = "TelemetryDashboard.exe";
		internal static string METADATA_BINARY_FILE_NAME = "metadata.tdm";

		internal static string METADATA_EXTENSIONS_FILE_NAME = "extensions.csv";
		internal static string METADATA_EXTENSIONSCHEMAS_FILE_NAME = "extensionSchemas.csv";
		internal static string METADATA_ENGINEINFOS_FILE_NAME = "engineInfos.csv";
		internal static string METADATA_USERS_FILE_NAME = "users.csv";
		internal static string METADATA_APPS_FILE_NAME = "apps.csv";
		internal static string METADATA_SHEETS_FILE_NAME = "sheets.csv";
		internal static string METADATA_VISUALIZATIONS_FILE_NAME = "visualizations.csv";

		internal static string WorkingDirectory
		{
			get
			{
				return AppDomain.CurrentDomain.BaseDirectory;
			}
		}

		internal static string WorkingTelemetryDashboardExePath
		{
			get
			{
				return System.Reflection.Assembly.GetEntryAssembly().Location;
			}
		}

		internal static string GetTelemetrySharePath()
		{
			Tuple<HttpStatusCode, string> response = TelemetryDashboardMain.QRSRequest.MakeRequest("/servicecluster/full", HttpMethod.Get);
			if (response.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(response.Item1.ToString() + " returned when getting share path. Request failed.");
			}

			JArray listOfServiceClusters = JArray.Parse(response.Item2);

			if (listOfServiceClusters.Count == 0)
			{
				throw new Exception("Tried to get service cluster information. Request returned but no data was found.");
			}
			else if (listOfServiceClusters.Count > 1)
			{
				throw new Exception("Got service cluster information. More than 1 service cluster was found.");
			}

			string shareRootPath = listOfServiceClusters[0]["settings"]["sharedPersistenceProperties"]["rootFolder"].ToString();

			return Path.Combine(shareRootPath, TELEMETRY_FOLDER_NAME);
		}

		internal static string GetPath(string uncPath)
		{
			try
			{
				// remove the "\\" from the UNC path and split the path
				uncPath = uncPath.Replace(@"\\", "");
				string[] uncParts = uncPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
				if (uncParts.Length < 2)
					return "[UNRESOLVED UNC PATH: " + uncPath + "]";
				// Get a connection to the server as found in the UNC path
				ManagementScope scope = new ManagementScope(@"\\" + uncParts[0] + @"\root\cimv2");
				// Query the server for the share name
				SelectQuery query = new SelectQuery("Select * From Win32_Share Where Name = '" + uncParts[1] + "'");
				ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

				// Get the path
				string path = string.Empty;
				foreach (ManagementObject obj in searcher.Get())
				{
					path = obj["path"].ToString();
				}

				// Append any additional folders to the local path name
				if (uncParts.Length > 2)
				{
					for (int i = 2; i < uncParts.Length; i++)
						path = path.EndsWith(@"\") ? path + uncParts[i] : path + @"\" + uncParts[i];
				}

				return path;
			}
			catch (Exception ex)
			{
				return "[ERROR RESOLVING UNC PATH: " + uncPath + ": " + ex.Message + "]";
			}
		}
	}
}
