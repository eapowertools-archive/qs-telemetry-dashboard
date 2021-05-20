using System;
using System.ComponentModel.Design;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Initialize;
using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.MetadataFetch;

namespace qs_telemetry_dashboard
{
	class TelemetryDashboardMain
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

		internal static TelemetryLogger Logger { get; private set; }

		internal static ArgumentManager ArgsManager { get; private set; }

		internal static QlikRepositoryRequester QRSRequest
		{
			get
			{
				if (_qrsInstance == null)
				{
					throw new InvalidOperationException("QlikRepositoryRequester instance has not been initialized.");
				}
				return _qrsInstance;
			}
		}
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string assemblyName = args.Name.Split(',')[0];
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("qs_telemetry_dashboard.ReferencedAssemblies." + assemblyName + ".dll"))
			{
				var assemblyData = new Byte[stream.Length];
				stream.Read(assemblyData, 0, assemblyData.Length);
				return Assembly.Load(assemblyData);
			}
		}

		private static QlikRepositoryRequester _qrsInstance;

		static int Main(string[] args)
		{
			DateTime startTime = DateTime.UtcNow;
			// Load dlls
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			// Setup Args and Logging
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

			LogLocation location = LogLocation.FileAndConsole;
			LogLevel level = LogLevel.Info;
			if (ArgsManager.TaskTriggered)
			{
				location = LogLocation.File;
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

			// Get certificates and set up QRS Requester
			try
			{
				_qrsInstance = new QlikRepositoryRequester(CertificateConfigHelpers.Hostname, CertificateConfigHelpers.Certificate);
			}
			catch (UnauthorizedAccessException)
			{
				Logger.Log("Failed to get certificates and host.cfg file. This is probably because the executable is not being run either as an administrator or in an administrator command prompt.", LogLevel.Error);
				return 1;
			}

			// Main 

			if (ArgsManager.NoArgs)
			{
				Console.WriteLine(ArgumentManager.HELP_STRING);
				return 0;
			}
			else if (ArgsManager.TestRun)
			{
				Logger.Log("Test Mode:", LogLevel.Info);
				Logger.Log("Checking to see if repository is running.", LogLevel.Info);
				Tuple<bool, HttpStatusCode> responseIsRunning = _qrsInstance.IsRepositoryRunning();
				if (responseIsRunning.Item1)
				{
					Logger.Log(responseIsRunning.Item2.ToString() + " returned. Validation successful.", LogLevel.Debug);
					return 0;
				}
				else
				{
					Logger.Log(responseIsRunning.Item2.ToString() + " returned. Failed to get valid response from Qlik Sense Repository. Either the service is not running or the Telemetry Dashboard cannot connect.", LogLevel.Error);
					return 1;
				}
			}
			else if (ArgsManager.InitializeRun)
			{
				Tuple<bool, HttpStatusCode> responseIsRunning = _qrsInstance.IsRepositoryRunning();
				if (!responseIsRunning.Item1)
				{
					Logger.Log("Failed to connect to Qlik Sense Repository Service. Either it is not running, or the telemetry dashboard cannot reach it.", LogLevel.Error);
					return 1;
				}

				Logger.Log("Preparing to run initialize mode, this will create two tasks, import an application and create two data connections in your environment, press 'q' to quit or any other key to proceed:", LogLevel.Info);

				ConsoleKeyInfo keyPressed = Console.ReadKey();
				Logger.Log("Please enter the 'DOMAIN\\user' for the service account running Qlik Sense Enterprise (without quotes, example: QLIK\\jparis): ", LogLevel.Info);

				string serviceAccount = Console.ReadLine();
				if (serviceAccount.StartsWith("'"))
				{
					serviceAccount = serviceAccount.Substring(1);
				}
				if (serviceAccount.EndsWith("'"))
				{
					serviceAccount = serviceAccount.Substring(0, serviceAccount.Length - 1);
				}
				if (serviceAccount.StartsWith(".\\"))
				{
					serviceAccount = Environment.MachineName + "\\" + serviceAccount.Substring(2);
				}
				Logger.Log("The user '" + serviceAccount + "' was entered.", LogLevel.Info);


				if (keyPressed.Key != ConsoleKey.Q)
				{
					Logger.Log("Initialize Mode:", LogLevel.Info);

					return InitializeEnvironment.Run(serviceAccount, ArgsManager.SkipCopy);
				}
				return 0;
			}
			else if (ArgsManager.FetchMetadataRun)
			{
				Tuple<bool, HttpStatusCode> responseIsRunning = _qrsInstance.IsRepositoryRunning();
				if (!responseIsRunning.Item1)
				{
					Logger.Log("Failed to connect to Qlik Sense Repository Service. Either it is not running, or the telemetry dashboard cannot reach it.", LogLevel.Error);
					return 1;
				}

				Logger.Log("Fetch Metadata Mode:", LogLevel.Info);

				if (ArgsManager.TaskTriggered)
				{
					Logger.Log("Running Fetch Metadata from external program task.", LogLevel.Debug);
				}
				else
				{
					Logger.Log("Running Fetch Metadata from command line.", LogLevel.Debug);
				}

				// fetch metadata and wriet to csv
				int returnVal = MetadataFetchRunner.Run();

				DateTime endTime = DateTime.UtcNow;
				TimeSpan totalTime = endTime - startTime;
				Logger.Log("Fetch metadata took: " + totalTime.ToString(@"hh\:mm\:ss"), LogLevel.Info);

				return returnVal;
			}
			else
			{
				Logger.Log("Unhandled argument.", LogLevel.Error);
				return 1;
			}
		}
	}
}
