using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;

namespace qs_telemetry_dashboard.Initialize
{
	internal class InitializeEnvironment
	{
		private static string _hostname;

		internal static string Hostname
		{
			get
			{
				if (string.IsNullOrEmpty(_hostname))
				{
					string hostnameBase64 = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Host.cfg");
					byte[] data = Convert.FromBase64String(hostnameBase64);
					_hostname = Encoding.ASCII.GetString(data);
				}
				return _hostname;
			}
		}
		internal static int Run()
		{
			TelemetryDashboardMain.Logger.Log("Running in initialize mode.", LogLevel.Info);

			// get location to copy exe to
			string telemetryPath = FileLocationManager.GetTelemetrySharePath();

			TelemetryDashboardMain.Logger.Log("Ready to import app.", LogLevel.Debug);
			string appGUID = ImportApp();

			TelemetryDashboardMain.Logger.Log("Ready to create data connections.", LogLevel.Debug);
			CreateDataConnections();

			TelemetryDashboardMain.Logger.Log("Ready to create tasks.", LogLevel.Debug);
			CreateTasks(appGUID);

			return 0;
		}

		private static bool IsRepositoryRunning()
		{
			TelemetryDashboardMain.Logger.Log("Checking to see if repository is running.", LogLevel.Info);
			TelemetryDashboardMain.Logger.Log(string.Format("Sending request to 'https://{0}:4242'.", Hostname), LogLevel.Info);

			Tuple<HttpStatusCode, string> response = TelemetryDashboardMain.QRSRequest.MakeRequest("/about", HttpMethod.Get);
			if (response.Item1 == HttpStatusCode.OK)
			{
				TelemetryDashboardMain.Logger.Log("Repository responded with OK.", LogLevel.Info);
				return true;
			}
			else
			{
				TelemetryDashboardMain.Logger.Log(string.Format("Repository responded with '{0}'. Body was: {1}", response.Item1.ToString(), response.Item2), LogLevel.Error);
				return false;
			}
		}

		private static string ImportApp()
		{
			Tuple<HttpStatusCode, string> apps = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/full?filter=name eq 'Telemetry Dashboard'", HttpMethod.Get);
			if (apps.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(apps.Item1.ToString() + " returned checking Telemetry Dashboard app exists. Request failed.");
			}

			JArray listOfApps = JArray.Parse(apps.Item2);

			if (listOfApps.Count == 1)
			{
				string appID = listOfApps[0]["id"].ToString();
				Tuple<HttpStatusCode, string> replaceAppResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/upload/replace?targetappid=" + appID, HttpMethod.Post, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
				if (replaceAppResponse.Item1 == HttpStatusCode.Created)
				{
					return JObject.Parse(replaceAppResponse.Item2)["id"].ToString();
				}
				else
				{
					throw new InvalidResponseException(apps.Item1.ToString() + " returned when trying to replace Telemetry Dashboard app. Request failed.");
				}
			}


			if (listOfApps.Count > 1)
			{
				for (int i = 0; i < listOfApps.Count; i++)
				{
					listOfApps[i]["name"] = listOfApps[i]["name"] + "-old";
					listOfApps[i]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
					string appId = listOfApps[i]["id"].ToString();
					Tuple<HttpStatusCode, string> updatedApp = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/" + appId, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfApps[i].ToString()));
					if (updatedApp.Item1 != HttpStatusCode.OK)
					{
						throw new InvalidResponseException(apps.Item1.ToString() + " returned when trying to rename old Telemetry Dashboard apps. Request failed.");
					}
				}
			}

			Tuple<HttpStatusCode, string> uploadAppResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/upload?name=Telemetry Dashboard", HttpMethod.Post, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
			if (uploadAppResponse.Item1 != HttpStatusCode.Created)
			{
				throw new InvalidResponseException(apps.Item1.ToString() + " returned when trying to upload Telemetry Dashboard app. Request failed.");
			}
			return JObject.Parse(uploadAppResponse.Item2)["id"].ToString();
		}

