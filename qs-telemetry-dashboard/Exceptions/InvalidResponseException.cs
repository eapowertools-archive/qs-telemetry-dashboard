using System;
namespace qs_telemetry_dashboard.Exceptions
{
	public class InvalidResponseException : Exception
	{
		public InvalidResponseException()
		{
		}

		public InvalidResponseException(string message)
			: base(message)
		{
		}

		public InvalidResponseException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
