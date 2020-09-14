using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Impersonation;

namespace qs_telemetry_dashboard
{
	class Program
	{

		private static TelemetryConfiguration _configuration;
		private static X509Certificate2 QlikCertificate { get; set; }
		
		static void Main(string[] args)
		{
			// Load Configuration from file or create a new one
			Console.Write("Enter password: ");
			_configuration = new TelemetryConfiguration();
			_configuration.UserDirectory = "qliktest";
			_configuration.UserName = "qservice";
			_configuration.Hostname = "localhost";
			_configuration.Password = GetPassword();


			QlikCertificate = FetchCertificate();
			TestConfiguration();
			Console.ReadKey();
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

		private static X509Certificate2 FetchCertificate()
		{
			X509Certificate2 cert = null;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator(_configuration.UserDirectory, _configuration.UserName, _configuration.Password))
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

		private static bool TestConfiguration()
		{
			//Create URL to REST endpoint for tickets
			string url = "https://" + _configuration.Hostname + ":4242/qrs/about";

			//Create the HTTP Request and add required headers and content in Xrfkey
			string Xrfkey = "0123456789abcdef";
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "?Xrfkey=" + Xrfkey);
			// Add the method to authentication the user
			request.ClientCertificates.Add(QlikCertificate);
			request.Method = "GET";
			request.Accept = "application/json";
			request.Headers.Add("X-Qlik-Xrfkey", Xrfkey);
			request.Headers.Add("X-Qlik-User", "userDirectory=INTERNAL;UserId=sa_api");

			// make the web request and return the content
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();
			Console.WriteLine(stream != null ? new StreamReader(stream).ReadToEnd() : string.Empty);

			return true;
		}
	}
}
