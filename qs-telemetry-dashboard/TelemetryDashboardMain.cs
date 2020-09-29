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

		internal static QlikRepositoryRequester QRSRequest {
			get
			{
				if (_qrsInstance == null)
				{
					throw new InvalidOperationException("QlikRepositoryRequester instance has not been initialized.");
				}
				return _qrsInstance;
			}
		}

		private static QlikRepositoryRequester _qrsInstance;

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
				Logger = new TelemetryLogger(FileLocationManager.WorkingDirectory, location, level);
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
			Logger.Log("Current working directory: " + FileLocationManager.WorkingDirectory, LogLevel.Debug);

			if (ArgsManager.TestCredentialRun)
			{
				// this one is done, but needs logging.
				return TestCredentialRun();
			}
			// wrap stuff in try catches and catch exceptions
			else if (ArgsManager.UpdateCertificateRun)
			{
				// doneish, needs to be testing and logged
				QlikCredentials creds = IOHelpers.GetCredentials();
				TelemetryConfiguration tConfig = new TelemetryConfiguration();
				tConfig.QlikClientCertificate = CertificateHelpers.FetchCertificate(creds);
				tConfig.Hostname = InitializeEnvironment.Hostname;
				_qrsInstance = new QlikRepositoryRequester(tConfig);
				ConfigurationManager.SaveConfiguration(tConfig);
				return 0;
			}
			else if (ArgsManager.TestConfigurationRun)
			{
				// try to load from file.
				// validate you could get the config file
				TelemetryConfiguration tConfig;
				if (!ConfigurationManager.HasConfiguration)
				{
					Logger.Log("Failed to get configuration from '" + FileLocationManager.WorkingDirectory + "'. Will need user credentials.", LogLevel.Info);
					QlikCredentials creds = IOHelpers.GetCredentials();
					tConfig = new TelemetryConfiguration();
					tConfig.QlikClientCertificate = CertificateHelpers.FetchCertificate(creds);
					tConfig.Hostname = InitializeEnvironment.Hostname;
					_qrsInstance = new QlikRepositoryRequester(tConfig);
					tConfig = ConfigurationManager.GetConfiguration(false);
				}
				else
				{
					tConfig = ConfigurationManager.GetConfiguration(true);
				}
				_qrsInstance = new QlikRepositoryRequester(tConfig);

				HttpStatusCode statusCode = TestConfiguration();
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
				// validate they want to proceed, will overwrite all existing setup

				QlikCredentials creds = IOHelpers.GetCredentials();
				TelemetryConfiguration tConfig = new TelemetryConfiguration();
				tConfig.QlikClientCertificate = CertificateHelpers.FetchCertificate(creds);
				tConfig.Hostname = InitializeEnvironment.Hostname;
				ConfigurationManager.SaveConfiguration(tConfig);
				_qrsInstance = new QlikRepositoryRequester(tConfig);

				// todo copy telemetrydashboard to correct folder

				return InitializeEnvironment.Run();
			}
			else if (ArgsManager.MetadataFetchRun)
			{
				// is it interactive or not, depends if config should exist or not. log accordingly
				return MetadataFetchRunner.Run();
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
			_qrsInstance = new QlikRepositoryRequester(configuration);
			HttpStatusCode statusCode = TestConfiguration();
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

		private static HttpStatusCode TestConfiguration()
		{
			Tuple<HttpStatusCode, string> response = TelemetryDashboardMain.QRSRequest.MakeRequest("/about", HttpMethod.Get);
			return response.Item1;
		}
	}
}
