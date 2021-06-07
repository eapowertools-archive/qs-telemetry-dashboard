using System;
using System.IO;

using qs_telemetry_dashboard.Exceptions;

namespace qs_telemetry_dashboard.Logging
{
	internal enum LogLocation
	{
		File,
		FileAndConsole
	}
	internal enum LogLevel
	{
		Error,
		Info,
		Debug
	}

	internal class TelemetryLogger
	{
		private static string LOGFILENAME = "log";
		private readonly string _logFileFullPath = "";
		private readonly LogLocation _logLocation;
		private readonly LogLevel _logLevel;
		private int _ticker;

		public TelemetryLogger(string logFilePath, LogLocation logLocation, LogLevel level)
		{
			_logFileFullPath = Path.Combine(logFilePath, LOGFILENAME+".txt");
			_logLocation = logLocation;
			_logLevel = level;
			_ticker = 0;

			string tdVersion = typeof(TelemetryLogger).Assembly.GetName().Version.ToString();
			tdVersion = tdVersion.Substring(0, tdVersion.Length - 2);
			tdVersion = " v" + tdVersion;
			if(tdVersion.Length > 30)
			{
				throw new Exception("Version number is way too long.");
			}
			while (tdVersion.Length < 30)
			{
				tdVersion += ' ';
			}

			try
			{
				if (File.Exists(_logFileFullPath))
				{
					File.Delete(_logFileFullPath);
				}
				using (StreamWriter sw = new StreamWriter(_logFileFullPath))
				{
					sw.WriteLine("--------------------------------");
					sw.WriteLine("| Telemetry Dashboard log file |");
					sw.WriteLine("|" + tdVersion + "|");
					sw.WriteLine("| " + DateTime.UtcNow.ToString("o") + " |");
					sw.WriteLine("--------------------------------");
					sw.WriteLine(_logFileFullPath + '\n');
				}
			}
			catch (UnauthorizedAccessException unauthorizedException)
			{
				throw new LoggingException("Unauthorized Access Exception thrown.", unauthorizedException);
			}
			catch (IOException ioException)
			{
				throw new LoggingException("IO Exception thrown.", ioException);

			}
			catch (Exception ex)
			{
				throw new LoggingException("Unhandled exception thrown", ex);
			}
		}

		private void WriteToFile(string message)
		{
			try
			{
				using (TextWriter w = File.AppendText(_logFileFullPath))
				{
					w.WriteLine(message);
				}
			}
			catch (UnauthorizedAccessException unauthorizedException)
			{
				throw new LoggingException("Unauthorized Access Exception thrown.", unauthorizedException);
			}
			catch (IOException ioException)
			{
				throw new LoggingException("IO Exception thrown.", ioException);

			}
			catch (Exception ex)
			{
				throw new LoggingException("Unhandled exception thrown", ex);
			}
		}

		public void Log(string message, LogLevel level)
		{
			if (level <= _logLevel)
			{
				_ticker++;
				// generate message
				string logMessage = String.Format("{0}\t{1}\t{2}\t{3}", _ticker, DateTime.UtcNow.ToString("o"), level, message);

				if (_logLocation == LogLocation.FileAndConsole)
				{
					Console.WriteLine(logMessage);
				}
				WriteToFile(logMessage);
			}
		}
	}
}
