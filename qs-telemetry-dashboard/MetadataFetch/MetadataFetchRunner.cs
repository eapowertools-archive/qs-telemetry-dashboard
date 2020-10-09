using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json.Linq;
using Qlik.Engine;
using Qlik.Sense.Client;
using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models.TelemetryMetadata;
using qs_telemetry_dashboard.Models.TelemetryMetadata.UnparsedObject;
using qs_telemetry_dashboard.ModelWriter;

namespace qs_telemetry_dashboard.MetadataFetch
{
	internal class MetadataFetchRunner
	{
		internal static int PAGESIZE = 200;

		internal static int Run()
		{
			// check to see if metadata binary exists
			string telemetryMetadataFile = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.METADATA_BINARY_FILE_NAME);
			TelemetryDashboardMain.Logger.Log("Trying to get metadata binary file: " + telemetryMetadataFile, LogLevel.Debug);
			TelemetryMetadata oldMeta, newMetadata;
			if (File.Exists(telemetryMetadataFile))
			{
				TelemetryDashboardMain.Logger.Log("Found metadata file, will load contents.", LogLevel.Info);
				Stream openFileStream = File.OpenRead(telemetryMetadataFile);
				BinaryFormatter deserializer = new BinaryFormatter();
				oldMeta = (TelemetryMetadata)deserializer.Deserialize(openFileStream);
				openFileStream.Close();
			}
			else
			{
				oldMeta = new TelemetryMetadata();
			}

			newMetadata = new TelemetryMetadata(true);

			GetRepositoryApps(newMetadata);
			IList<UnparsedSheet> unparsedSheets = GetRepositorySheets();
			newMetadata.ParseSheets(unparsedSheets);

			GetEngineObjects(newMetadata);

			MetadataWriter.WriteMetadataToFile(newMetadata);

			return 0;
		}

		private static void GetRepositoryApps(TelemetryMetadata metadataObject)
		{
			TelemetryDashboardMain.Logger.Log("Fetching all apps.", LogLevel.Info);
			Tuple<HttpStatusCode, string> numOfApps = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/count", HttpMethod.Get);
			if (numOfApps.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(numOfApps.Item1.ToString() + " returned when trying to get a count of all the apps. Request failed.");
			}
			int appCount = JObject.Parse(numOfApps.Item2)["value"].ToObject<int>();


			string appBody = @"
				{
					'columns':
						[{
							'columnType': 'Property',
							'definition': 'id',
							'name': 'id'
						},
						{
							'columnType': 'Property',
							'definition': 'name',
							'name': 'name'
						},
						{
							'columnType': 'Property',
							'definition': 'modifiedDate',
							'name': 'modifiedDate'
						}
						{
							'columnType': 'Property',
							'definition': 'owner.id',
							'name': 'owner.id'
						},
						{
							'columnType': 'Property',
							'definition': 'published',
							'name': 'published'
						},
{
							'columnType': 'Property',
							'definition': 'publishtime',
							'name': 'publishtime'
						},
						
						{
							'columnType': 'Property',
							'definition': 'stream.id',
							'name': 'stream.id'
						},
						{
							'columnType': 'Property',
							'definition': 'stream.name',
							'name': 'stream.name'
						}],
						'entity': 'App'
				}";

			int startLocation = 0;
			Tuple<HttpStatusCode, string> appResponse;
			do
			{
				appResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/table?skip=" + startLocation + "&take=" + PAGESIZE, HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(appBody));
				if (appResponse.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(appResponse.Item1.ToString() + " returned when trying to get apps. Request failed.");
				}
				JArray returnedApps = JObject.Parse(appResponse.Item2).Value<JArray>("rows");
				foreach (JArray app in returnedApps)
				{
					Guid appID = app[0].ToObject<Guid>();
					string appName = app[1].ToString();
					TelemetryDashboardMain.Logger.Log(string.Format("Processing app '{0}' with ID '{1}'", appID, appName), LogLevel.Debug);

					bool published = app[4].ToObject<bool>();
					QRSApp newApp;
					if (!published)
					{
						newApp = new QRSApp(appID, appName, app[2].ToObject<DateTime>(), app[3].ToObject<Guid>(), published);
					}
					else
					{
						newApp = new QRSApp(appID, appName, app[2].ToObject<DateTime>(), app[3].ToObject<Guid>(), published, app[5].ToObject<DateTime>(), app[6].ToObject<Guid>(), app[7].ToString());
					}
					metadataObject.Apps.Add(appID, newApp);
				}
				startLocation += PAGESIZE;
			} while (startLocation < appCount);
		}

