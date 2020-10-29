using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.ModelWriter
{
	internal class MetadataWriter
	{
		internal static char CSV_SEPARATOR = '`';
		internal static string[] HEADERS_APPS = new string[] { "AppID", "AppName", "AppOwnerID", "IsPublished", "PublishedDate", "StreamID", "StreamName" };
		internal static string[] HEADERS_SHEETS = new string[] { "AppID", "SheetID", "SheetName", "OwnerID", "Published", "Approved" };
		internal static string[] HEADERS_VISUALIZATIONS = new string[] { "AppID|SheetID", "VisualizationID", "Type" };
		internal static string[] HEADERS_USERS = new string[] { "ID", "UserId", "UserDirectory", "Username" };
		internal static string[] HEADERS_ENGINEINFOS = new string[] { "Hostname", "Key", "Value" };
		internal static string[] HEADERS_EXTENSIONSCHEMAS = new string[] { "ID", "Name", "Type" };
		internal static string[] HEADERS_EXTENSIONS = new string[] { "ID", "CreatedDate", "Name", "OwnerID", "DashboardBundle", "VisualizationBundle" };
		internal static string[] HEADERS_SYSTEMINFO = new string[] { "version", "ReleaseLabel" };

		internal static void DeleteMetadataFiles()
		{
			TelemetryDashboardMain.Logger.Log("Deleting old metadata files.", LogLevel.Info);

			try
			{
				string outputDir = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME);
				if (Directory.Exists(outputDir))
				{
					DirectoryInfo di = new DirectoryInfo(outputDir);

					foreach (FileInfo file in di.GetFiles())
					{
						TelemetryDashboardMain.Logger.Log("Deleting: " + file.FullName, LogLevel.Debug);

						file.Delete();
					}
				}
			}
			catch (Exception e)
			{
				TelemetryDashboardMain.Logger.Log("Error deleting file. Message: " + e.Message, LogLevel.Error);

			}
		}

		internal static void WriteMetadataToFile(TelemetryMetadata meta)
		{
			TelemetryDashboardMain.Logger.Log("Writing metadata files to disk.", LogLevel.Info);

			// check to see if folder exists
			TelemetryDashboardMain.Logger.Log("Checking to see if output dir exists.", LogLevel.Debug);
			string outputDir = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME);
			if (!Directory.Exists(outputDir))
			{
				TelemetryDashboardMain.Logger.Log("Output dir didn't exist. Creating.", LogLevel.Debug);

				Directory.CreateDirectory(outputDir);
			}

			TelemetryDashboardMain.Logger.Log("Writing System Info file to disk.", LogLevel.Info);
			WriteSystemInfoFile(meta);

			TelemetryDashboardMain.Logger.Log("Writing Extensions file to disk.", LogLevel.Info);
			WriteExtensionsMetadataFile(meta);

			TelemetryDashboardMain.Logger.Log("Writing Extension Schemas file to disk.", LogLevel.Info);
			WriteExtensionSchemasMetadataFile(meta);

			TelemetryDashboardMain.Logger.Log("Writing Engine Infos file to disk.", LogLevel.Info);
			WriteEngineInfosMetadataFile(meta);

			TelemetryDashboardMain.Logger.Log("Writing User file to disk.", LogLevel.Info);
			WriteUsersMetadataFile(meta);

			TelemetryDashboardMain.Logger.Log("Writing all App metadata files to disk.", LogLevel.Info);
			WriteAppsMetadataFiles(meta);
		}

		private static void WriteSystemInfoFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_SYSTEMINFO);

			foreach (Extension extension in meta.Extensions)
			{
				sb.Append(meta.Version);
				sb.Append(CSV_SEPARATOR);
				sb.Append(meta.ReleaseLabel);
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_SYSTEMINFO_FILE_NAME));
		}

		private static void WriteExtensionsMetadataFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_EXTENSIONS);

			foreach (Extension extension in meta.Extensions)
			{
				sb.Append(extension.ID.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(extension.CreatedDate.ToString("O"));
				sb.Append(CSV_SEPARATOR);
				sb.Append(extension.Name);
				sb.Append(CSV_SEPARATOR);
				sb.Append(extension.OwnerID.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(extension.DashboardBundle.ToString());
				sb.Append(CSV_SEPARATOR);
				sb.Append(extension.VisualizationBundle.ToString());
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_EXTENSIONS_FILE_NAME));
		}

		private static void WriteExtensionSchemasMetadataFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_EXTENSIONSCHEMAS);

			foreach (ExtensionSchema schema in meta.ExtensionSchemas)
			{
				sb.Append(schema.ID);
				sb.Append(CSV_SEPARATOR);
				sb.Append(schema.Name);
				sb.Append(CSV_SEPARATOR);
				sb.Append(schema.Type);
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_EXTENSIONSCHEMAS_FILE_NAME));
		}

		private static void WriteEngineInfosMetadataFile(TelemetryMetadata meta)
		{
			StringBuilder sb = new StringBuilder();

			WriteHeaders(sb, HEADERS_ENGINEINFOS);

			foreach (EngineInfo engine in meta.EngineInfos)
			{
				sb.Append(engine.Hostname);
				sb.Append(CSV_SEPARATOR);
				sb.Append("WorkingSetMin");
				sb.Append(CSV_SEPARATOR);
				sb.Append(engine.WorkingSetMin);
				sb.Append('\n');

				sb.Append(engine.Hostname);
				sb.Append(CSV_SEPARATOR);
				sb.Append("WorkingSetMax");
				sb.Append(CSV_SEPARATOR);
				sb.Append(engine.WorkingSetMax);
				sb.Append('\n');

				sb.Append(engine.Hostname);
				sb.Append(CSV_SEPARATOR);
				sb.Append("PerformanceLogLevel");
				sb.Append(CSV_SEPARATOR);
				sb.Append(engine.PerformanceLogLevel);
				sb.Append('\n');

				sb.Append(engine.Hostname);
				sb.Append(CSV_SEPARATOR);
				sb.Append("QIXPerformanceLogLevel");
				sb.Append(CSV_SEPARATOR);
				sb.Append(engine.QIXPerformanceLogLevel);
				sb.Append('\n');

				sb.Append(engine.Hostname);
				sb.Append(CSV_SEPARATOR);
				sb.Append("SessionLogLevel");
				sb.Append(CSV_SEPARATOR);
				sb.Append(engine.SessionLogLevel);
				sb.Append('\n');
			}

			WriteFile(sb, Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME, FileLocationManager.METADATA_ENGINEINFOS_FILE_NAME));
		}

		private static void WriteUsersMetadataFile(TelemetryMetadata meta)
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
					sheetsSB.Append(sheet.Value.EngineObjectID);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Name);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.OwnerID);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Published);
					sheetsSB.Append(CSV_SEPARATOR);
					sheetsSB.Append(sheet.Value.Approved);
					sheetsSB.Append('\n');

					foreach (Visualization viz in sheet.Value.Visualizations)
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
