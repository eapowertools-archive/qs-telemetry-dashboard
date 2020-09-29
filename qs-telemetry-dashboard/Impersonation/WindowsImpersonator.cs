using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace qs_telemetry_dashboard.Impersonation
{
	internal class WindowsImpersonator : IDisposable
	{
		public WindowsImpersonator(string userName, string domain, SecureString password)
		{
			impersonateValidUser(userName, domain, password);
		}

		public void Dispose()
		{
			undoImpersonation();
		}

		public const int LOGON32_LOGON_SERVICE = 5;
		public const int LOGON32_PROVIDER_DEFAULT = 0;

		WindowsImpersonationContext impersonationContext;

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool LogonUser(String username, String domain, IntPtr password, int logonType, int logonProvider, ref IntPtr token);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int DuplicateToken(IntPtr hToken, int impersonationLevel, ref IntPtr hNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern bool CloseHandle(IntPtr handle);

		private bool impersonateValidUser(String userName, String domain, SecureString password)
		{
			WindowsIdentity tempWindowsIdentity;
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;
			IntPtr passwordPtr = IntPtr.Zero;

			if (RevertToSelf())
			{
				// Marshal the SecureString to unmanaged memory.
				passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);

				if (LogonUser(userName, domain, passwordPtr, LOGON32_LOGON_SERVICE,
				LOGON32_PROVIDER_DEFAULT, ref token))
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
			if (impersonationContext != null)
			{
				impersonationContext.Undo();
			}
		}
	}
}
