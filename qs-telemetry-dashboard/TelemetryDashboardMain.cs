using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Impersonation;
using qs_telemetry_dashboard.Initialize;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.MetadataFetch;
using qs_telemetry_dashboard.Models;
using qs_telemetry_dashboard.QRSHelpers;

namespace qs_telemetry_dashboard
{
	class TelemetryDashboardMain
	{
		private static Logger _logger;
		private static ArgumentManager _argsManager;
		
		static int Main(string[] args)
		{
			try
			{
				_argsManager = new ArgumentManager(args);
			}
			catch (ArgumentManagerException argManagerEx)
			{
				Console.WriteLine("Error handling arguments:\n" + argManagerEx.Message);
				return 1;
			}

			string pwd = AppDomain.CurrentDomain.BaseDirectory;
			LogLocation location = LogLocation.File;
			LogLevel level = LogLevel.Info;
			if (_argsManager.Interactive)
			{
				location = LogLocation.FileAndConsole;
			}
			if (_argsManager.DebugLog)
			{
				level = LogLevel.Debug;
			}

			try
			{
				_logger = new Logger(pwd, location, level);
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
			_logger.Log("Arguments handled and logging initialized.", LogLevel.Info);
			_logger.Log("Current working directory: " + pwd, LogLevel.Debug);

			if (_argsManager.TestCredentialRun)
			{
				return TestCredentialRun();
			}
			// wrap stuff in try catches and catch exceptions
			else if (_argsManager.UpdateCertificateRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(_logger, pwd);
				QlikCredentials creds = GetCredentials();
				string hostname = GetHostname();
				configManager.SetConfig(hostname, FetchCertificate(creds));
				return 0;
			}
			else if (_argsManager.TestConfigurationRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(_logger, pwd);
				if (!configManager.HasConfiguration)
				{
					_logger.Log("Failed to get configuration. Unable to test configuration. Test failed.", LogLevel.Error);
					return 1;
				}
				HttpStatusCode statusCode = TestConfiguration(new QRSRequest(configManager.Configuration));
				if (statusCode == HttpStatusCode.OK)
				{
					_logger.Log(statusCode.ToString() + " returned. Validation successful.", LogLevel.Debug);
					return 0;
				}
				else
				{
					_logger.Log(statusCode.ToString() + " returned. Failed to get valid response from Qlik Sense Repository.", LogLevel.Error);
					return 1;
				}
			}
			else if (_argsManager.InitializeRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(_logger, pwd);
				if (!configManager.HasConfiguration)
				{
					QlikCredentials creds = GetCredentials();
					string hostname = GetHostname();
					configManager.SetConfig(hostname, FetchCertificate(creds));
				}
				InitializeEnvironment initEnv = new InitializeEnvironment(_logger, new QRSRequest(configManager.Configuration), pwd);
				return initEnv.Run();
			}
			else if (_argsManager.MetadataFetchRun)
			{
				ConfigurationManager configManager = new ConfigurationManager(_logger, pwd);
				return MetadataFetchRunner.Run(_logger, new QRSRequest(configManager.Configuration));
				// fetch metadata and wriet to csv
			}
			else
			{
				_logger.Log("Unhandled argument.", LogLevel.Error);
				return 1;
			}
		}

		private static int TestCredentialRun()
		{
			TelemetryConfiguration configuration = new TelemetryConfiguration();
			configuration.Hostname = "localhost";
			_logger.Log("Test Credential Mode", LogLevel.Debug);
			QlikCredentials creds = GetCredentials();
			_logger.Log(string.Format("Credentials entered, attempting to fetch Qlik client certificate with user {0}\\{1}.", creds.UserDirectory, creds.UserName), LogLevel.Debug);
			configuration.QlikClientCertificate = FetchCertificate(creds);
			if (configuration.QlikClientCertificate == null)
			{
				_logger.Log("Failed to fetch certificate.", LogLevel.Error);
				return 1;
			}
			_logger.Log("Successfully got Qlik client certificates.", LogLevel.Debug);
			_logger.Log("Querying Qlik Sense Repository GET '\\qrs\\about'", LogLevel.Debug);
			HttpStatusCode statusCode = TestConfiguration(new QRSRequest(configuration));
			if (statusCode == HttpStatusCode.OK)
			{
				_logger.Log(statusCode.ToString() + " returned. Validation successful.", LogLevel.Debug);
				return 0;
			}
			else
			{
				_logger.Log(statusCode.ToString() + " returned. Failed to get valid response from Qlik Sense Repository.", LogLevel.Error);
				return 1;
			}
		}

		private static QlikCredentials GetCredentials()
		{
			QlikCredentials creds = new QlikCredentials();
			_logger.Log("Below you will need to enter in credentials for the user running the Qlik Sense Repository Service on this server.", LogLevel.Info);
			Console.Write("User Domain: ");
			creds.UserDirectory = Console.ReadLine();
			Console.Write("Username: ");
			creds.UserName = Console.ReadLine();
			Console.Write("User Password: ");
			creds.Password = GetPassword();

			return creds;
		}

		public static SecureString GetPassword()
		{
			SecureString password = new SecureString();

			// get the first character of the password
			ConsoleKeyInfo nextKey = Console.ReadKey(true);

			while (nextKey.Key != ConsoleKey.Enter)
			{
				if (nextKey.Key == ConsoleKey.Backspace)
				{
					if (password.Length > 0)
					{
						password.RemoveAt(password.Length - 1);

						// erase the last * as well
						Console.Write(nextKey.KeyChar);
						Console.Write(" ");
						Console.Write(nextKey.KeyChar);
					}
				}
				else
				{
					password.AppendChar(nextKey.KeyChar);
					Console.Write("*");
				}

				nextKey = Console.ReadKey(true);
			}

			Console.WriteLine();

			// lock the password down
			password.MakeReadOnly();
			return password;
		}

		private static string GetHostname()
		{
			Console.Write("Central node hostname: ");
			string hostname = Console.ReadLine();
			_logger.Log("Hostname entered: '" + hostname + "'.", LogLevel.Info);
			return hostname;
		}

		private static X509Certificate2 FetchCertificate(QlikCredentials credentials)
		{
			X509Certificate2 cert = null;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator(credentials.UserDirectory, credentials.UserName, credentials.Password))
				{
					X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
					store.Open(OpenFlags.ReadOnly);
					cert = store.Certificates.Cast<X509Certificate2>().FirstOrDefault(c => c.FriendlyName == "QlikClient");
					store.Close();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}

			return cert;
		}

		private static HttpStatusCode TestConfiguration(QRSRequest qrsRequest)
		{
			Tuple<HttpStatusCode, string> response = qrsRequest.MakeRequest("/about", HttpMethod.Get);
			return response.Item1;
		}
	}
}
