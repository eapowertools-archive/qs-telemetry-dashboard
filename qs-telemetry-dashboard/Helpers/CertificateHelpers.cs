using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Impersonation;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class CertificateHelpers
	{
		internal static X509Certificate2 FetchCertificate(QlikCredentials credentials)
		{
			IList<X509Certificate2> certs = null;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator(credentials.UserDirectory, credentials.UserName, credentials.Password))
				{
					X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
					store.Open(OpenFlags.ReadOnly);
					certs = store.Certificates.Cast<X509Certificate2>().Where(c => c.FriendlyName == "QlikClient").ToList();
					store.Close();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}
			if (certs.Count == 0)
			{
				TelemetryDashboardMain.Logger.Log("Failed to find certificate with Friendly Name 'QlikClient'", Logging.LogLevel.Error);
				throw new InvalidResponseException("Could not get QlikClient certificate from certificate store.");
			}
			else if (certs.Count > 1)
			{
				TelemetryDashboardMain.Logger.Log("Found more than 1 certificate with Friendly Name 'QlikClient'", Logging.LogLevel.Error);

				for (int i = 0; i < certs.Count; i++)
				{
					TelemetryDashboardMain.Logger.Log("Got certificate " + i + " with thumbprint: " + certs[i].Thumbprint.ToString(), Logging.LogLevel.Debug);
					TelemetryDashboardMain.Logger.Log("Got certificate " + i + " with friendly name: " + certs[i].FriendlyName.ToString(), Logging.LogLevel.Debug);
					TelemetryDashboardMain.Logger.Log("Got certificate " + i + " with serial number: " + certs[i].SerialNumber.ToString(), Logging.LogLevel.Debug);
				}

				throw new InvalidResponseException("Multiple certificates matched. Only 1 certificate should exist.");


			}

			TelemetryDashboardMain.Logger.Log("Got certificate with thumbprint: " + certs[0].Thumbprint.ToString(), Logging.LogLevel.Debug);
			TelemetryDashboardMain.Logger.Log("Got certificate with friendly name: " + certs[0].FriendlyName.ToString(), Logging.LogLevel.Debug);
			TelemetryDashboardMain.Logger.Log("Got certificate with serial number: " + certs[0].SerialNumber.ToString(), Logging.LogLevel.Debug);
			return certs[0];
		}
	}
}
