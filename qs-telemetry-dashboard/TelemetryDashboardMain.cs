using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;

using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Initialize;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.MetadataFetch;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard
{
	class TelemetryDashboardMain
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

		internal static TelemetryLogger Logger { get; private set; }

		internal static ArgumentManager ArgsManager { get; private set; }

		static int Main(string[] args)
		{
			bool isCMDRun = GetConsoleProcessList(new uint[1], 1) == 2;
			try
			{
				ArgsManager = new ArgumentManager(args, isCMDRun);
			}
			catch (ArgumentManagerException argManagerEx)
			{
				Console.WriteLine("Error handling arguments:\n" + argManagerEx.Message);
				return 1;
			}

			string pwd = AppDomain.CurrentDomain.BaseDirectory;
			LogLocation location = LogLocation.File;
			LogLevel level = LogLevel.Info;
			if (ArgsManager.Interactive)
			{
				location = LogLocation.FileAndConsole;
			}
			if (ArgsManager.DebugLog)
			{
				level = LogLevel.Debug;
			}

			try
			{
				Logger = new TelemetryLogger(pwd, location, level);
			}
			catch (LoggingException logException)
			{
				Console.WriteLine("Error setting up logging:\n" + logException.Message);
				if (logException.InnerException != null)
				{
					Console.WriteLine("\nInner exception:\n" + logException.InnerException.Message);
				}
				return 1;
			}
			Logger.Log("Arguments handled and logging initialized.", LogLevel.Info);
			Logger.Log("Current working directory: " + pwd, LogLevel.Debug);

			if (ArgsManager.TestCredentialRun)
			{
				// this one is done, but needs logging.
				return TestCredentialRun();
			}
			// wrap stuff in try catches and catch exceptions
			else if (ArgsManager.UpdateCertificateRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(pwd);
				QlikCredentials creds = IOHelpers.GetCredentials();
				string hostname = IOHelpers.GetHostname();
				configManager.SetConfig(hostname, CertificateHelpers.FetchCertificate(creds));
				return 0;
			}
			else if (ArgsManager.TestConfigurationRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(pwd);
				if (!configManager.HasConfiguration)
				{
					Logger.Log("Failed to get configuration. Unable to test configuration. Test failed.", LogLevel.Error);
					return 1;
				}
				HttpStatusCode statusCode = TestConfiguration(new QRSRequest(configManager.Configuration));
				if (statusCode == HttpStatusCode.OK)
				{
					Logger.Log(statusCode.ToString() + " returned. Validation successful.", LogLevel.Debug);
					return 0;
				}
				else
				{
					Logger.Log(statusCode.ToString() + " returned. Failed to get valid response from Qlik Sense Repository.", LogLevel.Error);
					return 1;
				}
			}
			else if (ArgsManager.InitializeRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(pwd);
				if (!configManager.HasConfiguration)
				{
					QlikCredentials creds = IOHelpers.GetCredentials();
					string hostname = IOHelpers.GetHostname();
					configManager.SetConfig(hostname, CertificateHelpers.FetchCertificate(creds));
				}
				InitializeEnvironment initEnv = new InitializeEnvironment(new QRSRequest(configManager.Configuration), pwd);
				return initEnv.Run();
			}
			else if (ArgsManager.MetadataFetchRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(pwd);
				return MetadataFetchRunner.Run(Logger, new QRSRequest(configManager.Configuration));
				// fetch metadata and wriet to csv
			}
			else
			{
				Logger.Log("Unhandled argument.", LogLevel.Error);
				return 1;
			}
		}

		private static int TestCredentialRun()
		{
			TelemetryConfiguration configuration = new TelemetryConfiguration();
			configuration.Hostname = "localhost";
			Logger.Log("Test Credential Mode", LogLevel.Debug);
			QlikCredentials creds = IOHelpers.GetCredentials();
			Logger.Log(string.Format("Credentials entered, attempting to fetch Qlik client certificate with user {0}\\{1}.", creds.UserDirectory, creds.UserName), LogLevel.Debug);
			configuration.QlikClientCertificate = CertificateHelpers.FetchCertificate(creds);
			if (configuration.QlikClientCertificate == null)
			{
				Logger.Log("Failed to fetch certificate.", LogLevel.Error);
				return 1;
			}
			Logger.Log("Successfully got Qlik client certificates.", LogLevel.Debug);
			Logger.Log("Querying Qlik Sense Repository GET '\\qrs\\about'", LogLevel.Debug);
			HttpStatusCode statusCode = TestConfiguration(new QRSRequest(configuration));
			if (statusCode == HttpStatusCode.OK)
			{
				Logger.Log(statusCode.ToString() + " returned. Validation successful.", LogLevel.Debug);
				return 0;
			}
			else
			{
				Logger.Log(statusCode.ToString() + " returned. Failed to get valid response from Qlik Sense Repository.", LogLevel.Error);
				return 1;
			}
		}









		private static HttpStatusCode TestConfiguration(QRSRequest qrsRequest)
		{
			Tuple<HttpStatusCode, string> response = qrsRequest.MakeRequest("/about", HttpMethod.Get);
			return response.Item1;
		}
	}
}
