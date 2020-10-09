using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace qs_telemetry_dashboard.Helpers
{
	internal class CertificateConfigHelpers
	{
		private static string _hostname;

		private static X509Certificate2 _certificate;

		internal static string Hostname
		{
			get
			{
				if (string.IsNullOrEmpty(_hostname))
				{
					string hostnameBase64 = File.ReadAllText(FileLocationManager.HOST_CONFIG_PATH);
					byte[] data = Convert.FromBase64String(hostnameBase64);
					_hostname = Encoding.ASCII.GetString(data);
				}
				return _hostname;
			}
		}

		internal static X509Certificate2 Certificate
		{
			get
			{
				if (_certificate == null)
				{
					byte[] certArray = File.ReadAllBytes(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem");

					StreamReader sr = new StreamReader(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem");
					PemReader pr = new PemReader(sr);
					AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
					RSA rsa = DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)KeyPair.Private);

					_certificate = new X509Certificate2(certArray);
					_certificate.PrivateKey = rsa;
				}
				return _certificate;
			}
		}
	}
}
