using System;
using System.IO;
using qs_telemetry_dashboard.Helpers;
using qs_telemetry_dashboard.Logging;

namespace qs_telemetry_dashboard.MetadataFetch
{
	internal class MetadataFetchRunner
	{
		internal static int Run()
		{
			string outputPath = Path.Combine(FileLocationManager.GetTelemetrySharePath(), FileLocationManager.TELEMETRY_OUTPUT_FOLDER);
			throw new NotImplementedException();
		}

		internal static void GetRepositorySheets()
		{
  //          var path = "app/object/table?filter=objectType eq 'sheet'&skip=" + startLocation + "&take=" + pageSize;
  //          return instance.Post(path, {
  //          columns:
  //              [{
  //              columnType: "Property",
  //                          definition: "app.id",
  //                          name: "app.id"
  //                      },
  //                      {
  //              columnType: "Property",
  //                          definition: "engineObjectId",
  //                          name: "engineObjectId"
  //                      },
  //                      {
  //              columnType: "Property",
  //                          definition: "name",
  //                          name: "name"
  //                      },
  //                      {
  //              columnType: "Property",
  //                          definition: "owner.id",
  //                          name: "owner.id"
  //                      },
  //                      {
  //              columnType: "Property",
  //                          definition: "published",
  //                          name: "published"
  //                      },
  //                      {
  //              columnType: "Property",
  //                          definition: "approved",
  //                          name: "approved"
  //                      }
  //                  ],
  //                  entity: "App.Object"
  //                  },
  //              'json')
		}
	}
}
