using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
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

	internal class Logger
	{
		private static string LOGFILENAME = "log";
		private readonly string _logFileFullPath = "";
		private readonly LogLocation _logLocation;
		private readonly LogLevel _logLevel;
		private int _ticker;

		public Logger(string logFilePath, LogLocation logLocation, LogLevel level)
		{
			_logFileFullPath = Path.Combine(logFilePath, LOGFILENAME+".txt");
			_logLocation = logLocation;
			_logLevel = level;
			_ticker = 0;

			try
			{
				if (File.Exists(_logFileFullPath))
				{
					File.Delete(_logFileFullPath);
				}
				File.Create(_logFileFullPath);
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
				string logMessage = String.Format("{0}\t\t{1}\t\t{2}\t\t{3}", _ticker, DateTime.Now.ToLongDateString(), level, message);

				if (_logLocation == LogLocation.FileAndConsole)
				{
					Console.WriteLine(logMessage);
				}
				WriteToFile(logMessage);
			}
		}
	}
}
