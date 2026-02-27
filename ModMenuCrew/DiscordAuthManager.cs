using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ModMenuCrew;

public static class DiscordAuthManager
{
	private static readonly HttpClient _pinnedClient = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (HttpRequestMessage request, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) => ModKeyValidator.UnifiedSslCallback(request, cert, chain, errors, runSpkiPin: true),
		AllowAutoRedirect = false
	})
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	private static string _identityCredential = null;

	private static string _discordUsername = null;

	private static string _discordAvatar = null;

	private static string _discordId = null;

	private static DateTime _credentialExpires = DateTime.MinValue;

	private const string PlayerPrefsCredentialKey = "ModMenuCrew_IdentityCredential";

	private const string PlayerPrefsUsernameKey = "ModMenuCrew_IdentityUsername";

	private const string PlayerPrefsAvatarKey = "ModMenuCrew_IdentityAvatar";

	private const string PlayerPrefsDiscordIdKey = "ModMenuCrew_IdentityDiscordId";

	private static volatile bool _pendingSave = false;

	private static Texture2D _avatarTexture = null;

	private static readonly Regex _snowflakeRegex = new Regex("^\\d{17,20}$", RegexOptions.Compiled);

	private static readonly Regex _avatarHashRegex = new Regex("^(a_)?[a-f0-9]{32}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static volatile string _loginStatusMessage = "";

	private static volatile bool _pendingCallbackReady = false;

	private static bool _pendingCallbackSuccess = false;

	private static string _pendingCallbackMessage = "";

	private static string ApiBaseUrl => "https://dev.crewcore.online";

	public static bool IsLoggedIn
	{
		get
		{
			if (!string.IsNullOrEmpty(_identityCredential))
			{
				return DateTime.UtcNow < _credentialExpires;
			}
			return false;
		}
	}

	public static string DiscordUsername => _discordUsername;

	public static string DiscordAvatar => _discordAvatar;

	public static string DiscordId => _discordId;

	public static string IdentityCredential
	{
		get
		{
			if (!IsLoggedIn)
			{
				return null;
			}
			return _identityCredential;
		}
	}

	public static Texture2D AvatarTexture => _avatarTexture;

	public static bool IsAvatarLoaded => (Object)(object)_avatarTexture != (Object)null;

	public static bool IsLoggingIn { get; private set; } = false;

	public static string LoginStatusMessage
	{
		get
		{
			return _loginStatusMessage;
		}
		private set
		{
			_loginStatusMessage = value;
		}
	}

	internal static string LastAuthUrl { get; private set; } = null;

	internal static bool HasPendingSave => _pendingSave;

	internal static bool HasPendingCallback => _pendingCallbackReady;

	public static event System.Action<bool, string> OnLoginComplete;

	internal static HttpClient GetPinnedClient()
	{
		return _pinnedClient;
	}

	internal static void SetAvatarTexture(Texture2D tex)
	{
		_avatarTexture = tex;
	}

	public static string GetAvatarUrl(int size = 128)
	{
		if (string.IsNullOrEmpty(_discordId) || string.IsNullOrEmpty(_discordAvatar))
		{
			return null;
		}
		if (!_snowflakeRegex.IsMatch(_discordId))
		{
			return null;
		}
		if (!_avatarHashRegex.IsMatch(_discordAvatar))
		{
			return null;
		}
		return $"https://cdn.discordapp.com/avatars/{_discordId}/{_discordAvatar}.png?size={size}";
	}

	internal static void ProcessPendingCallbacks()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		if (!_pendingCallbackReady)
		{
			return;
		}
		_pendingCallbackReady = false;
		if (_pendingSave && !string.IsNullOrEmpty(_identityCredential))
		{
			PlayerPrefs.SetString("ModMenuCrew_IdentityCredential", _identityCredential);
			PlayerPrefs.SetString("ModMenuCrew_IdentityUsername", _discordUsername ?? "");
			PlayerPrefs.SetString("ModMenuCrew_IdentityAvatar", _discordAvatar ?? "");
			PlayerPrefs.SetString("ModMenuCrew_IdentityDiscordId", _discordId ?? "");
			PlayerPrefs.Save();
			_pendingSave = false;
			ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = ((BasePlugin)instance).Log;
				if (log != null)
				{
					bool flag = default(bool);
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(36, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[DiscordAuth] Credentials saved for ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_discordUsername);
					}
					log.LogInfo(val);
				}
			}
		}
		DiscordAuthManager.OnLoginComplete?.Invoke(_pendingCallbackSuccess, _pendingCallbackMessage);
	}

	private static void QueueCallback(bool success, string message)
	{
		_pendingCallbackSuccess = success;
		_pendingCallbackMessage = message;
		_pendingCallbackReady = true;
	}

	internal static void Logout()
	{
		_identityCredential = null;
		_discordUsername = null;
		_discordAvatar = null;
		_discordId = null;
		_credentialExpires = DateTime.MinValue;
		PlayerPrefs.DeleteKey("ModMenuCrew_IdentityCredential");
		PlayerPrefs.DeleteKey("ModMenuCrew_IdentityUsername");
		PlayerPrefs.DeleteKey("ModMenuCrew_IdentityAvatar");
		PlayerPrefs.DeleteKey("ModMenuCrew_IdentityDiscordId");
		PlayerPrefs.Save();
		ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
		if (instance != null)
		{
			ManualLogSource log = ((BasePlugin)instance).Log;
			if (log != null)
			{
				log.LogInfo((object)"[DiscordAuth] Logged out - credentials cleared.");
			}
		}
	}

	internal static void TryRestoreSession()
	{
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			if (!PlayerPrefs.HasKey("ModMenuCrew_IdentityCredential"))
			{
				return;
			}
			string text = PlayerPrefs.GetString("ModMenuCrew_IdentityCredential", "");
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (ParseIdentityToken(text))
			{
				_identityCredential = text;
				_discordUsername = PlayerPrefs.GetString("ModMenuCrew_IdentityUsername", "Discord User");
				_discordAvatar = PlayerPrefs.GetString("ModMenuCrew_IdentityAvatar", "");
				_discordId = PlayerPrefs.GetString("ModMenuCrew_IdentityDiscordId", "");
				ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				ManualLogSource log = ((BasePlugin)instance).Log;
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(42, 2, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[DiscordAuth] Session restored for ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_discordUsername);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" (ID: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_discordId);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(")");
					}
					log.LogInfo(val);
				}
				return;
			}
			_discordUsername = null;
			_discordAvatar = null;
			_discordId = null;
			_identityCredential = null;
			_credentialExpires = DateTime.MinValue;
			PlayerPrefs.DeleteKey("ModMenuCrew_IdentityCredential");
			PlayerPrefs.DeleteKey("ModMenuCrew_IdentityUsername");
			PlayerPrefs.DeleteKey("ModMenuCrew_IdentityAvatar");
			PlayerPrefs.DeleteKey("ModMenuCrew_IdentityDiscordId");
			PlayerPrefs.Save();
			ModMenuCrewPlugin instance2 = ModMenuCrewPlugin.Instance;
			if (instance2 != null)
			{
				ManualLogSource log2 = ((BasePlugin)instance2).Log;
				if (log2 != null)
				{
					log2.LogInfo((object)"[DiscordAuth] Saved token expired or invalid — cleared. Please re-login.");
				}
			}
		}
		catch (Exception ex)
		{
			ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = ((BasePlugin)instance).Log;
			if (log != null)
			{
				BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(39, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[DiscordAuth] Error restoring session: ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
				}
				log.LogError(val2);
			}
		}
	}

	private static bool ParseIdentityToken(string token)
	{
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		try
		{
			if (string.IsNullOrEmpty(token))
			{
				return false;
			}
			string[] array = token.Split('.');
			if (array.Length != 2)
			{
				return false;
			}
			Dictionary<string, object> dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(array[0].Replace('-', '+').Replace('_', '/').PadRight(array[0].Length + (4 - array[0].Length % 4) % 4, '='))));
			if (dictionary == null)
			{
				return false;
			}
			if (dictionary.TryGetValue("exp", out var value) && value is JsonElement jsonElement)
			{
				_credentialExpires = System.DateTimeOffset.FromUnixTimeSeconds(jsonElement.GetInt64()).UtcDateTime;
				if (DateTime.UtcNow >= _credentialExpires)
				{
					ModMenuCrewPlugin instance = ModMenuCrewPlugin.Instance;
					if (instance != null)
					{
						ManualLogSource log = ((BasePlugin)instance).Log;
						if (log != null)
						{
							log.LogInfo((object)"[DiscordAuth] Token expired");
						}
					}
					return false;
				}
			}
			if (dictionary.TryGetValue("discord_id", out var value2) && value2 is JsonElement jsonElement2)
			{
				_discordId = jsonElement2.GetString();
			}
			if (dictionary.TryGetValue("username", out var value3) && value3 is JsonElement jsonElement3)
			{
				_discordUsername = jsonElement3.GetString();
			}
			if (dictionary.TryGetValue("avatar", out var value4) && value4 is JsonElement jsonElement4)
			{
				_discordAvatar = jsonElement4.GetString();
			}
			return true;
		}
		catch (Exception ex)
		{
			ModMenuCrewPlugin instance2 = ModMenuCrewPlugin.Instance;
			if (instance2 != null)
			{
				ManualLogSource log2 = ((BasePlugin)instance2).Log;
				if (log2 != null)
				{
					bool flag = default(bool);
					BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[DiscordAuth] Token parse error: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
					}
					log2.LogError(val);
				}
			}
			return false;
		}
	}

	internal static async Task StartLoginAsync()
	{
		if (IsLoggingIn)
		{
			LoginStatusMessage = "Login already in progress...";
			return;
		}
		IsLoggingIn = true;
		LoginStatusMessage = "Initializing Discord login...";
		try
		{
			LoginStatusMessage = "Connecting to authentication server...";
			HttpClient client = _pinnedClient;
			HttpResponseMessage httpResponseMessage = await client.PostAsync(ApiBaseUrl + "/api/auth/init-poll", new StringContent("{}", Encoding.UTF8, "application/json"));
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				LoginStatusMessage = "Failed to initialize login. Please try again.";
				QueueCallback(success: false, LoginStatusMessage);
				return;
			}
			Dictionary<string, JsonElement> dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await httpResponseMessage.Content.ReadAsStringAsync());
			if (dictionary == null || !dictionary.TryGetValue("poll_code", out var value) || !dictionary.TryGetValue("poll_secret", out var value2) || !dictionary.TryGetValue("auth_url", out var value3))
			{
				LoginStatusMessage = "Invalid server response.";
				QueueCallback(success: false, LoginStatusMessage);
				return;
			}
			string pollCode = value.GetString();
			string pollSecret = value2.GetString();
			string authUrl = value3.GetString();
			if (string.IsNullOrEmpty(pollCode) || string.IsNullOrEmpty(pollSecret) || string.IsNullOrEmpty(authUrl))
			{
				LoginStatusMessage = "Server returned incomplete auth data.";
				QueueCallback(success: false, LoginStatusMessage);
				return;
			}
			LoginStatusMessage = "Opening browser for Discord login...";
			LastAuthUrl = authUrl;
			TaskCompletionSource<bool> browserTcs = new TaskCompletionSource<bool>();
			ActionPermitSystem.EnqueueMainThread(delegate
			{
				try
				{
					if (!ModMenuCrewPlugin.DebuggerComponent.OpenBrowser(authUrl))
					{
						try
						{
							GUIUtility.systemCopyBuffer = authUrl;
						}
						catch
						{
						}
						LoginStatusMessage = "Browser blocked! Use 'Copy Login Link' or open:\n" + authUrl;
					}
				}
				catch
				{
					try
					{
						GUIUtility.systemCopyBuffer = authUrl;
					}
					catch
					{
					}
					LoginStatusMessage = "Browser blocked! Use 'Copy Login Link' or open:\n" + authUrl;
				}
				browserTcs.TrySetResult(result: true);
			});
			await Task.WhenAny(browserTcs.Task, Task.Delay(5000));
			string loginStatusMessage = LoginStatusMessage;
			if (loginStatusMessage == null || !loginStatusMessage.Contains("blocked"))
			{
				LoginStatusMessage = "Complete login in browser. Waiting...";
			}
			int maxAttempts = 60;
			int attempt = 0;
			int consecutiveErrors = 0;
			while (attempt < maxAttempts)
			{
				await Task.Delay(2000);
				attempt++;
				int remaining = (maxAttempts - attempt) * 2;
				if (attempt == 7 && consecutiveErrors >= 3)
				{
					LoginStatusMessage = $"Browser not responding? Use 'Copy Login Link' button. ({remaining}s remaining)";
				}
				else if (consecutiveErrors < 5)
				{
					LoginStatusMessage = $"Waiting for Discord login... ({remaining}s remaining)";
				}
				try
				{
					Dictionary<string, JsonElement> dictionary2 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await (await client.GetAsync($"{ApiBaseUrl}/api/auth/poll-token?poll_code={Uri.EscapeDataString(pollCode)}&poll_secret={Uri.EscapeDataString(pollSecret)}")).Content.ReadAsStringAsync());
					if (dictionary2 == null)
					{
						continue;
					}
					consecutiveErrors = 0;
					if (!dictionary2.TryGetValue("status", out var value4))
					{
						continue;
					}
					switch (value4.GetString())
					{
					case "success":
					{
						if (dictionary2.TryGetValue("identity_token", out var value6))
						{
							_identityCredential = value6.GetString();
						}
						if (dictionary2.TryGetValue("username", out var value7))
						{
							_discordUsername = value7.GetString();
						}
						if (dictionary2.TryGetValue("discord_id", out var value8))
						{
							_discordId = value8.GetString();
						}
						if (dictionary2.TryGetValue("avatar", out var value9))
						{
							_discordAvatar = value9.GetString();
						}
						if (!ParseIdentityToken(_identityCredential))
						{
							_credentialExpires = DateTime.UtcNow.AddHours(24.0);
						}
						_pendingSave = true;
						LoginStatusMessage = "[OK] Logged in as " + _discordUsername + "!";
						QueueCallback(success: true, LoginStatusMessage);
						return;
					}
					case "error":
					{
						JsonElement value5;
						string text = (dictionary2.TryGetValue("message", out value5) ? value5.GetString() : "Authentication failed.");
						LoginStatusMessage = "[ERROR] " + text;
						QueueCallback(success: false, LoginStatusMessage);
						return;
					}
					}
				}
				catch (Exception)
				{
					consecutiveErrors++;
					if (consecutiveErrors >= 5)
					{
						LoginStatusMessage = $"Network issues detected ({consecutiveErrors} errors). Check your connection. ({remaining}s)";
					}
				}
			}
			LoginStatusMessage = "Login timed out. Please try again.";
			QueueCallback(success: false, LoginStatusMessage);
		}
		catch (Exception ex2)
		{
			string text2 = ex2.ToString();
			if (text2.Contains("SSL") || text2.Contains("TLS") || text2.Contains("certificate") || text2.Contains("AuthenticationException") || text2.Contains("SecureChannel"))
			{
				LoginStatusMessage = "Connection error — your version may be outdated.\nPlease download the latest version at crewcore.online";
				try
				{
					ModMenuCrewPlugin.DebuggerComponent.OpenBrowser("https://crewcore.online/");
				}
				catch
				{
				}
			}
			else
			{
				LoginStatusMessage = "Login error: " + ex2.Message;
			}
			QueueCallback(success: false, LoginStatusMessage);
		}
		finally
		{
			IsLoggingIn = false;
			LastAuthUrl = null;
		}
	}
}