		private static void CreateDataConnections()
		{

			// Add TelemetryMetadata dataconnection
			Tuple<HttpStatusCode, string> dataConnections = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection?filter=name eq 'TelemetryMetadata'", HttpMethod.Get);
			if (dataConnections.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to get 'TelmetryMetadata' data connections. Request failed.");
			}
			JArray listOfDataconnections = JArray.Parse(dataConnections.Item2);
			if (listOfDataconnections.Count == 0)
			{
				string connectionStringPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER) + @"\";
				TelemetryDashboardMain.Logger.Log("Building body for 'TelemetryMetadata' data connection. Connection string path: " + connectionStringPath, LogLevel.Debug);
				connectionStringPath = connectionStringPath.Replace("\\", "\\\\");
				string body = @"
			{
				'name': 'TelemetryMetadata',
				'connectionstring': '" + connectionStringPath + @"',
				'type': 'folder',
				'username': ''
			}";


				Tuple<HttpStatusCode, string> createdConnection = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createdConnection.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to create 'TelemetryMetadata' data connection. Request failed.");
				}
			}
			else
			{
				listOfDataconnections[0]["connectionstring"] = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER) + "\\";
				listOfDataconnections[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
				string appId = listOfDataconnections[0]["id"].ToString();
				Tuple<HttpStatusCode, string> updatedConnection = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection/" + appId, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfDataconnections[0].ToString()));
				if (updatedConnection.Item1 != HttpStatusCode.OK)
				{
					throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to update 'TelemetryMetadata' data connection. Request failed.");
				}
			}

