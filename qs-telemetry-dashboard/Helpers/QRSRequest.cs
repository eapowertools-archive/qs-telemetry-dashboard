using qs_telemetry_dashboard.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace qs_telemetry_dashboard.Helpers
{
	internal enum HTTPContentType
	{
		json,
		app
	}

	internal class QRSRequest
	{
		private readonly TelemetryConfiguration _configuration;

		public QRSRequest(TelemetryConfiguration config)
		{
			_configuration = config;
		}

		internal Tuple<HttpStatusCode, string> MakeRequest(string path, HttpMethod method, HTTPContentType contentType = HTTPContentType.json, byte[] body = null)
		{
			// Fix Path
			if (!path.StartsWith("/"))
			{
				path = '/' + path;
			}
			if (path.EndsWith("/"))
			{
				path = path.Substring(0, path.Length - 1);
			}
			int indexOfSlash = path.LastIndexOf('/');
			int indexOfQuery = path.LastIndexOf('?');
			if (indexOfQuery <= indexOfSlash)
			{
				path += "?";
			}
			else
			{
				path += "&";
			}

			string responseString = "";
			HttpStatusCode responseCode = 0;
			string xrfkey = "0123456789abcdef";

			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

			HttpRequestMessage req = new HttpRequestMessage();
			req.RequestUri = new Uri(@"https://" + _configuration.Hostname + ":4242/qrs" + path + "xrfkey=" + xrfkey);
			req.Method = method;
			req.Headers.Add("X-Qlik-xrfkey", xrfkey);
			req.Headers.Add("X-Qlik-User", @"UserDirectory=internal;UserId=sa_api");

			WebRequestHandler handler = new WebRequestHandler();
			handler.ClientCertificates.Add(_configuration.QlikClientCertificate);

			if (method == HttpMethod.Post || method == HttpMethod.Put)
			{
				req.Content = new ByteArrayContent(body);

				// Set Headers
				if (contentType == HTTPContentType.json)
				{
					req.Content.Headers.Remove("Content-Type");
					req.Content.Headers.Add("Content-Type", "application/json");

				}
				else if (contentType == HTTPContentType.app)
				{
					req.Content.Headers.Remove("Content-Type");
					req.Content.Headers.Add("Content-Type", "application/vnd.qlik.sense.app");
				}
				else
				{
					throw new ArgumentException("Content type '" + contentType.ToString() + "' is not supported.");
				}
			}

			using (HttpClient client = new HttpClient(handler))
			{
				client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
				try
				{
					Task<HttpResponseMessage> responseTask = client.SendAsync(req);
					responseTask.Wait();
					responseTask.Result.EnsureSuccessStatusCode();
					responseCode = responseTask.Result.StatusCode;
					Task<string> responseBodyTask = responseTask.Result.Content.ReadAsStringAsync();
					responseBodyTask.Wait();
					responseString = responseBodyTask.Result;
				}
				catch (Exception e)
				{
					if (responseCode != 0)
					{
						return new Tuple<HttpStatusCode, string>(responseCode, e.Message);
					}
					else
					{
						return new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, e.Message);

					}
				}
			}

			return new Tuple<HttpStatusCode, string>(responseCode, responseString);
		}
	}
}
