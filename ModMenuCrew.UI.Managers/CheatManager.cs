using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppSystem;
using InnerNet;
using ModMenuCrew.Features;
using ModMenuCrew.UI.Menus;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew.UI.Managers;

public class CheatManager
{
	[System.Serializable]
	internal class CheatsStateDto
	{
		public bool godMode { get; set; }

		public bool scannerBypass { get; set; }

		public float scannerTimeLeft { get; set; }

		public bool allowVenting { get; set; }

		public bool teleportWithCursor { get; set; }

		public bool freeCam { get; set; }

		public bool noClipSmooth { get; set; }

		public bool radarEnabled { get; set; }

		public float radarScale { get; set; }

		public bool tracersEnabled { get; set; }

		public bool seeGhosts { get; set; }

		public bool seeDeadChat { get; set; }

		public bool showKillCooldowns { get; set; }

		public bool noShadows { get; set; }

		public float visionMultiplier { get; set; }

		public bool killAlerts { get; set; }

		public bool eventLogger { get; set; }

		public bool noSabotageCooldown { get; set; }

		public bool endlessVentTime { get; set; }

		public bool noVentCooldown { get; set; }

		public bool endlessShapeshiftDuration { get; set; }

		public bool noShapeshiftCooldown { get; set; }

		public bool endlessBattery { get; set; }

		public bool noVitalsCooldown { get; set; }

		public bool endlessTracking { get; set; }

		public bool noTrackingCooldown { get; set; }

		public bool phantomMode { get; set; }

		public bool mer { get; set; }

		public float merTL { get; set; }

		public object evTgts { get; set; }

		public bool revealSus { get; set; }
	}

	[System.Serializable]
	internal class CheatsUiStateDto
	{
		public bool showQuickActions { get; set; } = true;

		public bool showMovement { get; set; } = true;

		public bool showESP { get; set; } = true;

		public bool showRoleBuffs { get; set; } = true;

		public bool showProtectPlayer { get; set; }

		public int selectedProtectPlayer { get; set; }

		public float protectionDuration { get; set; }

		public bool hasProtectedPlayer { get; set; }

		public int forceVotesTarget { get; set; }

		public int forceVotesRemaining { get; set; }

		public string forceVotesTargetName { get; set; } = "Skip";
	}

	private ServerData.UISnapshot _safeSnapshot;

	private byte[] _cachedCHBytecode;

	private byte[] _cachedCHInverseMap;

	private long _cachedCHToken;

	private bool showGeneralCheats = true;

	private bool showRoleCheats = true;

	private bool showAdvancedCheats = true;

	private bool showAnimationCheats = true;

	private bool showNewFeatures = true;

	private bool previousAllowVenting;

	private bool previousSeeGhosts;

	private bool previousSeeDeadChat;

	private bool previousRadarEnabled;

	private float previousRadarScale = 0.08f;

	private float previousVisionMultiplier = 3f;

	private bool previousFreeCam;

	private bool previousTracers;

	private bool previousNoClipSmooth;

	private float _lastUpdateTime;

	private float _lastRevealSusTime;

	private float _lastAnimationTime;

	private float _lastKillCooldownTime;

	private const float UPDATE_INTERVAL = 0.1f;

	private static HudManager _hudCache;

	private static int _selectedPlayerIndex = 0;

	private static float _protectionDuration = 10f;

	private static bool _showProtectPlayerUI = false;

	private static bool _showQuickActions = true;

	private static bool _showMovement = true;

	private static bool _showESP = true;

	private static bool _showRoleBuffs = true;

	private static Dictionary<string, System.Action<long>> _actionRegistry;

	private static readonly CheatsStateDto _cheatsStateDto = new CheatsStateDto();

	private static readonly CheatsUiStateDto _cheatsUiDto = new CheatsUiStateDto();

	public static CheatManager Instance { get; private set; }

