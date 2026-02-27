using System;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using InnerNet;
using ModMenuCrew.Features;
using ModMenuCrew.Networking;
using ModMenuCrew.UI.Managers;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew.UI.Menus;

public class PlayerPickMenu
{
	private static bool _showFilters = false;

	private static bool _filterImpostors = true;

	private static bool _filterCrewmates = true;

	private static byte? _openRoleDropdown = null;

	private static byte? _openPreAssignDropdown = null;

	private static Vector2 _roleDropdownScroll = Vector2.zero;

	private static readonly string[] ALL_ROLES = new string[11]
	{
		"Crewmate", "Engineer", "Scientist", "Noisemaker", "Tracker", "Detective", "Impostor", "Shapeshifter", "Phantom", "Viper",
		"GuardianAngel"
	};

	private static readonly System.Collections.Generic.HashSet<string> PREMIUM_ROLES = new System.Collections.Generic.HashSet<string> { "Phantom", "Viper" };

	private static System.Collections.Generic.Dictionary<string, System.Action<long>> _actionRegistry;

	private ServerData.UISnapshot _safeSnapshot;

	private byte[] _cachedPPBytecode;

	private byte[] _cachedPPInverseMap;

	private long _cachedPPToken;

	private static int _lastPlayerHash = 0;

	private static int _lastCheckedFrame = -1;

	private static float _lastSendTime = 0f;

	private static float _lastWakeupTime = 0f;

	private static float _lastHashCheckTime = 0f;

	private const float SEND_THROTTLE_SECONDS = 0.05f;

	private const float HASH_CHECK_INTERVAL_WS_DEAD = 0.25f;

	private const float HEARTBEAT_WAKEUP_THROTTLE = 3f;

	private const float HEARTBEAT_WAKEUP_THROTTLE_PRIORITY = 0.5f;

	internal static volatile bool PendingImmediateHeartbeat = false;

