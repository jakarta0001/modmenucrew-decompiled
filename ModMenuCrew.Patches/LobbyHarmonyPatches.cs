using System.Collections.Generic;
using System.Text;
using AmongUs.GameOptions;
using BepInEx.Configuration;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using ModMenuCrew.Features;
using ModMenuCrew.UI.Menus;
using TMPro;
using UnityEngine;

namespace ModMenuCrew.Patches;

[HarmonyPatch]
public static class LobbyHarmonyPatches
{
	public enum Lang
	{
		PTBR,
		EN,
		ES
	}

	private enum LogLevel
	{
		Info,
		Warning,
		Error
	}

	private const string COLOR_GREEN = "#80FF80";

	private const string COLOR_YELLOW = "#FFFF80";

	private const string COLOR_RED = "#FF8080";

	private const string COLOR_CYAN = "#80FFFF";

	private const string COLOR_ORANGE = "#FFBF80";

	private const string COLOR_PURPLE = "#D980FF";

	private const string COLOR_WHITE = "#FFFFFF";

	private const string COLOR_ROLES = "#a7f";

	private const string SIZE_HEADER = "<size=37%>";

	private const string SIZE_DETAILS = "<size=30%>";

	private const string SIZE_ENDTAG = "</size>";

	private const float DEFAULT_LOBBY_TIMER_DURATION = 600f;

	private const int VANILLA_MAX_PLAYERS = 15;

	private const int VANILLA_MAX_IMPOSTORS = 3;

	private const float VANILLA_MAX_SPEED = 3f;

	private const float VANILLA_MIN_KILL_CD = 10f;

	private static readonly string[] KNOWN_MOD_SIGNATURES = new string[3] { "(M)", "[MOD]", "REACTOR" };

	public static ConfigFile Config;

	private static ConfigEntry<bool> cfgEnableDebugLogging;

	private static ConfigEntry<string> cfgLanguage;

	public static ConfigEntry<bool> cfgDisableLobbyMusic;

	public static ConfigEntry<bool> cfgShowLobbyInfo;

	private static ConfigEntry<bool> cfgShowGameSettings;

	private static ConfigEntry<bool> cfgShowRolesInfo;

	private static ConfigEntry<bool> cfgShowPlayerList;

	private static ConfigEntry<int> cfgPlayerListLimit;

	public static ConfigEntry<bool> cfgShowLobbyTimer;

	public static ConfigEntry<bool> cfgRgbLobbyCode;

	public static ConfigEntry<bool> cfgStreamerMode;

	private static ConfigEntry<bool> cfgHideCode;

	public static ConfigEntry<string> cfgCustomCode;

	public static ConfigEntry<bool> cfgAutoExtendTimer;

	private static ConfigEntry<int> cfgAutoExtendThreshold;

	private static int rgbFrame = 0;

	private static float rgbTimer = 0f;

	private const float RGB_UPDATE_INTERVAL = 0.05f;

	private static float _lastLobbyUpdateTime;

	private const float LOBBY_UPDATE_INTERVAL = 0.25f;

	private static readonly StringBuilder _sbCache = new StringBuilder(256);

	private static readonly StringBuilder _sbSettings = new StringBuilder(128);

	private static GameStartManager _cachedGameStartManager;

	private static string _cachedLobbyCode;

	private static int _lastGameId;

	private static readonly Dictionary<Lang, Dictionary<string, string>> UI_STRINGS = new Dictionary<Lang, Dictionary<string, string>>
	{
		{
			Lang.PTBR,
			new Dictionary<string, string>
			{
				{ "Players", "Jogadores" },
				{ "Time", "Tempo" },
				{ "Lobby", "LOBBY" },
				{ "Impostors", "Imps" },
				{ "KillCD", "Kill CD" },
				{ "Speed", "Veloc." },
				{ "Map", "Mapa" },
				{ "Roles", "Roles Ativos" },
				{ "MODDED", "MOD" },
				{ "Reason", "Motivo" },
				{ "Signature", "Assinatura de Mod" },
				{ "CustomMode", "Modo de Jogo Custom" },
				{ "InvalidRules", "Regras InvÃ¡lidas" }
			}
		},
		{
			Lang.EN,
			new Dictionary<string, string>
			{
				{ "Players", "Players" },
				{ "Time", "Time" },
				{ "Lobby", "LOBBY" },
				{ "Impostors", "Imps" },
				{ "KillCD", "Kill CD" },
				{ "Speed", "Speed" },
				{ "Map", "Map" },
				{ "Roles", "Roles Active" },
				{ "MODDED", "MOD" },
				{ "Reason", "Reason" },
				{ "Signature", "Mod Signature" },
				{ "CustomMode", "Custom Game Mode" },
				{ "InvalidRules", "Invalid Rules" }
			}
		}
	};

