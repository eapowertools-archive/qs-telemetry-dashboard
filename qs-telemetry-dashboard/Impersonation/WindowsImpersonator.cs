using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace qs_telemetry_dashboard.Impersonation
{
	internal class WindowsImpersonator : IDisposable
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
