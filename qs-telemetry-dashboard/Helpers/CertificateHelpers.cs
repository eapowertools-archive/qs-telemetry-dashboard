using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using qs_telemetry_dashboard.Exceptions;
using qs_telemetry_dashboard.Impersonation;
using qs_telemetry_dashboard.Models;

namespace qs_telemetry_dashboard.Helpers
{
	internal class CertificateHelpers
	{
		internal static X509Certificate2 FetchCertificate()
		{
			X509Certificate2 cert = null;
			byte[] certArray = File.ReadAllBytes(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem");

			StreamReader sr = new StreamReader(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem");
			PemReader pr = new PemReader(sr);
			AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
			RSA rsa = DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)KeyPair.Private);

			cert = new X509Certificate2(certArray);

			//RSACryptoServiceProvider prov = Crypto.DecodeRsaPrivateKey(keyBuffer);
			cert.PrivateKey = rsa;

			return cert;
		}
	}
}
