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
using qs_telemetry_dashboard.Models;
using qs_telemetry_dashboard.Models.UnparsedObject;
using qs_telemetry_dashboard.ModelWriter;

namespace qs_telemetry_dashboard.MetadataFetch
{
	internal class MetadataFetchRunner
	{
		internal static int PAGESIZE = 200;

		internal static int Run(int engineRequestTimeoutMS)
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

			GetAboutServiceInfo(newMetadata);
			GetRepositoryExtensions(newMetadata);
			GetRepositoryExtensionSchemas(newMetadata);
			GetRepositoryEngineInfos(newMetadata);
			GetRepositoryUsers(newMetadata);
			GetRepositoryApps(newMetadata);
			IList<UnparsedSheet> unparsedSheets = GetRepositorySheets();
			newMetadata.ParseSheets(unparsedSheets);
			newMetadata.PopulateFromCachedMetadata(oldMeta);


			string centralNodeHost;
			if (!TelemetryDashboardMain.ArgsManager.UseLocalEngine)
			{
				centralNodeHost = GetCentralNodeHostname();
				TelemetryDashboardMain.Logger.Log("Got central node hostname for engine calls: " + centralNodeHost, LogLevel.Info);
			}
			else
			{
				centralNodeHost = CertificateConfigHelpers.Hostname;
				TelemetryDashboardMain.Logger.Log("Arg '-uselocalengine' was used. Using hostname '" + centralNodeHost + "' for all engine calls.", LogLevel.Info);
			}
			GetEngineObjects(centralNodeHost, newMetadata, engineRequestTimeoutMS);

			Stream SaveFileStream = File.Create(telemetryMetadataFile);
			BinaryFormatter serializer = new BinaryFormatter();
			serializer.Serialize(SaveFileStream, newMetadata);
			SaveFileStream.Close();

			MetadataWriter.DeleteMetadataFiles();
			MetadataWriter.WriteMetadataToFile(newMetadata);

			return 0;
		}

		private static void GetAboutServiceInfo(TelemetryMetadata telemetryMetadata)
		{
			TelemetryDashboardMain.Logger.Log("Getting system info from About Service on port 9032.", LogLevel.Info);

			Tuple<HttpStatusCode, string> aboutResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/v1/systemInfo", HttpMethod.Get, HTTPContentType.json, null, false, "9032");
			JObject aboutJSON = JObject.Parse(aboutResponse.Item2);
			telemetryMetadata.Version = aboutJSON["version"].ToString();
			telemetryMetadata.ReleaseLabel = aboutJSON["releaseLabel"].ToString();
		}

		private static string GetCentralNodeHostname()
		{
			TelemetryDashboardMain.Logger.Log("Getting central node hostname for engine calls.", LogLevel.Info);
			Tuple<HttpStatusCode, string> centralNodeResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/servernodeconfiguration?filter=isCentral eq true", HttpMethod.Get);
			if (centralNodeResponse.Item1 != HttpStatusCode.OK)
			{
				TelemetryDashboardMain.Logger.Log("Failed to get central node server config. Reponse was: '" + centralNodeResponse.Item1 + "'. Will use default host.cfg instead.", LogLevel.Error);
				return CertificateConfigHelpers.Hostname;
			}
			JArray centralNodeObject = JArray.Parse(centralNodeResponse.Item2);
			string hostname = centralNodeObject[0]["hostName"].ToString();
			return hostname;
		}

		private static void GetRepositoryExtensions(TelemetryMetadata metadataObject)
		{
			string extensionBody = @"
				{
					'columns':
						[{
							'columnType': 'Property',
							'definition': 'id',
							'name': 'id'
						},
						{
							'columnType': 'Property',
							'definition': 'createdDate',
							'name': 'createdDate'
						},
						{
							'columnType': 'Property',
							'definition': 'name',
							'name': 'name'
						},
						{
							'columnType': 'Property',
							'definition': 'owner.id',
							'name': 'owner.id'
						},
						{
							'name':'customProperties',
							'columnType':'List',
							'definition':'CustomPropertyValue',
							'list':[
								{
									'name':'definition',
									'columnType':'Property',
									'definition':'definition'
								},
								{
									'name':'value',
									'columnType':'Property',
									'definition':'value'
								}
							]
						}
						],
						'entity': 'Extension'
				}";


			Action<JArray> addAction = (extension) =>
			{
				bool dashboardBundle = false;
				bool visualizationBundle = false;
				foreach (JArray cp in extension[4]["rows"])
				{
					if (cp[0]["name"].ToString() == "ExtensionBundle")
					{
						string cpValue = cp[1].ToString();
						if (cpValue == "Dashboard-bundle")
						{
							dashboardBundle = true;
						}
						else if (cpValue == "Visualization-bundle")
						{
							visualizationBundle = true;
						}
						else
						{
							TelemetryDashboardMain.Logger.Log("Found invalid custom property for 'ExtensionBundle'. Value was: " + cpValue, LogLevel.Error);
						}
					}
				}
				metadataObject.Extensions.Add(new Extension(extension[0].ToObject<Guid>(), extension[1].ToObject<DateTime>(), extension[2].ToString(), extension[3].ToObject<Guid>(), dashboardBundle, visualizationBundle));
			};

			GetRepositoryPagedObjects("extension", extensionBody, addAction);
		}

