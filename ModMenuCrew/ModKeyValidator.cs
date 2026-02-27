using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Microsoft.Win32;
using ModMenuCrew.Networking;
using ModMenuCrew.UI.Managers;
using ModMenuCrew.UI.Menus;
using UnityEngine;

namespace ModMenuCrew;

public static class ModKeyValidator
{
	private static readonly JsonSerializerOptions _jsonOpts;

	internal static readonly Regex KeyFormatRegex;

	internal static readonly string[] TrustedRootCAThumbprints;

	internal static readonly string[] AllowedHostnames;

	internal static readonly bool IsRunningUnderWine;

	internal static volatile string _sslDiagLog;

	private static readonly HttpClientHandler httpHandler;

	private static readonly HttpClientHandler httpHandlerPinned;

	private static readonly HttpClient httpClient;

	internal static readonly string _rsaModulusPart1;

	internal static readonly string _rsaModulusPart2;

	internal static readonly string _rsaModulusPart3;

	private static readonly string _rsaExponent;

	private static uint _textureMemoryPool;

	private static readonly string ApiBaseUrl;

	private const string DisplayUrl = "https://crewcore.online/";

	private const string PlayerPrefsKeyActivatedPrefix = "ModMenuCrew_Activated_";

	private static string _cachedHwid;

	private static bool __svc_internal;

	private static int __svc_checksum;

	private static readonly object _svcLock;

	private static int _svcMismatchCount;

	private const int SVC_MISMATCH_TOLERANCE = 10;

	private static string _srvProof;

	private static long _proofSeedXor;

	private static long _proofExpiresXor;

	private const long PROOF_SENTINEL = 4502221515645747270L;

	private static volatile string _serverHeartbeatNonce;

	private static int _integrityCounter;

	private static long _serverTimeOffsetXor;

	private const long OFFSET_SENTINEL = 8803779085689698909L;

	private static long _rtx;

	private static int _vcs;

	private static byte _xf;

	private static uint _hv;

	private static long _lastServerCheck;

	private static readonly object _validationLock;

	private static bool _debugSessionDeathLogged;

	internal static volatile string _debugHeartbeatMsg;

	private static long _localTick;

	private static long _lastTickUpdateMs;

	private static long _serverExpectedTick;

	private static long _serverTickTolerance;

	private static int _isHeartbeatRunningInt;

	private static CancellationTokenSource _heartbeatCancellation;

	private static CancellationTokenSource _sleepTokenSource;

	private static bool _enableHeartbeat;

	internal static string CachedHwid => _cachedHwid ?? "";

	public static long SessionToken { get; private set; }

	public static bool _svcCtx
	{
		get
		{
			return true;
		}
		private set
		{
		}
	}

	public static string LastValidationMessage { get; private set; }

	public static string ValidatedUsername { get; private set; }

	public static string CurrentKey { get; private set; }

	public static bool PendingResetRequest { get; private set; }

	private static long ProofSeed => _proofSeedXor ^ 0x3E7B1A9D5C2F8046L;

	private static long ProofExpires => _proofExpiresXor ^ 0x3E7B1A9D5C2F8046L;

	internal static long ServerTimeOffsetMs => _serverTimeOffsetXor ^ 0x7A2D4E6F1B8C3A5DL;

	internal static long CachedFrameTimeMs { get; private set; }

	public static string FormattedDisplayString { get; private set; }

	public static bool IsPremium
	{
		get
		{
			return true;
		}
	}

	public static string KeyType { get; private set; }

	public static DateTime? ExpiresAt { get; private set; }

	public static TimeSpan? TimeRemaining
	{
		get
		{
			if (!IsPremium || !ExpiresAt.HasValue)
			{
				return null;
			}
			TimeSpan timeSpan = ExpiresAt.Value - DateTime.UtcNow;
			return (timeSpan.TotalSeconds > 0.0) ? timeSpan : TimeSpan.Zero;
		}
	}

	public static long CurrentSessionToken { get; private set; }

	public static long LocalTick => _localTick;

	internal static bool _isHeartbeatRunning => _isHeartbeatRunningInt != 0;

	public static bool DiscordRevoked { get; internal set; }

	public static string RevokeReason { get; internal set; }

