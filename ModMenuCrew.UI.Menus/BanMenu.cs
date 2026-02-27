using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using ModMenuCrew.Features;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew.UI.Menus;

public class BanMenu
{
	[System.Serializable]
	internal class BanMenuStateDto
	{
		public bool h { get; set; }

		public bool g { get; set; }

		public float bp { get; set; }

		public int bm { get; set; }

		public int pc { get; set; }

		public int st { get; set; }

		public int tz { get; set; }

		public int th { get; set; }

		public int tm { get; set; }
	}

	[System.Serializable]
	internal class BanMenuUiDto
	{
		public bool da { get; set; }

		public bool db { get; set; }

		public int sa { get; set; }

		public int sb { get; set; }

		public bool ro { get; set; }

		public string rs { get; set; } = "Impostor";
	}

	private static ServerData.UISnapshot _safeSnapshot;

	private static byte[] _cachedBMBytecode;

	private static byte[] _cachedBMInverseMap;

	private static long _cachedBMToken;

	private static bool _dropdownA = false;

	private static bool _dropdownB = false;

	private static int _selectorA = 0;

	private static int _selectorB = 0;

	private static Dictionary<string, System.Action<long>> _actionRegistry;

	private static readonly BanMenuStateDto _banMenuStateDto = new BanMenuStateDto();

	private static readonly BanMenuUiDto _banMenuUiDto = new BanMenuUiDto();

