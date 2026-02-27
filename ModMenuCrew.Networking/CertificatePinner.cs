using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ModMenuCrew.Networking;

public static class CertificatePinner
{
	private static readonly System.Collections.Generic.HashSet<string> _pins = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal) { "T6VlexaJ/mwSs9GKinH9ZLOf/VkRtYD8iSPX+oi0g5A=" };

	private const bool _pinningEnabled = true;

	private static int _failureCountXor = 1266559517;

	private const int FAILURE_SENTINEL = 1266559517;

	private const int MAX_FAILURES_BEFORE_BAN = 3;

	private const int MAX_PINS = 5;

	private const string ORIGINAL_PIN = "T6VlexaJ/mwSs9GKinH9ZLOf/VkRtYD8iSPX+oi0g5A=";

	internal static volatile string _lastDiag = null;

	private static int FailureCount => _failureCountXor ^ 0x4B7E2A1D;

	private static void SetFailureCount(int value)
	{
		_failureCountXor = value ^ 0x4B7E2A1D;
	}

	public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		if (certificate == null)
		{
			return false;
		}
		try
		{
			if (ModKeyValidator.IsRunningUnderWine)
			{
				_lastDiag = "[CertPinner] SKIPPED: Running under Wine/Proton (crypto APIs unsafe)";
				return true;
			}
		}
		catch
		{
		}
		bool num = (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0;
		bool flag = (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0;
		if (num || flag)
		{
			return false;
		}
		bool flag2 = sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors;
		try
		{
			using X509Certificate2 x509Certificate = new X509Certificate2(certificate);
			string value = "unknown";
			byte[] buffer;
			using (RSA rSA = x509Certificate.GetRSAPublicKey())
			{
				if (rSA != null)
				{
					value = "RSA";
					buffer = rSA.ExportSubjectPublicKeyInfo();
				}
				else
				{
					using ECDsa eCDsa = x509Certificate.GetECDsaPublicKey();
					if (eCDsa == null)
					{
						_lastDiag = "[CertPinner] REJECTED: No RSA or ECDSA public key found";
						return false;
					}
					value = "ECDSA";
					buffer = eCDsa.ExportSubjectPublicKeyInfo();
				}
			}
			string text;
			using (SHA256 sHA = SHA256.Create())
			{
				text = Convert.ToBase64String(sHA.ComputeHash(buffer));
			}
			_lastDiag = $"[CertPinner] KeyType={value} ComputedSPKI={text} ExpectedPin={"T6VlexaJ/mwSs9GKinH9ZLOf/VkRtYD8iSPX+oi0g5A="} Match={_pins.Contains(text)} Subject={x509Certificate.Subject}";
			if (!_pins.Contains("T6VlexaJ/mwSs9GKinH9ZLOf/VkRtYD8iSPX+oi0g5A="))
			{
				_lastDiag += " | REJECTED: ORIGINAL_PIN missing from _pins (reflection attack?)";
				ServerData.TriggerSilentDenial();
				return false;
			}
			if (_pins.Contains(text))
			{
				_lastDiag += " | ACCEPTED: SPKI pin match";
				SetFailureCount(0);
				return true;
			}
			_lastDiag = _lastDiag + " | REJECTED: SPKI pin mismatch (computed=" + text + " not in pins)";
			if (!flag2)
			{
				SetFailureCount(FailureCount + 1);
				if (FailureCount >= 3)
				{
					ServerData.TriggerSilentDenial();
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			_lastDiag = "[CertPinner] EXCEPTION: " + ex.GetType().Name + ": " + ex.Message;
			return false;
		}
	}

	internal static void UpdatePinsFromServer(string[] serverPins)
	{
		if (serverPins == null || serverPins.Length == 0 || _pins.Count == 0 || _pins.Count >= 5)
		{
			return;
		}
		foreach (string text in serverPins)
		{
			if (_pins.Count < 5)
			{
				if (!string.IsNullOrEmpty(text) && text.Length == 44)
				{
					_pins.Add(text);
				}
				continue;
			}
			break;
		}
	}
}

