using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class ImpostorForcer
{
	[HarmonyPatch(typeof(RoleManager), "SelectRoles")]
	public static class RoleSelectionPatch
	{
		public static bool Prefix(RoleManager __instance)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
				{
					return true;
				}
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)localPlayer == (Object)null)
				{
					return true;
				}
				if (PreGameRoleAssignments.Count > 0)
				{
					AssignPreGameRolesAsHost();
					return false;
				}
				if (RoleOverrideEnabled)
				{
					AssignRolesAsHostCustom(localPlayer, SelectedRoleForHost);
					return false;
				}
				if (AlwaysImpostorAsHostEnabled)
				{
					AssignRolesAsHost(localPlayer);
					return false;
				}
				return true;
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[RoleSelectionPatch] Error: {value}"));
				return true;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
	public static class HandleRpcPatch
	{
		public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
		{
			if (callId != byte.MaxValue)
			{
				return;
			}
			try
			{
				byte b = reader.ReadByte();
				byte b2 = reader.ReadByte();
				reader.ReadUInt64();
				reader.ReadInt32();
				InteropFix.Cast((Il2CppArrayBase<byte>)(object)reader.ReadBytes(8));
				reader.ReadByte();
				reader.ReadByte();
				if ((Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer && b == PlayerControl.LocalPlayer.PlayerId)
				{
					Debug.Log(InteropFix.Cast($"[HandleRpcPatch] Recebido CUSTOM_RPC para o jogador local. Atualizando papel localmente para {b2}."));
					UpdateRoleLocally(PlayerControl.LocalPlayer, (RoleTypes)b2);
				}
				else
				{
					Debug.LogWarning(InteropFix.Cast($"[HandleRpcPatch] Recebido CUSTOM_RPC para outro jogador (PID: {b}). Ignorando."));
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HandleRpcPatch] Error processing CUSTOM_RPC: {value}"));
			}
		}
	}

	public static class ImpostorUtils
	{
		public static List<NetworkedPlayerInfo> GetImpostorsManual()
		{
			List<NetworkedPlayerInfo> list = new List<NetworkedPlayerInfo>();
			FieldInfo field = typeof(GameData).GetField("allPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null && field.GetValue(GameData.Instance) is IEnumerable enumerable)
			{
				foreach (object item in enumerable)
				{
					NetworkedPlayerInfo val = (NetworkedPlayerInfo)((item is NetworkedPlayerInfo) ? item : null);
					if ((Object)(object)val != (Object)null && (Object)(object)val.Role != (Object)null && val.Role.IsImpostor)
					{
						list.Add(val);
					}
				}
			}
			return list;
		}
	}

	public const byte CUSTOM_RPC = byte.MaxValue;

	internal static readonly Dictionary<byte, RoleTypes> PreGameRoleAssignments = new Dictionary<byte, RoleTypes>();

	private static readonly System.Random SharedRandom = new System.Random();

	private static readonly Dictionary<byte, DateTime> lastAttempts = new Dictionary<byte, DateTime>();

	private static readonly TimeSpan ATTEMPT_COOLDOWN = TimeSpan.FromSeconds(1.5);

	private static PlayerControl selectedPlayer = null;

	private static bool enabled = false;

	private static GameObject selectionIndicator;

	private static LineRenderer selectionLine;

	private const int circleSegments = 32;

	private static readonly Color circleColor = Color.yellow;

	public static bool AlwaysImpostorAsHostEnabled { get; private set; } = false;

	public static bool RoleOverrideEnabled { get; private set; } = false;

	public static RoleTypes SelectedRoleForHost { get; private set; } = (RoleTypes)1;

	internal static void SetAlwaysImpostorAsHost(bool enabled)
	{
		AlwaysImpostorAsHostEnabled = false;
		Debug.Log(InteropFix.Cast("[ImpostorForcer] Always Impostor (Host) is deprecated. Use Role Override (Host)."));
	}

	internal static void SetRoleOverrideEnabled(bool enabled)
	{
		RoleOverrideEnabled = enabled;
		Debug.Log(InteropFix.Cast("[ImpostorForcer] Role Override (Host) " + (enabled ? "ON" : "OFF")));
	}

	internal static void SetSelectedRoleForHost(RoleTypes role)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		if (((int)role == 18 || (int)role == 9) && (!ModKeyValidator.IsPremium || !ModKeyValidator.V()))
		{
			NotifyUtils.Warning($"★ {role} is PREMIUM ONLY! Using Impostor.");
			role = (RoleTypes)1;
		}
		SelectedRoleForHost = role;
		Debug.Log(InteropFix.Cast($"[ImpostorForcer] Selected role for Host: {role}"));
		if ((Object)(object)PlayerControl.LocalPlayer != (Object)null && (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			PreGameRoleAssignments[PlayerControl.LocalPlayer.PlayerId] = role;
			Debug.Log(InteropFix.Cast($"[ImpostorForcer] Pre-assign synced for Host: {role}"));
		}
	}

	public static RoleTypes[] GetSupportedRoles()
	{
		return new RoleTypes[11]
		{
			(RoleTypes)1,
			(RoleTypes)0,
			(RoleTypes)5,
			(RoleTypes)2,
			(RoleTypes)3,
			(RoleTypes)4,
			(RoleTypes)6,
			(RoleTypes)7,
			(RoleTypes)8,
			(RoleTypes)9,
			(RoleTypes)18
		};
	}

	internal static void SetPreGameRoleForPlayer(PlayerControl player, RoleTypes role)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)player == (Object)null || (Object)(object)player.Data == (Object)null)
		{
			return;
		}
		if (((int)role == 18 || (int)role == 9) && (!ModKeyValidator.IsPremium || !ModKeyValidator.V()))
		{
			NotifyUtils.Warning($"★ {role} is PREMIUM ONLY!");
			role = (RoleTypes)1;
		}
		if (PreGameRoleAssignments.TryGetValue(player.PlayerId, out var value) && value == role)
		{
			ClearPreGameRoleForPlayer(player.PlayerId);
			Debug.Log(InteropFix.Cast($"[ImpostorForcer] Pre-assignment REMOVED (toggle): {player.Data.PlayerName} had {role}"));
			return;
		}
		PreGameRoleAssignments[player.PlayerId] = role;
		Debug.Log(InteropFix.Cast($"[ImpostorForcer] Pre-assignment: {player.Data.PlayerName} -> {role}"));
		if ((Object)(object)player == (Object)(object)PlayerControl.LocalPlayer && (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			SelectedRoleForHost = role;
			Debug.Log(InteropFix.Cast($"[ImpostorForcer] Host Override synced: {role}"));
		}
	}

	internal static void ClearPreGameRoleForPlayer(byte playerId)
	{
		if (PreGameRoleAssignments.Remove(playerId))
		{
			Debug.Log(InteropFix.Cast($"[ImpostorForcer] Pre-assignment removed for PlayerId {playerId}"));
		}
	}

	internal static void ClearAllPreGameRoleAssignments()
	{
		PreGameRoleAssignments.Clear();
		Debug.Log(InteropFix.Cast("[ImpostorForcer] All pre-assignments cleared."));
	}

	private static bool IsTamperingEnvironment()
	{
		try
		{
			if (Debugger.IsAttached)
			{
				return true;
			}
			string[] array = new string[7] { "dnspy", "ilspy", "x64dbg", "cheatengine", "ida64", "ida", "ghidra" };
			foreach (string processName in array)
			{
				try
				{
					if (Process.GetProcessesByName(processName).Length != 0)
					{
						return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static void TrySetLocalPlayerAsImpostor()
	{
		try
		{
			if (ValidateGameState() && !IsOnCooldown(PlayerControl.LocalPlayer.PlayerId))
			{
				SetAttemptTimestamp(PlayerControl.LocalPlayer.PlayerId);
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if (((InnerNetClient)AmongUsClient.Instance).AmHost)
				{
					Debug.Log(InteropFix.Cast("[ImpostorForcer] Attempting to set as Impostor (Host Mode)."));
					AssignRolesAsHost(localPlayer);
				}
				else
				{
					Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] Attempting to set as Impostor (Client Mode - Bypass). This will likely only have local effect."));
					ForceImpostorBypassClientSide(localPlayer);
				}
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ImpostorForcer] Error in TrySetLocalPlayerAsImpostor: {value}"));
		}
	}

	private static void AssignRolesAsHost(PlayerControl targetImpostor)
	{
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost || (Object)(object)targetImpostor == (Object)null)
		{
			return;
		}
		Il2CppArrayBase<PlayerControl> val = PlayerControl.AllPlayerControls.ToArray();
		List<PlayerControl> source = ((IEnumerable<PlayerControl>)val).Where((PlayerControl p) => (Object)(object)p != (Object)null && (Object)(object)p.Data != (Object)null && p.PlayerId != targetImpostor.PlayerId).ToList();
		int maxImpostorsForPlayerCount = GetMaxImpostorsForPlayerCount(val.Length);
		int num = 1;
		num = ((GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) ? Mathf.Clamp(2, 1, maxImpostorsForPlayerCount) : Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, maxImpostorsForPlayerCount));
		List<PlayerControl> list = new List<PlayerControl> { targetImpostor };
		System.Random rng = new System.Random();
		source = source.OrderBy((PlayerControl x) => rng.Next()).ToList();
		for (int num2 = 0; num2 < num - 1 && num2 < source.Count; num2++)
		{
			list.Add(source[num2]);
		}
		foreach (PlayerControl player in val)
		{
			if (!((Object)(object)player == (Object)null) && !((Object)(object)player.Data == (Object)null))
			{
				if (list.Any((PlayerControl p) => p.PlayerId == player.PlayerId))
				{
					player.RpcSetRole((RoleTypes)1, false);
					UpdateRoleLocally(player, (RoleTypes)1);
				}
				else
				{
					player.RpcSetRole((RoleTypes)0, false);
					UpdateRoleLocally(player, (RoleTypes)0);
				}
			}
		}
		NetworkedPlayerInfo data = targetImpostor.Data;
		NotifyUtils.Success((((data != null) ? data.PlayerName : null) ?? "You") + " is now Impostor (Host)");
	}

	private static bool IsImpostorTeam(RoleTypes role)
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

	private static int GetMaxImpostorsForPlayerCount(int playerCount)
	{
		if (playerCount <= 5)
		{
			return 1;
		}
		if (playerCount <= 8)
		{
			return 2;
		}
		return 3;
	}

	private static void AssignRolesAsHostCustom(PlayerControl host, RoleTypes selectedRole)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost || (Object)(object)host == (Object)null)
		{
			return;
		}
		Il2CppArrayBase<PlayerControl> val = PlayerControl.AllPlayerControls.ToArray();
		List<PlayerControl> source = ((IEnumerable<PlayerControl>)val).Where((PlayerControl p) => (Object)(object)p != (Object)null && (Object)(object)p.Data != (Object)null && p.PlayerId != host.PlayerId).ToList();
		int maxImpostorsForPlayerCount = GetMaxImpostorsForPlayerCount(val.Length);
		int num = 1;
		num = ((GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) ? Mathf.Clamp(2, 1, maxImpostorsForPlayerCount) : Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, maxImpostorsForPlayerCount));
		List<PlayerControl> list = new List<PlayerControl>();
		if (IsImpostorTeam(selectedRole))
		{
			list.Add(host);
		}
		System.Random rng = new System.Random();
		source = source.OrderBy((PlayerControl x) => rng.Next()).ToList();
		for (int num2 = 0; num2 < num - list.Count && num2 < source.Count; num2++)
		{
			list.Add(source[num2]);
		}
		foreach (PlayerControl player in val)
		{
			if (!((Object)(object)player == (Object)null) && !((Object)(object)player.Data == (Object)null))
			{
				if (player.PlayerId == host.PlayerId)
				{
					player.RpcSetRole(selectedRole, false);
					UpdateRoleLocally(player, selectedRole);
				}
				else if (list.Any((PlayerControl p) => p.PlayerId == player.PlayerId))
				{
					player.RpcSetRole((RoleTypes)1, false);
					UpdateRoleLocally(player, (RoleTypes)1);
				}
				else
				{
					player.RpcSetRole((RoleTypes)0, false);
					UpdateRoleLocally(player, (RoleTypes)0);
				}
			}
		}
		NotifyUtils.Info($"Host role: {selectedRole} | Total impostors: {list.Count}");
	}

	internal static void HostApplySelectedRoleNow()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] HostApplySelectedRoleNow: not host."));
			return;
		}
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if (!((Object)(object)localPlayer == (Object)null))
		{
			AssignRolesAsHostCustom(localPlayer, SelectedRoleForHost);
		}
	}

	private static void AssignPreGameRolesAsHost()
	{
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] AssignPreGameRolesAsHost: not host."));
			return;
		}
		if (PreGameRoleAssignments.Count == 0)
		{
			Debug.Log(InteropFix.Cast("[ImpostorForcer] AssignPreGameRolesAsHost: no pre-assignments."));
			return;
		}
		Il2CppArrayBase<PlayerControl> val = PlayerControl.AllPlayerControls.ToArray();
		if (val == null || val.Length == 0)
		{
			return;
		}
		int maxImpostorsForPlayerCount = GetMaxImpostorsForPlayerCount(val.Length);
		int num = 1;
		num = ((GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) ? Mathf.Clamp(2, 1, maxImpostorsForPlayerCount) : Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, maxImpostorsForPlayerCount));
		List<PlayerControl> impostors = new List<PlayerControl>();
		List<PlayerControl> list = new List<PlayerControl>();
		foreach (KeyValuePair<byte, RoleTypes> kvp in PreGameRoleAssignments)
		{
			PlayerControl p = ((IEnumerable<PlayerControl>)val).FirstOrDefault((System.Func<PlayerControl, bool>)((PlayerControl ap) => (Object)(object)ap != (Object)null && ap.PlayerId == kvp.Key));
			if (!((Object)(object)p == (Object)null) && !((Object)(object)p.Data == (Object)null))
			{
				list.Add(p);
				if (IsImpostorTeam(kvp.Value) && impostors.All((PlayerControl x) => x.PlayerId != p.PlayerId))
				{
					impostors.Add(p);
				}
			}
		}
		if (impostors.Count > num)
		{
			Debug.LogWarning(InteropFix.Cast($"[ImpostorForcer] Pre-assignments have {impostors.Count} impostors, exceeding limit {num}. Some will be reverted to crewmate."));
			impostors = impostors.Take(num).ToList();
		}
		if (impostors.Count < num)
		{
			System.Random rng = new System.Random();
			foreach (PlayerControl item in (from _ in (IEnumerable<PlayerControl>)val
				where (Object)(object)_ != (Object)null && (Object)(object)_.Data != (Object)null && !impostors.Any((PlayerControl i) => i.PlayerId == _.PlayerId)
				orderby rng.Next()
				select _).ToList())
			{
				if (impostors.Count >= num)
				{
					break;
				}
				if (!PreGameRoleAssignments.TryGetValue(item.PlayerId, out var value) || IsImpostorTeam(value))
				{
					impostors.Add(item);
				}
			}
		}
		foreach (PlayerControl player in val)
		{
			if (!((Object)(object)player == (Object)null) && !((Object)(object)player.Data == (Object)null))
			{
				RoleTypes value2;
				RoleTypes val2 = ((!PreGameRoleAssignments.TryGetValue(player.PlayerId, out value2)) ? ((RoleTypes)(impostors.Any((PlayerControl i) => i.PlayerId == player.PlayerId) ? 1 : 0)) : ((!IsImpostorTeam(value2)) ? value2 : ((RoleTypes)(impostors.Any((PlayerControl i) => i.PlayerId == player.PlayerId) ? ((int)value2) : 0))));
				player.RpcSetRole(val2, false);
				UpdateRoleLocally(player, val2);
			}
		}
		NotifyUtils.Success("Pre-assigned roles applied. Impostors: " + string.Join(", ", impostors.Select(delegate(PlayerControl i)
		{
			NetworkedPlayerInfo data = i.Data;
			return (data == null) ? null : data.PlayerName;
		})));
		PreGameRoleAssignments.Clear();
		Debug.Log(InteropFix.Cast("[ImpostorForcer] Pre-assignments cleared after applying (one-shot per game start)."));
	}

	internal static void HostForceImpostorNow(PlayerControl targetImpostor)
	{
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] HostForceImpostorNow: not host."));
		}
		else if (!((Object)(object)targetImpostor == (Object)null))
		{
			AssignRolesAsHost(targetImpostor);
		}
	}

	internal static void TrySetLocalPlayerRole(RoleTypes role)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!ValidateGameState())
			{
				return;
			}
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			if (!((Object)(object)localPlayer == (Object)null))
			{
				if (((InnerNetClient)AmongUsClient.Instance).AmHost)
				{
					localPlayer.RpcSetRole(role, false);
					UpdateRoleLocally(localPlayer, role);
				}
				else
				{
					UpdateRoleLocally(localPlayer, role);
					Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] TrySetLocalPlayerRole executed as client: effect likely local only."));
				}
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ImpostorForcer] Error in TrySetLocalPlayerRole: {value}"));
		}
	}

	private static void ForceImpostorBypassClientSide(PlayerControl localPlayer)
	{
		try
		{
			if (!((Object)(object)localPlayer == (Object)null) && !((Object)(object)AmongUsClient.Instance == (Object)null))
			{
				int num = new System.Random().Next(100000, 999999);
				long ticks = DateTime.UtcNow.Ticks;
				byte[] array = new byte[8];
				new System.Random().NextBytes(array);
				Debug.Log(InteropFix.Cast($"[ImpostorForcer] Sending CUSTOM_RPC ({255}) to attempt client bypass."));
				MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)localPlayer).NetId, byte.MaxValue, (SendOption)1, -1);
				val.Write(localPlayer.PlayerId);
				val.Write((byte)1);
				val.Write((float)ticks);
				val.Write(num);
				val.Write(InteropFix.Cast(array));
				val.Write((byte)2);
				val.Write((byte)3);
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
				Debug.Log(InteropFix.Cast("[ImpostorForcer] Updating role locally via Reflection."));
				UpdateRoleLocally(localPlayer, (RoleTypes)1);
				NotifyUtils.Warning("Attempted to force Impostor (Client - likely local effect)");
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ImpostorForcer] Error in ForceImpostorBypassClientSide: {value}"));
		}
	}

	public static void UpdateRoleLocally(PlayerControl player, RoleTypes roleType)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)player == (Object)null || (Object)(object)player.Data == (Object)null)
			{
				return;
			}
			Debug.Log(InteropFix.Cast($"[ImpostorForcer] Updating locally {player.Data.PlayerName} to {roleType}"));
			player.Data.RoleType = roleType;
			player.Data.IsDead = false;
			RoleBehaviour component = ((Component)player).GetComponent<RoleBehaviour>();
			if ((Object)(object)component != (Object)null)
			{
				try
				{
					FieldInfo field = typeof(RoleBehaviour).GetField("role", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(component, roleType);
					}
					else
					{
						Debug.LogWarning(InteropFix.Cast("Field 'role' not found in RoleBehaviour."));
					}
					FieldInfo fieldInfo = typeof(RoleBehaviour).GetField("teamType", BindingFlags.Instance | BindingFlags.NonPublic) ?? typeof(RoleBehaviour).GetField("<TeamType>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
					if (fieldInfo != null)
					{
						fieldInfo.SetValue(component, (object)(RoleTeamTypes)(IsImpostorTeam(roleType) ? 1 : 0));
					}
					else
					{
						Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] TeamType field not found in RoleBehaviour"));
					}
				}
				catch (Exception ex)
				{
					Debug.LogError(InteropFix.Cast("[ImpostorForcer] Error reflecting on RoleBehaviour: " + ex.Message));
				}
			}
			else
			{
				Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] RoleBehaviour not found on player " + player.Data.PlayerName));
			}
			GameData instance = GameData.Instance;
			NetworkedPlayerInfo val = ((instance != null) ? instance.GetPlayerById(player.PlayerId) : null);
			if ((Object)(object)val != (Object)null)
			{
				val.RoleType = roleType;
				if ((Object)(object)val.Role != (Object)null)
				{
					val.Role.TeamType = (RoleTeamTypes)(IsImpostorTeam(roleType) ? 1 : 0);
					val.Role.Role = roleType;
				}
				else
				{
					Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] pInfo.Role is null for " + player.Data.PlayerName + ", could not update TeamType/Role in RoleInfo."));
				}
				val.IsDead = false;
				val.MarkDirty();
			}
			else
			{
				Debug.LogWarning(InteropFix.Cast($"[ImpostorForcer] NetworkedPlayerInfo not found for PlayerId {player.PlayerId}"));
			}
			Debug.Log(InteropFix.Cast("[ImpostorForcer] Local update for " + player.Data.PlayerName + " completed."));
		}
		catch (Exception value)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 2);
			defaultInterpolatedStringHandler.AppendLiteral("[ImpostorForcer] Error in UpdateRoleLocally for ");
			object value2;
			if (player == null)
			{
				value2 = null;
			}
			else
			{
				NetworkedPlayerInfo data = player.Data;
				value2 = ((data != null) ? data.PlayerName : null);
			}
			defaultInterpolatedStringHandler.AppendFormatted((string?)value2);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(value);
			Debug.LogError(InteropFix.Cast(defaultInterpolatedStringHandler.ToStringAndClear()));
		}
	}

	public static void SetEnabled(bool value)
	{
		enabled = value;
		if (!enabled)
		{
			selectedPlayer = null;
		}
		Debug.Log(InteropFix.Cast("[PlayerMover] Move players mode " + (enabled ? "ON" : "OFF")));
	}

	public static bool IsEnabled()
	{
		return enabled;
	}

	public static void Update()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if (!enabled || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			return;
		}
		if (Input.GetMouseButtonDown(1))
		{
			if ((Object)(object)Camera.main == (Object)null)
			{
				return;
			}
			Vector2 val = InteropFix.Cast(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			float num = float.MaxValue;
			selectedPlayer = null;
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (!((Object)(object)current.Data == (Object)null) && !current.Data.IsDead)
				{
					float num2 = Vector2.Distance(InteropFix.Cast(((Component)current).transform.position), val);
					if (num2 < num)
					{
						num = num2;
						selectedPlayer = current;
					}
				}
			}
			if ((Object)(object)selectedPlayer != (Object)null)
			{
				Debug.Log(InteropFix.Cast("[PlayerMover] Player " + selectedPlayer.Data.PlayerName + " selected!"));
			}
		}
		if (Input.GetMouseButtonUp(1) && (Object)(object)selectedPlayer != (Object)null)
		{
			Debug.Log(InteropFix.Cast("[PlayerMover] Player released."));
			selectedPlayer = null;
		}
		if ((Object)(object)selectedPlayer != (Object)null && Input.GetMouseButton(0))
		{
			if ((Object)(object)Camera.main == (Object)null)
			{
				return;
			}
			Vector2 val2 = InteropFix.Cast(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			Vector2 val3 = Vector2.Lerp(InteropFix.Cast(((Component)selectedPlayer).transform.position), val2, 1f);
			if ((Object)(object)selectedPlayer.NetTransform != (Object)null)
			{
				selectedPlayer.NetTransform.RpcSnapTo(val3);
			}
		}
		if ((Object)(object)selectedPlayer != (Object)null)
		{
			if (Input.GetKeyDown((KeyCode)107))
			{
				selectedPlayer.Die((DeathReason)1, true);
				Debug.Log(InteropFix.Cast("[PlayerMover] " + selectedPlayer.Data.PlayerName + " was killed!"));
			}
			if (Input.GetKeyDown((KeyCode)114))
			{
				selectedPlayer.Revive();
				Debug.Log(InteropFix.Cast("[PlayerMover] " + selectedPlayer.Data.PlayerName + " was revived!"));
			}
			if (Input.GetKeyDown((KeyCode)101))
			{
				selectedPlayer.Exiled();
				Debug.Log(InteropFix.Cast("[PlayerMover] " + selectedPlayer.Data.PlayerName + " was exiled!"));
			}
		}
		UpdateSelectionIndicator();
	}

	private static void UpdateSelectionIndicator()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)selectionIndicator == (Object)null)
		{
			selectionIndicator = new GameObject("PlayerSelectionIndicator");
			selectionLine = selectionIndicator.AddComponent<LineRenderer>();
			selectionLine.positionCount = 33;
			selectionLine.loop = true;
			selectionLine.widthMultiplier = 0.05f;
			selectionLine.useWorldSpace = true;
			Material val = new Material(Shader.Find("Sprites/Default"));
			val.color = circleColor;
			((Renderer)selectionLine).material = val;
			LineRenderer obj = selectionLine;
			Color startColor = (selectionLine.endColor = circleColor);
			obj.startColor = startColor;
		}
		if ((Object)(object)selectedPlayer != (Object)null)
		{
			if (!selectionIndicator.activeSelf)
			{
				selectionIndicator.SetActive(true);
			}
			Il2CppArrayBase<SpriteRenderer> componentsInChildren = ((Component)selectedPlayer).GetComponentsInChildren<SpriteRenderer>();
			float num = 0f;
			Vector3 position = ((Component)selectedPlayer).transform.position;
			foreach (SpriteRenderer item in componentsInChildren)
			{
				Bounds bounds = ((Renderer)item).bounds;
				Vector3 extents = bounds.extents;
				float magnitude = extents.magnitude;
				if (magnitude > num)
				{
					num = magnitude;
				}
			}
			float num2 = num + 0.05f;
			for (int i = 0; i <= 32; i++)
			{
				float num3 = (float)System.Math.PI * 2f * (float)i / 32f;
				float num4 = position.x + Mathf.Cos(num3) * num2;
				float num5 = position.y + Mathf.Sin(num3) * num2;
				selectionLine.SetPosition(i, new Vector3(num4, num5, position.z));
			}
		}
		else if (selectionIndicator.activeSelf)
		{
			selectionIndicator.SetActive(false);
		}
	}

	public static void ForceNameImpostorReveal()
	{
		MethodInfo method = typeof(PlayerControl).GetMethod("CmdCheckName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = current.Data;
				obj = ((data != null) ? data.Role : null);
			}
			if (!((Object)obj == (Object)null))
			{
				string specificRoleName = GameCheats.GetSpecificRoleName(current.Data.Role);
				string text = $"{specificRoleName}<size=0><{current.PlayerId}></size>";
				method?.Invoke(current, new object[1] { text });
			}
		}
	}

	public static bool ValidateGameState()
	{
		if ((Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).AmConnected && (Object)(object)PlayerControl.LocalPlayer != (Object)null && (Object)(object)PlayerControl.LocalPlayer.Data != (Object)null)
		{
			return (Object)(object)GameData.Instance != (Object)null;
		}
		return false;
	}

	public static bool IsOnCooldown(byte playerId)
	{
		if (!lastAttempts.TryGetValue(playerId, out var value))
		{
			return false;
		}
		bool num = DateTime.UtcNow - value < ATTEMPT_COOLDOWN;
		if (num)
		{
			Debug.LogWarning(InteropFix.Cast("[ImpostorForcer] Cooldown active. Please wait."));
		}
		return num;
	}

	public static void SetAttemptTimestamp(byte playerId)
	{
		lastAttempts[playerId] = DateTime.UtcNow;
	}

	public static int GetClientIdByPlayer(PlayerControl player)
	{
		if ((Object)(object)player == (Object)null || (Object)(object)player.Data == (Object)null)
		{
			return -1;
		}
		var enumerator = ((InnerNetClient)AmongUsClient.Instance).allClients.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ClientData current = enumerator.Current;
			if (current != null && current.PlayerName == player.Data.PlayerName)
			{
				return current.Id;
			}
		}
		return -1;
	}

	public static PlayerControl GetPlayerUnderMouse()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Camera.main == (Object)null)
		{
			return null;
		}
		Vector2 val = InteropFix.Cast(Camera.main.ScreenToWorldPoint(Input.mousePosition));
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null) && Vector2.Distance(InteropFix.Cast(((Component)current).transform.position), val) < 0.5f)
			{
				return current;
			}
		}
		return null;
	}
}








