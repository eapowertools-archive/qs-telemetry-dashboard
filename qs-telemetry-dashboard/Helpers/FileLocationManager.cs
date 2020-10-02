using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using qs_telemetry_dashboard.Exceptions;

namespace qs_telemetry_dashboard.Helpers
{
	internal class FileLocationManager
	{
		internal static string TELEMETRY_FOLDER = "TelemetryDashboard";
		internal static string TELEMETRY_OUTPUT_FOLDER = "output";
		internal static string TELEMETRY_EXE_FILENAME = "TelemetryDashboard.exe";
		internal static string TELEMETRY_CONFIG_FILENAME = "config.tdc";
		internal static string TELEMETRY_METADATA_BINARY = "metadata.tdm";

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

			return Path.Combine(shareRootPath, TELEMETRY_FOLDER);
		}
	}
}