	internal static void RegisterActions(Dictionary<string, System.Action<long>> registry)
	{
		if (registry == null)
		{
			return;
		}
		for (int i = 0; i <= 200; i += 5)
		{
			int amt = i;
			registry[$"bp_add_{i}"] = guard(delegate(long t)
			{
				//IL_0051: Unknown result type (might be due to invalid IL or missing references)
				if (GhostUI.CheckToken(t))
				{
					PlayerData player = DataManager.Player;
					PlayerBanData val = ((player != null) ? player.ban : null);
					if (val != null)
					{
						val.BanPoints += (float)amt;
						Il2CppSystem.Action onBanPointsChanged = val.OnBanPointsChanged;
						if (onBanPointsChanged != null)
						{
							onBanPointsChanged.Invoke();
						}
					val.PreviousGameStartDate = new Il2CppSystem.DateTime(System.DateTime.UtcNow.Ticks);
					}
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			});
			registry[$"bp_sub_{i}"] = guard(delegate(long t)
			{
				if (GhostUI.CheckToken(t))
				{
					PlayerData player = DataManager.Player;
					PlayerBanData val = ((player != null) ? player.ban : null);
					if (val != null)
					{
						val.BanPoints = Mathf.Max(0f, val.BanPoints - (float)amt);
						Il2CppSystem.Action onBanPointsChanged = val.OnBanPointsChanged;
						if (onBanPointsChanged != null)
						{
							onBanPointsChanged.Invoke();
						}
					}
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			});
		}
		registry["bp_clear"] = guard(delegate(long t)
		{
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if (GhostUI.CheckToken(t))
			{
				PlayerData player = DataManager.Player;
				PlayerBanData val = ((player != null) ? player.ban : null);
				if (val != null)
				{
					val.BanPoints = 0f;
					Il2CppSystem.Action onBanPointsChanged = val.OnBanPointsChanged;
					if (onBanPointsChanged != null)
					{
						onBanPointsChanged.Invoke();
					}
					val.PreviousGameStartDate = new Il2CppSystem.DateTime(System.DateTime.MinValue.Ticks);
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		});
		registry["h_start"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost && !((UnityEngine.Object)(object)LobbyBehaviour.Instance == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)ShipStatus.Instance != (UnityEngine.Object)null))
				{
					GameCheats.InstantStartGame();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		});
		registry["h_rnd_col"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.HostRandomizeAllColors();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		});
		registry["h_col_p"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_selectorA = System.Math.Max(0, _selectorA - 1);
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["h_col_n"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_selectorA = System.Math.Min((((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)?.Length ?? 18) - 1, _selectorA + 1);
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		registry["h_col_apply"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.HostSetAllPlayersColor(_selectorA);
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		});
		registry["h_map_rm"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.MapCheats.DestroyMap();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		});
		registry["h_map_lobby"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance != null && ((InnerNetClient)instance).AmHost)
				{
					GameCheats.MapCheats.SpawnLobby();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
			}
		});
		registry["ro_toggle"] = guard(delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				ImpostorForcer.SetRoleOverrideEnabled(!ImpostorForcer.RoleOverrideEnabled);
				_dropdownA = false;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		});
		registry["ro_dd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_dropdownA = !_dropdownA;
				_dropdownB = false;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		for (int num = 0; num <= 15; num++)
		{
			int idx = num;
			registry[$"ro_sel_{num}"] = guard(delegate(long t)
			{
				if (GhostUI.CheckToken(t))
				{
					AmongUsClient instance = AmongUsClient.Instance;
					if (instance != null && ((InnerNetClient)instance).AmHost)
					{
						RoleTypes[] supportedRoles = ImpostorForcer.GetSupportedRoles();
						if (idx >= 0 && idx < supportedRoles.Length)
						{
							ImpostorForcer.SetSelectedRoleForHost(supportedRoles[idx]);
						}
						_dropdownA = false;
						PlayerPickMenu.TriggerRealtimeUpdate(force: true);
					}
				}
			});
		}
		registry["bt_dd"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t))
			{
				_dropdownB = !_dropdownB;
				_dropdownA = false;
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		for (int num2 = 0; num2 <= 5; num2++)
		{
			int idx2 = num2;
			registry[$"bt_sel_{num2}"] = guard(delegate(long t)
			{
				if (GhostUI.CheckToken(t))
				{
					AmongUsClient instance = AmongUsClient.Instance;
					if (instance != null && ((InnerNetClient)instance).AmHost)
					{
						_selectorB = idx2;
						PlayerBodyTypes[] availableTypes = GameCheats.BodyTypeCheats.AvailableTypes;
						if (idx2 >= 0 && idx2 < availableTypes.Length)
						{
							GameCheats.BodyTypeCheats.CurrentTypeIndex = idx2;
							GameCheats.BodyTypeCheats.SetAllPlayersBodyType(availableTypes[idx2]);
						}
						_dropdownB = false;
						PlayerPickMenu.TriggerRealtimeUpdate(force: true);
					}
				}
			});
		}
		registry["cheat_mer"] = delegate(long t)
		{
			if (GhostUI.CheckToken(t) && ModKeyValidator.IsPremium && ModKeyValidator.V())
			{
				GameCheats.ToggleMapEventRelay(!GameCheats.MapEventRelayActive);
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
		};
		static System.Action<long> guard(System.Action<long> inner)
		{
			return delegate(long t)
			{
				if (IntegrityGuard.IsIntact)
				{
					inner(t);
				}
			};
		}
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

	public static void Draw()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 8)
		{
			PlayerPickMenu.CheckRealtimeUpdate();
			_safeSnapshot = ServerData.CurrentSnapshot;
		}
		ServerData.UISnapshot safeSnapshot = _safeSnapshot;
		if (safeSnapshot?.BanMenuBytecode != null && safeSnapshot.BanMenuBytecode.Length != 0)
		{
			if (_cachedBMBytecode != safeSnapshot.BanMenuBytecode)
			{
				_cachedBMBytecode = safeSnapshot.BanMenuBytecode;
				_cachedBMInverseMap = ((safeSnapshot.BanMenuBytecode.Length >= 524) ? new byte[256] : null);
				if (_cachedBMInverseMap != null)
				{
					System.Array.Copy(safeSnapshot.BanMenuBytecode, 268, _cachedBMInverseMap, 0, 256);
				}
				_cachedBMToken = ((safeSnapshot.BanMenuBytecode.Length >= 268) ? System.BitConverter.ToInt64(safeSnapshot.BanMenuBytecode, 260) : safeSnapshot.SessionToken);
			}
			GhostUI.Execute(safeSnapshot.BanMenuBytecode, _cachedBMToken, GetActionRegistry(), _cachedBMInverseMap);
		}
		else
		{
			GUILayout.Label("<color=#888888>Loading from server...</color>", GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(50f) });
		}
	}

	internal static int GetStateHash()
	{
		return (((17 * 31 + (_dropdownA ? 1 : 0)) * 31 + (_dropdownB ? 1 : 0)) * 31 + _selectorA) * 31 + _selectorB;
	}

	public static object GetBanMenuState()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PlayerData player = DataManager.Player;
			PlayerBanData val = ((player != null) ? player.ban : null);
			AmongUsClient instance = AmongUsClient.Instance;
			int? val2 = (instance != null) ? (int?)((int)((InnerNetClient)instance).GameState) : null;
			BanMenuStateDto banMenuStateDto = _banMenuStateDto;
			AmongUsClient instance2 = AmongUsClient.Instance;
			banMenuStateDto.h = instance2 != null && ((InnerNetClient)instance2).AmHost;
			_banMenuStateDto.g = (UnityEngine.Object)(object)ShipStatus.Instance != (UnityEngine.Object)null;
			_banMenuStateDto.bp = ((val != null) ? val.BanPoints : 0f);
			_banMenuStateDto.bm = ((val != null) ? val.BanMinutesLeft : 0);
			_banMenuStateDto.pc = PlayerControl.AllPlayerControls?.Count ?? 0;
			_banMenuStateDto.st = (val2.HasValue ? val2.Value : 0);
			_banMenuStateDto.tz = (int)System.TimeZoneInfo.Local.GetUtcOffset(System.DateTime.Now).TotalMinutes;
			_banMenuStateDto.th = System.DateTime.Now.Hour;
			_banMenuStateDto.tm = System.DateTime.Now.Minute;
			return _banMenuStateDto;
		}
		catch
		{
			_banMenuStateDto.h = false;
			_banMenuStateDto.g = false;
			_banMenuStateDto.bp = 0f;
			_banMenuStateDto.bm = 0;
			_banMenuStateDto.pc = 0;
			_banMenuStateDto.st = 0;
			_banMenuStateDto.tz = 0;
			_banMenuStateDto.th = 0;
			_banMenuStateDto.tm = 0;
			return _banMenuStateDto;
		}
	}

	public static object GetUIState()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			_banMenuUiDto.da = _dropdownA;
			_banMenuUiDto.db = _dropdownB;
			_banMenuUiDto.sa = _selectorA;
			_banMenuUiDto.sb = _selectorB;
			_banMenuUiDto.ro = ImpostorForcer.RoleOverrideEnabled;
			_banMenuUiDto.rs = ((object)ImpostorForcer.SelectedRoleForHost/*cast due to .constrained prefix*/).ToString();
			return _banMenuUiDto;
		}
		catch
		{
			_banMenuUiDto.da = false;
			_banMenuUiDto.db = false;
			_banMenuUiDto.sa = 0;
			_banMenuUiDto.sb = 0;
			_banMenuUiDto.ro = false;
			_banMenuUiDto.rs = "Impostor";
			return _banMenuUiDto;
		}
	}
}





