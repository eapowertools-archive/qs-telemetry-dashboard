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
using System.IO;
using System.Configuration;

namespace qs_telemetry_dashboard
{
	class Impersonation
	{
		private static X509Certificate2 certificate_ { get; set; }

		static void Main(string[] args)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			try
			{
				using (WindowsImpersonator svc = new WindowsImpersonator("qlikservice", "DESKTOP-GS8LAA5", "qlik123"))
				{
					X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
					store.Open(OpenFlags.ReadOnly);
					certificate_ = store.Certificates.Cast<X509Certificate2>().FirstOrDefault(c => c.FriendlyName == "QlikClient");
					store.Close();

					Console.WriteLine(certificate_.PrivateKey.ToString());
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}

			Console.WriteLine("Done.");

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
			//X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			//store.Open(OpenFlags.ReadOnly);
			//certificate_ = store.Certificates.Cast<X509Certificate2>().FirstOrDefault(c => c.FriendlyName == "QlikClient");
			//store.Close();
			//ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			//Console.WriteLine();



			//try
			//{
			//	string storeName = "Tang App";
			//	StoreLocation storeLocation = StoreLocation.CurrentUser;
			//	using (Impersonator imp = new Impersonator("TANG_PROC", "", ".123.456.Abc!"))
			//	{
			//		X509Store store = new X509Store(storeName, storeLocation);
			//		store.Open(OpenFlags.ReadWrite);
			//		X509Certificate2 importcert = new X509Certificate2();
			//		importcert.Import(certToImport, certPassword, X509KeyStorageFlags.PersistKeySet);
			//		store.Add(importcert);
			//		store.Close();
			//	}
			//	Console.WriteLine("Certificate is imported.");
			//}
			//catch (Exception ex)
			//{
			//	Console.WriteLine(ex);
			//}
		}
	}


	class Impersonator : IDisposable
	{
		public Impersonator(string userName, string domainName, string password)
		{
			ImpersonateValidUser(userName, domainName, password);
		}

		public void Dispose()
		{
			UndoImpersonation();
		}

		#region P/Invoke.

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int LogonUser(
			string lpszUserName,
			string lpszDomain,
			string lpszPassword,
			int dwLogonType,
			int dwLogonProvider,
			ref IntPtr phToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int DuplicateToken(
			IntPtr hToken,
			int impersonationLevel,
			ref IntPtr hNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern bool CloseHandle(
			IntPtr handle);

		private const int LOGON32_LOGON_NETWORK = 3;
		private const int LOGON32_PROVIDER_DEFAULT = 0;

		#endregion

		private void ImpersonateValidUser(string userName, string domain, string password)
		{
			WindowsIdentity tempWindowsIdentity = null;
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;

			try
			{
				if (RevertToSelf())
				{
					if (LogonUser(
						userName,
						domain,
						password,
						LOGON32_LOGON_NETWORK,
						LOGON32_PROVIDER_DEFAULT,
						ref token) != 0)
					{
						if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
						{
							tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
							impersonationContext = tempWindowsIdentity.Impersonate();
						}
						else
						{
							throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to DuplicateToken.");
						}
					}
					else
					{
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to LogonUser.");
					}
				}
				else
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to RevertToSelf.");
				}
			}
			finally
			{
				if (token != IntPtr.Zero)
				{
					CloseHandle(token);
				}
				if (tokenDuplicate != IntPtr.Zero)
				{
					CloseHandle(tokenDuplicate);
				}
			}
		}

		private void UndoImpersonation()
		{
			if (impersonationContext != null)
			{
				impersonationContext.Undo();
			}
		}

		private WindowsImpersonationContext impersonationContext = null;
	}

	public class WindowsImpersonator : IDisposable
	{
		public WindowsImpersonator(string userName, string domain, string password)
		{
			impersonateValidUser(userName, domain, password);
		}

		public void Dispose()
		{
			//undoImpersonation();
		}

		public const int LOGON32_LOGON_SERVICE = 5;
		public const int LOGON32_PROVIDER_DEFAULT = 0;

		WindowsImpersonationContext impersonationContext;

		[DllImport("advapi32.dll")]
		public static extern int LogonUserA(String lpszUserName,
		String lpszDomain,
		String lpszPassword,
		int dwLogonType,
		int dwLogonProvider,
		ref IntPtr phToken);
		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int DuplicateToken(IntPtr hToken,
		int impersonationLevel,
		ref IntPtr hNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern bool CloseHandle(IntPtr handle);

		public void Page_Load(Object s, EventArgs e)
		{
			if (impersonateValidUser("username", "domain", "password"))
			{
				//Insert your code that runs under the security context of a specific user here.
				undoImpersonation();
			}
			else
			{
				//Your impersonation failed. Therefore, include a fail-safe mechanism here.
			}
		}

		private bool impersonateValidUser(String userName, String domain, String password)
		{
			WindowsIdentity tempWindowsIdentity;
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;

			if (RevertToSelf())
			{
				if (LogonUserA(userName, domain, password, LOGON32_LOGON_SERVICE,
				LOGON32_PROVIDER_DEFAULT, ref token) != 0)
				{
					if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
					{
						tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
						impersonationContext = tempWindowsIdentity.Impersonate();
						if (impersonationContext != null)
						{
							CloseHandle(token);
							CloseHandle(tokenDuplicate);
							return true;
						}
					}
				}
			}
			if (token != IntPtr.Zero)
				CloseHandle(token);
			if (tokenDuplicate != IntPtr.Zero)
				CloseHandle(tokenDuplicate);
			return false;
		}

		private void undoImpersonation()
		{
			impersonationContext.Undo();
		}
	}
}