	public bool NoShapeshiftCooldown
	{
		get
		{
			return CheatConfig.NoShapeshiftCooldown?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoShapeshiftCooldown != null)
			{
				CheatConfig.NoShapeshiftCooldown.Value = value;
			}
		}
	}

	public bool EndlessVentTime
	{
		get
		{
			return CheatConfig.EndlessVentTime?.Value ?? false;
		}
		set
		{
			if (CheatConfig.EndlessVentTime != null)
			{
				CheatConfig.EndlessVentTime.Value = value;
			}
		}
	}

	public bool NoVentCooldown
	{
		get
		{
			return CheatConfig.NoVentCooldown?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoVentCooldown != null)
			{
				CheatConfig.NoVentCooldown.Value = value;
			}
		}
	}

	public bool EndlessShapeshiftDuration
	{
		get
		{
			return CheatConfig.EndlessShapeshiftDuration?.Value ?? false;
		}
		set
		{
			if (CheatConfig.EndlessShapeshiftDuration != null)
			{
				CheatConfig.EndlessShapeshiftDuration.Value = value;
			}
		}
	}

	public bool NoVitalsCooldown
	{
		get
		{
			return CheatConfig.NoVitalsCooldown?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoVitalsCooldown != null)
			{
				CheatConfig.NoVitalsCooldown.Value = value;
			}
		}
	}

	public bool EndlessBattery
	{
		get
		{
			return CheatConfig.EndlessBattery?.Value ?? false;
		}
		set
		{
			if (CheatConfig.EndlessBattery != null)
			{
				CheatConfig.EndlessBattery.Value = value;
			}
		}
	}

	public bool NoTrackingCooldown
	{
		get
		{
			return CheatConfig.NoTrackingCooldown?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoTrackingCooldown != null)
			{
				CheatConfig.NoTrackingCooldown.Value = value;
			}
		}
	}

	public bool EndlessTracking
	{
		get
		{
			return CheatConfig.EndlessTracking?.Value ?? false;
		}
		set
		{
			if (CheatConfig.EndlessTracking != null)
			{
				CheatConfig.EndlessTracking.Value = value;
			}
		}
	}

	public bool AllowVenting
	{
		get
		{
			return CheatConfig.AllowVenting?.Value ?? false;
		}
		set
		{
			if (CheatConfig.AllowVenting != null)
			{
				CheatConfig.AllowVenting.Value = value;
			}
		}
	}

	public bool TeleportWithCursor
	{
		get
		{
			return CheatConfig.TeleportWithCursor?.Value ?? false;
		}
		set
		{
			if (CheatConfig.TeleportWithCursor != null)
			{
				CheatConfig.TeleportWithCursor.Value = value;
			}
		}
	}

	public bool SeeGhosts
	{
		get
		{
			return CheatConfig.SeeGhosts?.Value ?? false;
		}
		set
		{
			if (CheatConfig.SeeGhosts != null)
			{
				CheatConfig.SeeGhosts.Value = value;
			}
		}
	}

	public bool SeeDeadChat
	{
		get
		{
			return CheatConfig.SeeDeadChat?.Value ?? false;
		}
		set
		{
			if (CheatConfig.SeeDeadChat != null)
			{
				CheatConfig.SeeDeadChat.Value = value;
			}
		}
	}

	public bool RadarEnabled
	{
		get
		{
			return CheatConfig.RadarEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.RadarEnabled != null)
			{
				CheatConfig.RadarEnabled.Value = value;
			}
		}
	}

	public float RadarScale
	{
		get
		{
			return CheatConfig.RadarScale?.Value ?? 0.08f;
		}
		set
		{
			if (CheatConfig.RadarScale != null)
			{
				CheatConfig.RadarScale.Value = value;
			}
		}
	}

	public bool FreeCamEnabled
	{
		get
		{
			return CheatConfig.FreeCamEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.FreeCamEnabled != null)
			{
				CheatConfig.FreeCamEnabled.Value = value;
			}
		}
	}

	public bool TracersEnabled
	{
		get
		{
			return CheatConfig.TracersEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.TracersEnabled != null)
			{
				CheatConfig.TracersEnabled.Value = value;
			}
		}
	}

	public bool NoClipSmoothEnabled
	{
		get
		{
			return CheatConfig.NoClipSmoothEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoClipSmoothEnabled != null)
			{
				CheatConfig.NoClipSmoothEnabled.Value = value;
			}
		}
	}

	public bool ShowKillCooldowns
	{
		get
		{
			return CheatConfig.ShowKillCooldowns?.Value ?? false;
		}
		set
		{
			if (CheatConfig.ShowKillCooldowns != null)
			{
				CheatConfig.ShowKillCooldowns.Value = value;
			}
		}
	}

	public bool NoSabotageCooldown
	{
		get
		{
			return CheatConfig.NoSabotageCooldown?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoSabotageCooldown != null)
			{
				CheatConfig.NoSabotageCooldown.Value = value;
			}
		}
	}

	public bool GodMapEnabled
	{
		get
		{
			return CheatConfig.GodMapEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.GodMapEnabled != null)
			{
				CheatConfig.GodMapEnabled.Value = value;
			}
		}
	}

	public bool EventLoggerEnabled
	{
		get
		{
			return CheatConfig.EventLoggerEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.EventLoggerEnabled != null)
			{
				CheatConfig.EventLoggerEnabled.Value = value;
			}
			EventLogger.ShowUI = value;
		}
	}

	public bool KillAlertsEnabled
	{
		get
		{
			return CheatConfig.KillAlertsEnabled?.Value ?? false;
		}
		set
		{
			if (CheatConfig.KillAlertsEnabled != null)
			{
				CheatConfig.KillAlertsEnabled.Value = value;
			}
		}
	}

	public bool NoShadowsEnabled
	{
		get
		{
			return CheatConfig.NoShadows?.Value ?? false;
		}
		set
		{
			if (CheatConfig.NoShadows != null)
			{
				CheatConfig.NoShadows.Value = value;
			}
		}
	}

	public bool GodModeEnabled
	{
		get
		{
			return CheatConfig.GodMode?.Value ?? false;
		}
		set
		{
			if (CheatConfig.GodMode != null)
			{
				CheatConfig.GodMode.Value = value;
			}
		}
	}

	public CheatManager()
	{
		Instance = this;
	}

	private static Dictionary<string, System.Action<long>> GetActionRegistry()
	{
		if (_actionRegistry == null)
		{
			_actionRegistry = new Dictionary<string, System.Action<long>>();
			RegisterActions(_actionRegistry);
		}
		return _actionRegistry;
	}

	internal static void RegisterActions(Dictionary<string, System.Action<long>> registry)
	{
		if (registry == null)
		{
			return;
		}
		_ = Instance;
		string[] array = new string[51]
		{
			"cheat_section_quick", "cheat_section_movement", "cheat_section_esp", "cheat_section_roles", "cheat_auto_tasks", "cheat_skip_meeting", "cheat_scanner_bypass", "cheat_reveal_sus", "cheat_god_mode", "cheat_kill_all",
			"cheat_kill_crew", "cheat_kill_imps", "cheat_protect_expand", "cheat_protect_prev", "cheat_protect_next", "cheat_protect_duration", "cheat_protect_apply", "cheat_protect_self", "cheat_protect_remove", "cheat_vote_prev",
			"cheat_vote_next", "cheat_vote_execute", "cheat_crew_vent", "cheat_click_tp", "cheat_drone_cam", "cheat_ghost_walk", "cheat_radar", "cheat_tracers", "cheat_radar_scale", "cheat_see_ghosts",
			"cheat_dead_chat", "cheat_kill_timer", "cheat_no_shadows", "cheat_vision", "cheat_vision_reset", "cheat_kill_alert", "cheat_event_log", "cheat_log_open", "cheat_log_clear", "cheat_infinite_sabo",
			"cheat_sabo_map", "cheat_endless_vent", "cheat_no_vent_cd", "cheat_endless_shift", "cheat_no_shift_cd", "cheat_endless_battery", "cheat_no_vitals_cd", "cheat_endless_track", "cheat_no_track_cd", "cheat_phantom_god",
			"cheat_mer"
		};
		for (int i = 0; i < array.Length; i++)
		{
			ServerData.RegisterControlId(array[i]);
		}
		if (Instance != null)
		{
			ServerData.SetSliderValueInternal("cheat_radar_scale", Instance.RadarScale);
			ServerData.SetSliderValueInternal("cheat_vision", CheatConfig.VisionMultiplier);
			ServerData.SetSliderValueInternal("cheat_protect_duration", _protectionDuration);
		}
		registry["cheat_section_quick"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_showQuickActions = !_showQuickActions;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_section_movement"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_showMovement = !_showMovement;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_section_esp"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_showESP = !_showESP;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_section_roles"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_showRoleBuffs = !_showRoleBuffs;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_auto_tasks"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null)
				{
					MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, GameCheats.CompleteAllTasksWithDelay(0.2f));
				}
				else
				{
					GameCheats.CompleteAllTasks();
				}
			}
		};
		registry["cheat_skip_meeting"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.CloseMeeting();
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_scanner_bypass"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.SetScannerBypass(!GameCheats.ScannerBypassEnabled);
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_reveal_sus"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.RevealImpostors();
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_god_mode"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					if (Instance != null)
					{
						Instance.GodModeEnabled = !Instance.GodModeEnabled;
					}
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_kill_all"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.KillAll();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_kill_crew"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.KillAll(crewOnly: true);
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_kill_imps"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.KillAll(crewOnly: false, impostorsOnly: true);
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_protect_expand"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_showProtectPlayerUI = !_showProtectPlayerUI;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_protect_prev"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				var alivePlayersForProtection = GameCheats.GetAlivePlayersForProtection();
				if (alivePlayersForProtection.Count > 0)
				{
					_selectedPlayerIndex = (_selectedPlayerIndex - 1 + alivePlayersForProtection.Count) % alivePlayersForProtection.Count;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_protect_next"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				var alivePlayersForProtection = GameCheats.GetAlivePlayersForProtection();
				if (alivePlayersForProtection.Count > 0)
				{
					_selectedPlayerIndex = (_selectedPlayerIndex + 1) % alivePlayersForProtection.Count;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_protect_duration"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_protectionDuration = ServerData.GetSliderValue("cheat_protect_duration");
				PlayerPickMenu.TriggerRealtimeUpdate();
			}
		};
		registry["cheat_protect_apply"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					var alivePlayersForProtection = GameCheats.GetAlivePlayersForProtection();
					if (alivePlayersForProtection.Count > 0 && _selectedPlayerIndex >= 0 && _selectedPlayerIndex < alivePlayersForProtection.Count)
					{
						GameCheats.ProtectPlayer(alivePlayersForProtection[_selectedPlayerIndex].Item1, _protectionDuration);
					}
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_protect_self"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.ProtectSelf(_protectionDuration);
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_protect_remove"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.RemoveProtection();
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_vote_prev"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.ForceVotesSelectPrevious();
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_vote_next"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.ForceVotesSelectNext();
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_vote_execute"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.ForceVotesExecute();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		};
		registry["cheat_crew_vent"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.AllowVenting = !Instance.AllowVenting;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_click_tp"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.TeleportWithCursor = !Instance.TeleportWithCursor;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_drone_cam"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.FreeCamEnabled = !Instance.FreeCamEnabled;
					GameCheats.ToggleFreeCam(Instance.FreeCamEnabled);
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_ghost_walk"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoClipSmoothEnabled = !Instance.NoClipSmoothEnabled;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_radar"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.RadarEnabled = !Instance.RadarEnabled;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_tracers"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.TracersEnabled = !Instance.TracersEnabled;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_radar_scale"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.RadarScale = ServerData.GetSliderValue("cheat_radar_scale");
				}
				PlayerPickMenu.TriggerRealtimeUpdate();
			}
		};
		registry["cheat_see_ghosts"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.SeeGhosts = !Instance.SeeGhosts;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_dead_chat"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.SeeDeadChat = !Instance.SeeDeadChat;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_kill_timer"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.ShowKillCooldowns = !Instance.ShowKillCooldowns;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_no_shadows"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoShadowsEnabled = !Instance.NoShadowsEnabled;
				}
				HudManager instance = DestroyableSingleton<HudManager>.Instance;
				if ((Object)(object)((instance != null) ? instance.ShadowQuad : null) != (Object)null)
				{
					((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(!(Instance?.NoShadowsEnabled ?? false));
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_vision"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				float sliderValue = ServerData.GetSliderValue("cheat_vision");
				Debug.Log(InteropFix.Cast($"[SAT-DBG] cheat_vision handler FIRED. SliderValue={sliderValue} OldVisionMult={CheatConfig.VisionMultiplier}"));
				if (sliderValue > 0.5f)
				{
					CheatConfig.VisionMultiplier = sliderValue;
					Debug.Log(InteropFix.Cast($"[SAT-DBG] VisionMultiplier SET to {CheatConfig.VisionMultiplier}"));
				}
				PlayerPickMenu.TriggerRealtimeUpdate();
			}
		};
		registry["cheat_vision_reset"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				CheatConfig.VisionMultiplier = 3f;
				ServerData.SetSliderValueInternal("cheat_vision", 3f);
				if ((Object)(object)Camera.main != (Object)null && Camera.main.orthographicSize > 4f)
				{
					Camera.main.orthographicSize = 3f;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_kill_alert"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.KillAlertsEnabled = !Instance.KillAlertsEnabled;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_event_log"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.EventLoggerEnabled = !Instance.EventLoggerEnabled;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_log_open"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.ToggleEventLoggerUI();
			}
		};
		registry["cheat_log_clear"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				EventLogger.Clear();
			}
		};
		registry["cheat_infinite_sabo"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoSabotageCooldown = !Instance.NoSabotageCooldown;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_sabo_map"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				GameCheats.OpenSabotageMap();
			}
		};
		registry["cheat_endless_vent"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.EndlessVentTime = !Instance.EndlessVentTime;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_no_vent_cd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoVentCooldown = !Instance.NoVentCooldown;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_endless_shift"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.EndlessShapeshiftDuration = !Instance.EndlessShapeshiftDuration;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_no_shift_cd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoShapeshiftCooldown = !Instance.NoShapeshiftCooldown;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_endless_battery"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.EndlessBattery = !Instance.EndlessBattery;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_no_vitals_cd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoVitalsCooldown = !Instance.NoVitalsCooldown;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_endless_track"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.EndlessTracking = !Instance.EndlessTracking;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_no_track_cd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (Instance != null)
				{
					Instance.NoTrackingCooldown = !Instance.NoTrackingCooldown;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_phantom_god"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				if (CheatConfig.PhantomMode != null)
				{
					CheatConfig.PhantomMode.Value = !CheatConfig.PhantomMode.Value;
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["cheat_mer"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t) && ModKeyValidator.IsPremium && ModKeyValidator.V())
			{
				GameCheats.ToggleMapEventRelay(!GameCheats.MapEventRelayActive);
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
	}

	internal static object GetCheatsState()
	{
		try
		{
			CheatManager instance = Instance;
			_cheatsStateDto.godMode = instance?.GodModeEnabled ?? false;
			_cheatsStateDto.scannerBypass = GameCheats.ScannerBypassEnabled;
			_cheatsStateDto.scannerTimeLeft = GameCheats.GetScannerBypassRemainingTime();
			_cheatsStateDto.allowVenting = instance?.AllowVenting ?? false;
			_cheatsStateDto.teleportWithCursor = instance?.TeleportWithCursor ?? false;
			_cheatsStateDto.freeCam = instance?.FreeCamEnabled ?? false;
			_cheatsStateDto.noClipSmooth = instance?.NoClipSmoothEnabled ?? false;
			_cheatsStateDto.radarEnabled = instance?.RadarEnabled ?? false;
			_cheatsStateDto.radarScale = instance?.RadarScale ?? 0.08f;
			_cheatsStateDto.tracersEnabled = instance?.TracersEnabled ?? false;
			_cheatsStateDto.seeGhosts = instance?.SeeGhosts ?? false;
			_cheatsStateDto.seeDeadChat = instance?.SeeDeadChat ?? false;
			_cheatsStateDto.showKillCooldowns = instance?.ShowKillCooldowns ?? false;
			_cheatsStateDto.noShadows = instance?.NoShadowsEnabled ?? false;
			_cheatsStateDto.visionMultiplier = CheatConfig.VisionMultiplier;
			_cheatsStateDto.killAlerts = instance?.KillAlertsEnabled ?? false;
			_cheatsStateDto.eventLogger = instance?.EventLoggerEnabled ?? false;
			_cheatsStateDto.noSabotageCooldown = instance?.NoSabotageCooldown ?? false;
			_cheatsStateDto.endlessVentTime = instance?.EndlessVentTime ?? false;
			_cheatsStateDto.noVentCooldown = instance?.NoVentCooldown ?? false;
			_cheatsStateDto.endlessShapeshiftDuration = instance?.EndlessShapeshiftDuration ?? false;
			_cheatsStateDto.noShapeshiftCooldown = instance?.NoShapeshiftCooldown ?? false;
			_cheatsStateDto.endlessBattery = instance?.EndlessBattery ?? false;
			_cheatsStateDto.noVitalsCooldown = instance?.NoVitalsCooldown ?? false;
			_cheatsStateDto.endlessTracking = instance?.EndlessTracking ?? false;
			_cheatsStateDto.noTrackingCooldown = instance?.NoTrackingCooldown ?? false;
			_cheatsStateDto.phantomMode = CheatConfig.PhantomMode?.Value ?? false;
			_cheatsStateDto.mer = GameCheats.MapEventRelayActive;
			_cheatsStateDto.merTL = GameCheats.GetMapEventRelayRemaining();
			var activePlayerEventRelays = GameCheats.GetActivePlayerEventRelays();
			if (activePlayerEventRelays != null && activePlayerEventRelays.Count > 0)
			{
				Dictionary<string, float> dictionary = new Dictionary<string, float>(activePlayerEventRelays.Count);
				foreach (var item in activePlayerEventRelays)
				{
					dictionary[item.Key.ToString()] = item.Value;
				}
				_cheatsStateDto.evTgts = dictionary;
			}
			else
			{
				_cheatsStateDto.evTgts = null;
			}
			_cheatsStateDto.revealSus = GameCheats.IsRevealSusActive;
			return _cheatsStateDto;
		}
		catch
		{
			return _cheatsStateDto;
		}
	}

	internal static int GetUiStateHash()
	{
		int num = 17;
		num = num * 31 + (_showQuickActions ? 1 : 0);
		num = num * 31 + (_showMovement ? 1 : 0);
		num = num * 31 + (_showESP ? 1 : 0);
		num = num * 31 + (_showRoleBuffs ? 1 : 0);
		num = num * 31 + (_showProtectPlayerUI ? 1 : 0);
		num = num * 31 + _selectedPlayerIndex;
		num = num * 31 + (int)(_protectionDuration * 10f);
		num = num * 31 + GameCheats.ForceVotesSelectedTargetIndex;
		num = num * 31 + (GameCheats.HasProtectedPlayer ? 1 : 0);
		try
		{
			num = num * 31 + GameCheats.ForceVotesCountRemaining();
		}
		catch
		{
		}
		return num;
	}

	internal static object GetCheatsUiState()
	{
		try
		{
			var alivePlayersForProtection = GameCheats.GetAlivePlayersForProtection();
			if (alivePlayersForProtection.Count > 0 && _selectedPlayerIndex >= 0 && _selectedPlayerIndex < alivePlayersForProtection.Count)
			{
				_ = alivePlayersForProtection[_selectedPlayerIndex];
				_ = alivePlayersForProtection[_selectedPlayerIndex];
			}
			_cheatsUiDto.showQuickActions = _showQuickActions;
			_cheatsUiDto.showMovement = _showMovement;
			_cheatsUiDto.showESP = _showESP;
			_cheatsUiDto.showRoleBuffs = _showRoleBuffs;
			_cheatsUiDto.showProtectPlayer = _showProtectPlayerUI;
			_cheatsUiDto.selectedProtectPlayer = _selectedPlayerIndex;
			_cheatsUiDto.protectionDuration = _protectionDuration;
			_cheatsUiDto.hasProtectedPlayer = GameCheats.HasProtectedPlayer;
			_cheatsUiDto.forceVotesTarget = GameCheats.ForceVotesSelectedTargetIndex;
			_cheatsUiDto.forceVotesRemaining = GameCheats.ForceVotesCountRemaining();
			_cheatsUiDto.forceVotesTargetName = GameCheats.ForceVotesGetCurrentTargetName();
			return _cheatsUiDto;
		}
		catch
		{
			_cheatsUiDto.showQuickActions = true;
			_cheatsUiDto.showMovement = true;
			_cheatsUiDto.showESP = true;
			_cheatsUiDto.showRoleBuffs = true;
			return _cheatsUiDto;
		}
	}

	internal static List<object> GetAlivePlayersForServer()
	{
		List<object> list = new List<object>();
		try
		{
			foreach (var item in GameCheats.GetAlivePlayersForProtection())
			{
				list.Add(new
				{
					name = item.Name,
					isImpostor = item.IsImpostor,
					playerId = item.PlayerId
				});
			}
		}
		catch
		{
		}
		return list;
	}

	public void DrawCheatsTab()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 8)
		{
			PlayerPickMenu.CheckRealtimeUpdate();
			_safeSnapshot = ServerData.CurrentSnapshot;
		}
		ServerData.UISnapshot safeSnapshot = _safeSnapshot;
		if (safeSnapshot?.CheatsBytecode == null || safeSnapshot.CheatsBytecode.Length == 0)
		{
			GUILayout.Label("<color=#888888>Loading from server...</color>", GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(50f) });
			return;
		}
		if (_cachedCHBytecode != safeSnapshot.CheatsBytecode)
		{
			_cachedCHBytecode = safeSnapshot.CheatsBytecode;
			_cachedCHInverseMap = ((safeSnapshot.CheatsBytecode.Length >= 524) ? new byte[256] : null);
			if (_cachedCHInverseMap != null)
			{
				System.Array.Copy(safeSnapshot.CheatsBytecode, 268, _cachedCHInverseMap, 0, 256);
			}
			_cachedCHToken = ((safeSnapshot.CheatsBytecode.Length >= 268) ? BitConverter.ToInt64(safeSnapshot.CheatsBytecode, 260) : safeSnapshot.SessionToken);
		}
		GhostUI.Execute(safeSnapshot.CheatsBytecode, _cachedCHToken, GetActionRegistry(), _cachedCHInverseMap);
	}

	public void Update()
	{
		try
		{
			bool flag = CheatConfig.OfflineLocalMode?.Value ?? false;
			if (!InteropFix.Cast((Object)(object)PlayerControl.LocalPlayer) || (!ServerData.IsLoaded && !flag) || (!ModKeyValidator.V() && !flag))
			{
				return;
			}
			GameCheats.ResetSatelliteIfNotInGame();
			GameCheats.UpdateFreeCam();
			GameCheats.UpdateScannerBypass();
			if (NoClipSmoothEnabled)
			{
				GameCheats.UpdateNoClipSmooth();
			}
			if (NoShadowsEnabled)
			{
				HudManager instance = DestroyableSingleton<HudManager>.Instance;
				if ((Object)(object)((instance != null) ? instance.ShadowQuad : null) != (Object)null && ((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.activeSelf)
				{
					((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(false);
				}
			}
			GameCheats.UpdateProtection();
			GameCheats.UpdateGodMode();
			GameCheats.UpdateMapEventRelay();
			GameCheats.UpdatePlayerEventRelays();
			if (Time.time - _lastRevealSusTime >= 0.25f)
			{
				_lastRevealSusTime = Time.time;
				GameCheats.UpdateRevealSus();
			}
			if (Time.time - _lastAnimationTime >= 0.1f)
			{
				_lastAnimationTime = Time.time;
				GameCheats.ProcessAnimationCheats();
			}
			GameCheats.UpdateRadarLogic();
			SabotageService.UpdateLockedDoors();
			if (GameCheats.RadarEnabled != RadarEnabled)
			{
				GameCheats.RadarEnabled = RadarEnabled;
			}
			if (ShowKillCooldowns && Time.time - _lastKillCooldownTime >= 0.25f)
			{
				_lastKillCooldownTime = Time.time;
				GameCheats.DrawKillCooldowns();
			}
			if (Time.time - _lastUpdateTime < 0.1f)
			{
				return;
			}
			_lastUpdateTime = Time.time;
			if ((Object)(object)_hudCache == (Object)null)
			{
				_hudCache = DestroyableSingleton<HudManager>.Instance;
			}
			if (TeleportWithCursor != GameCheats.TeleportToCursorEnabled)
			{
				GameCheats.TeleportToCursorEnabled = TeleportWithCursor;
			}
			if (AllowVenting && !previousAllowVenting && (Object)(object)_hudCache != (Object)null)
			{
				RoleCheats.EnableVentingForAll(_hudCache);
			}
			if (AllowVenting)
			{
				HudManager hudCache = _hudCache;
				if ((Object)(object)((hudCache != null) ? hudCache.ImpostorVentButton : null) != (Object)null)
				{
					((Component)_hudCache.ImpostorVentButton).gameObject.SetActive(true);
				}
			}
			previousAllowVenting = AllowVenting;
			if (SeeGhosts != previousSeeGhosts)
			{
				GameCheats.ToggleSeeGhosts(SeeGhosts);
				previousSeeGhosts = SeeGhosts;
			}
			if (SeeDeadChat != previousSeeDeadChat)
			{
				GameCheats.ToggleSeeDeadChat(SeeDeadChat);
				previousSeeDeadChat = SeeDeadChat;
			}
			if (RadarEnabled != previousRadarEnabled)
			{
				GameCheats.ToggleRadar(RadarEnabled);
				previousRadarEnabled = RadarEnabled;
			}
			if (Mathf.Abs(RadarScale - previousRadarScale) > 0.001f)
			{
				GameCheats.SetRadarScale(RadarScale);
				previousRadarScale = RadarScale;
			}
			if (FreeCamEnabled != previousFreeCam)
			{
				GameCheats.ToggleFreeCam(FreeCamEnabled);
				previousFreeCam = FreeCamEnabled;
			}
			if (TracersEnabled != previousTracers)
			{
				GameCheats.ToggleTracers(TracersEnabled);
				previousTracers = TracersEnabled;
			}
			if (NoClipSmoothEnabled != previousNoClipSmooth)
			{
				GameCheats.ToggleNoClipSmooth(NoClipSmoothEnabled);
				previousNoClipSmooth = NoClipSmoothEnabled;
			}
			if (TracersEnabled)
			{
				GameCheats.UpdateTracers();
			}
			NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
			RoleBehaviour val = ((data != null) ? data.Role : null);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			try
			{
				EngineerRole val2 = (EngineerRole)(object)((val is EngineerRole) ? val : null);
				if (val2 != null)
				{
					if (EndlessVentTime)
					{
						val2.inVentTimeRemaining = 30f;
					}
					if (NoVentCooldown && val2.cooldownSecondsRemaining > 0f)
					{
						val2.cooldownSecondsRemaining = 0.01f;
					}
				}
				ShapeshifterRole val3 = (ShapeshifterRole)(object)((val is ShapeshifterRole) ? val : null);
				if (val3 != null)
				{
					if (EndlessShapeshiftDuration)
					{
						val3.durationSecondsRemaining = RoleCheats.MAX_SAFE_VALUE;
					}
					if (NoShapeshiftCooldown && val3.cooldownSecondsRemaining > 0f)
					{
						val3.cooldownSecondsRemaining = 0.01f;
					}
				}
				TrackerRole val4 = (TrackerRole)(object)((val is TrackerRole) ? val : null);
				if (val4 != null)
				{
					if (EndlessTracking)
					{
						val4.durationSecondsRemaining = RoleCheats.MAX_SAFE_VALUE;
					}
					if (NoTrackingCooldown)
					{
						if (val4.cooldownSecondsRemaining > 0f)
						{
							val4.cooldownSecondsRemaining = 0.01f;
						}
						val4.delaySecondsRemaining = 0.01f;
					}
				}
				ScientistRole val5 = (ScientistRole)(object)((val is ScientistRole) ? val : null);
				if (val5 != null)
				{
					if (EndlessBattery)
					{
						val5.currentCharge = RoleCheats.MAX_SAFE_VALUE;
					}
					if (NoVitalsCooldown && val5.currentCooldown > 0f)
					{
						val5.currentCooldown = 0.01f;
					}
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"Error updating continuous role cheats: {value}"));
			}
		}
		catch (Exception value2)
		{
			Debug.LogError(InteropFix.Cast($"[CheatManager.Update] Error: {value2}"));
		}
	}

	private static bool IsHost()
	{
		if ((Object)(object)AmongUsClient.Instance != (Object)null)
		{
			return ((InnerNetClient)AmongUsClient.Instance).AmHost;
		}
		return false;
	}

	public void OnGUI()
	{
		if (RadarEnabled)
		{
			GameCheats.DrawRadar();
		}
		GameCheats.DrawKillAlert();
		if (EventLoggerEnabled)
		{
			GameCheats.DrawEventLoggerUI();
		}
	}
}




