using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

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
		private int _ticker;

		public Logger(string logFilePath, LogLocation logLocation)
		{
			_logFileFullPath = Path.Combine(logFilePath, LOGFILENAME+".txt");
			_logLocation = logLocation;
			_ticker = 0;
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
			catch (IOException ioException)
			{
			}
			catch (Exception ex)
			{

			}
		}

		public void Log(string message, LogLevel level)
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
