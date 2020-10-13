using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Models.TelemetryMetadata;

namespace qs_telemetry_dashboard.ModelWriter
{
	internal class MetadataWriter
	{
		internal static char CSV_SEPARATOR = '`';
		internal static string[] HEADERS_APPS = new string[] { "AppID", "AppName", "AppOwnerID", "IsPublished", "PublishedDate", "StreamID", "StreamName" };
		internal static string[] HEADERS_SHEETS = new string[] { "AppID", "SheetID", "SheetName", "OwnerID", "Published", "Approved" };
		internal static string[] HEADERS_VISUALIZATIONS = new string[] { "AppID|SheetID", "VisualizationID", "Type" };
		internal static string[] HEADERS_USERS = new string[] { "ID", "UserId", "UserDirectory", "Username" };

		internal static void WriteMetadataToFile(TelemetryMetadata meta)
		{
			// check to see if folder exists
			string outputDir = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME);
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			WriteAppsMetadataFiles(meta);
		}

		private static void WriteUsersMetadataFiles(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_USERS);

			foreach (User user in meta.Users)
			{
				sb.Append(user.ID.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(user.UserID);
				sb.Append(CSV_SEPARATOR);
				sb.Append(user.UserDirectory);
				sb.Append(CSV_SEPARATOR);
				sb.Append(user.Username);
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_USERS_FILE_NAME));

		}

		private static void WriteAppsMetadataFiles(TelemetryMetadata meta)
		{
			StringBuilder appsSB = new StringBuilder();
			StringBuilder sheetsSB = new StringBuilder();
			StringBuilder visualizationsSB = new StringBuilder();

			WriteHeaders(appsSB, HEADERS_APPS);
			WriteHeaders(sheetsSB, HEADERS_SHEETS);
			WriteHeaders(visualizationsSB, HEADERS_VISUALIZATIONS);

			foreach (KeyValuePair<Guid, QRSApp> app in meta.Apps)
			{
				appsSB.Append(app.Key.ToString());
				appsSB.Append(CSV_SEPARATOR);
				appsSB.Append(app.Value.Name);
				appsSB.Append(CSV_SEPARATOR);
				appsSB.Append(app.Value.AppOwnerID.ToString());
				appsSB.Append(CSV_SEPARATOR);
				appsSB.Append(app.Value.Published);
				if (app.Value.Published)
				{
					appsSB.Append(CSV_SEPARATOR);
					appsSB.Append(app.Value.PublishedDateTime.ToString("o"));
					appsSB.Append(CSV_SEPARATOR);
					appsSB.Append(app.Value.StreamID.ToString());
					appsSB.Append(CSV_SEPARATOR);
					appsSB.Append(app.Value.StreamName);
				}
				appsSB.Append('\n');

				foreach (KeyValuePair<Guid, QRSSheet> sheet in app.Value.Sheets)
				{
					sheetsSB.Append(app.Key.ToString());
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Key.ToString());
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Name);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.OwnerID);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Published);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Approved);
					sheetsSB.Append('\n');

					foreach(Visualization viz in sheet.Value.Visualizations)
					{
						visualizationsSB.Append(app.Key.ToString() + '|' + sheet.Value.EngineObjectID);
						visualizationsSB.Append(CSV_SEPARATOR);
						visualizationsSB.Append(viz.ObjectName);
						visualizationsSB.Append(CSV_SEPARATOR);
						visualizationsSB.Append(viz.ObjectType);
						visualizationsSB.Append('\n');
					}
				}
			}

			WriteFile(appsSB, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_APPS_FILE_NAME));
			WriteFile(sheetsSB, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_SHEETS_FILE_NAME));
			WriteFile(visualizationsSB, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_VISUALIZATIONS_FILE_NAME));
		}

		private static void WriteHeaders(StringBuilder sb, string[] headers)
		{
			for (int i = 0; i < headers.Length; i++)
			{
				if (i < (headers.Length - 1))
				{
					sb.Append(headers[i] + CSV_SEPARATOR);
				}
				else
				{
					sb.Append(headers[i] + '\n');
				}
			}
		}

		private static void WriteFile(StringBuilder sb, string filepath)
		{
			System.IO.File.WriteAllText(filepath, sb.ToString());

		}
	}
}
