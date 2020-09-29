using System;

namespace qs_telemetry_dashboard.Exceptions
{
	public class ArgumentManagerException : Exception
	{
		public ArgumentManagerException()
		{
		}

		public ArgumentManagerException(string message)
			: base(message)
		{
		}

		public ArgumentManagerException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
