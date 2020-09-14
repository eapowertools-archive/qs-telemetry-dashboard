using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;

using qs_telemetry_dashboard.Impersonation;

namespace qs_telemetry_dashboard
{
	class Program
	{
		private static X509Certificate2 QlikCertificate { get; set; }
		
		static void Main(string[] args)
		{
			QlikCertificate = FetchCertificate();
			Console.ReadKey();
		}

		private static X509Certificate2 FetchCertificate()
		{
			X509Certificate2 cert = null;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator("qlikservice", "qliktest", "Qlik!234"))
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

			//Create URL to REST endpoint for tickets
			string url = "https://localhost:4242/qrs/about";

			//Create the HTTP Request and add required headers and content in Xrfkey
			string Xrfkey = "0123456789abcdef";
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "?Xrfkey=" + Xrfkey);
			// Add the method to authentication the user
			request.ClientCertificates.Add(certificate_);
			request.Method = "GET";
			request.Accept = "application/json";
			request.Headers.Add("X-Qlik-Xrfkey", Xrfkey);
			request.Headers.Add("X-Qlik-User", "userDirectory=INTERNAL;UserId=sa_api");

			// make the web request and return the content
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();
			Console.WriteLine(stream != null ? new StreamReader(stream).ReadToEnd() : string.Empty);
			Console.WriteLine("Done.");
		}
	}
}
