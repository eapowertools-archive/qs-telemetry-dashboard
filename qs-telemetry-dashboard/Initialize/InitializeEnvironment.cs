using System;
using System.IO;
using System.Management;
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
		internal static int Run()
		{
			TelemetryDashboardMain.Logger.Log("Running in initialize mode.", LogLevel.Info);

			string telemetrySharePath = FileLocationManager.GetTelemetrySharePath();
			string telemetryExePath = Path.Combine(telemetrySharePath, FileLocationManager.TELEMETRY_EXE_FILE_NAME);

			TelemetryDashboardMain.Logger.Log("Current TelemetryDashboard.exe path: " + FileLocationManager.WorkingTelemetryDashboardExePath, LogLevel.Debug);
			TelemetryDashboardMain.Logger.Log("Share folder TelemetryDashboard.exe path: " + telemetryExePath, LogLevel.Debug);
			TelemetryDashboardMain.Logger.Log("Share folder TelemetryDashboard.exe root path: " + telemetrySharePath, LogLevel.Debug);

			string telemetryExeNonUNCPath = FileLocationManager.GetPath(telemetryExePath);
			TelemetryDashboardMain.Logger.Log("GetPath() for telemetryExePath: " + telemetryExeNonUNCPath, LogLevel.Debug);

			if (telemetryExeNonUNCPath != FileLocationManager.WorkingTelemetryDashboardExePath)
			{
				TelemetryDashboardMain.Logger.Log("Not running executable in Telemetry Folder, will copy file.", LogLevel.Debug);

				bool doCopy = true;
				if (File.Exists(telemetryExePath))
				{
					try
					{
						TelemetryDashboardMain.Logger.Log("Old TelemetryDashboard.exe file found. Deleting.", LogLevel.Debug);

						File.Delete(telemetryExePath);
					}
					catch (UnauthorizedAccessException)
					{
						TelemetryDashboardMain.Logger.Log(string.Format("Tried to delete '{0}', unauthorizaed access. This probably means you are running this executable and can ignore the error, OR you do not have access to delete this file.", telemetryExePath), LogLevel.Debug);
						doCopy = false;
					}
				}
				if (doCopy)
				{
					if (!Directory.Exists(telemetrySharePath))
					{
						Directory.CreateDirectory(telemetrySharePath);

					}
					TelemetryDashboardMain.Logger.Log("Copying TelemetryDashboard.exe.", LogLevel.Debug);
					File.Copy(FileLocationManager.WorkingTelemetryDashboardExePath, telemetryExePath);
				}
			}


			TelemetryDashboardMain.Logger.Log("Ready to import app.", LogLevel.Debug);
			string appGUID = ImportApp();

			TelemetryDashboardMain.Logger.Log("Ready to create data connections.", LogLevel.Debug);
			CreateDataConnections();

			TelemetryDashboardMain.Logger.Log("Ready to create tasks.", LogLevel.Debug);
			CreateTasks(appGUID, telemetryExePath);

			return 0;
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
				TelemetryDashboardMain.Logger.Log("A single existing App 'TelemetryDashboard' was found.", LogLevel.Info);

				string appID = listOfApps[0]["id"].ToString();
				Tuple<HttpStatusCode, string> replaceAppResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/app/upload/replace?targetappid=" + appID, HttpMethod.Post, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
				if (replaceAppResponse.Item1 == HttpStatusCode.Created)
				{
					TelemetryDashboardMain.Logger.Log("App 'TelemetryDashboard' was replaced.", LogLevel.Info);
					return JObject.Parse(replaceAppResponse.Item2)["id"].ToString();
				}
				else
				{
					throw new InvalidResponseException(apps.Item1.ToString() + " returned when trying to replace Telemetry Dashboard app. Request failed.");
				}
			}


			if (listOfApps.Count > 1)
			{
				TelemetryDashboardMain.Logger.Log("Multiple existing Apps 'TelemetryDashboard' were found, they will be renamed.", LogLevel.Info);

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
			TelemetryDashboardMain.Logger.Log("App 'TelemetryDashboard' was successfully imported.", LogLevel.Info);

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
				TelemetryDashboardMain.Logger.Log("Existing Data Connection 'TelemetryMetadata' was not found.", LogLevel.Info);

				string connectionStringPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME) + @"\";
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
				TelemetryDashboardMain.Logger.Log("Data Connection 'TelemetryMetadata' was created.", LogLevel.Info);

			}
			else
			{
				TelemetryDashboardMain.Logger.Log("Existing Data Connection 'TelemetryMetadata' was found.", LogLevel.Info);

				listOfDataconnections[0]["connectionstring"] = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER_NAME) + "\\";
				listOfDataconnections[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
				string appId = listOfDataconnections[0]["id"].ToString();
				Tuple<HttpStatusCode, string> updatedConnection = TelemetryDashboardMain.QRSRequest.MakeRequest("/dataconnection/" + appId, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfDataconnections[0].ToString()));
				if (updatedConnection.Item1 != HttpStatusCode.OK)
				{
					throw new InvalidResponseException(dataConnections.Item1.ToString() + " returned when trying to update 'TelemetryMetadata' data connection. Request failed.");
				}
				TelemetryDashboardMain.Logger.Log("Data Connection 'TelemetryMetadata' was updated.", LogLevel.Info);
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
				TelemetryDashboardMain.Logger.Log("Existing Data Connection 'EngineSettingsFolder' was not found.", LogLevel.Info);

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
				TelemetryDashboardMain.Logger.Log("Data Connection 'EngineSettingsFolder' was updated.", LogLevel.Info);
			}
			else
			{
				TelemetryDashboardMain.Logger.Log("Existing Data Connection 'EngineSettingsFolder' already exists.", LogLevel.Info);
			}

			return;
		}

		private static void CreateTasks(string appId, string telemetryDashboardPath)
		{
			string externalTaskID = "";
			// External Task
			Tuple<HttpStatusCode, string> hasExternalTask = TelemetryDashboardMain.QRSRequest.MakeRequest("/externalprogramtask/count?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
			if (hasExternalTask.Item1 != HttpStatusCode.OK)
			{
				throw new InvalidResponseException(hasExternalTask.Item1.ToString() + " returned when trying to get 'TelemetryDashboard-1-Generate-Metadata' external task. Request failed.");
			}
			if (JObject.Parse(hasExternalTask.Item2)["value"].ToObject<int>() == 0)
			{
				TelemetryDashboardMain.Logger.Log("No 'TelemetryDashboard-1-Generate-Metadata' was found. Creating new task.", LogLevel.Info);

				string body = @"
			{
				'path': '" + telemetryDashboardPath.Replace("\\", "\\\\") + @"',
				'parameters': '-fetchmetadata',
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
					TelemetryDashboardMain.Logger.Log("Task 'TelemetryDashboard-1-Generate-Metadata' was created.", LogLevel.Info);
					externalTaskID = JObject.Parse(createExternalTask.Item2)["id"].ToString();
					TelemetryDashboardMain.Logger.Log("Task 'TelemetryDashboard-1-Generate-Metadata' ID is: " + externalTaskID, LogLevel.Debug);

				}
			}
			else
			{
				TelemetryDashboardMain.Logger.Log("Existing 'TelemetryDashboard-1-Generate-Metadata' was found.", LogLevel.Info);
				Tuple<HttpStatusCode, string> getExternalTaskId = TelemetryDashboardMain.QRSRequest.MakeRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
				externalTaskID = JArray.Parse(getExternalTaskId.Item2)[0]["id"].ToString();
				TelemetryDashboardMain.Logger.Log("Task 'TelemetryDashboard-1-Generate-Metadata' ID is: " + externalTaskID, LogLevel.Debug);
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
				TelemetryDashboardMain.Logger.Log("Existing 'TelemetryDashboard-2-Reload-Dashboard' was not found.", LogLevel.Info);

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
				TelemetryDashboardMain.Logger.Log("Task 'TelemetryDashboard-2-Reload-Dashboard' was created.", LogLevel.Info);
			}
			else
			{
				TelemetryDashboardMain.Logger.Log("Existing 'TelemetryDashboard-2-Reload-Dashboard' was found.", LogLevel.Info);
				listOfTasks[0]["app"] = JObject.Parse(@"{ 'id': '" + appId + "'}");
				listOfTasks[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
				string reloadTaskID = listOfTasks[0]["id"].ToString();
				Tuple<HttpStatusCode, string> updatedTaskResponse = TelemetryDashboardMain.QRSRequest.MakeRequest("/reloadtask/" + reloadTaskID, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfTasks[0].ToString()));
				if (updatedTaskResponse.Item1 != HttpStatusCode.OK)
				{
					throw new InvalidResponseException(updatedTaskResponse.Item1.ToString() + " returned when trying to update 'TelemetryDashboard-2-Reload-Dashboard' external task. Request failed.");
				}
				TelemetryDashboardMain.Logger.Log("Task 'TelemetryDashboard-2-Reload-Dashboard' was updated.", LogLevel.Info);
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
