using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;

namespace qs_telemetry_dashboard.Impersonation
{
	internal class Impersonator : IDisposable
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
}