			// Add EngineSettings dataconnection
			Tuple<HttpStatusCode, string> engineSettingDataconnection = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection?filter=name eq 'EngineSettingsFolder'", HttpMethod.Get);
			if (dataConnections.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to get 'EngineSettingsFolder' data connection. Request failed.");
			}
			listOfDataconnections = JArray.Parse(engineSettingDataconnection.Item2);
			if (listOfDataconnections.Count == 0)
			{
				string body = @"
					{
						'name': 'EngineSettingsFolder',
						'connectionstring': 'C:\\ProgramData\\Qlik\\Sense\\Engine\\',
						'type': 'folder',
						'username': ''
					}";

				Tuple<HttpStatusCode, string> createdConnection = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createdConnection.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to create 'EngineSettingsFolder' data connection. Request failed.");
				}
			}

			return;
		}

		private static void CreateTasks(string appId)
		{
			string telemetryDashboardPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_EXE_FILENAME);

			string externalTaskID = "";
			// External Task
			Tuple<HttpStatusCode, string> hasExternalTask = TelemetryDashboardMain.QRSRequest.MakeRequest("/externalprogramtask/count?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
			if (hasExternalTask.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(hasExternalTask.Item1.ToString() + " returned when trying to get 'TelemetryDashboard-1-Generate-Metadata' external task. Request failed.");
			}
			if (JObject.Parse(hasExternalTask.Item2)["value"].ToObject<int>() == 0)
			{
				string body = @"
			{
				'path': '" + telemetryDashboardPath + @"',
				'parameters': '-metadatafetch',
				'name': 'TelemetryDashboard-1-Generate-Metadata',
				'taskType': 1,
				'enabled': true,
				'taskSessionTimeout': 1440,
				'maxRetries': 0,
				'impactSecurityAccess': false,
				'schemaPath': 'ExternalProgramTask'
			}";
				Tuple<HttpStatusCode, string> createExternalTask = TelemetryDashboardMain.QRSRequest.MakeRequest("/externalprogramtask", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createExternalTask.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(createExternalTask.Item1.ToString() + " returned when trying to create 'TelemetryDashboard-1-Generate-Metadata' external task. Request failed.");
				}
				else
				{
					externalTaskID = JObject.Parse(createExternalTask.Item2)["id"].ToString();
				}
			}
			else
			{
				Tuple<HttpStatusCode, string> getExternalTaskId = TelemetryDashboardMain.QRSRequest.MakeRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
				externalTaskID = JArray.Parse(getExternalTaskId.Item2)[0]["id"].ToString();

			}

			// Reload Task
			Tuple<HttpStatusCode, string> reloadTasks = TelemetryDashboardMain.QRSRequest.MakeRequest("/reloadtask/full?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HttpMethod.Get);
			if (reloadTasks.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(reloadTasks.Item1.ToString() + " returned when trying to get 'TelemetryDashboard-2-Reload-Dashboard' external task. Request failed.");
			}

			JArray listOfTasks = JArray.Parse(reloadTasks.Item2);

			if (listOfTasks.Count == 0)
			{
				string body = @"
				{
					'compositeEvents': [
					{
						'compositeRules': [
						{
							'externalProgramTask': {
								'id': '" + externalTaskID + @"',
								'name': 'TelemetryDashboard-1-Generate-Metadata'
							},
							'ruleState': 1
						}
						],
						'enabled': true,
						'eventType': 1,
						'name': 'telemetry-metadata-trigger',
						'privileges': [
							'read',
							'update',
							'create',
							'delete'
						],
						'timeConstraint': {
							'days': 0,
							'hours': 0,
							'minutes': 360,
							'seconds': 0
						}
					}
					],
					'schemaEvents': [],
					'task': {
						'app': {
							'id': '" + appId + @"',
							'name': 'Telemetry Dashboard'
						},
						'customProperties': [],
						'enabled': true,
						'isManuallyTriggered': false,
						'maxRetries': 0,
						'name': 'TelemetryDashboard-2-Reload-Dashboard',
						'tags': [],
						'taskSessionTimeout': 1440,
						'taskType': 0
					}
				}";

				Tuple<HttpStatusCode, string> createTaskResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/reloadtask/create", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createTaskResponse.Item1 != HttpStatusCode.Created)
				{
					throw new InvalidResponseException(createTaskResponse.Item1.ToString() + " returned when trying to create 'TelemetryDashboard-2-Reload-Dashboard' external task. Request failed.");
				}
			}
			else
			{
				listOfTasks[0]["app"] = JObject.Parse(@"{ 'id': '" + appId + "'}");
				listOfTasks[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
				string reloadTaskID = listOfTasks[0]["id"].ToString();
				Tuple<HttpStatusCode, string> updatedTaskResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/reloadtask/" + reloadTaskID, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfTasks[0].ToString()));
				if (updatedTaskResponse.Item1 != HttpStatusCode.OK)
				{
					throw new InvalidResponseException(updatedTaskResponse.Item1.ToString() + " returned when trying to update 'TelemetryDashboard-2-Reload-Dashboard' external task. Request failed.");
				}
			}

			return;
		}



		//public string RemoveTasks()
		//{
		//	Tuple<HttpStatusCode, string> getReloadTaskId = _qrsRequest.MakeRequest("/reloadtask?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HttpMethod.Get);
		//	if (getReloadTaskId.Item1 == HttpStatusCode.OK)
		//	{
		//		JArray reloadTasks = JArray.Parse(getReloadTaskId.Item2);
		//		foreach (JToken t in reloadTasks)
		//		{
		//			_qrsRequest.MakeRequest("/reloadtask/" + t["id"], HttpMethod.Delete);
		//		}
		//	}

		//	Tuple<HttpStatusCode, string> getExternalTaskId = _qrsRequest.MakeRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
		//	if (getExternalTaskId.Item1 == HttpStatusCode.OK)
		//	{
		//		JArray externalTasks = JArray.Parse(getExternalTaskId.Item2);
		//		foreach (JToken t in externalTasks)
		//		{
		//			_qrsRequest.MakeRequest("/externalprogramtask/" + t["id"], HttpMethod.Delete);
		//		}
		//	}

		//	return "Success";
		//}
	}
}