		private static IList<UnparsedSheet> GetRepositorySheets()
		{
			TelemetryDashboardMain.Logger.Log("Fetching all sheets.", LogLevel.Info);
			Tuple<HttpStatusCode, string> numberOfSheets = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/object/count?filter=objectType eq 'sheet'", HttpMethod.Get);
			if (numberOfSheets.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(numberOfSheets.Item1.ToString() + " returned when trying to get a count of all the sheets. Request failed.");
			}
			int sheetCount = JObject.Parse(numberOfSheets.Item2)["value"].ToObject<int>();


			string sheetBody = @"
				{
					'columns':
						[{
							'columnType': 'Property',
							'definition': 'id',
							'name': 'id'
						},
						{
							'columnType': 'Property',
							'definition': 'app.id',
							'name': 'app.id'
						},
						{
							'columnType': 'Property',
							'definition': 'engineObjectId',
							'name': 'engineObjectId'
						},
						{
							'columnType': 'Property',
							'definition': 'name',
							'name': 'name'
						},
						{
							'columnType': 'Property',
							'definition': 'modifiedDate',
							'name': 'modifiedDate'
						},
						{
							'columnType': 'Property',
							'definition': 'owner.id',
							'name': 'owner.id'
						},
						{
							'columnType': 'Property',
							'definition': 'published',
							'name': 'published'
						},
						{
							'columnType': 'Property',
							'definition': 'approved',
							'name': 'approved'
						}],
						'entity': 'App.Object'
				}";

			int startLocation = 0;
			IList<UnparsedSheet> allSheets = new List<UnparsedSheet>();
			Tuple<HttpStatusCode, string> sheetResponse;
			do
			{
				sheetResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/object/table?filter=objectType eq 'sheet'&skip=" + startLocation + "&take=" + PAGESIZE, HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(sheetBody));
				if (sheetResponse.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(sheetResponse.Item1.ToString() + " returned when trying to get sheets. Request failed.");
				}
				JArray returnedSheets = JObject.Parse(sheetResponse.Item2).Value<JArray>("rows");
				foreach (JArray sheet in returnedSheets)
				{
					allSheets.Add(new UnparsedSheet(sheet[0].ToObject<Guid>(), sheet[1].ToObject<Guid>(), sheet[2].ToString(), sheet[3].ToString(), sheet[4].ToObject<DateTime>(), sheet[5].ToObject<Guid>(), sheet[6].ToObject<bool>(), sheet[7].ToObject<bool>()));
				}
				startLocation += PAGESIZE;
			} while (startLocation < sheetCount);

			return allSheets;
		}

		private static void GetEngineObjects(TelemetryMetadata metadata)
		{
			string wssPath = "https://" + CertificateConfigHelpers.Hostname + ":4747";
			ILocation location = Location.FromUri(new Uri(wssPath));

			X509Certificate2Collection certificateCollection = new X509Certificate2Collection(CertificateConfigHelpers.Certificate);
			// Defining the location as a direct connection to Qlik Sense Server
			location.AsDirectConnection("INTERNAL", "sa_api", certificateCollection: certificateCollection);

			foreach (KeyValuePair<Guid, QRSApp> appTuple in metadata.Apps)
			{
				TelemetryDashboardMain.Logger.Log(string.Format("Getting visualaizations for app '{0}' with ID '{1}' ", appTuple.Value.Name, appTuple.Key.ToString()), LogLevel.Info);

				if (appTuple.Value.VisualizationUpdateNeeded)
				{
					IAppIdentifier appIdentifier = new AppIdentifier() { AppId = appTuple.Key.ToString() };
					using (IApp app = location.App(appIdentifier, null, true))
					{
						IEnumerable<ISheet> sheetList = app.GetSheets();
						foreach(ISheet sheet in sheetList)
						{
							ISheetLayout sheetObject = (SheetLayout)sheet.GetLayout();
							IList<Visualization> vizs = new List<Visualization>();
							sheetObject.Cells.ToList().ForEach(c => vizs.Add(new Visualization(c.Name, c.Type)));							
							metadata.Apps[appTuple.Key].Sheets.FirstOrDefault(s => s.Value.EngineObjectID == sheetObject.Info.Id).Value.SetSheetsList(vizs);
						}
					}
				}
			}
		}
	}
}
