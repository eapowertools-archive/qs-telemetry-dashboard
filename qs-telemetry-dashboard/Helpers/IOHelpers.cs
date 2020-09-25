using System;
using System.Security;

using qs_telemetry_dashboard.Logging;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class IOHelpers
	{
		internal static QlikCredentials GetCredentials()
		{
			QlikCredentials creds = new QlikCredentials();
			TelemetryDashboardMain.Logger.Log("Below you will need to enter in credentials for the user running the Qlik Sense Repository Service on this server.", LogLevel.Info);
			Console.Write("User Domain: ");
			creds.UserDirectory = Console.ReadLine();
			Console.Write("Username: ");
			creds.UserName = Console.ReadLine();
			Console.Write("User Password: ");
			creds.Password = IOHelpers.GetPassword();

			return creds;
		}

		internal static SecureString GetPassword()
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

		internal static string GetHostname()
		{
			Console.Write("Central node hostname: ");
			string hostname = Console.ReadLine();
			TelemetryDashboardMain.Logger.Log("Hostname entered: '" + hostname + "'.", LogLevel.Info);
			return hostname;
		}
	}
}