		private static void GetRepositoryExtensionSchemas(TelemetryMetadata metadataObject)
		{
			TelemetryDashboardMain.Logger.Log("Fetching all extension schemas.", LogLevel.Info);
			Tuple<HttpStatusCode, string> extensionSchemas = TelemetryDashboardMain.QRSRequest.MakeRequest("/extension/schema", HttpMethod.Get);
			if (extensionSchemas.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(extensionSchemas.Item1.ToString() + " returned when trying to get all extension schemas. Request failed.");
			}
			TelemetryDashboardMain.Logger.Log("Got some extension schemas.", LogLevel.Debug);

			JObject parsedExtensionSchemas = JObject.Parse(extensionSchemas.Item2);

			foreach (JProperty schema in parsedExtensionSchemas.Children())
			{
				JToken type = schema.Value["type"];
				if (type != null)
				{
					string typeString = type.ToString();
					if (typeString == "visualization")
					{
						string name;
						JToken jTokenName = schema.Value["name"];
						if (jTokenName != null)
						{
							name = jTokenName.ToString();
						}
						else
						{
							name = schema.Name;
							TelemetryDashboardMain.Logger.Log("No 'name' property found for extension schema object '" + name + "', using object name instead", LogLevel.Debug);
						}
						metadataObject.ExtensionSchemas.Add(new ExtensionSchema(schema.Name, name, typeString));
					}
				}
			}
		}

		private static void GetRepositoryEngineInfos(TelemetryMetadata metadataObject)
		{
			string engineBody = @"
				{
					'columns':
						[{
							'columnType': 'Property',
							'definition': 'servernodeconfiguration.hostname',
							'name': 'servernodeconfiguration.hostname'
						},
						{
							'columnType': 'Property',
							'definition': 'settings.workingSetSizeLoPct',
							'name': 'settings.workingSetSizeLoPct'
						},
						{
							'columnType': 'Property',
							'definition': 'settings.workingSetSizeHiPct',
							'name': 'settings.workingSetSizeHiPct'
						},
						{
							'columnType': 'Property',
							'definition': 'settings.performanceLogVerbosity',
							'name': 'settings.performanceLogVerbosity'
						},
						{
							'columnType': 'Property',
							'definition': 'settings.qixPerformanceLogVerbosity',
							'name': 'settings.qixPerformanceLogVerbosity'
						},
						{
							'columnType': 'Property',
							'definition': 'settings.sessionLogVerbosity',
							'name': 'settings.sessionLogVerbosity'
						}],
						'entity': 'EngineService'
				}";


			Action<JArray> addAction = (engine) => metadataObject.EngineInfos.Add(new EngineInfo(engine[0].ToString(), engine[1].ToObject<int>(), engine[2].ToObject<int>(), (EngineLogLevel)engine[3].ToObject<int>(), (EngineLogLevel)engine[4].ToObject<int>(), (EngineLogLevel)engine[5].ToObject<int>()));

			GetRepositoryPagedObjects("engineservice", engineBody, addAction);
		}



		private static void GetRepositoryUsers(TelemetryMetadata metadataObject)
		{
			string userBody = @"
				{
					'columns':
						[{
							'columnType': 'Property',
							'definition': 'id',
							'name': 'id'
						},
						{
							'columnType': 'Property',
							'definition': 'userid',
							'name': 'userid'
						},
						{
							'columnType': 'Property',
							'definition': 'userdirectory',
							'name': 'userdirectory'
						},
						{
							'columnType': 'Property',
							'definition': 'name',
							'name': 'name'
						}],
						'entity': 'User'
				}";

			Action<JArray> addAction = (user) => metadataObject.Users.Add(new User(user[0].ToObject<Guid>(), user[1].ToString(), user[2].ToString(), user[3].ToString()));

			GetRepositoryPagedObjects("user", userBody, addAction);
		}

		private static void GetRepositoryApps(TelemetryMetadata metadataObject)
		{
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

			Action<JArray> addAction = (app) =>
			{
				Guid appID = app[0].ToObject<Guid>();
				string appName = app[1].ToString();
				TelemetryDashboardMain.Logger.Log(string.Format("Processing app '{1}' with ID '{0}'", appID, appName), LogLevel.Debug);

				bool published = app[4].ToObject<bool>();
				QRSApp newApp;
				if (!published)
				{
					newApp = new QRSApp(appName, app[2].ToObject<DateTime>(), app[3].ToObject<Guid>(), published);
				}
				else
				{
					newApp = new QRSApp(appName, app[2].ToObject<DateTime>(), app[3].ToObject<Guid>(), published, app[5].ToObject<DateTime>(), app[6].ToObject<Guid>(), app[7].ToString());
				}
				try
				{
					metadataObject.Apps.Add(appID, newApp);
				}
				catch (Exception e)
				{
					TelemetryDashboardMain.Logger.Log(string.Format("App '{1}' with ID '{0}' has already been added. This is probably due to an app being added while fetching the metadata. This error can likely be ignored. Internal error: {2}", appID, appName, e.Message), LogLevel.Error);
				}
			};

			GetRepositoryPagedObjects("app", appBody, addAction);
		}

