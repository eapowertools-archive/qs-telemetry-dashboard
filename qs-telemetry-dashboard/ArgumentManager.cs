using qs_telemetry_dashboard.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;

namespace qs_telemetry_dashboard
{
	internal class ArgumentManager
	{
		internal bool NoArgs { get; }
		internal bool Interactive { get; }
		internal bool DebugLog { get; }
		internal bool TestCredentialRun { get; }
		internal bool TestConfigurationRun { get; }
		internal bool UpdateCertificateRun { get; }
		internal bool InitializeRun { get; }
		internal bool MetadataFetchRun { get; }


		internal const string HELP_STRING =
@"Telemetry Dashboard

Arguments:
--help				Show help
--fetchMetadata		";

		public ArgumentManager(string[] args)
		{
			Interactive = true;
			if (args.Length == 0)
			{
				NoArgs = true;
			}
			else
			{
				NoArgs = false;
				MetadataFetchRun = false;
				DebugLog = false;
				TestCredentialRun = false;
				UpdateCertificateRun = false;
				InitializeRun = false;

				Dictionary<string, string> argDic = args.ToDictionary(
					x => {
						if (x.Contains('='))
						{
							return x.Split('=')[0].Substring(1);
						}
						else
						{
							return x;
						}
					},
					y => {
						if (y.Contains('='))
						{
							return y.Split('=')[1];
						}
						else
						{
							return "";
						}
					});

				string argValue = "";

				if (argDic.TryGetValue("-metadatafetch", out argValue))
				{
					argDic.Remove("-metadatafetch");
					MetadataFetchRun = true;
					Interactive = false;
				}

				if (argDic.TryGetValue("-interactive", out argValue))
				{
					argDic.Remove("-interactive");
					Interactive = true;
				}

				if (argDic.TryGetValue("-debug", out argValue))
				{
					argDic.Remove("-debug");
					DebugLog = true;
				}

				if (argDic.TryGetValue("-testcredentials", out argValue))
				{
					argDic.Remove("-testcredentials");
					TestCredentialRun = true;
					DebugLog = true;
				}

				if (argDic.TryGetValue("-testconfiguration", out argValue))
				{
					argDic.Remove("-testconfiguration");
					TestConfigurationRun = true;
					DebugLog = true;
				}
				if (argDic.TryGetValue("-updatecertificate", out argValue))
				{
					argDic.Remove("-updatecertificate");
					UpdateCertificateRun = true;
				}

				if (argDic.TryGetValue("-initialize", out argValue))
				{
					argDic.Remove("-initialize");
					InitializeRun = true;
				}


				if (argDic.Count > 0)
				{
					throw new ArgumentManagerException("Unhandled argument: " + string.Join(",", argDic.Select(kv => kv.Key).ToArray()));
				}
			}
		}
	}
}
