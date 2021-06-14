using System;
using System.Collections.Generic;
using System.Linq;

using qs_telemetry_dashboard.Exceptions;

namespace qs_telemetry_dashboard
{
	internal class ArgumentManager
	{
		internal bool NoArgs { get; }
		internal bool TaskTriggered { get; }
		internal bool DebugLog { get; }
		internal bool TestRun { get; }
		internal bool InitializeRun { get; }
		internal bool FetchMetadataRun { get; }

		internal bool UseLocalEngine { get; }
		internal bool SkipCopy { get; }

		internal int EngineTimeout { get; }
		internal int RepositoryTimeout { get; }

		internal const string HELP_STRING =
@"Telemetry Dashboard

Arguments:
-debug				activate debug logging
-initialize			initialize the environment with app, tasks and data connections
-fetchmetadata		generated metadata files to Telemetry Dashboard output folder
-test				test to make sure certificates are in place and the QRS is running";

		public ArgumentManager(string[] args, bool isComandLineRun)
		{
			TaskTriggered = false;
			NoArgs = false;
			DebugLog = false;
			TestRun = false;
			InitializeRun = false;
			FetchMetadataRun = false;
			UseLocalEngine = false;
			SkipCopy = false;
			EngineTimeout = 30000;
			RepositoryTimeout = 100000;

			if (args.Length == 0)
			{
				if (isComandLineRun)
				{
					NoArgs = true;
				}
				else
				{
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
							return x.Split('=')[0].Substring(0).ToLowerInvariant();
						}
						else
						{
							return x.ToLowerInvariant();
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
				}

				if (argDic.TryGetValue("-tasktriggered", out argValue))
				{
					argDic.Remove("-tasktriggered");
					TaskTriggered = true;
				}

				if (argDic.TryGetValue("-debug", out argValue))
				{
					argDic.Remove("-debug");
					DebugLog = true;
				}

				if (argDic.TryGetValue("-test", out argValue))
				{
					argDic.Remove("-test");
					TestRun = true;
					DebugLog = true;
				}
				if (argDic.TryGetValue("-initialize", out argValue))
				{
					argDic.Remove("-initialize");
					InitializeRun = true;
				}
				if (argDic.TryGetValue("-uselocalengine", out argValue))
				{
					argDic.Remove("-uselocalengine");
					UseLocalEngine = true;
				}
				if (argDic.TryGetValue("-skipcopy", out argValue))
				{
					argDic.Remove("-skipcopy");
					SkipCopy = true;
				}
				if (argDic.TryGetValue("-enginetimeout", out argValue))
				{
					argDic.Remove("-enginetimeout");
					int engineTimeoutValue;
					if (Int32.TryParse(argValue, out engineTimeoutValue))
					{
						EngineTimeout = engineTimeoutValue;
					}
					else {
						throw new ArgumentManagerException("Failed to parse argument '-enginetimeout' with value '" + argValue + "'. Value must be an integer.");
					}
				}
				if (argDic.TryGetValue("-repositorytimeout", out argValue))
				{
					argDic.Remove("-repositorytimeout");
					int repositoryTimeoutValue;
					if (Int32.TryParse(argValue, out repositoryTimeoutValue))
					{
						RepositoryTimeout = repositoryTimeoutValue;
					}
					else
					{
						throw new ArgumentManagerException("Failed to parse argument '-repositorytimeout' with value '" + argValue + "'. Value must be an integer.");
					}
				}

				if (argDic.Count > 0)
				{
					throw new ArgumentManagerException("Unhandled argument: " + string.Join(",", argDic.Select(kv => kv.Key).ToArray()));
				}
			}
		}
	}
}