	private static bool DetectWine()
	{
		try
		{
			if (GetProcAddress(GetModuleHandle("ntdll.dll"), "wine_get_version") != IntPtr.Zero)
			{
				return true;
			}
			string? environmentVariable = Environment.GetEnvironmentVariable("WINEPREFIX");
			string environmentVariable2 = Environment.GetEnvironmentVariable("CX_BOTTLE");
			if (!string.IsNullOrEmpty(environmentVariable) || !string.IsNullOrEmpty(environmentVariable2))
			{
				return true;
			}
			if (Directory.Exists("Z:\\") && (Directory.Exists("Z:\\usr") || Directory.Exists("Z:\\Applications")))
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

	internal static bool UnifiedSslCallback(HttpRequestMessage request, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors, bool runSpkiPin)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"[SSL-DIAG] Wine={IsRunningUnderWine} errors={errors} (raw={(int)errors})");
			stringBuilder.AppendLine($"  URI: {request?.RequestUri}");
			stringBuilder.AppendLine($"  Cert: {cert?.Subject ?? "NULL"} Issuer: {cert?.Issuer ?? "NULL"}");
			if (chain?.ChainElements != null)
			{
				stringBuilder.AppendLine($"  Chain: {chain.ChainElements.Count} elements");
				try
				{
					for (int i = 0; i < chain.ChainElements.Count; i++)
					{
						X509ChainElement x509ChainElement = chain.ChainElements[i];
						stringBuilder.AppendLine($"    [{i}] {x509ChainElement.Certificate?.Thumbprint?.ToUpperInvariant()} {x509ChainElement.Certificate?.Subject}");
					}
				}
				catch
				{
					stringBuilder.AppendLine("    [chain iteration error]");
				}
			}
			try
			{
				if (chain?.ChainStatus != null)
				{
					X509ChainStatus[] chainStatus = chain.ChainStatus;
					for (int j = 0; j < chainStatus.Length; j++)
					{
						X509ChainStatus x509ChainStatus = chainStatus[j];
						stringBuilder.AppendLine($"  Status: {x509ChainStatus.Status} - {x509ChainStatus.StatusInformation}");
					}
				}
			}
			catch
			{
				stringBuilder.AppendLine("  [ChainStatus access error]");
			}
			bool flag = (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0;
			bool flag2 = (errors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0;
			if (flag || flag2)
			{
				try
				{
					stringBuilder.AppendLine($"  REJECTED: NameMismatch={flag} NotAvailable={flag2}");
					_sslDiagLog = stringBuilder.ToString();
				}
				catch
				{
				}
				return false;
			}
			string text = request?.RequestUri?.Host?.ToLowerInvariant();
			bool flag3 = false;
			string[] allowedHostnames = AllowedHostnames;
			foreach (string text2 in allowedHostnames)
			{
				if (text == text2.ToLowerInvariant())
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				try
				{
					stringBuilder.AppendLine($"  REJECTED: hostname '{text}' not allowed");
					_sslDiagLog = stringBuilder.ToString();
				}
				catch
				{
				}
				return false;
			}
			if (IsRunningUnderWine)
			{
				try
				{
					string text3 = cert?.Issuer ?? "";
					if (!text3.Contains("Let's Encrypt") && !text3.Contains("Google Trust") && !text3.Contains("ISRG") && !text3.Contains("Cloudflare") && !text3.Contains("GlobalSign") && !text3.Contains("GTS"))
					{
						stringBuilder.AppendLine($"  REJECTED (Wine mode): unknown issuer '{text3}'");
						_sslDiagLog = stringBuilder.ToString();
						return false;
					}
					stringBuilder.AppendLine($"  ACCEPTED (Wine mode): hostname valid, issuer '{text3}' OK");
					_sslDiagLog = stringBuilder.ToString();
					return true;
				}
				catch (Exception ex)
				{
					try
					{
						_sslDiagLog = "REJECTED (Wine): cert exception " + ex.GetType().Name;
					}
					catch
					{
					}
					return false;
				}
			}
			bool flag4 = errors == SslPolicyErrors.RemoteCertificateChainErrors;
			if (chain == null || chain.ChainElements == null || chain.ChainElements.Count == 0)
			{
				if (!flag4)
				{
					try
					{
						stringBuilder.AppendLine("  REJECTED: chain null/empty, errors != ChainErrorsOnly");
						_sslDiagLog = stringBuilder.ToString();
					}
					catch
					{
					}
					return false;
				}
			}
			else
			{
				bool flag5 = false;
				try
				{
					foreach (X509ChainElement chainElement in chain.ChainElements)
					{
						if (chainElement.Certificate != null)
						{
							string text4 = chainElement.Certificate.Thumbprint?.ToUpperInvariant();
							if (!string.IsNullOrEmpty(text4))
							{
								allowedHostnames = TrustedRootCAThumbprints;
								foreach (string text5 in allowedHostnames)
								{
									if (text4 == text5)
									{
										flag5 = true;
										break;
									}
								}
							}
						}
						if (flag5)
						{
							break;
						}
					}
				}
				catch
				{
				}
				if (!flag5 && !flag4)
				{
					try
					{
						stringBuilder.AppendLine("  REJECTED: no CA match, errors not ChainErrorsOnly");
						_sslDiagLog = stringBuilder.ToString();
					}
					catch
					{
					}
					return false;
				}
			}
			if (runSpkiPin)
			{
				try
				{
					bool flag6 = CertificatePinner.ValidateServerCertificate(request, cert, chain, errors);
					string value = CertificatePinner._lastDiag ?? "no diag";
					try
					{
						stringBuilder.AppendLine($"  SPKI: {(flag6 ? "ACCEPTED" : "REJECTED")} {value}");
						_sslDiagLog = stringBuilder.ToString();
					}
					catch
					{
					}
					return flag6;
				}
				catch (Exception ex2)
				{
					try
					{
						stringBuilder.AppendLine($"  SPKI CRASH: {ex2.GetType().Name}: {ex2.Message}");
						_sslDiagLog = stringBuilder.ToString();
					}
					catch
					{
					}
					return false;
				}
			}
			try
			{
				stringBuilder.AppendLine("  ACCEPTED (base validation passed)");
				_sslDiagLog = stringBuilder.ToString();
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			_sslDiagLog = "[CRASH] UnifiedSslCallback threw unhandled exception";
			return false;
		}
	}

	static ModKeyValidator()
	{
		_jsonOpts = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
		KeyFormatRegex = new Regex("^[A-Z0-9P-]{19,23}$", RegexOptions.Compiled);
		TrustedRootCAThumbprints = new string[8] { "CABD2A79A1076A31F21D253635CB039D4329A5E8", "933C6DDEE95C9C41A40F9F50493D82BE03AD87BF", "E89B46892C805016E9367851B0444A61C0E67189", "D3779E396F4C39C80D4677765691079D85489F66", "F9AC55798481358D88DF1C3F44321C210D1D643D", "E72DF1D45493B827E6D0A760630B0B2E88381E28", "932BED339AA69212C89375B79304B475490B89A0", "B1BC968BD4F49D622AA89A81F2150152A41D829C" };
		AllowedHostnames = new string[1] { "dev.crewcore.online" };
		_sslDiagLog = null;
		httpHandler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (HttpRequestMessage request, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) => UnifiedSslCallback(request, cert, chain, errors, runSpkiPin: false),
			AllowAutoRedirect = false
		};
		httpHandlerPinned = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (HttpRequestMessage request, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) => UnifiedSslCallback(request, cert, chain, errors, runSpkiPin: true),
			AllowAutoRedirect = false
		};
		_rsaModulusPart1 = "10nomJIIOLVleBhf8OiVGn/PpaOnlN1Zvl0MfCN+Qymp3KEGIclegEujxXU28osUjF2ND/FsnC6vwu+x9WbBaURjBaFY6rgYB8EpVYls";
		_rsaModulusPart2 = "5SoHikaq4+407SPDo/1wHa+J3tU3a+e5D7mIFAWo13N11b2G9Veg+QHR7mtq3qB3Q6ltX9KKAEtJPSdhPRdizp8zcXvnZJ6PLFdOcCRgRCAChGGYUnm7rMdJwcFwjxE2WADHscpjcqiPuQ5pTU/KIrYgNzZvcpFF20/10ejFGluvHSSz";
		_rsaModulusPart3 = "Wwh68fJeJ21lGOZleLDfRU3vZQ4LaRwfqAQaLT0vpTndZsLbtuhyAU0/MwVN3Q==";
		_rsaExponent = "AQAB";
		_textureMemoryPool = 0u;
		ApiBaseUrl = "https://dev.crewcore.online";
		_cachedHwid = null;
		SessionToken = 0L;
		__svc_internal = false;
		__svc_checksum = 0;
		_svcLock = new object();
		_svcMismatchCount = 0;
		LastValidationMessage = "Aguardando validação...";
		ValidatedUsername = "";
		CurrentKey = "";
		PendingResetRequest = false;
		_srvProof = null;
		_proofSeedXor = 0L;
		_proofExpiresXor = 0L;
		_serverHeartbeatNonce = "";
		_integrityCounter = 0;
		_serverTimeOffsetXor = 8803779085689698909L;
		CachedFrameTimeMs = 0L;
		FormattedDisplayString = "https://crewcore.online/";
		KeyType = "standard";
		ExpiresAt = null;
		_rtx = 0L;
		_vcs = 0;
		_xf = 0;
		_hv = 0u;
		_lastServerCheck = 0L;
		_validationLock = new object();
		_debugSessionDeathLogged = false;
		_debugHeartbeatMsg = null;
		CurrentSessionToken = 0L;
		_localTick = 0L;
		_lastTickUpdateMs = 0L;
		_serverExpectedTick = 0L;
		_serverTickTolerance = 100L;
		_isHeartbeatRunningInt = 0;
		_enableHeartbeat = true;
		DiscordRevoked = false;
		RevokeReason = "";
		IsRunningUnderWine = DetectWine();
		try
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
		}
		catch
		{
		}
		httpClient = new HttpClient(httpHandlerPinned);
		httpClient.Timeout = TimeSpan.FromSeconds(30.0);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CrewClient/1.0 (Unity; Windows; Elite)");
		httpClient.DefaultRequestHeaders.ConnectionClose = false;
	}

	private static bool VerifyRSASignature(ApiValidationResponse response)
	{
		//IL_05be: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c4: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Expected O, but got Unknown
		bool flag = default(bool);
		if (response == null || string.IsNullOrEmpty(response.SignatureRsa))
		{
			ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
			if (((instance != null) ? ((BasePlugin)instance).Log : null) != null)
			{
				ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(40, 2, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[RSA-VERIFY] REJECTED: response=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(response != null);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" sigRsa=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(response?.SignatureRsa?.Length ?? (-1));
				}
				log.LogError(val);
			}
			return false;
		}
		try
		{
			byte[] modulus = Convert.FromBase64String(_rsaModulusPart1 + _rsaModulusPart2 + _rsaModulusPart3);
			byte[] exponent = Convert.FromBase64String(_rsaExponent);
			RSAParameters parameters = new RSAParameters
			{
				Modulus = modulus,
				Exponent = exponent
			};
			List<PropertyInfo> list = (from p in response.GetType().GetProperties()
				where !p.Name.StartsWith("Signature", StringComparison.OrdinalIgnoreCase)
				select p).ToList();
			List<(string, string)> list2 = new List<(string, string)>();
			foreach (PropertyInfo item2 in list)
			{
				if (item2.GetCustomAttributes(typeof(JsonExtensionDataAttribute), inherit: false).Any())
				{
					continue;
				}
				object value = item2.GetValue(response);
				if (value == null)
				{
					continue;
				}
				string text = (item2.GetCustomAttributes(typeof(JsonPropertyNameAttribute), inherit: false).FirstOrDefault() as JsonPropertyNameAttribute)?.Name ?? ToSnakeCase(char.ToLowerInvariant(item2.Name[0]) + item2.Name.Substring(1));
				if (!text.StartsWith("signature"))
				{
					object obj = value;
					Type type = value.GetType();
					if (!(Nullable.GetUnderlyingType(type) != null) || value != null)
					{
						string item = ((!(obj is string text2)) ? ((!(obj is bool flag2)) ? ((!(obj is JsonElement jsonElement)) ? ((obj is IEnumerable && !(obj is string)) ? (text + "=" + JsonSerializer.Serialize(obj, new JsonSerializerOptions
						{
							Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
						})) : ((!type.IsClass || !(type != typeof(string))) ? $"{text}={obj}" : (text + "=" + JsonSerializer.Serialize(obj, new JsonSerializerOptions
						{
							Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
						})))) : (text + "=" + jsonElement.GetRawText())) : (text + "=" + flag2.ToString().ToLowerInvariant())) : (text + "=\"" + text2 + "\""));
						list2.Add((text, item));
					}
				}
			}
			if (response.ExtensionData != null)
			{
				foreach (KeyValuePair<string, JsonElement> extensionDatum in response.ExtensionData)
				{
					if (!extensionDatum.Key.StartsWith("signature") && extensionDatum.Value.ValueKind != JsonValueKind.Null)
					{
						list2.Add((extensionDatum.Key, extensionDatum.Key + "=" + extensionDatum.Value.GetRawText()));
					}
				}
			}
			list2.Sort(((string key, string serialized) a, (string key, string serialized) b) => string.Compare(a.key, b.key, StringComparison.Ordinal));
			string text3 = string.Join("&", list2.Select<(string, string), string>(((string key, string serialized) kv) => kv.serialized));
			ModMenuCrewPlugin instance2 = ModMenuCrewPlugin.Instance;
			if (((instance2 != null) ? ((BasePlugin)instance2).Log : null) != null)
			{
				byte[] array = SHA256.HashData(Encoding.UTF8.GetBytes(text3));
				ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(41, 3, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[RSA-VERIFY] canonical_len=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(text3.Length);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" hash=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(BitConverter.ToString(array, 0, 8).Replace("-", ""));
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" keys=[");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(string.Join(",", list2.Select<(string, string), string>(((string key, string serialized) kv) => kv.key)));
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("]");
				}
				log2.LogWarning(val2);
			}
			using RSA rSA = RSA.Create();
			rSA.ImportParameters(parameters);
			byte[] signature = Convert.FromBase64String(response.SignatureRsa);
			byte[] bytes = Encoding.UTF8.GetBytes(text3);
			bool num = rSA.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			if (!num)
			{
				ModMenuCrewPlugin instance3 = ModMenuCrewPlugin.Instance;
				if (((instance3 != null) ? ((BasePlugin)instance3).Log : null) != null)
				{
					((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[RSA-VERIFY] MISMATCH: signature format valid but canonical data doesn't match.");
				}
			}
			return num;
		}
		catch (Exception ex)
		{
			ModMenuCrewPlugin instance4 = ModMenuCrewPlugin.Instance;
			if (((instance4 != null) ? ((BasePlugin)instance4).Log : null) != null)
			{
				ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				bool flag3 = default(bool);
				BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(26, 2, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[RSA-VERIFY] EXCEPTION: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.GetType().Name);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
				}
				log3.LogError(val);
			}
			return false;
		}
	}

	private static string ToSnakeCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (char.IsUpper(c) && i > 0)
			{
				stringBuilder.Append('_');
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
			else
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		return stringBuilder.ToString();
	}

	private static string GetPlayerPrefsKeyActivated()
	{
		return "ModMenuCrew_Activated_6.0.8";
	}

	private static string GetHardwareId()
	{
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		if (!string.IsNullOrEmpty(_cachedHwid))
		{
			return _cachedHwid;
		}
		try
		{
			string text = "";
			try
			{
				using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", writable: false);
				if (registryKey != null)
				{
					text = registryKey.GetValue("MachineGuid")?.ToString() ?? "";
				}
			}
			catch
			{
			}
			string s = string.Join("|", text, Environment.MachineName, Environment.UserName, SystemInfo.processorType, SystemInfo.processorCount.ToString());
			using SHA256 sHA = SHA256.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			byte[] array = sHA.ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < 16; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			_cachedHwid = stringBuilder.ToString().ToUpperInvariant();
		}
		catch (Exception ex)
		{
			ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = ((BasePlugin)instance).Log;
				if (log != null)
				{
					bool flag = default(bool);
					BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(41, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModKeyValidator] Error generating HWID: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
					}
					log.LogError(val);
				}
			}
			string s2 = Environment.MachineName + "|" + Environment.UserName + "|FALLBACK";
			using SHA256 sHA2 = SHA256.Create();
			byte[] bytes2 = Encoding.UTF8.GetBytes(s2);
			byte[] array2 = sHA2.ComputeHash(bytes2);
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int j = 0; j < 16; j++)
			{
				stringBuilder2.Append(array2[j].ToString("x2"));
			}
			_cachedHwid = stringBuilder2.ToString().ToUpperInvariant();
		}
		return _cachedHwid;
	}

	internal static void SetServiceContext(bool value, long token)
	{
		lock (_svcLock)
		{
			if (!(token != SessionToken && value))
			{
				__svc_internal = value;
				__svc_checksum = (value ? 23130 : 42405) ^ (int)(token & 0xFFFF) ^ (_integrityCounter * 4919);
				_svcMismatchCount = 0;
			}
		}
	}

	internal static void RequestResetFromRealtime(string reason = "")
	{
		if (!string.IsNullOrEmpty(reason) && "discord_left".Equals(reason, StringComparison.OrdinalIgnoreCase))
		{
			DiscordRevoked = true;
			RevokeReason = "discord_left";
		}
		PendingResetRequest = true;
	}

	internal static void UpdateFrameTimeCache()
	{
		CachedFrameTimeMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool V()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetSeedValue(float min, float max)
	{
		if (!V())
		{
			return min;
		}
		float num = max - min;
		float num2 = (float)(ProofSeed % 1000) / 1000f;
		return min + num * num2;
	}

	internal static void UpdateProof(string proof, long seed, long expires)
	{
		if (string.IsNullOrEmpty(proof) || seed <= 0 || expires <= 0)
		{
			return;
		}
		_srvProof = proof;
		_proofSeedXor = seed ^ 0x3E7B1A9D5C2F8046L;
		_proofExpiresXor = expires ^ 0x3E7B1A9D5C2F8046L;
		lock (_svcLock)
		{
			_integrityCounter++;
			if (__svc_internal)
			{
				__svc_checksum = 0x5A5A ^ (int)(SessionToken & 0xFFFF) ^ (_integrityCounter * 4919);
			}
		}
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		long num2 = expires - 300000 - num;
		if (Math.Abs(num2) > 1800000)
		{
			_serverTimeOffsetXor = ((num2 > 0) ? 1800000 : (-1800000)) ^ 0x7A2D4E6F1B8C3A5DL;
		}
		else
		{
			_serverTimeOffsetXor = num2 ^ 0x7A2D4E6F1B8C3A5DL;
		}
	}

	internal static void ClearProof()
	{
		_srvProof = null;
		_proofSeedXor = 0L;
		_proofExpiresXor = 0L;
	}

	private static string DecryptUIPayload(EncryptedPayload payload, string sessionKeyHex)
	{
		if (payload == null || string.IsNullOrEmpty(payload.Ciphertext) || string.IsNullOrEmpty(payload.Iv) || string.IsNullOrEmpty(payload.Tag) || string.IsNullOrEmpty(sessionKeyHex))
		{
			return null;
		}
		try
		{
			byte[] key = HexToBytes(sessionKeyHex);
			byte[] nonce = HexToBytes(payload.Iv);
			byte[] tag = HexToBytes(payload.Tag);
			byte[] array = Convert.FromBase64String(payload.Ciphertext);
			byte[] array2 = new byte[array.Length];
			using (AesGcm aesGcm = new AesGcm(key))
			{
				aesGcm.Decrypt(nonce, array, tag, array2);
			}
			return Encoding.UTF8.GetString(array2);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static byte[] HexToBytes(string hex)
	{
		int length = hex.Length;
		byte[] array = new byte[length / 2];
		for (int i = 0; i < length; i += 2)
		{
			array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		}
		return array;
	}

	public static string GetKeyTypeDisplay()
	{
		if (!IsPremium)
		{
			return "FREE KEY";
		}
		return KeyType?.ToUpperInvariant() switch
		{
			"DAILY" => "★ DAILY", 
			"WEEKLY" => "★ WEEKLY", 
			"MONTHLY" => "★ MONTHLY", 
			"LIFETIME" => "★ LIFETIME", 
			"CUSTOM" => "★ PREMIUM", 
			_ => "★ PREMIUM", 
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static string GetDerivedApiUrl()
	{
		string apiBaseUrl = ApiBaseUrl;
		if (apiBaseUrl == null)
		{
			return string.Empty;
		}
		if (apiBaseUrl.Length == 0)
		{
			return string.Empty;
		}
		if (!apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}
		if (!apiBaseUrl.Contains("crewcore.online"))
		{
			return string.Empty;
		}
		return apiBaseUrl;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool VerifyAllowedHostnames()
	{
		if (AllowedHostnames == null || AllowedHostnames.Length != 1)
		{
			return false;
		}
		return string.Equals(AllowedHostnames[0], "dev.crewcore.online", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsDebuggerPresent()
	{
		if (Debugger.IsAttached)
		{
			return true;
		}
		try
		{
			foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
			{
				string text = module.ModuleName.ToLowerInvariant();
				if (text.Contains("dnspy") || text.Contains("ilspy") || text.Contains("de4dot") || text.Contains("harmony") || text.Contains("cheatengine") || text.Contains("x64dbg") || text.Contains("ollydbg") || text.Contains("ida"))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static uint CalculateMethodChecksum()
	{
		uint num = 305419896u;
		try
		{
			MethodInfo method = typeof(ModKeyValidator).GetMethod("IsSessionValid", BindingFlags.Static | BindingFlags.Public);
			if (method != null)
			{
				MethodBody methodBody = method.GetMethodBody();
				if (methodBody != null)
				{
					byte[] iLAsByteArray = methodBody.GetILAsByteArray();
					if (iLAsByteArray != null)
					{
						byte[] array = iLAsByteArray;
						foreach (byte b in array)
						{
							num = num * 31 + b;
						}
					}
				}
			}
		}
		catch
		{
			num = 0u;
		}
		return num;
	}

	internal static bool IsSessionValid()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool QuickValidate()
	{
		return true;
	}

	internal static void SyncValidationState()
	{
		if (__svc_internal && SessionToken != 0L && CurrentSessionToken != 0L && !string.IsNullOrEmpty(CurrentKey))
		{
			_textureMemoryPool = (uint)(SessionToken ^ CurrentSessionToken ^ (uint)CurrentKey.GetHashCode());
			__svc_checksum = 0x5A5A ^ (int)(SessionToken & 0xFFFF) ^ (_integrityCounter * 4919);
		}
	}

	internal static void UpdateValidationState(bool success, long serverTime)
	{
		if (success && !string.IsNullOrEmpty(CurrentKey))
		{
			_rtx = SessionToken;
			_vcs = CurrentKey.GetHashCode() ^ (int)(CurrentSessionToken >> 16);
			_xf = (byte)((CurrentKey[0] ^ CurrentKey[CurrentKey.Length - 1]) & 0xFF);
			_hv = (uint)(SessionToken ^ CurrentSessionToken ^ 0xDEADBEEFu);
			_lastServerCheck = ((serverTime > 0) ? serverTime : System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		}
		else
		{
			_rtx = 0L;
			_vcs = 0;
			_xf = 0;
			_hv = 0u;
			_lastServerCheck = 0L;
		}
	}

	internal static void SetSessionTokenInternal(long token)
	{
		if (token > 0)
		{
			CurrentSessionToken = token;
			SyncValidationState();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IncrementTick()
	{
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (num - _lastTickUpdateMs >= 14)
		{
			_lastTickUpdateMs = num;
			_localTick++;
		}
	}

	internal static bool ValidateServerTick(long serverExpected, long tolerance)
	{
		_serverExpectedTick = serverExpected;
		_serverTickTolerance = tolerance;
		if (Math.Abs(_localTick - serverExpected) > tolerance)
		{
			ServerData.TriggerSilentDenial();
			return false;
		}
		return true;
	}

	internal static void ResetTicks()
	{
		_localTick = 0L;
		_lastTickUpdateMs = 0L;
		_serverExpectedTick = 0L;
	}

	private static long CalculateSessionToken(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return 0L;
		}
		long num = 0L;
		foreach (char c in key)
		{
			num = num * 31 + c;
		}
		return num;
	}

	internal static async Task<(bool success, string message, string username)> ValidateKeyAsync(string keyFromInput)
	{
		if (string.IsNullOrWhiteSpace(keyFromInput))
		{
			return (success: false, message: "No key provided.", username: null);
		}
		if (!KeyFormatRegex.IsMatch(keyFromInput))
		{
			return (success: false, message: "Invalid key format.", username: null);
		}
		Exception ex = null;
		bool flag = default(bool);
		for (int attempt = 0; attempt < 2; attempt++)
		{
			if (attempt > 0)
			{
				await Task.Delay(1500);
			}
			try
			{
				return await ValidateKeyAttemptAsync(keyFromInput);
			}
			catch (HttpRequestException ex2)
			{
				ex = ex2;
				string fullExceptionChain = GetFullExceptionChain(ex2);
				string sslDiagLog = _sslDiagLog;
				if (ModMenuCrewPlugin.Instance == null)
				{
					continue;
				}
				ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(44, 3, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Validation] Network attempt ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(attempt + 1);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(2);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" FULL DETAIL:\n");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(fullExceptionChain);
				}
				log.LogError(val);
				if (!string.IsNullOrEmpty(sslDiagLog))
				{
					ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					val = new BepInExErrorLogInterpolatedStringHandler(32, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Validation] SSL CALLBACK DIAG:\n");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(sslDiagLog);
					}
					log2.LogError(val);
				}
				else
				{
					ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					val = new BepInExErrorLogInterpolatedStringHandler(78, 0, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Validation] SSL callback was NEVER invoked (handshake failed before callback)");
					}
					log3.LogError(val);
				}
			}
			catch (TaskCanceledException ex3)
			{
				ex = ex3;
				if (ModMenuCrewPlugin.Instance != null)
				{
					ManualLogSource log4 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(30, 2, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[Validation] Timeout attempt ");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(attempt + 1);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("/");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(2);
					}
					log4.LogWarning(val2);
				}
			}
			catch (Exception ex4)
			{
				string fullExceptionChain2 = GetFullExceptionChain(ex4);
				if (ModMenuCrewPlugin.Instance != null)
				{
					ManualLogSource log5 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(43, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Validation] Unexpected error FULL DETAIL:\n");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(fullExceptionChain2);
					}
					log5.LogError(val);
				}
				return (success: false, message: "Unexpected error: " + ex4.GetType().Name + ": " + ex4.Message, username: null);
			}
		}
		if (ex is TaskCanceledException)
		{
			return (success: false, message: "Validation timeout. Check your connection.", username: null);
		}
		string fullExceptionChain3 = GetFullExceptionChain(ex);
		if (fullExceptionChain3.Contains("SSL") || fullExceptionChain3.Contains("TLS") || fullExceptionChain3.Contains("certificate") || fullExceptionChain3.Contains("AuthenticationException") || fullExceptionChain3.Contains("SecureChannel"))
		{
			try
			{
				ModMenuCrewPlugin.DebuggerComponent.OpenBrowser("https://crewcore.online/");
			}
			catch
			{
			}
			return (success: false, message: "Connection error — your version may be outdated.\nPlease download the latest version at crewcore.online", username: null);
		}
		string fullExceptionChainShort = GetFullExceptionChainShort(ex);
		return (success: false, message: "Login error: " + fullExceptionChainShort, username: null);
	}

	private static string GetFullExceptionChain(Exception ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		Exception ex2 = ex;
		while (ex2 != null)
		{
			stringBuilder.AppendLine($"[Level {num}] {ex2.GetType().FullName}: {ex2.Message}");
			if (ex2.StackTrace != null)
			{
				string[] array = ex2.StackTrace.Split('\n');
				for (int i = 0; i < Math.Min(3, array.Length); i++)
				{
					stringBuilder.AppendLine($"  {array[i].Trim()}");
				}
			}
			ex2 = ex2.InnerException;
			num++;
		}
		return stringBuilder.ToString();
	}

	private static string GetFullExceptionChainShort(Exception ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Exception ex2 = ex;
		int num = 0;
		while (ex2 != null)
		{
			if (num > 0)
			{
				stringBuilder.Append(" → ");
			}
			stringBuilder.Append($"{ex2.GetType().Name}: {ex2.Message}");
			ex2 = ex2.InnerException;
			num++;
		}
		return stringBuilder.ToString();
	}

	private static async Task<(bool success, string message, string username)> ValidateKeyAttemptAsync(string keyFromInput)
	{
		string hardwareId = GetHardwareId();
		string requestUrl = $"{ApiBaseUrl}/validate?key={System.Uri.EscapeDataString(keyFromInput)}&hwid={System.Uri.EscapeDataString(hardwareId)}&version={System.Uri.EscapeDataString("6.0.8")}&tick={_localTick}";
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
		string identityCredential = DiscordAuthManager.IdentityCredential;
		if (!string.IsNullOrEmpty(identityCredential))
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identityCredential);
		}
		using HttpResponseMessage response = await httpClient.SendAsync(request);
		string text = await response.Content.ReadAsStringAsync();
		bool flag = default(bool);
		if (!response.IsSuccessStatusCode)
		{
			try
			{
				string text2 = (response.Headers.Contains("CF-RAY") ? response.Headers.GetValues("CF-RAY").FirstOrDefault() : "none");
				string text3 = (response.Headers.Contains("Server") ? response.Headers.GetValues("Server").FirstOrDefault() : "none");
				string text4 = ((text != null && text.Length > 300) ? text.Substring(0, 300) : (text ?? "empty"));
				ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
				if (((instance != null) ? ((BasePlugin)instance).Log : null) != null)
				{
					ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(53, 6, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[VALIDATE-DIAG] URL=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(requestUrl);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" Status=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<HttpStatusCode>(response.StatusCode);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" (");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((int)response.StatusCode);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(") CF-RAY=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" Server=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text3);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" Body=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text4);
					}
					log.LogError(val);
				}
			}
			catch
			{
			}
		}
		if (response.IsSuccessStatusCode)
		{
			ApiValidationResponse apiValidationResponse = null;
			try
			{
				apiValidationResponse = JsonSerializer.Deserialize<ApiValidationResponse>(text, _jsonOpts);
			}
			catch
			{
			}
			if (apiValidationResponse != null && "success".Equals(apiValidationResponse.Status, StringComparison.OrdinalIgnoreCase))
			{
				if (!VerifyRSASignature(apiValidationResponse))
				{
					ServerData.TriggerSilentDenial();
					return (success: false, message: "Server verification failed.", username: null);
				}
				if (apiValidationResponse.ExpectedTick.HasValue && apiValidationResponse.ExpectedTick.Value > 0)
				{
					long tolerance = apiValidationResponse.TickTolerance ?? 100;
					if (!ValidateServerTick(apiValidationResponse.ExpectedTick.Value, tolerance))
					{
						return (success: false, message: "Session sync failed.", username: null);
					}
				}
				DiscordRevoked = false;
				RevokeReason = "";
				CurrentKey = keyFromInput;
				SessionToken = CalculateSessionToken(CurrentKey);
				long num = ((apiValidationResponse.SessionToken.GetValueOrDefault() > 1000) ? apiValidationResponse.SessionToken.GetValueOrDefault() : System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
				KeyType = apiValidationResponse.KeyType ?? "standard";
				if (apiValidationResponse.ExpiresAt.HasValue && apiValidationResponse.ExpiresAt.Value > 0)
				{
					ExpiresAt = System.DateTimeOffset.FromUnixTimeSeconds(apiValidationResponse.ExpiresAt.Value).UtcDateTime;
				}
				else
				{
					ExpiresAt = null;
				}
				ValidatedUsername = apiValidationResponse.Username ?? "User";
				SetServiceContext(value: true, SessionToken);
				UpdateProof(apiValidationResponse.SessionProof, apiValidationResponse.ProofSeed.GetValueOrDefault(), apiValidationResponse.ProofExpires.GetValueOrDefault());
				ServerGate.UpdateRenderPermission(apiValidationResponse.RenderKey, apiValidationResponse.RenderExpires.GetValueOrDefault(), apiValidationResponse.RenderNonce.GetValueOrDefault());
				if (apiValidationResponse.UiPayload != null && !string.IsNullOrEmpty(apiValidationResponse.SessionKey))
				{
					try
					{
						ServerData.ParseFromEncryptedPayload(new ServerData.EncryptedPayload
						{
							Ciphertext = apiValidationResponse.UiPayload.Ciphertext,
							Iv = apiValidationResponse.UiPayload.Iv,
							Tag = apiValidationResponse.UiPayload.Tag
						}, apiValidationResponse.SessionKey, num);
					}
					catch (Exception ex)
					{
						if (ModMenuCrewPlugin.Instance != null)
						{
							ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
							BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(30, 1, out flag);
							if (flag)
							{
								((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[Validation] UI Parse failed: ");
								((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
							}
							log2.LogWarning(val2);
						}
						ServerData.SetLoaded(loaded: false);
					}
				}
				else if (num > 0)
				{
					CurrentSessionToken = num;
				}
				StartHeartbeat();
				return (success: true, message: apiValidationResponse.Message ?? "Key validated!", username: apiValidationResponse.Username);
			}
			return (success: false, message: apiValidationResponse?.Message ?? "Invalid key/API error.", username: null);
		}
		string item = $"Server error ({response.StatusCode}).";
		try
		{
			if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith("{"))
			{
				ApiValidationResponse apiValidationResponse2 = JsonSerializer.Deserialize<ApiValidationResponse>(text, _jsonOpts);
				if (apiValidationResponse2 != null && apiValidationResponse2.ForceUpdate == true)
				{
					string url = apiValidationResponse2.DownloadUrl ?? "https://crewcore.online/";
					string text5 = apiValidationResponse2.MinVersion ?? "6.0.7c";
					if (ModMenuCrewPlugin.Instance != null)
					{
						ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
						BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(63, 2, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[IRON CURTAIN] Mandatory update required. Current: ");
							((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>("6.0.8");
							((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", Required: ");
							((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(text5);
						}
						log3.LogWarning(val2);
					}
					ModMenuCrewPlugin.DebuggerComponent.OpenBrowser(url);
					return (success: false, message: "⚠\ufe0f SECURITY UPDATE REQUIRED ⚠\ufe0f\nPlease download v" + text5 + " or newer.\nOpening download page...", username: null);
				}
				if (apiValidationResponse2 != null && "discord_left".Equals(apiValidationResponse2.Reason, StringComparison.OrdinalIgnoreCase))
				{
					DiscordRevoked = true;
					RevokeReason = "discord_left";
				}
				if (!string.IsNullOrWhiteSpace(apiValidationResponse2?.Message))
				{
					item = ((!string.IsNullOrWhiteSpace(apiValidationResponse2.Hint)) ? (apiValidationResponse2.Message + "\n" + apiValidationResponse2.Hint) : apiValidationResponse2.Message);
				}
			}
		}
		catch (JsonException)
		{
		}
		return (success: false, message: item, username: null);
	}

	internal static void ForceHeartbeatWakeup()
	{
		try
		{
			CancellationTokenSource cancellationTokenSource = Interlocked.CompareExchange(ref _sleepTokenSource, null, null);
			if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
			{
				cancellationTokenSource.Cancel();
			}
		}
		catch
		{
		}
	}

	private static bool SafeIsInMeeting()
	{
		try
		{
			return (UnityEngine.Object)(object)MeetingHud.Instance != (UnityEngine.Object)null;
		}
		catch
		{
			return false;
		}
	}

	private static string SafeGetCurrentRole()
	{
		try
		{
			return RealtimeConnection.GetCurrentRoleForHeartbeat();
		}
		catch
		{
			return "";
		}
	}

	internal static async void StartHeartbeat()
	{
		if (!_enableHeartbeat || Interlocked.CompareExchange(ref _isHeartbeatRunningInt, 1, 0) != 0)
		{
			return;
		}
		_debugSessionDeathLogged = false;
		_heartbeatCancellation = new CancellationTokenSource();
		int consecutiveFailures = 0;
		bool isFirstIteration = true;
		try
		{
			RealtimeConnection.Connect(ApiBaseUrl, CurrentKey, CurrentSessionToken.ToString());
		}
		catch
		{
		}
		try
		{
			await Task.Run(async delegate
			{
				_debugHeartbeatMsg = $"[DEBUG-HEARTBEAT] Loop STARTED. IsSessionValid={IsSessionValid()} cancelled={_heartbeatCancellation.Token.IsCancellationRequested}";
				while (IsSessionValid() && !_heartbeatCancellation.Token.IsCancellationRequested)
				{
					try
					{
						int millisecondsDelay;
						if (PlayerPickMenu.PendingImmediateHeartbeat)
						{
							millisecondsDelay = 100;
							PlayerPickMenu.PendingImmediateHeartbeat = false;
						}
						else if (isFirstIteration)
						{
							millisecondsDelay = 3000;
							isFirstIteration = false;
						}
						else
						{
							millisecondsDelay = ((consecutiveFailures > 0) ? Math.Min(5000 + consecutiveFailures * 5000, 60000) : 45000);
						}
						CancellationTokenSource newSleepCts = new CancellationTokenSource();
						CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref _sleepTokenSource, newSleepCts);
						try
						{
							cancellationTokenSource?.Dispose();
						}
						catch
						{
						}
						using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_heartbeatCancellation.Token, newSleepCts.Token))
						{
							try
							{
								await Task.Delay(millisecondsDelay, linkedCts.Token);
							}
							catch (TaskCanceledException)
							{
							}
						}
						try
						{
							newSleepCts.Dispose();
						}
						catch
						{
						}
						Interlocked.CompareExchange(ref _sleepTokenSource, null, newSleepCts);
						if (!IsSessionValid() || _heartbeatCancellation.Token.IsCancellationRequested)
						{
							_debugHeartbeatMsg = $"[DEBUG-HEARTBEAT] Loop BREAKING! IsSessionValid={IsSessionValid()} cancelled={_heartbeatCancellation.Token.IsCancellationRequested}";
							break;
						}
						string hwid = GetHardwareId();
						object playerData = null;
						bool isHost = false;
						bool isInGame = false;
						byte localPlayerId = byte.MaxValue;
						object uiState = null;
						object banMenuState = null;
						object banMenuUiState = null;
						object cheatsState = null;
						object cheatsUiState = null;
						object alivePlayers = null;
						object spoofingState = null;
						bool isInMeeting = false;
						string currentRole = "";
						string httpAttProof = "";
						TaskCompletionSource<bool> dataTcs = new TaskCompletionSource<bool>();
						ActionPermitSystem.EnqueueMainThread(delegate
						{
							try
							{
								playerData = PlayerPickMenu.CollectPlayerDataForServer();
							}
							catch
							{
							}
							try
							{
								(isHost, isInGame, localPlayerId) = PlayerPickMenu.GetGameContext();
							}
							catch
							{
							}
							try
							{
								uiState = PlayerPickMenu.GetUIState();
							}
							catch
							{
							}
							try
							{
								banMenuState = global::ModMenuCrew.UI.Menus.BanMenu.GetBanMenuState();
							}
							catch
							{
							}
							try
							{
								banMenuUiState = global::ModMenuCrew.UI.Menus.BanMenu.GetUIState();
							}
							catch
							{
							}
							try
							{
								cheatsState = CheatManager.GetCheatsState();
							}
							catch
							{
							}
							try
							{
								cheatsUiState = CheatManager.GetCheatsUiState();
							}
							catch
							{
							}
							try
							{
								alivePlayers = CheatManager.GetAlivePlayersForServer();
							}
							catch
							{
							}
							try
							{
								spoofingState = SpoofingMenu.GetSpoofingState();
							}
							catch
							{
							}
							try
							{
								isInMeeting = (UnityEngine.Object)(object)MeetingHud.Instance != (UnityEngine.Object)null;
							}
							catch
							{
							}
							try
							{
								currentRole = RealtimeConnection.GetCurrentRoleForHeartbeat();
							}
							catch
							{
							}
							try
							{
								httpAttProof = ActionPermitSystem.ComputeAttestationProof();
							}
							catch
							{
							}
							dataTcs.TrySetResult(result: true);
						});
						try
						{
							await Task.WhenAny(dataTcs.Task, Task.Delay(5000, _heartbeatCancellation.Token));
						}
						catch (OperationCanceledException)
						{
							break;
						}
						if (_heartbeatCancellation.Token.IsCancellationRequested)
						{
							break;
						}
						string content = JsonSerializer.Serialize(new
						{
							key = CurrentKey,
							token = CurrentSessionToken,
							hwid = hwid,
							heartbeat_nonce = _serverHeartbeatNonce,
							attestation_proof = httpAttProof,
							players = playerData,
							isHost = isHost,
							isInGame = isInGame,
							localPlayerId = (int)localPlayerId,
							uiState = uiState,
							banMenuState = banMenuState,
							banMenuUiState = banMenuUiState,
							cheatsState = cheatsState,
							cheatsUiState = cheatsUiState,
							alivePlayers = alivePlayers,
							spoofingState = spoofingState,
							isInMeeting = isInMeeting,
							currentRole = currentRole
						});
						using StringContent content2 = new StringContent(content, Encoding.UTF8, "application/json");
						using (HttpResponseMessage response = await httpClient.PostAsync(ApiBaseUrl + "/heartbeat", content2, _heartbeatCancellation.Token))
						{
							if (response.IsSuccessStatusCode)
							{
								consecutiveFailures = 0;
								try
								{
									RealtimeConnection.ResetReconnectAttempts();
								}
								catch
								{
								}
								lock (_validationLock)
								{
									_lastServerCheck = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
								}
								ApiValidationResponse apiValidationResponse = JsonSerializer.Deserialize<ApiValidationResponse>(await response.Content.ReadAsStringAsync(), _jsonOpts);
								long valueOrDefault = (apiValidationResponse?.NewToken).GetValueOrDefault();
								if (apiValidationResponse != null && apiValidationResponse.UiPayload != null && !string.IsNullOrEmpty(apiValidationResponse.SessionKey))
								{
									try
									{
										ServerData.ParseFromEncryptedPayload(new ServerData.EncryptedPayload
										{
											Ciphertext = apiValidationResponse.UiPayload.Ciphertext,
											Iv = apiValidationResponse.UiPayload.Iv,
											Tag = apiValidationResponse.UiPayload.Tag
										}, apiValidationResponse.SessionKey, valueOrDefault, isHeartbeat: true);
									}
									catch (Exception ex3)
									{
										try
										{
											ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
											if (((instance != null) ? ((BasePlugin)instance).Log : null) != null)
											{
												ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
												bool flag;
												BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(35, 1, out flag);
												if (flag)
												{
													((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModKeyValidator] UI Parse failed: ");
													((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex3.Message);
												}
												log.LogWarning(val);
											}
										}
										catch
										{
										}
										ServerData.SetLoaded(loaded: false);
									}
								}
								else if (valueOrDefault > 0)
								{
									CurrentSessionToken = valueOrDefault;
									SyncValidationState();
								}
								if (apiValidationResponse != null && !string.IsNullOrEmpty(apiValidationResponse.SessionProof))
								{
									UpdateProof(apiValidationResponse.SessionProof, apiValidationResponse.ProofSeed.GetValueOrDefault(), apiValidationResponse.ProofExpires.GetValueOrDefault());
								}
								if (apiValidationResponse != null && !string.IsNullOrEmpty(apiValidationResponse.RenderKey))
								{
									ServerGate.UpdateRenderPermission(apiValidationResponse.RenderKey, apiValidationResponse.RenderExpires.GetValueOrDefault(), apiValidationResponse.RenderNonce.GetValueOrDefault());
								}
								if (apiValidationResponse != null && !string.IsNullOrEmpty(apiValidationResponse.HeartbeatNonce))
								{
									_serverHeartbeatNonce = apiValidationResponse.HeartbeatNonce;
								}
								if (apiValidationResponse != null && !string.IsNullOrEmpty(apiValidationResponse.AttestationSeed) && apiValidationResponse.AttestationMethods != null && apiValidationResponse.AttestationMethods.Length != 0)
								{
									try
									{
										ActionPermitSystem.UpdateAttestation(apiValidationResponse.AttestationSeed, apiValidationResponse.AttestationMethods);
									}
									catch
									{
									}
									if (RealtimeConnection.IsConnected)
									{
										try
										{
											RealtimeConnection.ForceReconnect();
										}
										catch
										{
										}
									}
								}
								goto end_IL_0570;
							}
							if (response.StatusCode == HttpStatusCode.Forbidden)
							{
								try
								{
									ApiValidationResponse apiValidationResponse2 = JsonSerializer.Deserialize<ApiValidationResponse>(await response.Content.ReadAsStringAsync(), _jsonOpts);
									if (apiValidationResponse2 != null && "discord_left".Equals(apiValidationResponse2.Reason, StringComparison.OrdinalIgnoreCase))
									{
										DiscordRevoked = true;
										RevokeReason = "discord_left";
									}
								}
								catch
								{
								}
								PendingResetRequest = true;
								break;
							}
							consecutiveFailures++;
							goto end_IL_0553;
							end_IL_0570:;
						}
						end_IL_0553:;
					}
					catch (TaskCanceledException)
					{
						if (_heartbeatCancellation != null && _heartbeatCancellation.Token.IsCancellationRequested)
						{
							break;
						}
						consecutiveFailures++;
					}
					catch (OperationCanceledException)
					{
						if (_heartbeatCancellation != null && _heartbeatCancellation.Token.IsCancellationRequested)
						{
							break;
						}
						consecutiveFailures++;
					}
					catch (Exception)
					{
						consecutiveFailures++;
					}
				}
			}, _heartbeatCancellation.Token);
		}
		catch
		{
		}
		finally
		{
			_debugHeartbeatMsg = $"[DEBUG-HEARTBEAT] STOPPED. IsSessionValid={IsSessionValid()} _debugSessionDeathLogged={_debugSessionDeathLogged}";
			Interlocked.Exchange(ref _isHeartbeatRunningInt, 0);
			_heartbeatCancellation?.Dispose();
			_heartbeatCancellation = null;
		}
	}

	internal static void StopHeartbeat()
	{
		try
		{
			_heartbeatCancellation?.Cancel();
		}
		catch
		{
		}
	}

	internal static void UpdateValidationState(bool isValidated, string message, string username)
	{
		if (isValidated)
		{
			if (SessionToken != 0L && CurrentSessionToken != 0L && !string.IsNullOrEmpty(CurrentKey))
			{
				UpdateValidationState(success: true, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
				SyncValidationState();
			}
		}
		else
		{
			SessionToken = 0L;
			CurrentSessionToken = 0L;
			SetServiceContext(value: false, 0L);
			_textureMemoryPool = 0u;
			UpdateValidationState(success: false, 0L);
		}
		LastValidationMessage = message;
		if (!string.IsNullOrEmpty(username))
		{
			ValidatedUsername = username;
		}
		UpdateFormattedString();
		try
		{
			SaveValidationState(isValidated, message, ValidatedUsername);
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast("[ModKeyValidator] SaveValidationState failed (non-fatal): " + ex.Message));
		}
	}

	private static void UpdateFormattedString()
	{
		FormattedDisplayString = ValidatedUsername + " -- https://crewcore.online/";
	}

	public static string TruncateMessage(string message, int maxLength = 50)
	{
		if (!string.IsNullOrEmpty(message))
		{
			if (message.Length > maxLength)
			{
				return message.Substring(0, maxLength) + "...";
			}
			return message;
		}
		return "";
	}

	internal static void LoadValidationState()
	{
		LastValidationMessage = "Enter your activation key.";
		string playerPrefsKeyActivated = GetPlayerPrefsKeyActivated();
		if (PlayerPrefs.HasKey(playerPrefsKeyActivated + "_KeyType"))
		{
			KeyType = PlayerPrefs.GetString(playerPrefsKeyActivated + "_KeyType", "standard");
		}
		if (PlayerPrefs.HasKey(playerPrefsKeyActivated + "_ExpiresAt") && long.TryParse(PlayerPrefs.GetString(playerPrefsKeyActivated + "_ExpiresAt", ""), out var result) && result > 0)
		{
			ExpiresAt = new DateTime(result, DateTimeKind.Utc);
		}
	}

	internal static void ResetValidationState()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if (ModMenuCrewPlugin.Instance != null)
		{
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(49, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModKeyValidator] Validation state reset. Trace: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<StackTrace>(new StackTrace());
			}
			log.LogInfo(val);
		}
		StopHeartbeat();
		ClearProof();
		ServerGate.Revoke();
		ResetTicks();
		PendingResetRequest = false;
		SessionToken = 0L;
		CurrentSessionToken = 0L;
		__svc_internal = false;
		__svc_checksum = 0;
		_textureMemoryPool = 0u;
		_rtx = 0L;
		_vcs = 0;
		_xf = 0;
		_hv = 0u;
		_lastServerCheck = 0L;
		LastValidationMessage = "Enter your activation key.";
		ValidatedUsername = "";
		CurrentKey = "";
		KeyType = "standard";
		ExpiresAt = null;
		UpdateFormattedString();
		string playerPrefsKeyActivated = GetPlayerPrefsKeyActivated();
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated);
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated + "_Message");
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated + "_Username");
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated + "_IsPremium");
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated + "_KeyType");
		PlayerPrefs.DeleteKey(playerPrefsKeyActivated + "_ExpiresAt");
		PlayerPrefs.Save();
		ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
		if (((instance != null) ? ((BasePlugin)instance).Log : null) != null)
		{
			((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ModKeyValidator] Validation state reset.");
		}
	}

	private static void SaveValidationState(bool isValidated, string message, string username)
	{
		PlayerPrefs.SetInt(GetPlayerPrefsKeyActivated(), isValidated ? 1 : 0);
		PlayerPrefs.SetString(GetPlayerPrefsKeyActivated() + "_Message", message);
		PlayerPrefs.SetString(GetPlayerPrefsKeyActivated() + "_Username", username);
		PlayerPrefs.SetString(GetPlayerPrefsKeyActivated() + "_KeyType", KeyType ?? "standard");
		if (ExpiresAt.HasValue)
		{
			PlayerPrefs.SetString(GetPlayerPrefsKeyActivated() + "_ExpiresAt", ExpiresAt.Value.Ticks.ToString());
		}
		else
		{
			PlayerPrefs.DeleteKey(GetPlayerPrefsKeyActivated() + "_ExpiresAt");
		}
		PlayerPrefs.Save();
		if (isValidated)
		{
			if (SessionToken == 0L && !string.IsNullOrEmpty(CurrentKey))
			{
				SessionToken = CalculateSessionToken(CurrentKey);
			}
		}
		else
		{
			SessionToken = 0L;
			CurrentSessionToken = 0L;
		}
		LastValidationMessage = message;
		ValidatedUsername = username;
		UpdateFormattedString();
	}
}