	public static float LobbyTimer { get; private set; } = 600f;

	public static bool JoinedAsHost { get; private set; } = false;

	public static int PlayerCount { get; private set; } = 0;

	public static int MaxPlayers { get; private set; } = 15;

	public static Lang CurrentLang { get; private set; } = Lang.PTBR;

	private static string UI(string key)
	{
		if (UI_STRINGS.TryGetValue(CurrentLang, out var value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		if (UI_STRINGS[Lang.EN].TryGetValue(key, out var value3))
		{
			return value3;
		}
		return key;
	}

	public static void InitializeConfig(ConfigFile config)
	{
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ModTooltipHandler>();
		}
		catch
		{
		}
		if (config == null)
		{
			Debug.LogError(InteropFix.Cast("[LobbyPatches] ConfigFile is null!"));
			return;
		}
		Config = config;
		cfgLanguage = Config.Bind<string>("1. General", "Language", "PTBR", "Language for UI. Options: PTBR, EN, ES");
		cfgEnableDebugLogging = Config.Bind<bool>("1. General", "Enable Debug Logging", false, "Enable detailed logs for troubleshooting.");
		cfgDisableLobbyMusic = Config.Bind<bool>("1. General", "Disable Lobby Music", false, "Disable the music in the lobby.");
		cfgShowLobbyInfo = Config.Bind<bool>("2. Game List Info", "Show Extra Info Panel", true, "Show the entire extra information panel in the game list.");
		cfgShowGameSettings = Config.Bind<bool>("2. Game List Info", "Show Game Settings", true, "Show game settings like speed, kill cooldown, etc.");
		cfgShowRolesInfo = Config.Bind<bool>("2. Game List Info", "Show Roles Info", true, "Show if the lobby has special roles enabled.");
		cfgShowPlayerList = Config.Bind<bool>("2. Game List Info", "Show Player List", false, "Show a partial list of players in the lobby (can cause clutter).");
		cfgPlayerListLimit = Config.Bind<int>("2. Game List Info", "Player List Limit", 3, "How many player names to show in the list if enabled.");
		cfgStreamerMode = Config.Bind<bool>("3. In-Lobby Display", "Streamer Mode", false, "Enable features for streamers.");
		cfgHideCode = Config.Bind<bool>("3. In-Lobby Display", "Hide Code (Streamer Mode)", true, "Hides the lobby code when Streamer Mode is active.");
		cfgCustomCode = Config.Bind<string>("3. In-Lobby Display", "Hidden Code Text", "SECRET", "Text to display instead of the lobby code.");
		cfgRgbLobbyCode = Config.Bind<bool>("3. In-Lobby Display", "RGB Lobby Code", true, "Enable a rainbow RGB effect for the lobby code text.");
		cfgShowLobbyTimer = Config.Bind<bool>("3. In-Lobby Display", "Show Lobby Countdown", true, "Shows a countdown timer for when the lobby might expire.");
		cfgAutoExtendTimer = Config.Bind<bool>("4. Host Features", "Auto-Extend Timer", true, "Automatically extend lobby timer when it's low (host only).");
		cfgAutoExtendThreshold = Config.Bind<int>("4. Host Features", "Auto-Extend Threshold (s)", 90, "Time remaining in seconds to trigger the auto-extend.");
		if (System.Enum.TryParse<Lang>(cfgLanguage.Value, ignoreCase: true, out var result))
		{
			CurrentLang = result;
		}
		DebugLog("Configuration Initialized and Loaded.");
	}

	private static bool IsStreamerModeEnabled()
	{
		return cfgStreamerMode?.Value ?? false;
	}

	private static bool ShouldHideCode()
	{
		if (IsStreamerModeEnabled())
		{
			return cfgHideCode?.Value ?? false;
		}
		return false;
	}

	private static string Colorize(string text, string color)
	{
		return $"<color={color}>{text}</color>";
	}

	private static string FormatPlatform(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		switch ((int)platform - 1)
		{
		case 1:
			return "Steam";
		case 0:
			return "Epic";
		case 3:
			return "MS Store";
		case 5:
		case 6:
			return "Mobile";
		case 7:
			return "Switch";
		case 8:
			return "Xbox";
		case 9:
			return "PlayStation";
		default:
			return "PC";
		}
	}

	private static bool IsVanillaGameMode(GameModes mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		if ((int)mode != 1 && (int)mode != 2 && (int)mode != 3)
		{
			return (int)mode == 4;
		}
		return true;
	}

	private static bool IsVanillaRulesPreset(RulesPresets preset)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)preset != 0 && (int)preset != 100)
		{
			return (int)preset <= 10;
		}
		return true;
	}

	private static bool IsHideAndSeekMode(GameModes mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		if ((int)mode != 2)
		{
			return (int)mode == 4;
		}
		return true;
	}

	private static (bool IsModded, string Reason) DetectIfModded(GameListing listing)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		string[] kNOWN_MOD_SIGNATURES = KNOWN_MOD_SIGNATURES;
		foreach (string value in kNOWN_MOD_SIGNATURES)
		{
			if (listing.HostName.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return (IsModded: true, Reason: UI("Signature"));
			}
		}
		IGameOptions options = listing.Options;
		if (options == null)
		{
			return (IsModded: false, Reason: "");
		}
		if (!IsVanillaGameMode(options.GameMode) || !IsVanillaRulesPreset(options.RulesPreset))
		{
			return (IsModded: true, Reason: UI("CustomMode"));
		}
		if (listing.MaxPlayers > 15)
		{
			return (IsModded: true, Reason: UI("InvalidRules"));
		}
		if (!IsHideAndSeekMode(options.GameMode))
		{
			if (options.NumImpostors > 3)
			{
				return (IsModded: true, Reason: UI("InvalidRules"));
			}
			try
			{
				float num = options.GetFloat((FloatOptionNames)2);
				float num2 = options.GetFloat((FloatOptionNames)1);
				if (num > 3f || num2 < 10f)
				{
					return (IsModded: true, Reason: UI("InvalidRules"));
				}
			}
			catch
			{
			}
		}
		return (IsModded: false, Reason: "");
	}

	private static string FormatGameSettings(IGameOptions options)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected I4, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		ConfigEntry<bool> obj = cfgShowGameSettings;
		if (obj == null || !obj.Value || options == null)
		{
			return "";
		}
		_sbSettings.Clear();
		_sbSettings.Append("\n").Append("<size=30%>");
		GameModes gameMode = options.GameMode;
		string text;
		switch ((int)gameMode - 2)
		{
		case 0:
		case 2:
			text = "HnS";
			break;
		case 1:
			text = "Classic \ud83c\udf83";
			break;
		default:
			text = "Classic";
			break;
		}
		_sbSettings.Append(Colorize(text, "#D980FF")).Append(" | ");
		if (IsHideAndSeekMode(options.GameMode))
		{
			_sbSettings.Append(UI("Impostors")).Append(": ").Append(Colorize("1", "#FFFFFF"));
		}
		else
		{
			_sbSettings.Append(UI("Impostors")).Append(": ").Append(Colorize(options.NumImpostors.ToString(), "#FFFFFF"))
				.Append(" | ");
			try
			{
				float value = options.GetFloat((FloatOptionNames)1);
				_sbSettings.Append(UI("KillCD")).Append(": ").Append(Colorize($"{value:F0}s", "#FFFFFF"))
					.Append(" | ");
				float value2 = options.GetFloat((FloatOptionNames)2);
				_sbSettings.Append(UI("Speed")).Append(": ").Append(Colorize($"{value2:F1}x", "#FFFFFF"));
			}
			catch
			{
			}
		}
		_sbSettings.Append("</size>");
		return _sbSettings.ToString();
	}

	private static string FormatRolesInfo(IGameOptions options)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		ConfigEntry<bool> obj = cfgShowRolesInfo;
		if (obj == null || !obj.Value || options == null)
		{
			return "";
		}
		if ((int)options.RulesPreset == 1)
		{
			return "\n<size=30%>" + Colorize("â€¢ " + UI("Roles"), "#a7f") + "</size>";
		}
		return "";
	}

	[HarmonyPatch(typeof(GameContainer), "SetupGameInfo")]
	[HarmonyPostfix]
	public static void OnSetupGameInfo(GameContainer __instance)
	{
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		ConfigEntry<bool> obj = cfgShowLobbyInfo;
		if (obj == null || !obj.Value || __instance.gameListing == null || (Object)(object)__instance.capacity == (Object)null)
		{
			return;
		}
		try
		{
			GameListing gameListing = __instance.gameListing;
			var (flag, text) = DetectIfModded(gameListing);
			_sbCache.Clear();
			_sbCache.Append("<size=37%>");
			_sbCache.Append(Colorize(gameListing.TrueHostName ?? "Lobby", "#FFFFFF"));
			if (flag)
			{
				string value = UI("Reason") + ": " + text;
				string value2 = $"<link=\"{value}\">{Colorize("[" + UI("MODDED") + "]", "#FF8080")}</link>";
				_sbCache.Append(" ").Append(value2);
			}
			_sbCache.Append("\n");
			string text2 = $"{gameListing.PlayerCount}/{gameListing.MaxPlayers}";
			string color = ((gameListing.PlayerCount < 4) ? "#FF8080" : ((gameListing.PlayerCount < 10) ? "#FFFF80" : "#80FF80"));
			_sbCache.Append(Colorize(text2, color)).Append("   ");
			string text3 = ((!ShouldHideCode()) ? GameCode.IntToGameName(gameListing.GameId) : (cfgCustomCode?.Value ?? "SECRET"));
			_sbCache.Append(Colorize(text3, "#FFBF80")).Append("\n");
			StringBuilder sbCache = _sbCache;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, sbCache);
			handler.AppendFormatted(UI("Map"));
			handler.AppendLiteral(": ");
			handler.AppendFormatted(Colorize(((Il2CppArrayBase<string>)(object)Constants.MapNames)[(int)gameListing.MapId], "#FFFFFF"));
			handler.AppendLiteral(" | ");
			sbCache.Append(handler);
			_sbCache.Append(Colorize(FormatPlatform(gameListing.Platform), "#80FFFF"));
			_sbCache.Append("</size>");
			_sbCache.Append(FormatGameSettings(gameListing.Options));
			_sbCache.Append(FormatRolesInfo(gameListing.Options));
			((TMP_Text)__instance.capacity).text = _sbCache.ToString();
			((TMP_Text)__instance.capacity).richText = true;
			(((Component)__instance).gameObject.GetComponent<ModTooltipHandler>() ?? ((Component)__instance).gameObject.AddComponent<ModTooltipHandler>()).Setup(__instance.capacity);
		}
		catch (Exception value3)
		{
			DebugLog($"Error in OnSetupGameInfo: {value3}", LogLevel.Error);
			if ((Object)(object)((__instance != null) ? __instance.capacity : null) != (Object)null && __instance.gameListing != null)
			{
				try
				{
					((TMP_Text)__instance.capacity).text = $"{__instance.gameListing.PlayerCount}/{__instance.gameListing.MaxPlayers}";
					return;
				}
				catch
				{
					return;
				}
			}
		}
	}

	[HarmonyPatch(typeof(GameStartManager), "Update")]
	[HarmonyPostfix]
	public static void OnGameStartManagerUpdate(GameStartManager __instance)
	{
		if ((Object)(object)((__instance != null) ? __instance.GameRoomNameCode : null) == (Object)null)
		{
			return;
		}
		_cachedGameStartManager = __instance;
		try
		{
			AmongUsClient instance = AmongUsClient.Instance;
			int num = ((instance != null) ? ((InnerNetClient)instance).GameId : 0);
			if (num != _lastGameId || string.IsNullOrEmpty(_cachedLobbyCode))
			{
				_cachedLobbyCode = GameCode.IntToGameName(num);
				_lastGameId = num;
			}
			string text = ((!ShouldHideCode()) ? _cachedLobbyCode : (cfgCustomCode?.Value ?? "SECRET"));
			ConfigEntry<bool> obj = cfgRgbLobbyCode;
			string text2 = ((obj != null && obj.Value) ? GetRGBText(text) : text);
			string text3 = "";
			ConfigEntry<bool> obj2 = cfgShowLobbyTimer;
			if (obj2 != null && obj2.Value && JoinedAsHost)
			{
				int num2 = (int)LobbyTimer;
				string color = ((num2 <= 60) ? "#FF8080" : ((num2 <= 180) ? "#FFFF80" : "#80FF80"));
				text3 = " " + Colorize($"({num2 / 60}:{num2 % 60:D2})", color);
			}
			((TMP_Text)__instance.GameRoomNameCode).text = text2 + text3;
			((TMP_Text)__instance.GameRoomNameCode).richText = true;
			if ((Object)(object)__instance.PlayerCounter != (Object)null && PlayerControl.AllPlayerControls != null)
			{
				PlayerCount = PlayerControl.AllPlayerControls.Count;
			}
		}
		catch (Exception value)
		{
			DebugLog($"Error in OnGameStartManagerUpdate: {value}", LogLevel.Error);
		}
	}

	private static string GetRGBText(string text)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		rgbTimer += Time.deltaTime;
		if (rgbTimer > 0.05f)
		{
			rgbFrame = (rgbFrame + 4) % 360;
			rgbTimer = 0f;
		}
		Color val = Color.HSVToRGB((float)rgbFrame / 360f, 1f, 1f);
		return $"<color=#{ColorUtility.ToHtmlStringRGB(val)}>{text}</color>";
	}

	[HarmonyPatch(typeof(LobbyBehaviour), "Start")]
	[HarmonyPostfix]
	public static void OnLobbyStart()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		LobbyTimer = 600f;
		AmongUsClient instance = AmongUsClient.Instance;
		JoinedAsHost = instance != null && ((InnerNetClient)instance).AmHost;
		PlayerCount = PlayerControl.AllPlayerControls?.Count ?? 0;
		_cachedLobbyCode = null;
		_lastGameId = 0;
		try
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			if ((Object)(object)localPlayer != (Object)null)
			{
				if ((Object)(object)localPlayer.Collider != (Object)null && !((Behaviour)localPlayer.Collider).enabled)
				{
					((Behaviour)localPlayer.Collider).enabled = true;
				}
				localPlayer.moveable = true;
				if ((Object)(object)localPlayer.MyPhysics != (Object)null && (Object)(object)localPlayer.MyPhysics.body != (Object)null)
				{
					localPlayer.MyPhysics.body.velocity = Vector2.zero;
				}
			}
		}
		catch
		{
		}
		GameCheats.ClearEndGameState();
		try
		{
			if ((Object)(object)Camera.main != (Object)null && Camera.main.orthographicSize > 4f)
			{
				Camera.main.orthographicSize = 3f;
			}
		}
		catch
		{
		}
		try
		{
			PlayerPickMenu.TriggerRealtimeUpdate(force: true);
		}
		catch
		{
		}
		DebugLog($"Lobby started. Host: {JoinedAsHost}, Players: {PlayerCount}");
	}

	[HarmonyPatch(typeof(LobbyBehaviour), "Update")]
	[HarmonyPostfix]
	public static void OnLobbyUpdate(LobbyBehaviour __instance)
	{
		if (Time.time - _lastLobbyUpdateTime < 0.25f)
		{
			LobbyTimer -= Time.deltaTime;
			return;
		}
		_lastLobbyUpdateTime = Time.time;
		ConfigEntry<bool> obj = cfgDisableLobbyMusic;
		if (obj != null && obj.Value && (Object)(object)SoundManager.Instance != (Object)null && (Object)(object)__instance.MapTheme != (Object)null)
		{
			SoundManager.Instance.StopSound(__instance.MapTheme);
		}
		LobbyTimer -= Time.deltaTime;
		ConfigEntry<bool> obj2 = cfgAutoExtendTimer;
		if (obj2 != null && obj2.Value && JoinedAsHost && LobbyTimer <= (float)(cfgAutoExtendThreshold?.Value ?? 90))
		{
			__instance.RpcExtendLobbyTimer();
			LobbyTimer = 600f;
			DebugLog("Auto-extended lobby timer.");
		}
	}

	[HarmonyPatch(typeof(AmongUsClient), "OnPlayerJoined")]
	[HarmonyPostfix]
	public static void OnPlayerJoinedPatch(ClientData data)
	{
		if ((Object)(object)((data != null) ? data.Character : null) != (Object)null)
		{
			PlayerCount++;
			try
			{
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
			catch
			{
			}
			DebugLog($"Player joined: {data.PlayerName}. Total: {PlayerCount}");
		}
	}

	[HarmonyPatch(typeof(AmongUsClient), "OnPlayerLeft")]
	[HarmonyPostfix]
	public static void OnPlayerLeftPatch(ClientData data, DisconnectReasons reason)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (data != null)
		{
			PlayerCount = Math.Max(0, PlayerCount - 1);
			try
			{
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
			catch
			{
			}
			DebugLog($"Player left: {data.PlayerName} ({reason}). Total: {PlayerCount}");
		}
	}

	private static void DebugLog(string message, LogLevel level = LogLevel.Info)
	{
		ConfigEntry<bool> obj = cfgEnableDebugLogging;
		if (obj != null && obj.Value)
		{
			string text = "[LobbyPatches] " + message;
			switch (level)
			{
			case LogLevel.Warning:
				Debug.LogWarning(InteropFix.Cast(text));
				break;
			case LogLevel.Error:
				Debug.LogError(InteropFix.Cast(text));
				break;
			default:
				Debug.Log(InteropFix.Cast(text));
				break;
			}
		}
	}
}




