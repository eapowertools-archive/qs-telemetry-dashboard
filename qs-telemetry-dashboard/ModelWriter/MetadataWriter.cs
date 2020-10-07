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

			WriteAppsMetadataFile(meta);
			WriteSheetsMetadataFile(meta);
		}

		private static void WriteAppsMetadataFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_APPS);

			foreach (KeyValuePair<Guid, App> app in meta.Apps)
			{
				sb.Append(app.Key.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(app.Value.Name);
				sb.Append(CSV_SEPARATOR);
				sb.Append(app.Value.AppOwnerID.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(app.Value.Published);
				if (app.Value.Published)
				{
					sb.Append(CSV_SEPARATOR);
					sb.Append(app.Value.PublishedDateTime.ToString("o"));
					sb.Append(CSV_SEPARATOR);
					sb.Append(app.Value.StreamID.ToString());
					sb.Append(CSV_SEPARATOR);
					sb.Append(app.Value.StreamName);
				}
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_APPS_FILE_NAME));
		}

		private static void WriteSheetsMetadataFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_SHEETS);

			foreach (KeyValuePair<Guid, App> app in meta.Apps)
			{
				
				foreach (KeyValuePair<Guid, Sheet> sheet in app.Value.Sheets)
				{
					sb.Append(app.Key.ToString());
					sb.Append(CSV_SEPARATOR);
					sb.Append(sheet.Key.ToString());
					sb.Append(CSV_SEPARATOR);
					sb.Append(sheet.Value.Name);
					sb.Append(CSV_SEPARATOR);
					sb.Append(sheet.Value.OwnerID);
					sb.Append(CSV_SEPARATOR);
					sb.Append(sheet.Value.Published);
					sb.Append(CSV_SEPARATOR);
					sb.Append(sheet.Value.Approved);
					sb.Append('\n');
				}
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_SHEETS_FILE_NAME));
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
