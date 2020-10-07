using System.Collections.Generic;
using System.Linq;

using qs_telemetry_dashboard.Exceptions;

namespace qs_telemetry_dashboard
{
	internal class ArgumentManager
	{
		internal bool NoArgs { get; }
		internal bool Interactive { get; }
		internal bool DebugLog { get; }
		internal bool TestRun { get; }
		internal bool InitializeRun { get; }
		internal bool FetchMetadataRun { get; }

		internal const string HELP_STRING =
@"Telemetry Dashboard

Arguments:
--help				Show help
--fetchMetadata		";

		public ArgumentManager(string[] args, bool isComandLineRun)
		{
			Interactive = true;
			NoArgs = false;
			DebugLog = false;
			TestRun = false;
			InitializeRun = false;
			FetchMetadataRun = false;

			if (args.Length == 0)
			{
				if (isComandLineRun)
				{
					NoArgs = true;
				}
				else
				{
					Interactive = false;
					InitializeRun = true;
				}
			}
			else
			{
				Dictionary<string, string> argDic = args.ToDictionary(
					x =>
					{
						if (x.Contains('='))
						{
							return x.Split('=')[0].Substring(1);
						}
						else
						{
							return x;
						}
					},
					y =>
					{
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

				if (argDic.TryGetValue("-fetchmetadata", out argValue))
				{
					argDic.Remove("-fetchmetadata");
					FetchMetadataRun = true;
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

				if (argDic.TryGetValue("-testrun", out argValue))
				{
					argDic.Remove("-testrun");
					TestRun = true;
					DebugLog = true;
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