		private static IList<UnparsedSheet> GetRepositorySheets()
		{
			int sheetCount = GetRepositoryObjectsCount("app/object");

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

		private static void GetEngineObjects(string centralNodeHostname, TelemetryMetadata metadata, int engineRequestTimeoutMS)
		{
			TelemetryDashboardMain.Logger.Log(string.Format("Engine request timeout set to: {0} ms (default is: 30000 ms)", engineRequestTimeoutMS.ToString()), LogLevel.Info);

			Qlik.Sense.JsonRpc.RpcConnection.Timeout = engineRequestTimeoutMS;

			string wssPath = "https://" + centralNodeHostname + ":4747";
			ILocation location = Location.FromUri(new Uri(wssPath));

			X509Certificate2Collection certificateCollection = new X509Certificate2Collection(CertificateConfigHelpers.Certificate);
			// Defining the location as a direct connection to Qlik Sense Server
			location.AsDirectConnection("INTERNAL", "sa_api", certificateCollection: certificateCollection);

			int totalApps = metadata.Apps.Count;
			int currentApp = 0;

			TelemetryDashboardMain.Logger.Log("Will start to fetch all app objects from the engine.", LogLevel.Info);

			foreach (KeyValuePair<Guid, QRSApp> appTuple in metadata.Apps)
			{
				currentApp++;
				TelemetryDashboardMain.Logger.Log(string.Format("App {0} of {1} - Checking to see if visualaizations fetch is needed for app '{2}' with ID '{3}' ", currentApp, totalApps, appTuple.Value.Name, appTuple.Key.ToString()), LogLevel.Debug);

				if (appTuple.Value.VisualizationUpdateNeeded)
				{
					TelemetryDashboardMain.Logger.Log(string.Format("Getting visualizations for app '{0}' with ID '{1}' ", appTuple.Value.Name, appTuple.Key.ToString()), LogLevel.Info);
					try
					{
						IAppIdentifier appIdentifier = new AppIdentifier() { AppId = appTuple.Key.ToString() };
						using (IApp app = location.App(appIdentifier, null, true))
						{
							IEnumerable<ISheet> sheetList = app.GetSheets();
							foreach (ISheet sheet in sheetList)
							{
								ISheetLayout sheetObject = (SheetLayout)sheet.GetLayout();
								IList<Visualization> vizs = new List<Visualization>();
								sheetObject.Cells.ToList().ForEach(c => vizs.Add(new Visualization(c.Name, c.Type)));
								metadata.Apps[appTuple.Key].Sheets.FirstOrDefault(s => s.Value.EngineObjectID == sheetObject.Info.Id).Value.SetSheetsList(vizs);
							}
						}
					}
					catch (Exception e)
					{
						TelemetryDashboardMain.Logger.Log("Failed to get engine objects from App: " + appTuple.Key.ToString() + ". Message: " + e.Message, LogLevel.Error);
						TelemetryDashboardMain.Logger.Log("Skipping app: " + appTuple.Key.ToString(), LogLevel.Error);
					}
				}
			}
			TelemetryDashboardMain.Logger.Log("Done getting all app objects from the engine.", LogLevel.Info);
		}

		private static int GetRepositoryObjectsCount(string type)
		{
			TelemetryDashboardMain.Logger.Log("Fetching all objects of type '" + type + "'.", LogLevel.Info);
			Tuple<HttpStatusCode, string> numOfObjects = TelemetryDashboardMain.QRSRequest.MakeRequest("/" + type + "/count", HttpMethod.Get);
			if (numOfObjects.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(numOfObjects.Item1.ToString() + " returned when trying to get a count of all objects of type '" + type + "'. Request failed.");
			}
			TelemetryDashboardMain.Logger.Log("Count request returned with OK", LogLevel.Debug);
			int parsedInt = JObject.Parse(numOfObjects.Item2)["value"].ToObject<int>();
			TelemetryDashboardMain.Logger.Log("Fetched all '" + type + "' objects. There were: " + parsedInt.ToString(), LogLevel.Debug);
			return parsedInt;
		}

		private static void GetRepositoryPagedObjects(string type, string body, Action<JArray> addAction)
		{
			int objectCount = GetRepositoryObjectsCount(type);

			int startLocation = 0;
			Tuple<HttpStatusCode, string> objectResponse;
			do
			{
				objectResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/" + type + "/table?skip=" + startLocation + "&take=" + PAGESIZE, HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (objectResponse.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(objectResponse.Item1.ToString() + " returned when trying to get objects of type '" + type + "'. Request failed.");
				}
				JArray returnedObjects = JObject.Parse(objectResponse.Item2).Value<JArray>("rows");
				foreach (JArray jObject in returnedObjects)
				{
					addAction(jObject);
				}
				startLocation += PAGESIZE;
			} while (startLocation < objectCount);
		}
	}
}