	internal static void RegisterActions(System.Collections.Generic.Dictionary<string, System.Action<long>> registry)
	{
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		if (registry == null)
		{
			return;
		}
		RegisterPlayerAction(registry, "tp_", delegate(long token, byte playerId4)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact)
			{
				PlayerControl playerById = GetPlayerById(playerId4);
				if ((Object)(object)playerById != (Object)null && !playerById.Data.IsDead && (Object)(object)PlayerControl.LocalPlayer != (Object)null)
				{
					PlayerControl.LocalPlayer.NetTransform.SnapTo(InteropFix.Cast(((Component)playerById).transform.position));
				}
			}
		});
		RegisterPlayerAction(registry, "kill_", delegate(long token, byte playerId4)
		{
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && !((Object)(object)ShipStatus.Instance == (Object)null))
			{
				PlayerControl playerById = GetPlayerById(playerId4);
				if ((Object)(object)playerById != (Object)null && !playerById.Data.IsDead)
				{
					GameCheats.HostForceKillPlayer(playerById);
				}
			}
		});
		RegisterPlayerAction(registry, "kick_", delegate(long token, byte playerId4)
		{
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				PlayerControl playerById = GetPlayerById(playerId4);
				if ((Object)(object)playerById != (Object)null && (Object)(object)playerById != (Object)(object)PlayerControl.LocalPlayer)
				{
					GameCheats.KickPlayer(playerById);
				}
			}
		});
		RegisterPlayerAction(registry, "ban_", delegate(long token, byte playerId4)
		{
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && !((Object)(object)ShipStatus.Instance != (Object)null))
			{
				PlayerControl playerById = GetPlayerById(playerId4);
				if ((Object)(object)playerById != (Object)null && (Object)(object)playerById != (Object)(object)PlayerControl.LocalPlayer)
				{
					GameCheats.KickPlayer(playerById, ban: true);
				}
			}
		});
		RegisterPlayerAction(registry, "ev_", delegate(long token, byte playerId4)
		{
			if (GhostUI.CheckToken(token) && ModKeyValidator.IsPremium && ModKeyValidator.V())
			{
				GameCheats.TogglePlayerEventRelay(playerId4);
				TriggerRealtimeUpdate(force: true);
			}
		});
		registry["toggle_filters"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token))
			{
				_showFilters = !_showFilters;
				TriggerRealtimeUpdate(force: true);
			}
		};
		registry["filter_impostors"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token))
			{
				_filterImpostors = !_filterImpostors;
				TriggerRealtimeUpdate(force: true);
			}
		};
		registry["filter_crewmates"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token))
			{
				_filterCrewmates = !_filterCrewmates;
				TriggerRealtimeUpdate(force: true);
			}
		};
		RegisterPlayerAction(registry, "role_open_", delegate(long token, byte b4)
		{
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				_openRoleDropdown = ((_openRoleDropdown == b4) ? ((byte?)null) : new byte?(b4));
				_openPreAssignDropdown = null;
				_roleDropdownScroll = Vector2.zero;
				TriggerRealtimeUpdate(force: true);
			}
		});
		registry["role_close"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token))
			{
				_openRoleDropdown = null;
				TriggerRealtimeUpdate(force: true);
			}
		};
		string[] aLL_ROLES = ALL_ROLES;
		foreach (string text in aLL_ROLES)
		{
			string capturedRole = text;
			for (byte b = 0; b <= 15; b++)
			{
				byte playerId = b;
				registry[$"role_set_{capturedRole}_{playerId}"] = delegate(long token)
				{
					//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
					//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
					//IL_00af: Unknown result type (might be due to invalid IL or missing references)
					//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
					//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
					if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost)
					{
						PlayerControl playerById = GetPlayerById(playerId);
						if (!((Object)(object)playerById == (Object)null))
						{
							string value = capturedRole;
							if (PREMIUM_ROLES.Contains(capturedRole) && (!ModKeyValidator.IsPremium || !ModKeyValidator.V()))
							{
								NotifyUtils.Warning("â\u02dc… " + capturedRole + " is PREMIUM ONLY! Using Impostor.");
								value = "Impostor";
							}
							try
							{
								RoleTypes val2 = (RoleTypes)Enum.Parse(typeof(RoleTypes), value);
								playerById.RpcSetRole(val2, true);
								if (IsImpostorRole(val2))
								{
									playerById.Data.RpcSetTasks(InteropFix.Cast(System.Array.Empty<byte>()));
								}
								ImpostorForcer.UpdateRoleLocally(playerById, val2);
							}
							catch
							{
							}
							_openRoleDropdown = null;
							TriggerRealtimeUpdate(force: true);
						}
					}
				};
			}
		}
		RegisterPlayerAction(registry, "preassign_open_", delegate(long token, byte b4)
		{
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && !((Object)(object)ShipStatus.Instance != (Object)null))
			{
				_openPreAssignDropdown = ((_openPreAssignDropdown == b4) ? ((byte?)null) : new byte?(b4));
				_openRoleDropdown = null;
				TriggerRealtimeUpdate(force: true);
			}
		});
		registry["preassign_close"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token))
			{
				_openPreAssignDropdown = null;
				TriggerRealtimeUpdate(force: true);
			}
		};
		for (byte b2 = 0; b2 <= 15; b2++)
		{
			byte playerId2 = b2;
			registry[$"preassign_clear_{playerId2}"] = delegate(long token)
			{
				if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost)
				{
					ImpostorForcer.ClearPreGameRoleForPlayer(playerId2);
					_openPreAssignDropdown = null;
					TriggerRealtimeUpdate(force: true);
				}
			};
		}
		registry["preassign_clear_all"] = delegate(long token)
		{
			if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				ImpostorForcer.ClearAllPreGameRoleAssignments();
				_openPreAssignDropdown = null;
				TriggerRealtimeUpdate(force: true);
			}
		};
		RoleTypes[] supportedRoles = ImpostorForcer.GetSupportedRoles();
		foreach (RoleTypes val in supportedRoles)
		{
			RoleTypes capturedRoleType = val;
			for (byte b3 = 0; b3 <= 15; b3++)
			{
				byte playerId3 = b3;
				registry[$"preassign_set_{capturedRoleType}_{playerId3}"] = delegate(long token)
				{
					//IL_0055: Unknown result type (might be due to invalid IL or missing references)
					if (GhostUI.CheckToken(token) && IntegrityGuard.IsIntact && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && !((Object)(object)ShipStatus.Instance != (Object)null))
					{
						PlayerControl playerById = GetPlayerById(playerId3);
						if ((Object)(object)playerById != (Object)null)
						{
							ImpostorForcer.SetPreGameRoleForPlayer(playerById, capturedRoleType);
						}
						_openPreAssignDropdown = null;
						TriggerRealtimeUpdate(force: true);
					}
				};
			}
		}
	}

	private static bool IsImpostorRole(RoleTypes role)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)role != 1 && (int)role != 5 && (int)role != 9)
		{
			return (int)role == 18;
		}
		return true;
	}

	private static void RegisterPlayerAction(System.Collections.Generic.Dictionary<string, System.Action<long>> registry, string prefix, System.Action<long, byte> handler)
	{
		for (byte b = 0; b <= 15; b++)
		{
			string key = prefix + b;
			byte playerId = b;
			registry[key] = delegate(long token)
			{
				handler(token, playerId);
			};
		}
	}

	private static PlayerControl GetPlayerById(byte playerId)
	{
		if (PlayerControl.AllPlayerControls == null)
		{
			return null;
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current != (Object)null && current.PlayerId == playerId && !current.Data.Disconnected)
			{
				return current;
			}
		}
		return null;
	}

	private static System.Collections.Generic.Dictionary<string, System.Action<long>> GetActionRegistry()
	{
		if (_actionRegistry == null)
		{
			_actionRegistry = new System.Collections.Generic.Dictionary<string, System.Action<long>>();
			RegisterActions(_actionRegistry);
		}
		return _actionRegistry;
	}

	public void Draw()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 8)
		{
			CheckRealtimeUpdate();
			_safeSnapshot = ServerData.CurrentSnapshot;
		}
		ServerData.UISnapshot safeSnapshot = _safeSnapshot;
		if (safeSnapshot != null && safeSnapshot.PlayerPickBytecode != null && safeSnapshot.PlayerPickBytecode.Length != 0)
		{
			if (_cachedPPBytecode != safeSnapshot.PlayerPickBytecode)
			{
				_cachedPPBytecode = safeSnapshot.PlayerPickBytecode;
				if (safeSnapshot.PlayerPickBytecode.Length >= 524)
				{
					_cachedPPInverseMap = new byte[256];
					System.Array.Copy(safeSnapshot.PlayerPickBytecode, 268, _cachedPPInverseMap, 0, 256);
				}
				else
				{
					_cachedPPInverseMap = null;
				}
				_cachedPPToken = ((safeSnapshot.PlayerPickBytecode.Length >= 268) ? BitConverter.ToInt64(safeSnapshot.PlayerPickBytecode, 260) : safeSnapshot.SessionToken);
			}
			GhostUI.Execute(safeSnapshot.PlayerPickBytecode, _cachedPPToken, GetActionRegistry(), _cachedPPInverseMap);
		}
		else
		{
			GUILayout.BeginVertical(GuiStyles.SectionStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.ExpandWidth(true),
				GUILayout.ExpandHeight(true)
			});
			GUILayout.Label("<size=14><b>PLAYER SELECTION</b></size>", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(20f);
			GUILayout.Label("<color=#888888>Loading from server...</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndVertical();
		}
	}

	internal static object[] CollectPlayerDataForServer()
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected I4, but got Unknown
		try
		{
			if (PlayerControl.AllPlayerControls == null)
			{
				return System.Array.Empty<object>();
			}
			System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null)
				{
					continue;
				}
				int num = 0;
				int num2 = 0;
				var tasks = current.Data.Tasks;
				if (tasks != null)
				{
					num2 = tasks.Count;
					for (int i = 0; i < num2; i++)
					{
						if (tasks[i].Complete)
						{
							num++;
						}
					}
				}
				int platformId = 0;
				string platformName = "";
				uint level = 0u;
				int clientId = ((InnerNetObject)current).OwnerId;
				string friendCode = "";
				try
				{
					AmongUsClient instance = AmongUsClient.Instance;
					ClientData val = ((instance != null) ? ((InnerNetClient)instance).GetClient(((InnerNetObject)current).OwnerId) : null);
					if (val != null)
					{
						level = val.PlayerLevel;
						clientId = val.Id;
						friendCode = val.FriendCode ?? "";
						if (val.PlatformData != null)
						{
							platformId = (int)val.PlatformData.Platform;
							platformName = val.PlatformData.PlatformName ?? "";
						}
					}
				}
				catch
				{
				}
				byte playerId = current.PlayerId;
				string name = current.Data.PlayerName ?? "???";
				var defaultOutfit = current.Data.DefaultOutfit;
				int colorId = ((defaultOutfit != null) ? defaultOutfit.ColorId : 0);
				RoleBehaviour role = current.Data.Role;
				list.Add(new
				{
					id = playerId,
					name = name,
					colorId = colorId,
					isImpostor = (role != null && role.IsImpostor),
					isDead = current.Data.IsDead,
					isDisconnected = current.Data.Disconnected,
					tasksCompleted = num,
					tasksTotal = num2,
					isHost = ((Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetObject)current).OwnerId == ((InnerNetClient)AmongUsClient.Instance).HostId),
					platformId = platformId,
					platformName = platformName,
					level = level,
					clientId = clientId,
					friendCode = friendCode
				});
			}
			return list.ToArray();
		}
		catch
		{
			return System.Array.Empty<object>();
		}
	}

	internal static (bool isHost, bool isInGame, byte localPlayerId) GetGameContext()
	{
		try
		{
			bool item = (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).AmHost;
			bool item2 = (Object)(object)ShipStatus.Instance != (Object)null;
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			byte item3 = ((localPlayer != null) ? localPlayer.PlayerId : byte.MaxValue);
			return (isHost: item, isInGame: item2, localPlayerId: item3);
		}
		catch
		{
			return (isHost: false, isInGame: false, localPlayerId: byte.MaxValue);
		}
	}

	internal static object GetUIState()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			System.Collections.Generic.Dictionary<int, string> dictionary = new System.Collections.Generic.Dictionary<int, string>();
			foreach (var preGameRoleAssignment in ImpostorForcer.PreGameRoleAssignments)
			{
				dictionary[preGameRoleAssignment.Key] = ((object)preGameRoleAssignment.Value/*cast due to .constrained prefix*/).ToString();
			}
			return new
			{
				showFilters = _showFilters,
				filterImpostors = _filterImpostors,
				filterCrewmates = _filterCrewmates,
				openRoleDropdown = (_openRoleDropdown.HasValue ? new int?(_openRoleDropdown.Value) : ((int?)null)),
				openPreAssignDropdown = (_openPreAssignDropdown.HasValue ? new int?(_openPreAssignDropdown.Value) : ((int?)null)),
				hostRoleOverrideEnabled = ImpostorForcer.RoleOverrideEnabled,
				hostRoleOverrideRole = ((object)ImpostorForcer.SelectedRoleForHost/*cast due to .constrained prefix*/).ToString(),
				preAssignments = dictionary,
				isPremium = ModKeyValidator.IsPremium
			};
		}
		catch
		{
			return new
			{
				showFilters = _showFilters,
				filterImpostors = _filterImpostors,
				filterCrewmates = _filterCrewmates,
				openRoleDropdown = (int?)null,
				openPreAssignDropdown = (int?)null,
				hostRoleOverrideEnabled = false,
				hostRoleOverrideRole = "Impostor",
				preAssignments = new System.Collections.Generic.Dictionary<int, string>(),
				isPremium = false
			};
		}
	}

	internal static void ResetHashForReconnect()
	{
		_lastPlayerHash = 0;
	}

	internal static void CheckRealtimeUpdate()
	{
		int frameCount = Time.frameCount;
		if (frameCount == _lastCheckedFrame)
		{
			return;
		}
		_lastCheckedFrame = frameCount;
		if (!RealtimeConnection.IsConnected)
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup - _lastHashCheckTime < 0.25f)
			{
				return;
			}
			_lastHashCheckTime = realtimeSinceStartup;
		}
		if (CalculatePlayerHash() != _lastPlayerHash)
		{
			TriggerRealtimeUpdate();
		}
	}

	internal static void TriggerRealtimeUpdate(bool force = false)
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!force && realtimeSinceStartup - _lastSendTime < 0.05f)
		{
			return;
		}
		_lastPlayerHash = CalculatePlayerHash();
		_lastSendTime = realtimeSinceStartup;
		try
		{
			if (!RealtimeConnection.IsConnected)
			{
				PendingImmediateHeartbeat = true;
				float num = (force ? 0.5f : 3f);
				if (realtimeSinceStartup - _lastWakeupTime >= num)
				{
					_lastWakeupTime = realtimeSinceStartup;
					ModKeyValidator.ForceHeartbeatWakeup();
				}
			}
			else
			{
				object[] playerData = CollectPlayerDataForServer();
				object uIState = GetUIState();
				object banMenuState = global::ModMenuCrew.UI.Menus.BanMenu.GetBanMenuState();
				object uIState2 = global::ModMenuCrew.UI.Menus.BanMenu.GetUIState();
				object cheatsState = CheatManager.GetCheatsState();
				object cheatsUiState = CheatManager.GetCheatsUiState();
				System.Collections.Generic.List<object> alivePlayersForServer = CheatManager.GetAlivePlayersForServer();
				object spoofingState = SpoofingMenu.GetSpoofingState();
				RealtimeConnection.SendUpdate(playerData, uIState, banMenuState, uIState2, cheatsState, cheatsUiState, alivePlayersForServer, spoofingState, force);
			}
		}
		catch
		{
		}
	}

	private static int CalculatePlayerHash()
	{
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Expected I4, but got Unknown
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (PlayerControl.AllPlayerControls == null)
			{
				return 0;
			}
			int num = 17;
			int num2 = 0;
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null)
				{
					continue;
				}
				num2++;
				num = num * 31 + current.PlayerId;
				num = num * 31 + (current.Data.IsDead ? 1 : 0);
				num = num * 31 + (current.Data.Disconnected ? 1 : 0);
				int num3 = num * 31;
				RoleBehaviour role = current.Data.Role;
				num = num3 + ((role != null && role.IsImpostor) ? 1 : 0);
				int num4 = num * 31;
				var defaultOutfit = current.Data.DefaultOutfit;
				num = num4 + ((defaultOutfit != null) ? defaultOutfit.ColorId : 0);
				if (current.Data.PlayerName != null)
				{
					num = num * 31 + current.Data.PlayerName.GetHashCode();
				}
				if (current.Data.Tasks == null)
				{
					continue;
				}
				int num5 = 0;
				for (int i = 0; i < current.Data.Tasks.Count; i++)
				{
					if (current.Data.Tasks[i].Complete)
					{
						num5++;
					}
				}
				num = num * 31 + num5;
			}
			num = num * 31 + num2;
			int num6 = num * 31;
			AmongUsClient instance = AmongUsClient.Instance;
			num = num6 + ((instance != null && ((InnerNetClient)instance).AmHost) ? 1 : 0);
			num = num * 31 + (((Object)(object)ShipStatus.Instance != (Object)null) ? 1 : 0);
			num = num * 31 + (_openRoleDropdown.HasValue ? (_openRoleDropdown.Value + 1) : 0);
			num = num * 31 + (_openPreAssignDropdown.HasValue ? (_openPreAssignDropdown.Value + 1) : 0);
			num = num * 31 + (_showFilters ? 1 : 0);
			num = num * 31 + (_filterImpostors ? 1 : 0);
			num = num * 31 + (_filterCrewmates ? 1 : 0);
			try
			{
				num = num * 31 + ((CheatConfig.GodMode?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.SeeGhosts?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.SeeDeadChat?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.FreeCamEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoClipSmoothEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.RadarEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.TracersEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.AllowVenting?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.TeleportWithCursor?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.ShowKillCooldowns?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoShadows?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.KillAlertsEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.EventLoggerEnabled?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoSabotageCooldown?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.PhantomMode?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.EndlessVentTime?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoVentCooldown?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.EndlessShapeshiftDuration?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoShapeshiftCooldown?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.EndlessBattery?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoVitalsCooldown?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.EndlessTracking?.Value ?? false) ? 1 : 0);
				num = num * 31 + ((CheatConfig.NoTrackingCooldown?.Value ?? false) ? 1 : 0);
				num = num * 31 + (int)(CheatConfig.VisionMultiplier * 10f);
				num = num * 31 + (int)((CheatConfig.RadarScale?.Value ?? 0.08f) * 100f);
				num = num * 31 + (GameCheats.ScannerBypassEnabled ? 1 : 0);
				num = num * 31 + (GameCheats.IsRevealSusActive ? 1 : 0);
			}
			catch
			{
			}
			try
			{
				num = num * 31 + (SpoofingService.EnableLevelSpoof ? 1 : 0);
				num = num * 31 + (SpoofingService.EnablePlatformSpoof ? 1 : 0);
				num = num * 31 + (SpoofingService.EnableFriendCodeSpoof ? 1 : 0);
				num = num * 31 + (int)SpoofingService.GetEffectiveLevel();
				num = num * 31 + (int)SpoofingService.SpoofedPlatform;
				num = num * 31 + (SpoofingService.SpoofedFriendCode?.GetHashCode() ?? 0);
			}
			catch
			{
			}
			try
			{
				num = num * 31 + global::ModMenuCrew.UI.Menus.BanMenu.GetStateHash();
			}
			catch
			{
			}
			try
			{
				num = num * 31 + CheatManager.GetUiStateHash();
			}
			catch
			{
			}
			try
			{
				num = num * 31 + (ImpostorForcer.RoleOverrideEnabled ? 1 : 0);
				num = num * 31 + ((object)ImpostorForcer.SelectedRoleForHost/*cast due to .constrained prefix*/).GetHashCode();
			}
			catch
			{
			}
			try
			{
				num = num * 31 + (GameCheats.MapEventRelayActive ? 1 : 0);
				var activePlayerEventRelays = GameCheats.GetActivePlayerEventRelays();
				num = num * 31 + activePlayerEventRelays.Count;
				foreach (var item in activePlayerEventRelays)
				{
					num = num * 31 + item.Key;
				}
			}
			catch
			{
			}
			try
			{
				num = num * 31 + (((Object)(object)MeetingHud.Instance != (Object)null) ? 1 : 0);
			}
			catch
			{
			}
			return num;
		}
		catch
		{
			return 0;
		}
	}
}






