using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew;

public static class SabotageService
{
	private static readonly SystemTypes[] ValidDoorSystems;

	private static readonly Dictionary<SystemTypes, bool> _lockedDoors;

	private static SystemTypes[] _cachedMapDoors;

	private static int _lastMapId;

	private static readonly Dictionary<SystemTypes, float> _lastDoorCloseRpcTime;

	private const float DOOR_RECLOSE_THROTTLE_SECONDS = 2f;

	private static readonly Dictionary<SystemTypes, string> DoorRoomNames;

	public static SystemTypes[] GetDoorsForCurrentMap()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ShipStatus.Instance == (Object)null)
		{
			return System.Array.Empty<SystemTypes>();
		}
		int num = (int)ShipStatus.Instance.Type;
		if (_cachedMapDoors != null && _lastMapId == num)
		{
			return _cachedMapDoors;
		}
		System.Collections.Generic.HashSet<SystemTypes> rooms = new System.Collections.Generic.HashSet<SystemTypes>();
		Il2CppReferenceArray<OpenableDoor> allDoors = ShipStatus.Instance.AllDoors;
		if (allDoors != null)
		{
			for (int i = 0; i < ((Il2CppArrayBase<OpenableDoor>)(object)allDoors).Length; i++)
			{
				if ((Object)(object)((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i] != (Object)null)
				{
					rooms.Add(((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].Room);
				}
			}
		}
		_cachedMapDoors = rooms.OrderBy((SystemTypes r) => GetDoorRoomName(r)).ToArray();
		_lastMapId = num;
		foreach (SystemTypes item in _lockedDoors.Keys.Where((SystemTypes k) => !rooms.Contains(k)).ToList())
		{
			_lockedDoors.Remove(item);
		}
		return _cachedMapDoors;
	}

	public unsafe static string GetDoorRoomName(SystemTypes room)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		if (!DoorRoomNames.TryGetValue(room, out var value))
		{
			return ((object)(*(SystemTypes*)(&room))/*cast due to .constrained prefix*/).ToString();
		}
		return value;
	}

	public static void SetDoorLocked(SystemTypes room, bool locked)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		_lockedDoors[room] = locked;
		if ((Object)(object)ShipStatus.Instance == (Object)null)
		{
			return;
		}
		Il2CppReferenceArray<OpenableDoor> allDoors = ShipStatus.Instance.AllDoors;
		if (allDoors == null)
		{
			return;
		}
		if (locked)
		{
			SystemManager.CloseDoorsOfType(room);
			_lastDoorCloseRpcTime[room] = Time.realtimeSinceStartup;
			return;
		}
		for (int i = 0; i < ((Il2CppArrayBase<OpenableDoor>)(object)allDoors).Length; i++)
		{
			if ((Object)(object)((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i] != (Object)null && ((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].Room == room && !((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].IsOpen)
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)16, (byte)(((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].Id | 0x40));
			}
		}
		_lastDoorCloseRpcTime.Remove(room);
	}

	public static bool IsDoorLocked(SystemTypes room)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		bool value;
		return _lockedDoors.TryGetValue(room, out value) && value;
	}

	public static void UpdateLockedDoors()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ShipStatus.Instance == (Object)null || _lockedDoors.Count == 0)
		{
			return;
		}
		Il2CppReferenceArray<OpenableDoor> allDoors = ShipStatus.Instance.AllDoors;
		if (allDoors == null)
		{
			return;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<SystemTypes, bool> lockedDoor in _lockedDoors)
		{
			if (!lockedDoor.Value)
			{
				continue;
			}
			bool flag = false;
			for (int i = 0; i < ((Il2CppArrayBase<OpenableDoor>)(object)allDoors).Length; i++)
			{
				if ((Object)(object)((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i] != (Object)null && ((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].Room == lockedDoor.Key && ((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[i].IsOpen)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (_lastDoorCloseRpcTime.TryGetValue(lockedDoor.Key, out var value) && realtimeSinceStartup - value < 2f)
			{
				for (int j = 0; j < ((Il2CppArrayBase<OpenableDoor>)(object)allDoors).Length; j++)
				{
					if ((Object)(object)((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[j] != (Object)null && ((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[j].Room == lockedDoor.Key && ((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[j].IsOpen)
					{
						((SomeKindaDoor)((Il2CppArrayBase<OpenableDoor>)(object)allDoors)[j]).SetDoorway(false);
					}
				}
			}
			else
			{
				SystemManager.CloseDoorsOfType(lockedDoor.Key);
				_lastDoorCloseRpcTime[lockedDoor.Key] = realtimeSinceStartup;
			}
		}
	}

	public static void ResetDoorLocks()
	{
		_lockedDoors.Clear();
		_lastDoorCloseRpcTime.Clear();
		_cachedMapDoors = null;
		_lastMapId = -1;
	}

	private static bool HasSystem(SystemTypes system)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ShipStatus.Instance == (Object)null)
		{
			return false;
		}
		return ShipStatus.Instance.Systems.ContainsKey(system);
	}

	public static void TriggerReactorMeltdown()
	{
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			if (HasSystem((SystemTypes)58))
			{
				TriggerSabotage((SystemTypes)58, "Crash Course sabotage triggered!", 128);
			}
			else if (HasSystem((SystemTypes)21))
			{
				TriggerSabotage((SystemTypes)21, "Seismic Stabilizers sabotage triggered!", 128);
			}
			else if (HasSystem((SystemTypes)3))
			{
				TriggerSabotage((SystemTypes)3, "Reactor sabotage triggered!", 128);
			}
			else
			{
				NotifyUtils.Warning("Reactor sabotage not available on this map.");
			}
		}
	}

	public static void TriggerOxygenDepletion()
	{
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			if (HasSystem((SystemTypes)8))
			{
				TriggerSabotage((SystemTypes)8, "O2 Depletion sabotage triggered!", 128);
			}
			else
			{
				NotifyUtils.Warning("O2 sabotage not available on this map.");
			}
		}
	}

	public static void TriggerLightsOut()
	{
		if (!InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			return;
		}
		if (!HasSystem((SystemTypes)7))
		{
			NotifyUtils.Warning("Lights sabotage not available on this map.");
			return;
		}
		byte b = 4;
		for (int i = 0; i < 5; i++)
		{
			if (UnityEngine.Random.value > 0.5f)
			{
				b |= (byte)(1 << i);
			}
		}
		TriggerSabotage((SystemTypes)7, "Lights Out sabotage triggered!", (byte)(b | 0x80));
	}

	public static void TriggerComms()
	{
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			if (HasSystem((SystemTypes)14))
			{
				TriggerSabotage((SystemTypes)14, "Comms sabotage triggered!", 128);
			}
			else
			{
				NotifyUtils.Warning("Comms sabotage not available on this map.");
			}
		}
	}

	public static void TriggerMushroomMixup()
	{
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			if (HasSystem((SystemTypes)57))
			{
				TriggerSabotage((SystemTypes)57, "Mushroom Mixup sabotage triggered!", 1);
			}
			else
			{
				NotifyUtils.Warning("Mushroom Mixup not available on this map.");
			}
		}
	}

	public static void TriggerAllSabotages()
	{
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			int num = 0;
			if (HasSystem((SystemTypes)7))
			{
				TriggerLightsOut();
				num++;
			}
			if (HasSystem((SystemTypes)14))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)14, (byte)128);
				num++;
			}
			if (HasSystem((SystemTypes)58))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)58, (byte)128);
				num++;
			}
			else if (HasSystem((SystemTypes)21))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)21, (byte)128);
				num++;
			}
			else if (HasSystem((SystemTypes)3))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)3, (byte)128);
				num++;
			}
			if (HasSystem((SystemTypes)8))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)8, (byte)128);
				num++;
			}
			if (HasSystem((SystemTypes)57))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)57, (byte)1);
				num++;
			}
			NotifyUtils.Sabotage($"{num} sabotages triggered!");
		}
	}

	public static void RepairAllSabotages()
	{
		if (!InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			return;
		}
		try
		{
			if (HasSystem((SystemTypes)3))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)3, (byte)16);
			}
			if (HasSystem((SystemTypes)21))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)21, (byte)16);
			}
			if (HasSystem((SystemTypes)58))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)58, (byte)16);
			}
			if (HasSystem((SystemTypes)8))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)8, (byte)16);
			}
			if (HasSystem((SystemTypes)7))
			{
				RepairLights();
			}
			if (HasSystem((SystemTypes)14))
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)14, (byte)16);
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)14, (byte)0);
			}
			NotifyUtils.Repair("All sabotages repaired!");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[SabotageService] Error repairing sabotages: {value}"));
			NotifyUtils.Error("Failed to repair some sabotages.");
		}
	}

	public static void TriggerSabotage(SystemTypes system, string feedbackMsg = null, byte value = 128)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			ShipStatus.Instance.RpcUpdateSystem(system, value);
			if (!string.IsNullOrEmpty(feedbackMsg))
			{
				NotifyUtils.Sabotage(feedbackMsg);
			}
		}
	}

	public static void ToggleAllDoors(bool close = true)
	{
		if (close)
		{
			SystemTypes[] validDoorSystems = ValidDoorSystems;
			for (int i = 0; i < validDoorSystems.Length; i++)
			{
				SystemManager.CloseDoorsOfType(validDoorSystems[i]);
			}
			NotifyUtils.Door("All main doors closed!");
		}
	}

	public static void RepairLights()
	{
		if (!InteropFix.Cast((Object)(object)ShipStatus.Instance) || !HasSystem((SystemTypes)7))
		{
			return;
		}
		try
		{
			ISystemType val = ShipStatus.Instance.Systems[(SystemTypes)7];
			if (val == null)
			{
				return;
			}
			SwitchSystem val2 = ((Il2CppObjectBase)val).TryCast<SwitchSystem>();
			if (val2 == null)
			{
				return;
			}
			byte actualSwitches = val2.ActualSwitches;
			byte expectedSwitches = val2.ExpectedSwitches;
			byte b = (byte)(actualSwitches ^ expectedSwitches);
			if (b == 0)
			{
				NotifyUtils.Info("Lights are already repaired!");
				return;
			}
			for (byte b2 = 0; b2 < 5; b2++)
			{
				if ((b & (1 << (int)b2)) != 0)
				{
					ShipStatus.Instance.RpcUpdateSystem((SystemTypes)7, b2);
				}
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[SabotageService] Error repairing lights: {value}"));
			for (byte b3 = 0; b3 < 5; b3++)
			{
				ShipStatus.Instance.RpcUpdateSystem((SystemTypes)7, b3);
			}
		}
	}

	static SabotageService()
	{
		ValidDoorSystems = new SystemTypes[7]
		{
			(SystemTypes)7,
			(SystemTypes)10,
			(SystemTypes)11,
			(SystemTypes)1,
			(SystemTypes)2,
			(SystemTypes)4,
			(SystemTypes)13
		};
		_lockedDoors = new Dictionary<SystemTypes, bool>();
		_cachedMapDoors = null;
		_lastMapId = -1;
		_lastDoorCloseRpcTime = new Dictionary<SystemTypes, float>();
		DoorRoomNames = new Dictionary<SystemTypes, string>
		{
			{
				(SystemTypes)7,
				"Electrical"
			},
			{
				(SystemTypes)10,
				"MedBay"
			},
			{
				(SystemTypes)11,
				"Security"
			},
			{
				(SystemTypes)1,
				"Storage"
			},
			{
				(SystemTypes)2,
				"Cafeteria"
			},
			{
				(SystemTypes)4,
				"Upper Engine"
			},
			{
				(SystemTypes)13,
				"Lower Engine"
			},
			{
				(SystemTypes)3,
				"Reactor"
			},
			{
				(SystemTypes)21,
				"Laboratory"
			},
			{
				(SystemTypes)18,
				"Decontam"
			},
			{
				(SystemTypes)26,
				"Decontam 2"
			},
			{
				(SystemTypes)14,
				"Comms"
			},
			{
				(SystemTypes)6,
				"Admin"
			},
			{
				(SystemTypes)23,
				"Office"
			},
			{
				(SystemTypes)12,
				"Weapons"
			},
			{
				(SystemTypes)5,
				"Navigation"
			},
			{
				(SystemTypes)9,
				"Shields"
			},
			{
				(SystemTypes)39,
				"Engine"
			},
			{
				(SystemTypes)28,
				"Specimens"
			},
			{
				(SystemTypes)29,
				"Boiler Room"
			},
			{
				(SystemTypes)40,
				"Brig"
			},
			{
				(SystemTypes)33,
				"Kitchen"
			},
			{
				(SystemTypes)42,
				"Records"
			},
			{
				(SystemTypes)43,
				"Lounge"
			},
			{
				(SystemTypes)45,
				"Main Hall"
			},
			{
				(SystemTypes)46,
				"Medical"
			},
			{
				(SystemTypes)44,
				"Gap Room"
			},
			{
				(SystemTypes)0,
				"Hallway"
			},
			{
				(SystemTypes)30,
				"Vault Room"
			},
			{
				(SystemTypes)37,
				"Ventilation"
			},
			{
				(SystemTypes)31,
				"Cockpit"
			},
			{
				(SystemTypes)32,
				"Armory"
			},
			{
				(SystemTypes)41,
				"Meeting Room"
			},
			{
				(SystemTypes)34,
				"Viewing Deck"
			},
			{
				(SystemTypes)56,
				"Sleeping Quarters"
			}
		};
	}
}







