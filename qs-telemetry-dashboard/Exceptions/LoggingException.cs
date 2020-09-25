using System;

namespace qs_telemetry_dashboard.Exceptions
{
	public class LoggingException : Exception
	{
		public LoggingException()
		{
		}

		public LoggingException(string message)
			: base(message)
		{
		}

		public LoggingException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
