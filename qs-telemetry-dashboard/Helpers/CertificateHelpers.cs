using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using qs_telemetry_dashboard.Impersonation;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class CertificateHelpers
	{
		internal static X509Certificate2 FetchCertificate(QlikCredentials credentials)
		{
			X509Certificate2 cert = null;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator(credentials.UserDirectory, credentials.UserName, credentials.Password))
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
	}
}
