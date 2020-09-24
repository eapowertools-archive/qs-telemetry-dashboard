﻿using Newtonsoft.Json.Linq;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models;
using qs_telemetry_dashboard.QRSHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace qs_telemetry_dashboard.Initialize
{
	internal class InitializeEnvironment
	{
		private readonly Logger _logger;
		private readonly QRSRequest _qrsRequest;

		internal InitializeEnvironment(Logger logger, QRSRequest qrsRequest)
		{
			_logger = logger;
			_qrsRequest = qrsRequest;
		}
		internal int Run()
		{
			//todo this is sloppy. should be encapsulated or nonstatic
			_logger.Log("Running in initialize mode.", LogLevel.Info);



			return 0;
		}






			private static Lazy<string> HOSTNAME = new Lazy<string>(GetHostnameFromConfig);
			private static Lazy<X509Certificate2> SENSE_CERT = new Lazy<X509Certificate2>(SetTLSandGetCertificate);
			private static string OUTPUT_FOLDER = "TelemetryDashboard";
			private static string JS_LIBRARY_FOLDER = "MetadataGenerater";
			private static string METADATA_OUTPUT = "MetadataOutput";

			

			public string ValidateInstallDir()
			{
				string installDir = session["INSTALLFOLDER"];
				session.Log("Install directory to validate: " + installDir);

				if (!installDir.EndsWith("\\"))
				{
					installDir += "\\";
				}

				try
				{
					if (!Regex.IsMatch(installDir.Substring(0, 3), "\\\\[a-zA-Z0-9]"))
					{
						throw new ArgumentException("Installer path must be a network locattion (start with \"\\\\\").");
					}

					if (!installDir.EndsWith("\\" + OUTPUT_FOLDER + "\\"))
					{
						throw new ArgumentException("Telemetry Dashboard but be installed to \"" + OUTPUT_FOLDER + "\" folder on share (installer directory must end with \"\\" + OUTPUT_FOLDER + "\").");
					}

					installDir = installDir.Substring(0, installDir.Length - (OUTPUT_FOLDER.Length + 1));

					string[] dirs = Directory.GetDirectories(installDir);
					for (int i = 0; i < dirs.Length; i++)
					{
						dirs[i] = dirs[i].Substring(installDir.Length);
					}

					if (!(dirs.Contains("Apps") || dirs.Contains("ArchivedLogs") || dirs.Contains("CustomData") || dirs.Contains("StaticContent")))
					{
						session.Message(InstallMessage.Warning, new Record() { FormatString = "Installer did not find an 'Apps', 'StaticContent', 'ArchivedLogs' or 'StaticContent' folder. Install will proceed but Telemetry Dashboard may not function if not installed in root Qlik Sense share folder." });
					}
				}
				catch (ArgumentException e)
				{
					session.Message(InstallMessage.Error, new Record() { FormatString = "The install directory was not valid:\n" + e.Message });
					return "Failure";
				}
				catch (Exception e)
				{
					session.Message(InstallMessage.Error, new Record() { FormatString = "The install directory validation failed:\n" + e.Message });
					return "Failure";
				}

				return "Success";
			}

			public string SetOutputDir()
			{
				string installDir = session.CustomActionData["InstallDir"];
				string outputDir = Path.Combine(installDir, METADATA_OUTPUT);

				outputDir = outputDir.Replace('\\', '/');
				if (!outputDir.EndsWith("/"))
				{
					outputDir += '/';
				}
				string text = File.ReadAllText(installDir + JS_LIBRARY_FOLDER + "\\config\\config.js");
				text = text.Replace("outputFolderPlaceholder", outputDir);
				File.WriteAllText(installDir + JS_LIBRARY_FOLDER + "\\config\\config.js", text);

				return "Success";
			}

			public string IsRepositoryRunning()
			{
				session.Log("Making a request to 'https://{0}:4242", HOSTNAME.Value);
				Tuple<HttpStatusCode, string> response = _qrsRequest.MakeRequest("/about", HttpMethod.Get);
				if (response.Item1 == HttpStatusCode.OK)
				{
					return "Success";
				}
				else
				{
					session.Log("IsRepositoryRunning custom action failed.");
					session.Log("Response was: {0}", response.Item1.ToString());

					session.Message(InstallMessage.Error, new Record() { FormatString = "Cannot install the Telemetry Dashboard as the installer could not contact the 'Qlik Repository Service'. Response code was: " + response.Item1.ToString() });
					return "Failure";
				}
			}

			public string ImportApp()
			{
				Tuple<HttpStatusCode, string> apps = _qrsRequest.MakeRequest("/app/full?filter=name eq 'Telemetry Dashboard'", HttpMethod.Get);
				if (apps.Item1 != HttpStatusCode.OK)
				{
					return "Failure";
				}

				JArray listOfApps = JArray.Parse(apps.Item2);

				if (listOfApps.Count == 1)
				{
					string appID = listOfApps[0]["id"].ToString();
					Tuple<HttpStatusCode, string> replaceAppResponse = _qrsRequest.MakeRequest("/app/upload/replace?targetappid=" + appID, HttpMethod.Post, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
					if (replaceAppResponse.Item1 == HttpStatusCode.Created)
					{
						return "Success";
					}
				}

				else
				{
					if (listOfApps.Count > 1)
					{
						for (int i = 0; i < listOfApps.Count; i++)
						{
							listOfApps[i]["name"] = listOfApps[i]["name"] + "-old";
							listOfApps[i]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
							string appId = listOfApps[i]["id"].ToString();
							Tuple<HttpStatusCode, string> updatedApp = _qrsRequest.MakeRequest("/app/" + appId, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfApps[i].ToString()));
							if (updatedApp.Item1 != HttpStatusCode.OK)
							{
								return "Failure";
							}
						}
					}

					Tuple<HttpStatusCode, string> uploadAppResponse = _qrsRequest.MakeRequest("/app/upload?name=Telemetry Dashboard", HttpMethod.Post, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
					if (uploadAppResponse.Item1 == HttpStatusCode.Created)
					{
						return "Success";
					}
				}
				return "Failure";
			}

			public string CreateTasks()
			{
				string installDir = session.CustomActionData["InstallDir"];
				if (!installDir.EndsWith("\\"))
				{
					installDir += "\\";
				}

				string externalTaskID = "";
				// External Task
				Tuple<HttpStatusCode, string> hasExternalTask = _qrsRequest.MakeRequest("/externalprogramtask/count?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
				if (hasExternalTask.Item1 != HttpStatusCode.OK)
				{
					return "Failure";
				}
				if (JObject.Parse(hasExternalTask.Item2)["value"].ToObject<int>() == 0)
				{
					installDir = installDir.Replace("\\", "\\\\");
					string body = @"
				{
					'path': '..\\ServiceDispatcher\\Node\\node.exe',
					'parameters': '""" + Path.Combine(installDir, JS_LIBRARY_FOLDER) + @"\\fetchMetadata.js""',
					'name': 'TelemetryDashboard-1-Generate-Metadata',
					'taskType': 1,
					'enabled': true,
					'taskSessionTimeout': 1440,
					'maxRetries': 0,
					'impactSecurityAccess': false,
					'schemaPath': 'ExternalProgramTask'
				}";
					Tuple<HttpStatusCode, string> createExternalTask = _qrsRequest.MakeRequest("/externalprogramtask", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
					if (createExternalTask.Item1 != HttpStatusCode.Created)
					{
						return "Failure";
					}
					else
					{
						externalTaskID = JObject.Parse(createExternalTask.Item2)["id"].ToString();
					}
				}
				else
				{
					Tuple<HttpStatusCode, string> getExternalTaskId = _qrsRequest.MakeRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
					externalTaskID = JArray.Parse(getExternalTaskId.Item2)[0]["id"].ToString();

				}

				// Reload Task
				Tuple<HttpStatusCode, string> reloadTasks = _qrsRequest.MakeRequest("/reloadtask/full?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HttpMethod.Get);
				if (reloadTasks.Item1 != HttpStatusCode.OK)
				{
					return "Failure";
				}

				JArray listOfTasks = JArray.Parse(reloadTasks.Item2);

				// Get AppID for Telemetry Dashboard App
				Tuple<HttpStatusCode, string> getAppID = _qrsRequest.MakeRequest("/app?filter=name eq 'Telemetry Dashboard'", HttpMethod.Get);
				string appId = JArray.Parse(getAppID.Item2)[0]["id"].ToString();

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

					Tuple<HttpStatusCode, string> importExtensionResponse = _qrsRequest.MakeRequest("/reloadtask/create", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
					if (importExtensionResponse.Item1 != HttpStatusCode.Created)
					{
						return "Failure";
					}
				}
				else
				{
					listOfTasks[0]["app"] = JObject.Parse(@"{ 'id': '" + appId + "'}");
					listOfTasks[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
					string reloadTaskID = listOfTasks[0]["id"].ToString();
					Tuple<HttpStatusCode, string> updatedApp = _qrsRequest.MakeRequest("/reloadtask/" + reloadTaskID, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfTasks[0].ToString()));
					if (updatedApp.Item1 != HttpStatusCode.OK)
					{
						return "Failure";
					}
				}

				return "Success";
			}

			public string CreateDataConnections()
			{
				string installDir = session.CustomActionData["InstallDir"];
				installDir = installDir.Replace("\\", "\\\\");

				// Add TelemetryMetadata dataconnection
				Tuple<HttpStatusCode, string> dataConnections = _qrsRequest.MakeRequest("/dataconnection?filter=name eq 'TelemetryMetadata'", HttpMethod.Get);
				if (dataConnections.Item1 != HttpStatusCode.OK)
				{
					return "Failure";
				}
				JArray listOfDataconnections = JArray.Parse(dataConnections.Item2);
				if (listOfDataconnections.Count == 0)
				{
					string body = @"
				{
					'name': 'TelemetryMetadata',
					'connectionstring': '" + installDir + METADATA_OUTPUT + @"\\',
					'type': 'folder',
					'username': ''
				}";


					Tuple<HttpStatusCode, string> createdConnection = MakeRequest("/dataconnection", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
					if (createdConnection.Item1 != HttpStatusCode.Created)
					{
						return "Failure";
					}
				}
				else
				{
					installDir = installDir.Replace("\\\\", "\\");
					listOfDataconnections[0]["connectionstring"] = installDir + METADATA_OUTPUT + "\\";
					listOfDataconnections[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
					string appId = listOfDataconnections[0]["id"].ToString();
					Tuple<HttpStatusCode, string> updatedConnection = _qrsRequest.MakeRequest("/dataconnection/" + appId, HttpMethod.Put, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfDataconnections[0].ToString()));
					if (updatedConnection.Item1 != HttpStatusCode.OK)
					{
						return "Failure";
					}
				}

				// Add EngineSettings dataconnection
				Tuple<HttpStatusCode, string> engineSettingDataconnection = _qrsRequest.MakeRequest("/dataconnection?filter=name eq 'EngineSettingsFolder'", HttpMethod.Get);
				if (dataConnections.Item1 != HttpStatusCode.OK)
				{
					return "Failure";
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

					Tuple<HttpStatusCode, string> createdConnection = _qrsRequest.MakeRequest("/dataconnection", HttpMethod.Post, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
					if (createdConnection.Item1 != HttpStatusCode.Created)
					{
						return "Failure";
					}
				}

				return "Success";
			}

			public string CopyCertificates()
			{
				string installDir = session.CustomActionData["InstallDir"];
				File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\root.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\root.pem"), true);
				File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\client.pem"), true);
				File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\client_key.pem"), true);

				return "Success";
			}

			public string RemoveTasks()
			{
				Tuple<HttpStatusCode, string> getReloadTaskId = _qrsRequest.MakeRequest("/reloadtask?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HttpMethod.Get);
				if (getReloadTaskId.Item1 == HttpStatusCode.OK)
				{
					JArray reloadTasks = JArray.Parse(getReloadTaskId.Item2);
					foreach (JToken t in reloadTasks)
					{
						_qrsRequest.MakeRequest("/reloadtask/" + t["id"], HttpMethod.Delete);
					}
				}

				Tuple<HttpStatusCode, string> getExternalTaskId = _qrsRequest.MakeRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HttpMethod.Get);
				if (getExternalTaskId.Item1 == HttpStatusCode.OK)
				{
					JArray externalTasks = JArray.Parse(getExternalTaskId.Item2);
					foreach (JToken t in externalTasks)
					{
						_qrsRequest.MakeRequest("/externalprogramtask/" + t["id"], HttpMethod.Delete);
					}
				}

				return "Success";
			}

			private X509Certificate2 SetTLSandGetCertificate()
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
				var clientPem = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem");
				var clientKeyPem = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem");
				byte[] certBuffer = HelperFunctions.GetBytesFromPEM(clientPem, Helpers.PemStringType.Certificate);
				byte[] certKeyBuffer = HelperFunctions.GetBytesFromPEM(clientKeyPem, Helpers.PemStringType.RsaPrivateKey);

				X509Certificate2 cert = new X509Certificate2(certBuffer);

				RSACryptoServiceProvider provider = Crypto.DecodeRsaPrivateKey(certKeyBuffer);
				cert.PrivateKey = provider;
				return cert;
			}

			private string GetHostnameFromConfig()
			{
				string hostnameBase64 = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Host.cfg");
				byte[] data = Convert.FromBase64String(hostnameBase64);
				string hostname = Encoding.ASCII.GetString(data);
				return hostname;
			}
}
}
