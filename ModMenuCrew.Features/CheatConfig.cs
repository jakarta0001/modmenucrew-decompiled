using System;
using AmongUs.GameOptions;
using BepInEx.Configuration;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class CheatConfig
{
	public static ConfigFile Config { get; private set; }

	public static ConfigEntry<KeyCode> MenuToggleKey { get; private set; }

	public static ConfigEntry<bool> OfflineLocalMode { get; private set; }

	public static ConfigEntry<float> MenuOpacity { get; private set; }

	public static ConfigEntry<float> MenuWidth { get; private set; }

	public static ConfigEntry<float> MenuHeight { get; private set; }

	public static ConfigEntry<bool> SeeGhosts { get; private set; }

	public static ConfigEntry<bool> SeeDeadChat { get; private set; }

	public static ConfigEntry<bool> FreeCamEnabled { get; private set; }

	public static ConfigEntry<bool> NoClipSmoothEnabled { get; private set; }

	public static ConfigEntry<bool> CustomSpeedEnabled { get; private set; }

	public static ConfigEntry<bool> InfiniteVision { get; private set; }

	public static float VisionMultiplier { get; internal set; } = 3f;

	public static ConfigEntry<bool> RadarEnabled { get; private set; }

	public static ConfigEntry<bool> TracersEnabled { get; private set; }

	public static ConfigEntry<float> RadarScale { get; private set; }

	public static ConfigEntry<bool> RadarShowMapImage { get; private set; }

	public static ConfigEntry<float> RadarMapZoom { get; private set; }

	public static ConfigEntry<bool> AllowVenting { get; private set; }

	public static ConfigEntry<bool> TeleportWithCursor { get; private set; }

	public static ConfigEntry<bool> NoKillCooldown { get; private set; }

	public static ConfigEntry<bool> InstantWin { get; private set; }

	public static ConfigEntry<bool> DisableGameEnd { get; private set; }

	public static ConfigEntry<bool> ForceImpostor { get; private set; }

	public static ConfigEntry<bool> EndlessVentTime { get; private set; }

	public static ConfigEntry<bool> NoVentCooldown { get; private set; }

	public static ConfigEntry<bool> EndlessShapeshiftDuration { get; private set; }

	public static ConfigEntry<bool> NoShapeshiftCooldown { get; private set; }

	public static ConfigEntry<bool> EndlessBattery { get; private set; }

	public static ConfigEntry<bool> NoVitalsCooldown { get; private set; }

	public static ConfigEntry<bool> EndlessTracking { get; private set; }

	public static ConfigEntry<bool> NoTrackingCooldown { get; private set; }

	public static ConfigEntry<bool> ShowKillCooldowns { get; private set; }

	public static ConfigEntry<bool> NoSabotageCooldown { get; private set; }

	public static ConfigEntry<bool> CrewmateSabotage { get; private set; }

	public static ConfigEntry<bool> MultiSabotage { get; private set; }

	public static ConfigEntry<bool> GodMapEnabled { get; private set; }

	public static ConfigEntry<bool> EventLoggerEnabled { get; private set; }

	public static ConfigEntry<bool> KillAlertsEnabled { get; private set; }

	public static ConfigEntry<bool> NoShadows { get; private set; }

	public static ConfigEntry<bool> Nf4 { get; private set; }

	public static ConfigEntry<bool> PhantomMode { get; private set; }

	public static ConfigEntry<bool> HideMMCStar { get; private set; }

	public static ConfigEntry<bool> GodMode { get; private set; }

	public static ConfigEntry<bool> ShowPlayerInfo { get; private set; }

	public static ConfigEntry<KeyCode> KeybindRadar { get; private set; }

	public static ConfigEntry<KeyCode> KeybindFreeCam { get; private set; }

	public static ConfigEntry<KeyCode> KeybindNoClip { get; private set; }

	public static ConfigEntry<KeyCode> KeybindTracers { get; private set; }

	public static ConfigEntry<KeyCode> KeybindSeeGhosts { get; private set; }

	public static ConfigEntry<KeyCode> KeybindNoKillCooldown { get; private set; }

	public static void Initialize(ConfigFile config)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Expected O, but got Unknown
		Config = config;
		MenuToggleKey = config.Bind<KeyCode>("1. Menu", "Menu Toggle Key", (KeyCode)282, "Key to toggle the mod menu (default: F1). You can change to any key like F2, Insert, Delete, etc.");
		OfflineLocalMode = config.Bind<bool>("1. Menu", "Offline Local Mode", true, "Run menu locally without server UI payload/realtime. Uses built-in local tabs only.");
		MenuOpacity = config.Bind<float>("1. Menu", "Menu Opacity", 0.95f, new ConfigDescription("Menu transparency (1.0 = opaque, 0.3 = very transparent)", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0.3f, 1f), System.Array.Empty<object>()));
		MenuWidth = config.Bind<float>("1. Menu", "Menu Width", 500f, new ConfigDescription("Menu window width in pixels (use when resizer doesn't work in fullscreen)", (AcceptableValueBase)(object)new AcceptableValueRange<float>(400f, 1200f), System.Array.Empty<object>()));
		MenuHeight = config.Bind<float>("1. Menu", "Menu Height", 600f, new ConfigDescription("Menu window height in pixels (use when resizer doesn't work in fullscreen)", (AcceptableValueBase)(object)new AcceptableValueRange<float>(400f, 1000f), System.Array.Empty<object>()));
		SeeGhosts = config.Bind<bool>("8. Vision", "See Ghosts", false, "See ghosts while alive");
		SeeDeadChat = config.Bind<bool>("8. Vision", "See Dead Chat", false, "Read dead player chat while alive");
		FreeCamEnabled = config.Bind<bool>("8. Vision", "FreeCam", false, "Free camera mode (WASD + Q/E)");
		NoClipSmoothEnabled = config.Bind<bool>("8. Vision", "NoClip", false, "Walk through walls");
		CustomSpeedEnabled = config.Bind<bool>("8. Vision", "Custom Speed", false, "Custom player speed");
		InfiniteVision = config.Bind<bool>("8. Vision", "Infinite Vision", false, "See entire map");
		RadarEnabled = config.Bind<bool>("9. Radar", "Radar Enabled", false, "Show player positions on radar");
		TracersEnabled = config.Bind<bool>("9. Radar", "Tracers", false, "Lines showing player positions");
		RadarScale = config.Bind<float>("9. Radar", "Radar Scale", 0.08f, new ConfigDescription("Radar zoom level", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0.03f, 0.15f), System.Array.Empty<object>()));
		RadarShowMapImage = config.Bind<bool>("9. Radar", "Show Map Image", true, "Show actual map image as radar background (Skeld only)");
		RadarMapZoom = config.Bind<float>("9. Radar", "Map Zoom", 1f, new ConfigDescription("Zoom level for map image (scroll mouse on radar)", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0.5f, 3f), System.Array.Empty<object>()));
		AllowVenting = config.Bind<bool>("10. Gameplay", "Allow Venting", false, "All roles can use vents");
		TeleportWithCursor = config.Bind<bool>("10. Gameplay", "Teleport With Cursor", false, "Click to teleport");
		NoKillCooldown = config.Bind<bool>("10. Gameplay", "No Kill Cooldown", false, "Kill without cooldown");
		InstantWin = config.Bind<bool>("10. Gameplay", "Instant Win", false, "Force instant win");
		DisableGameEnd = config.Bind<bool>("10. Gameplay", "Disable Game End", false, "Prevent game from ending");
		ForceImpostor = config.Bind<bool>("10. Gameplay", "Force Impostor", false, "Always be impostor");
		if (ForceImpostor.Value)
		{
			ImpostorForcer.SetRoleOverrideEnabled(enabled: true);
			ImpostorForcer.SetSelectedRoleForHost((RoleTypes)1);
		}
		EndlessVentTime = config.Bind<bool>("11. Role Engineer", "Endless Vent Time", false, "Unlimited time in vents");
		NoVentCooldown = config.Bind<bool>("11. Role Engineer", "No Vent Cooldown", false, "No vent cooldown");
		EndlessShapeshiftDuration = config.Bind<bool>("12. Role Shapeshifter", "Endless Duration", false, "Unlimited shapeshift duration");
		NoShapeshiftCooldown = config.Bind<bool>("12. Role Shapeshifter", "No Cooldown", false, "No shapeshift cooldown");
		EndlessBattery = config.Bind<bool>("13. Role Scientist", "Endless Battery", false, "Unlimited vitals battery");
		NoVitalsCooldown = config.Bind<bool>("13. Role Scientist", "No Vitals Cooldown", false, "No vitals cooldown");
		EndlessTracking = config.Bind<bool>("14. Role Tracker", "Endless Tracking", false, "Unlimited tracking duration");
		NoTrackingCooldown = config.Bind<bool>("14. Role Tracker", "No Tracking Cooldown", false, "No tracking cooldown");
		ShowKillCooldowns = config.Bind<bool>("15. New Features", "Show Kill Cooldowns", false, "Show cooldown timer above impostors");
		NoSabotageCooldown = config.Bind<bool>("15. New Features", "No Sabotage Cooldown", false, "Bypass sabotage cooldown");
		CrewmateSabotage = config.Bind<bool>("15. New Features", "Crewmate Sabotage", false, "Allow crewmates to open sabotage map");
		MultiSabotage = config.Bind<bool>("15. New Features", "Multi Sabotage", false, "Allow multiple sabotages at once");
		GodMapEnabled = config.Bind<bool>("15. New Features", "God Map", false, "See all players on map");
		EventLoggerEnabled = config.Bind<bool>("15. New Features", "Event Logger", false, "Log game events (kills, tasks, etc)");
		KillAlertsEnabled = config.Bind<bool>("15. New Features", "Kill Alerts", false, "Alert when impostor kills someone");
		NoShadows = config.Bind<bool>("15. New Features", "No Shadows", false, "Remove fog and shadows for better visibility");
		Nf4 = config.Bind<bool>("15. New Features", "Network Filter", true, "Advanced network packet validation");
		PhantomMode = config.Bind<bool>("15. New Features", "Phantom Mode", false, "Infinite vanish + kill while invisible (requires Phantom role)");
		HideMMCStar = config.Bind<bool>("15. New Features", "Hide MMC Star", false, "Hide the star (â˜…) prefix on MMC user names");
		GodMode = config.Bind<bool>("16. Host Only", "God Mode", false, "Makes the host immortal - automatically reapplies protection (HOST ONLY)");
		ShowPlayerInfo = config.Bind<bool>("7. Lobby", "Show Player Info", false, "Show platform, level and ID next to each player in the player list");
		KeybindRadar = config.Bind<KeyCode>("17. Keybinds", "Radar Keybind", (KeyCode)0, "Hotkey to toggle Radar (set to None to disable)");
		KeybindFreeCam = config.Bind<KeyCode>("17. Keybinds", "FreeCam Keybind", (KeyCode)0, "Hotkey to toggle FreeCam (set to None to disable)");
		KeybindNoClip = config.Bind<KeyCode>("17. Keybinds", "NoClip Keybind", (KeyCode)0, "Hotkey to toggle NoClip (set to None to disable)");
		KeybindTracers = config.Bind<KeyCode>("17. Keybinds", "Tracers Keybind", (KeyCode)0, "Hotkey to toggle Tracers (set to None to disable)");
		KeybindSeeGhosts = config.Bind<KeyCode>("17. Keybinds", "See Ghosts Keybind", (KeyCode)0, "Hotkey to toggle See Ghosts (set to None to disable)");
		KeybindNoKillCooldown = config.Bind<KeyCode>("17. Keybinds", "No Kill CD Keybind", (KeyCode)0, "Hotkey to toggle No Kill Cooldown (set to None to disable)");
	}

	public static void Save()
	{
		ConfigFile config = Config;
		if (config != null)
		{
			config.Save();
		}
	}

	public static void Reload()
	{
		ConfigFile config = Config;
		if (config != null)
		{
			config.Reload();
		}
	}

	public static void ResetToDefaults()
	{
		if (Config == null)
		{
			return;
		}
		foreach (ConfigDefinition key in Config.Keys)
		{
			Config[key].BoxedValue = Config[key].DefaultValue;
		}
		VisionMultiplier = 3f;
		Config.Save();
	}
}

