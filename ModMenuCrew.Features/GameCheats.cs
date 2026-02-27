using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using AmongUs.GameOptions;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

using InnerNet;
using ModMenuCrew.UI.Managers;
using ModMenuCrew.UI.Menus;
using ModMenuCrew.UI.Styles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModMenuCrew.Features;

public static class GameCheats
{
	[HarmonyPatch(typeof(PhantomRole), "FixedUpdate")]
	public static class PhantomInfiniteDurationPatch
	{
		public static void Postfix(PhantomRole __instance)
		{
			if (CheatConfig.PhantomMode.Value && !((Object)(object)((__instance != null) ? ((RoleBehaviour)__instance).Player : null) == (Object)null) && ((InnerNetObject)((RoleBehaviour)__instance).Player).AmOwner && __instance.IsInvisible)
			{
				__instance.durationSecondsRemaining = 9999f;
			}
		}
	}

	[HarmonyPatch(typeof(PhantomRole), "IsValidTarget")]
	public static class PhantomKillWhileInvisiblePatch
	{
		public static bool Prefix(PhantomRole __instance, ref bool __result, NetworkedPlayerInfo target)
		{
			if (!CheatConfig.PhantomMode.Value)
			{
				return true;
			}
			if ((Object)(object)((__instance != null) ? ((RoleBehaviour)__instance).Player : null) == (Object)null || !((InnerNetObject)((RoleBehaviour)__instance).Player).AmOwner)
			{
				return true;
			}
			if (!__instance.IsInvisible)
			{
				return true;
			}
			if (!((Object)(object)target == (Object)null) && !target.Disconnected && !target.IsDead && target.PlayerId != ((RoleBehaviour)__instance).Player.PlayerId)
			{
				RoleBehaviour role = target.Role;
				if (role == null || !role.IsImpostor)
				{
					__result = true;
					return false;
				}
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
	public static class ImpostorForcer1Patch
	{
		public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
		{
			if (callId != 44 || !((Object)(object)AmongUsClient.Instance != (Object)null) || ((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				return;
			}
			byte playerId = reader.ReadByte();
			if (reader.ReadByte() == 1)
			{
				PlayerControl val = FindPlayerById(playerId);
				if ((Object)(object)val != (Object)null)
				{
					val.RpcSetRole((RoleTypes)1, false);
					LogCheat($"Player {playerId} forced to impostor.");
				}
			}
		}
	}

	public static class Impostor1Forcer
	{
		internal static void RequestImpostorRole()
		{
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null)
			{
				Debug.LogWarning(InteropFix.Cast("Local player not found."));
				return;
			}
			if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				Debug.LogWarning(InteropFix.Cast("You must be host to use this cheat."));
				return;
			}
			try
			{
				MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)44, (SendOption)1, -1);
				val.Write(PlayerControl.LocalPlayer.PlayerId);
				val.Write((byte)1);
				val.Write(GenerateFakeToken(PlayerControl.LocalPlayer.PlayerId));
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
				LogCheat("Impostor role requested with bypass.");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"Error in RequestImpostorRole: {value}"));
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
	public static class MurderBypassPatch
	{
		public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader, out PlayerControl __state)
		{
			__state = null;
			if (callId != 12)
			{
				return true;
			}
			try
			{
				int position = reader.Position;
				try
				{
					uint num = reader.ReadPackedUInt32();
					var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
					while (enumerator.MoveNext())
					{
						PlayerControl current = enumerator.Current;
						if ((Object)(object)current != (Object)null && ((InnerNetObject)current).NetId == num)
						{
							__state = current;
							break;
						}
					}
				}
				catch
				{
				}
				finally
				{
					reader.Position = position;
				}
			}
			catch
			{
			}
			return true;
		}

		public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, PlayerControl __state)
		{
			if (callId != 12 || (Object)(object)__state == (Object)null)
			{
				return;
			}
			if ((Object)(object)__state.Data != (Object)null && __state.Data.IsDead)
			{
				if ((Object)(object)__state.cosmetics != (Object)null)
				{
					((Component)__state).gameObject.layer = LayerMask.NameToLayer("Ghost");
				}
				if (((InnerNetObject)__state).AmOwner && (Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null)
				{
					if (InteropFix.Cast((Object)(object)Minigame.Instance))
					{
						try
						{
							Minigame.Instance.Close();
						}
						catch
						{
						}
					}
					try
					{
						__state.cosmetics.SetNameMask(false);
					}
					catch
					{
					}
				}
				Debug.Log(InteropFix.Cast("[KillBypassPatch] Visuals applied for " + __state.Data.PlayerName + " (Confirmed Dead)."));
			}
			else
			{
				NetworkedPlayerInfo data = __state.Data;
				Debug.Log(InteropFix.Cast("[KillBypassPatch] Target " + ((data != null) ? data.PlayerName : null) + " did not die (Protected/Failed). Visuals skipped."));
			}
		}
	}

	[HarmonyPatch(typeof(FollowerCamera), "Update")]
	public static class ZoomOutPatch
	{
		private static void Postfix(FollowerCamera __instance)
		{
			try
			{
				if ((Object)(object)__instance == (Object)null)
				{
					return;
				}
				Camera component = ((Component)__instance).GetComponent<Camera>();
				if ((Object)(object)component == (Object)null)
				{
					return;
				}
				float visionMultiplier = CheatConfig.VisionMultiplier;
				if (visionMultiplier > 3.01f && !FreeCamEnabled && !_endGameActive)
				{
					if (component.orthographicSize < visionMultiplier)
					{
						_defaultOrthoSize = component.orthographicSize;
					}
					component.orthographicSize = visionMultiplier;
					HudManager instance = DestroyableSingleton<HudManager>.Instance;
					if ((Object)(object)((instance != null) ? instance.ShadowQuad : null) != (Object)null && ((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.activeSelf)
					{
						((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(false);
					}
					return;
				}
				if (component.orthographicSize > 4f)
				{
					component.orthographicSize = _defaultOrthoSize;
				}
				else
				{
					_defaultOrthoSize = component.orthographicSize;
				}
				HudManager instance2 = DestroyableSingleton<HudManager>.Instance;
				if ((Object)(object)((instance2 != null) ? instance2.ShadowQuad : null) != (Object)null && !((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.activeSelf)
				{
					CheatManager instance3 = CheatManager.Instance;
					if ((instance3 == null || !instance3.NoShadowsEnabled) && !FreeCamEnabled)
					{
						((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(true);
					}
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(Controller), "Update")]
	public static class ControllerCameraFixPatch
	{
		private static void Prefix(Controller __instance)
		{
			try
			{
				if ((CheatConfig.VisionMultiplier > 3.01f || FreeCamEnabled) && !_endGameActive)
				{
					HudManager instance = DestroyableSingleton<HudManager>.Instance;
					Camera val = ((instance != null) ? instance.UICamera : null);
					if ((Object)(object)val != (Object)null)
					{
						__instance.mainCam = val;
					}
				}
				else if ((Object)(object)__instance.mainCam != (Object)(object)Camera.main && (Object)(object)Camera.main != (Object)null)
				{
					__instance.mainCam = Camera.main;
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(ShipStatus), "CalculateLightRadius")]
	public static class SatelliteLightRadiusPatch
	{
		private static void Postfix(float __result)
		{
			try
			{
				float visionMultiplier = CheatConfig.VisionMultiplier;
				if (visionMultiplier > 3.01f && !FreeCamEnabled)
				{
					__result = Mathf.Max(__result, visionMultiplier);
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(AspectPosition), "AdjustPosition", new System.Type[] { })]
	public static class AspectPositionParentFixPatch
	{
		private static void Prefix(AspectPosition __instance)
		{
			try
			{
				if ((Object)(object)__instance.parentCam == (Object)null || (Object)(object)__instance.parentCam == (Object)(object)Camera.main)
				{
					HudManager instance = DestroyableSingleton<HudManager>.Instance;
					Camera val = ((instance != null) ? instance.UICamera : null);
					if ((Object)(object)val != (Object)null)
					{
						__instance.parentCam = val;
					}
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(Minigame), "Begin")]
	public static class MinigameBeginZoomFix
	{
		private static void Prefix()
		{
			if ((Object)(object)Camera.main != (Object)null && (CheatConfig.VisionMultiplier > 3.01f || freecamActive))
			{
				Camera.main.orthographicSize = _defaultOrthoSize;
			}
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "Start")]
	public static class MeetingHudStartZoomFix
	{
		private static void Prefix()
		{
			if ((Object)(object)Camera.main != (Object)null && (CheatConfig.VisionMultiplier > 3.01f || freecamActive))
			{
				Camera.main.orthographicSize = _defaultOrthoSize;
			}
		}
	}

	[HarmonyPatch(typeof(AmongUsClient), "OnGameEnd")]
	public static class ResetZoomOnGameEndPatch
	{
		private static void Postfix()
		{
			_endGameActive = true;
			if ((Object)(object)Camera.main != (Object)null && Camera.main.orthographicSize > 4f)
			{
				Camera.main.orthographicSize = _defaultOrthoSize;
			}
			if (FreeCamEnabled)
			{
				FreeCamEnabled = false;
			}
			RpcSyncValidator.Reset();
		}
	}

	private struct MapInfo
	{
		public float xOffset;

		public float yOffset;

		public float scale;

		public float minX;

		public float maxX;

		public float minY;

		public float maxY;

		public MapInfo(float xOff, float yOff, float sc, float mnX, float mxX, float mnY, float mxY)
		{
			xOffset = xOff;
			yOffset = yOff;
			scale = sc;
			minX = mnX;
			maxX = mxX;
			minY = mnY;
			maxY = mxY;
		}
	}

	private struct RadarPlayerData
	{
		public float x;

		public float y;

		public bool isImpostor;

		public bool isDead;

		public int colorId;

		public string name;

		public byte playerId;
	}

	private struct RadarDeadBodyData
	{
		public float x;

		public float y;

		public int colorId;

		public string name;
	}

	private struct RoomData
	{
		public float x;

		public float y;

		public float w;

		public float h;

		public int shapeType;

		public RoomData(float x, float y, float w, float h, int shape)
		{
			this.x = x;
			this.y = y;
			this.w = w;
			this.h = h;
			shapeType = shape;
		}

		public RoomData(float x, float y, float w, float h, bool octagon = false, bool corridor = false)
		{
			this.x = x;
			this.y = y;
			this.w = w;
			this.h = h;
			shapeType = (corridor ? 5 : (octagon ? 1 : 0));
		}
	}

	[HarmonyPatch(typeof(LogicOptions), "GetKillDistance")]
	public static class NoKillDistancePatch
	{
		public static bool Prefix(ref float __result)
		{
			if (!NoKillDistanceLimitEnabled)
			{
				return true;
			}
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			if ((Object)(object)((localPlayer != null) ? localPlayer.Data : null) == (Object)null)
			{
				return true;
			}
			if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor)
			{
				return true;
			}
			__result = float.MaxValue;
			return false;
		}
	}

	[HarmonyPatch(typeof(ImpostorRole), "FindClosestTarget")]
	public static class NoKillDistanceTargetPatch
	{
		public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			if (!NoKillDistanceLimitEnabled)
			{
				return true;
			}
			try
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.Data : null) == (Object)null || !localPlayer.Data.Role.IsImpostor)
				{
					return true;
				}
				Vector2 truePosition = localPlayer.GetTruePosition();
				PlayerControl val = null;
				float num = float.MaxValue;
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null) && !current.Data.IsDead && !current.Data.Disconnected && current.PlayerId != localPlayer.PlayerId && !((Object)(object)current.Data.Role == (Object)null) && current.Data.Role.CanBeKilled && !current.inVent && !current.inMovingPlat && current.Visible && ((Behaviour)current.Collider).enabled)
					{
						Vector2 truePosition2 = current.GetTruePosition();
						float num2 = Vector2.Distance(truePosition, truePosition2);
						if (num2 < num)
						{
							num = num2;
							val = current;
						}
					}
				}
				__result = val;
				return false;
			}
			catch
			{
				return true;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "FixedUpdate")]
	public static class SeeGhostsPatch
	{
		public static void Postfix(PlayerControl __instance)
		{
			if (!SeeGhostsEnabled || (Object)(object)__instance == (Object)null || (Object)(object)__instance.Data == (Object)null || !__instance.Data.IsDead)
			{
				return;
			}
			try
			{
				__instance.Visible = true;
				((Component)__instance).gameObject.layer = LayerMask.NameToLayer("Players");
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
	public static class GhostChatRpcPatch
	{
		public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
		{
			switch (callId)
			{
			case 15:
				HandleScannerRPC(__instance, callId, reader);
				break;
			case 13:
				if (!SeeDeadChatEnabled)
				{
					break;
				}
				try
				{
					if (!((Object)(object)PlayerControl.LocalPlayer == (Object)null) && !((Object)(object)PlayerControl.LocalPlayer.Data == (Object)null) && !PlayerControl.LocalPlayer.Data.IsDead && !((Object)(object)__instance == (Object)null) && !((Object)(object)__instance.Data == (Object)null) && __instance.Data.IsDead)
					{
						int position = reader.Position;
						reader.Position = 0;
						string chatText = reader.ReadString();
						reader.Position = position;
						ForceAddGhostChat(__instance, chatText);
					}
					break;
				}
				catch (Exception ex)
				{
					Debug.LogError(InteropFix.Cast("[GhostChat] Erro ao processar RPC: " + ex.Message));
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ChatController), "AddChat")]
	public static class ForceShowDeadChatPatch
	{
		private static bool _originalDeadState;

		private static bool _stateModified;

		public static void Prefix(ChatController __instance, PlayerControl sourcePlayer, string chatText, bool censor)
		{
			_stateModified = false;
			if (SeeDeadChatEnabled && !((Object)(object)((sourcePlayer != null) ? sourcePlayer.Data : null) == (Object)null) && sourcePlayer.Data.IsDead)
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if (!((Object)(object)((localPlayer != null) ? localPlayer.Data : null) == (Object)null) && !PlayerControl.LocalPlayer.Data.IsDead)
				{
					_originalDeadState = PlayerControl.LocalPlayer.Data.IsDead;
					PlayerControl.LocalPlayer.Data.IsDead = true;
					_stateModified = true;
				}
			}
		}

		public static void Postfix(ChatController __instance, PlayerControl sourcePlayer)
		{
			if (_stateModified)
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.Data : null) != (Object)null)
				{
					PlayerControl.LocalPlayer.Data.IsDead = _originalDeadState;
					_stateModified = false;
				}
			}
		}
	}

	[HarmonyPatch(typeof(HudManager), "Update")]
	public static class AlwaysShowChatPatch
	{
		public static Exception Finalizer(Exception __exception)
		{
			return null;
		}

		public static void Postfix(HudManager __instance)
		{
			if (!AlwaysShowChatEnabled)
			{
				return;
			}
			try
			{
				if (!InteropFix.Cast((Object)(object)__instance))
				{
					return;
				}
				ChatController chat = __instance.Chat;
				if (InteropFix.Cast((Object)(object)chat) && InteropFix.Cast((Object)(object)ShipStatus.Instance) && InteropFix.Cast((Object)(object)PlayerControl.LocalPlayer))
				{
					GameObject gameObject = ((Component)chat).gameObject;
					if ((Object)(object)gameObject != (Object)null && !gameObject.activeSelf)
					{
						gameObject.SetActive(true);
					}
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(ChatController), "SendChat")]
	public static class BlockChatSendPatch
	{
		public static bool Prefix(ChatController __instance)
		{
			if (!AlwaysShowChatEnabled)
			{
				return true;
			}
			bool num = (Object)(object)MeetingHud.Instance != (Object)null;
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			bool? obj;
			if (localPlayer == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = localPlayer.Data;
				obj = ((data != null) ? new bool?(data.IsDead) : ((bool?)null));
			}
			bool? flag = obj;
			bool valueOrDefault = flag == true;
			bool flag2 = (Object)(object)LobbyBehaviour.Instance != (Object)null;
			if (num || flag2 || valueOrDefault)
			{
				return true;
			}
			if ((Object)(object)__instance != (Object)null)
			{
				FreeChatInputField freeChatField = __instance.freeChatField;
				if (freeChatField != null)
				{
					((AbstractChatInputField)freeChatField).Clear();
				}
				__instance.AddChat(PlayerControl.LocalPlayer, "<color=#FF4444>[MMC] Chat is READ-ONLY outside meetings to prevent kicks!</color>", false);
			}
			return false;
		}
	}

	public static class MapCheats
	{
		internal static void DestroyMap()
		{
			if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				Debug.LogWarning(InteropFix.Cast("[MapCheats] Only the host can remove the map or lobby."));
				return;
			}
			LobbyBehaviour instance = LobbyBehaviour.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				if (instance != null)
				{
					((InnerNetObject)instance).Despawn();
				}
				LobbyBehaviour.Instance = null;
				Debug.Log(InteropFix.Cast("[MapCheats] LobbyBehaviour despawned e singleton limpo."));
			}
			ShipStatus instance2 = ShipStatus.Instance;
			if ((Object)(object)instance2 != (Object)null)
			{
				if (instance2 != null)
				{
					((InnerNetObject)instance2).Despawn();
				}
				ShipStatus.Instance = null;
				Debug.Log(InteropFix.Cast("[MapCheats] ShipStatus despawned e singleton limpo."));
			}
		}

		internal static void SpawnLobby()
		{
			if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				Debug.LogWarning(InteropFix.Cast("[MapCheats] Only the host can create the lobby."));
			}
			else if ((Object)(object)LobbyBehaviour.Instance == (Object)null)
			{
				LobbyBehaviour lobbyPrefab = DestroyableSingleton<GameStartManager>.Instance.LobbyPrefab;
				if ((Object)(object)lobbyPrefab != (Object)null)
				{
					LobbyBehaviour.Instance = Object.Instantiate<LobbyBehaviour>(lobbyPrefab);
					((InnerNetClient)AmongUsClient.Instance).Spawn((InnerNetObject)(object)LobbyBehaviour.Instance, -2, (SpawnFlags)0);
					Debug.Log(InteropFix.Cast("[MapCheats] LobbyBehaviour spawned via prefab."));
				}
				else
				{
					Debug.LogWarning(InteropFix.Cast("[MapCheats] LobbyPrefab not found in GameStartManager."));
				}
			}
			else
			{
				Debug.LogWarning(InteropFix.Cast("[MapCheats] LobbyBehaviour already exists."));
			}
		}
	}

	[HarmonyPatch(typeof(MapBehaviour), "Show")]
	public static class LiveMapPatch
	{
		public static void Prefix(MapBehaviour __instance, MapOptions opts)
		{
			try
			{
				opts.ShowLivePlayerPosition = true;
				opts.IncludeDeadBodies = false;
				opts.AllowMovementWhileMapOpen = true;
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(SabotageSystemType), "Deteriorate")]
	public static class NoSabotageCooldownPatch
	{
		public static void Prefix(SabotageSystemType __instance)
		{
			try
			{
				ConfigEntry<bool> noSabotageCooldown = CheatConfig.NoSabotageCooldown;
				if (noSabotageCooldown != null && noSabotageCooldown.Value)
				{
					__instance.Timer = 0f;
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(MapBehaviour), "Show")]
	public static class CrewmateSabotagePatch
	{
		public static void Prefix(MapBehaviour __instance, MapOptions opts)
		{
			ConfigEntry<bool> crewmateSabotage = CheatConfig.CrewmateSabotage;
			if (crewmateSabotage != null && crewmateSabotage.Value && (Object)(object)PlayerControl.LocalPlayer != (Object)null && !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
			{
				opts.Mode = (MapOptions.Modes)3;
			}
		}
	}

	[HarmonyPatch(typeof(SabotageSystemType), "get_AnyActive")]
	public static class MultiSabotagePatch
	{
		public static bool Prefix(ref bool __result)
		{
			ConfigEntry<bool> multiSabotage = CheatConfig.MultiSabotage;
			if (multiSabotage != null && multiSabotage.Value)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	public static class BodyTypeCheats
	{
		public static readonly PlayerBodyTypes[] AvailableTypes;

		public static int CurrentTypeIndex;

		internal static void SetAllPlayersBodyType(PlayerBodyTypes bodyType)
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					if ((Object)(object)((current != null) ? current.MyPhysics : null) != (Object)null)
					{
						current.MyPhysics.SetBodyType(bodyType);
					}
				}
				LogCheat($"Body type set to {bodyType} for all players");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[BodyTypeCheats] Error: {value}"));
			}
		}

		internal static void SetLocalBodyType(PlayerBodyTypes bodyType)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.MyPhysics : null) != (Object)null)
				{
					localPlayer.MyPhysics.SetBodyType(bodyType);
					LogCheat($"Local body type set to {bodyType}");
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[BodyTypeCheats] Error: {value}"));
			}
		}

		internal static PlayerBodyTypes CycleBodyType()
		{
			CurrentTypeIndex = (CurrentTypeIndex + 1) % AvailableTypes.Length;
			return AvailableTypes[CurrentTypeIndex];
		}

		public static string GetBodyTypeName(PlayerBodyTypes type)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected I4, but got Unknown
			return (int)type switch
			{
				0 => "Normal", 
				3 => "Long", 
				1 => "Horse", 
				2 => "Seeker", 
				4 => "Long Seeker", 
				_ => "Unknown", 
			};
		}

		public static PlayerBodyTypes GetCurrentType()
		{
			return AvailableTypes[CurrentTypeIndex];
		}

		static BodyTypeCheats()
		{
			AvailableTypes = new PlayerBodyTypes[5]
			{
				(PlayerBodyTypes)0,
				(PlayerBodyTypes)3,
				(PlayerBodyTypes)1,
				(PlayerBodyTypes)2,
				(PlayerBodyTypes)4
			};
			CurrentTypeIndex = 0;
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "Start")]
	public static class RevealVotesStartPatch
	{
		public static void Postfix()
		{
			_revealedVoters.Clear();
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "Close")]
	public static class RevealVotesClosePatch
	{
		public static void Prefix()
		{
			_revealedVoters.Clear();
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "PopulateResults")]
	public static class ClearVotesBeforeResultsPatch
	{
		public static void Prefix(MeetingHud __instance)
		{
			try
			{
				if (((__instance != null) ? __instance.playerStates : null) == null)
				{
					return;
				}
				foreach (PlayerVoteArea item in (Il2CppArrayBase<PlayerVoteArea>)(object)__instance.playerStates)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					VoteSpreader componentInChildren = ((Component)item).GetComponentInChildren<VoteSpreader>();
					if (((componentInChildren != null) ? componentInChildren.Votes : null) == null)
					{
						continue;
					}
					var enumerator2 = componentInChildren.Votes.GetEnumerator();
					while (enumerator2.MoveNext())
					{
						SpriteRenderer current2 = enumerator2.Current;
						if ((Object)(object)current2 != (Object)null)
						{
							Object.Destroy((Object)(object)((Component)current2).gameObject);
						}
					}
					componentInChildren.Votes.Clear();
				}
				if (!((Object)(object)__instance.SkippedVoting != (Object)null))
				{
					return;
				}
				VoteSpreader componentInChildren2 = __instance.SkippedVoting.GetComponentInChildren<VoteSpreader>();
				if (((componentInChildren2 != null) ? componentInChildren2.Votes : null) == null)
				{
					return;
				}
				var enumerator3 = componentInChildren2.Votes.GetEnumerator();
				while (enumerator3.MoveNext())
				{
					SpriteRenderer current3 = enumerator3.Current;
					if ((Object)(object)current3 != (Object)null)
					{
						Object.Destroy((Object)(object)((Component)current3).gameObject);
					}
				}
				componentInChildren2.Votes.Clear();
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "Update")]
	public static class RevealVotesUpdatePatch
	{
		public static bool Prefix(MeetingHud __instance)
		{
			try
			{
				if ((Object)(object)__instance == (Object)null)
				{
					return false;
				}
				if (__instance.playerStates == null)
				{
					return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static void Postfix(MeetingHud __instance)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Invalid comparison between Unknown and I4
			try
			{
				if ((Object)(object)__instance == (Object)null || __instance.playerStates == null || (int)__instance.state >= 4)
				{
					return;
				}
				foreach (PlayerVoteArea item in (Il2CppArrayBase<PlayerVoteArea>)(object)__instance.playerStates)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					byte targetPlayerId = item.TargetPlayerId;
					byte votedFor = item.VotedFor;
					if (votedFor == byte.MaxValue || votedFor == 254 || votedFor == 252 || _revealedVoters.Contains(targetPlayerId))
					{
						continue;
					}
					NetworkedPlayerInfo val = null;
					int voterColorId = 0;
					var enumerator2 = PlayerControl.AllPlayerControls.GetEnumerator();
					while (enumerator2.MoveNext())
					{
						PlayerControl current2 = enumerator2.Current;
						if ((Object)(object)current2 != (Object)null && current2.PlayerId == targetPlayerId)
						{
							val = current2.Data;
							if ((Object)(object)current2.cosmetics != (Object)null)
							{
								voterColorId = current2.cosmetics.ColorId;
							}
							break;
						}
					}
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					_revealedVoters.Add(targetPlayerId);
					Transform val2 = null;
					if (votedFor == 253)
					{
						if ((Object)(object)__instance.SkippedVoting != (Object)null)
						{
							val2 = __instance.SkippedVoting.transform;
							__instance.BloopAVoteIcon(val, 0, val2);
							__instance.SkippedVoting.SetActive(true);
						}
					}
					else
					{
						foreach (PlayerVoteArea item2 in (Il2CppArrayBase<PlayerVoteArea>)(object)__instance.playerStates)
						{
							if ((Object)(object)item2 != (Object)null && item2.TargetPlayerId == votedFor)
							{
								val2 = ((Component)item2).transform;
								__instance.BloopAVoteIcon(val, 0, val2);
								break;
							}
						}
					}
					if ((Object)(object)val2 != (Object)null)
					{
						RevealAnonymousVoteColors(val2, voterColorId);
					}
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(KillButton), "DoClick")]
	public static class NoKillCooldownDoClickPatch
	{
		public static bool Prefix(KillButton __instance)
		{
			ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
			if (noKillCooldown == null || !noKillCooldown.Value)
			{
				return true;
			}
			if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
			{
				return true;
			}
			if (!LocalPlayerCanKill())
			{
				return true;
			}
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			if ((Object)(object)localPlayer == (Object)null || (Object)(object)localPlayer.Data == (Object)null || localPlayer.Data.IsDead)
			{
				return true;
			}
			PlayerControl currentTarget = __instance.currentTarget;
			if ((Object)(object)currentTarget == (Object)null || (Object)(object)currentTarget.Data == (Object)null || currentTarget.Data.IsDead)
			{
				return true;
			}
			localPlayer.killTimer = 0f;
			HostForceKillPlayer(currentTarget);
			localPlayer.killTimer = 0f;
			try
			{
				((ActionButton)__instance).SetCoolDown(0f, 1f);
				((ActionButton)__instance).canInteract = true;
			}
			catch
			{
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "SetKillTimer")]
	public static class NoKillCooldownSetTimerPatch
	{
		public static void Prefix(PlayerControl __instance, ref float time)
		{
			ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
			if (noKillCooldown != null && noKillCooldown.Value && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && (Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer && LocalPlayerCanKill())
			{
				time = 0f;
			}
		}

		public static void Postfix(PlayerControl __instance)
		{
			ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
			if (noKillCooldown != null && noKillCooldown.Value && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && (Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer && LocalPlayerCanKill())
			{
				__instance.killTimer = 0f;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "FixedUpdate")]
	public static class NoKillCooldownFixedUpdatePatch
	{
		public static void Postfix(PlayerControl __instance)
		{
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)__instance != (Object)(object)PlayerControl.LocalPlayer)
			{
				return;
			}
			ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
			if (noKillCooldown == null || !noKillCooldown.Value || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost || !LocalPlayerCanKill())
			{
				return;
			}
			__instance.killTimer = 0f;
			try
			{
				HudManager instance = DestroyableSingleton<HudManager>.Instance;
				KillButton val = ((instance != null) ? instance.KillButton : null);
				if ((Object)(object)val != (Object)null)
				{
					((ActionButton)val).SetCoolDown(0f, 1f);
					((ActionButton)val).canInteract = true;
					if ((Object)(object)((ActionButton)val).graphic != (Object)null)
					{
						((ActionButton)val).graphic.color = Color.white;
					}
				}
			}
			catch
			{
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "MurderPlayer")]
	public static class NoKillCooldownMurderPatch
	{
		public static void Postfix(PlayerControl __instance)
		{
			ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
			if (noKillCooldown != null && noKillCooldown.Value && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && (Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer)
			{
				__instance.killTimer = 0f;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "MurderPlayer")]
	public static class RemoteCooldownResetPatch
	{
		public static void Postfix(PlayerControl __instance)
		{
			if ((Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer)
			{
				ConfigEntry<bool> noKillCooldown = CheatConfig.NoKillCooldown;
				if (noKillCooldown != null && noKillCooldown.Value)
				{
					return;
				}
			}
			if ((Object)(object)__instance.Data != (Object)null && (Object)(object)__instance.Data.Role != (Object)null && __instance.Data.Role.IsImpostor)
			{
				float killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat((FloatOptionNames)1);
				__instance.killTimer = killTimer;
			}
		}
	}

	[HarmonyPatch(typeof(ExileController), "ReEnableGameplay")]
	public static class ResetCooldownsPostMeetingPatch
	{
		public static void Postfix()
		{
			float killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat((FloatOptionNames)1);
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if ((Object)(object)current.Data != (Object)null && (Object)(object)current.Data.Role != (Object)null && current.Data.Role.IsImpostor && !current.Data.IsDead)
				{
					current.killTimer = killTimer;
				}
			}
			LogCheat("[Sync] Kill cooldowns reset after meeting.");
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "OnGameStart")]
	public static class ResetCooldownsOnStartPatch
	{
		public static void Postfix()
		{
			float killTimer = 10f;
			if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
			{
				killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat((FloatOptionNames)1);
			}
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if ((Object)(object)current.Data != (Object)null && (Object)(object)current.Data.Role != (Object)null && current.Data.Role.IsImpostor)
				{
					current.killTimer = killTimer;
				}
			}
		}
	}

	[HarmonyPatch(typeof(LogicGameFlowNormal), "CheckEndCriteria")]
	public static class DisableGameEndNormalPatch
	{
		public static bool Prefix(LogicGameFlowNormal __instance)
		{
			if (ModMenuCrewPlugin.DisableGameEndFallback)
			{
				return false;
			}
			ConfigEntry<bool> disableGameEnd = CheatConfig.DisableGameEnd;
			if (disableGameEnd != null && disableGameEnd.Value)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LogicGameFlowHnS), "CheckEndCriteria")]
	public static class DisableGameEndHnSPatch
	{
		public static bool Prefix(LogicGameFlowHnS __instance)
		{
			if (ModMenuCrewPlugin.DisableGameEndFallback)
			{
				return false;
			}
			ConfigEntry<bool> disableGameEnd = CheatConfig.DisableGameEnd;
			if (disableGameEnd != null && disableGameEnd.Value)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "CheckEndGameViaTasks")]
	public static class DisableGameEndTaskPatch
	{
		public static bool Prefix(GameManager __instance)
		{
			if (ModMenuCrewPlugin.DisableGameEndFallback)
			{
				return false;
			}
			ConfigEntry<bool> disableGameEnd = CheatConfig.DisableGameEnd;
			if (disableGameEnd != null && disableGameEnd.Value)
			{
				return false;
			}
			return true;
		}
	}

	public struct ForceVotesPlayerInfo
	{
		public byte PlayerId;

		public string Name;
	}

	[HarmonyPatch(typeof(MeetingHud), "Start")]
	public static class ForceVotesMeetingStartPatch
	{
		public static void Postfix()
		{
			try
			{
				ForceVotesRefreshPlayerList();
				ForceVotesSelectedTargetIndex = 0;
				Debug.Log(InteropFix.Cast("[ForceVotes] Meeting started, player list refreshed"));
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ForceVotes] Error in MeetingStartPatch: {value}"));
			}
		}
	}

	[HarmonyPatch(typeof(MeetingHud), "Close")]
	public static class ForceVotesMeetingClosePatch
	{
		public static void Prefix()
		{
			ForceVotesAlivePlayers.Clear();
			ForceVotesSelectedTargetIndex = 0;
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
	public static class RpcSyncValidator
	{
		private static readonly Dictionary<byte, int> _c0 = new Dictionary<byte, int>();

		private static readonly Dictionary<byte, float> _t0 = new Dictionary<byte, float>();

		private static readonly System.Collections.Generic.HashSet<byte> _b0 = new System.Collections.Generic.HashSet<byte>();

		private static readonly Dictionary<byte, float> _e0 = new Dictionary<byte, float>();

		private static int _gc = 0;

		private static float _gt = 0f;

		private static bool _gf = false;

		private static float _ge = 0f;

		public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
		{
			if (CheatConfig.Nf4 == null || !CheatConfig.Nf4.Value)
			{
				return true;
			}
			if ((Object)(object)__instance == (Object)null)
			{
				return true;
			}
			byte playerId = __instance.PlayerId;
			float unscaledTime = Time.unscaledTime;
			if (_b0.Contains(playerId))
			{
				if (!_e0.TryGetValue(playerId, out var value) || !(unscaledTime >= value))
				{
					return false;
				}
				_b0.Remove(playerId);
				_e0.Remove(playerId);
				_c0.Remove(playerId);
				_t0.Remove(playerId);
			}
			if (callId != 54 && callId != 53)
			{
				return true;
			}
			if (_gf)
			{
				if (!(unscaledTime >= _ge))
				{
					return false;
				}
				_gf = false;
				_gc = 0;
			}
			if (unscaledTime - _gt >= 1f)
			{
				_gt = unscaledTime;
				_gc = 1;
			}
			else
			{
				_gc++;
				if (_gc > 10)
				{
					_gf = true;
					_ge = unscaledTime + 5f;
					return false;
				}
			}
			if (!_t0.TryGetValue(playerId, out var value2))
			{
				_t0[playerId] = unscaledTime;
				_c0[playerId] = 1;
				return true;
			}
			if (unscaledTime - value2 >= 1f)
			{
				_t0[playerId] = unscaledTime;
				_c0[playerId] = 1;
				return true;
			}
			int value3;
			int num = (_c0.TryGetValue(playerId, out value3) ? value3 : 0) + 1;
			_c0[playerId] = num;
			if (num > 5)
			{
				_b0.Add(playerId);
				_e0[playerId] = unscaledTime + 10f;
				return false;
			}
			return true;
		}

		internal static void Reset()
		{
			_c0.Clear();
			_t0.Clear();
			_b0.Clear();
			_e0.Clear();
			_gc = 0;
			_gt = 0f;
			_gf = false;
			_ge = 0f;
		}
	}

	public const byte RPC_SET_SCANNER = 15;

	public const byte RPC_SET_INVISIBILITY = 51;

	private static readonly System.Random SharedRandom = new System.Random();

	public const byte CUSTOM_RPC_PHANTOM_POOF = 112;

	public const byte CHECK_COLOR_RPC = 7;

	public static readonly byte[] HOST_COLOR_IDS = new byte[19]
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
		10, 11, 12, 13, 14, 15, 16, 17, 18
	};

	internal static bool ForceColorForEveryone = false;

	private static bool _hasUsedCamsCheatBefore = false;

	private static bool zoomOutEnabled = false;

	private static float customZoomValue = 10f;

	private static float _defaultOrthoSize = 3f;

	private static bool _endGameActive = false;

	internal static bool TeleportToCursorEnabled = false;

	internal static bool ScannerBypassEnabled = false;

	private static float _scannerAutoDisableTime = 0f;

	private const float SCANNER_AUTO_DISABLE_DURATION = 12f;

	private const int TASK_BATCH_SIZE = 3;

	private const float MIN_TASK_DELAY = 0.02f;

	private const float MAX_TASK_DELAY = 0.08f;

	private const float BATCH_PAUSE_DELAY = 0.05f;

	internal static bool IsRevealSusActive = false;

	private static readonly StringBuilder _revealSusSb = new StringBuilder(256);

	private static Coroutine invisibilityCoroutine;

	private static float _lastInstantStartTime = 0f;

	private static byte? _protectedPlayerId = null;

	private static float _protectionStartTime = 0f;

	private static float _protectionDuration = 10f;

	private static readonly List<(byte PlayerId, string Name, bool IsImpostor)> _alivePlayersBuffer = new List<(byte, string, bool)>(15);

	private static float _lastGodModeProtectionTime = 0f;

	private const float GOD_MODE_REPROTECT_INTERVAL = 0.5f;

	internal static bool MapEventRelayActive = false;

	private static float _mapEventRelayTime = 0f;

	private const float RELAY_WINDOW = 3f;

	private static readonly Dictionary<byte, float> _playerEventRelays = new Dictionary<byte, float>();

	internal static bool FreeCamEnabled = false;

	private static bool freecamActive = false;

	private static float freeCamSpeed = 10f;

	private static bool resolutionChangeNeeded = false;

	private const float SCROLL_ZOOM_STEP = 0.5f;

	private const float SCROLL_ZOOM_MIN = 3f;

	private const float SCROLL_ZOOM_MAX = 15f;

	internal static bool TracersEnabled = false;

	private static GameObject _tracerContainer;

	private static Dictionary<byte, LineRenderer> _tracerLines = new Dictionary<byte, LineRenderer>();

	private static Dictionary<byte, LineRenderer> _tracerGlow = new Dictionary<byte, LineRenderer>();

	private static Dictionary<byte, LineRenderer> _impostorOutlines = new Dictionary<byte, LineRenderer>();

	private static readonly System.Collections.Generic.HashSet<byte> _tracerActiveIdsBuffer = new System.Collections.Generic.HashSet<byte>(20);

	private static readonly List<byte> _tracerRemoveBuffer = new List<byte>(20);

	private static Material _tracerMaterial;

	private static Material _glowMaterial;

	private const float TRACER_WIDTH = 0.06f;

	private const float GLOW_WIDTH = 0.15f;

	private const float IMPOSTOR_OUTLINE_WIDTH = 0.12f;

	private static float _tracerPulse = 0f;

	internal static bool NoClipSmoothEnabled = false;

	private static float _noClipSpeed = 5f;

	internal static bool RadarEnabled = false;

	public static bool RadarShowGhosts = true;

	public static bool RadarShowDeadBodies = true;

	public static bool RadarDrawIcons = true;

	public static bool RadarRightClickTP = true;

	public static bool RadarShowBorder = true;

	public static bool RadarLocked = false;

	public static bool RadarRevealRoles = true;

	public static bool RadarMinimized = false;

	public static bool RadarShowVents = true;

	public static bool RadarShowTracers = false;

	public static bool RadarClickPlayerTP = true;

	public static bool RadarShowMapImage = true;

	public static float RadarMapZoom = 1f;

	private static Rect _radarRect = new Rect(10f, 80f, 280f, 280f);

	private static bool _isDragging = false;

	private static bool _isResizing = false;

	private static bool _radarBlockedMovement = false;

	private static Vector2 _dragOffset;

	private static Vector2 _resizeStart;

	private static Rect _resizeStartRect;

	private static float _lastRadarUpdate = 0f;

	private const float RADAR_UPDATE_INTERVAL = 0.1f;

	private static int _radarFrame = 0;

	private static float _pulseTime = 0f;

	private static float _lastDebugLogTime = 0f;

	private const float RADAR_MIN_SIZE = 200f;

	private const float RADAR_MAX_SIZE = 500f;

	private const float HEADER_HEIGHT = 24f;

	private const float FOOTER_HEIGHT = 18f;

	private const float RADAR_MAP_ZOOM_MIN = 0.5f;

	private const float RADAR_MAP_ZOOM_MAX = 3f;

	private static Texture2D _texSkeldMap;

	private static bool _mapTextureLoaded = false;

	private static float _sonarAngle = 0f;

	private static Texture2D _texSonarLine;

	private static float _nearestImpostorDist = float.MaxValue;

	private static bool _sonarInitialized = false;

	private static string _cachedMapName = "MAPA";

	private static int _cachedAliveCount = 0;

	private static Vector2 _localPos;

	private static int _currentMapType;

	private static readonly Dictionary<int, MapInfo> MapInfos = new Dictionary<int, MapInfo>
	{
		{
			(int)0,
			new MapInfo(0f, 0f, 4.2f, -25f, 20f, -18f, 8f)
		},
		{
			(int)1,
			new MapInfo(0f, 0f, 4f, -15f, 35f, -5f, 25f)
		},
		{
			(int)2,
			new MapInfo(0f, 0f, 4f, -30f, 45f, -35f, 10f)
		},
		{
			(int)3,
			new MapInfo(0f, 0f, 4f, -40f, 40f, -25f, 20f)
		},
		{
			(int)4,
			new MapInfo(0f, 0f, 3.5f, -30f, 50f, -25f, 30f)
		}
	};

	private static RadarPlayerData[] _playerCache = new RadarPlayerData[16];

	private static int _playerCacheCount = 0;

	private static RadarDeadBodyData[] _deadBodyCache = new RadarDeadBodyData[16];

	private static int _deadBodyCacheCount = 0;

	private const int SHAPE_RECT = 0;

	private const int SHAPE_OCTAGON = 1;

	private const int SHAPE_PENTAGON_L = 2;

	private const int SHAPE_PENTAGON_R = 3;

	private const int SHAPE_HEXAGON = 4;

	private const int SHAPE_CORRIDOR = 5;

	private static readonly RoomData[] SkeldRooms = new RoomData[30]
	{
		new RoomData(-1.17f, -0.33f, 8f, 12f, 4),
		new RoomData(9f, 1f, 5f, 5f, 2),
		new RoomData(6.5f, -3f, 4f, 4f, 3),
		new RoomData(17.1f, -4.7f, 5f, 5f, 2),
		new RoomData(9.17f, -12.59f, 5f, 4f, 0),
		new RoomData(4.02f, -15.74f, 5f, 4f, 0),
		new RoomData(4.69f, -8.21f, 5f, 4f, 0),
		new RoomData(-1f, -13f, 7f, 8f, 0),
		new RoomData(-8f, -9f, 5f, 5f, 0),
		new RoomData(-9f, -2f, 5f, 5f, 0),
		new RoomData(-13.5f, -5f, 4f, 4f, 0),
		new RoomData(-17.32f, 0.54f, 5f, 6f, 0),
		new RoomData(-17.32f, -10.54f, 5f, 6f, 0),
		new RoomData(-21f, -5f, 5f, 9f, 0),
		new RoomData(4f, 0.5f, 6f, 2f, octagon: false, corridor: true),
		new RoomData(-5f, -1f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(0f, -4f, 2f, 8f, octagon: false, corridor: true),
		new RoomData(7.5f, -1f, 2f, 4f, octagon: false, corridor: true),
		new RoomData(12f, -4f, 6f, 2f, octagon: false, corridor: true),
		new RoomData(8f, -8f, 2f, 6f, octagon: false, corridor: true),
		new RoomData(3f, -10.5f, 2f, 4f, octagon: false, corridor: true),
		new RoomData(2f, -14.5f, 2f, 3f, octagon: false, corridor: true),
		new RoomData(5f, -13f, 6f, 2f, octagon: false, corridor: true),
		new RoomData(-4.5f, -14f, 5f, 2f, octagon: false, corridor: true),
		new RoomData(-12f, -10f, 2f, 3f, octagon: false, corridor: true),
		new RoomData(-14f, -10.5f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(-13f, -0.5f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(-15.5f, -2f, 2f, 4f, octagon: false, corridor: true),
		new RoomData(-15.5f, -7.5f, 2f, 4f, octagon: false, corridor: true),
		new RoomData(-18f, -5f, 4f, 2f, octagon: false, corridor: true)
	};

	private static readonly RoomData[] MiraRooms = new RoomData[16]
	{
		new RoomData(-4f, 0f, 6f, 5f),
		new RoomData(2f, 11f, 6f, 6f, octagon: true),
		new RoomData(9f, 11f, 6f, 5f),
		new RoomData(15f, 17f, 6f, 5f),
		new RoomData(20f, 17f, 5f, 5f),
		new RoomData(17f, 22f, 6f, 4f),
		new RoomData(15f, 0f, 5f, 5f),
		new RoomData(14f, 3f, 4f, 3f),
		new RoomData(9f, 0f, 4f, 4f),
		new RoomData(6f, 5f, 3f, 3f),
		new RoomData(25f, 0f, 7f, 6f, octagon: true),
		new RoomData(19f, 3f, 4f, 4f),
		new RoomData(24f, 8f, 5f, 3f),
		new RoomData(5f, 3f, 8f, 2f, octagon: false, corridor: true),
		new RoomData(11f, 14f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(22f, 10f, 2f, 5f, octagon: false, corridor: true)
	};

	private static readonly RoomData[] PolusRooms = new RoomData[15]
	{
		new RoomData(16f, -2f, 6f, 5f, octagon: true),
		new RoomData(19f, -17f, 8f, 6f),
		new RoomData(21f, -22f, 5f, 4f),
		new RoomData(12f, -15f, 5f, 4f),
		new RoomData(13f, -9f, 5f, 4f),
		new RoomData(6f, -10f, 5f, 5f),
		new RoomData(20f, -11f, 5f, 5f),
		new RoomData(1f, -17f, 5f, 5f, octagon: true),
		new RoomData(35f, -7f, 7f, 6f),
		new RoomData(37f, -17f, 5f, 5f),
		new RoomData(2f, -24f, 5f, 4f),
		new RoomData(-5f, -9f, 4f, 4f),
		new RoomData(8f, -5f, 8f, 2f, octagon: false, corridor: true),
		new RoomData(26f, -12f, 8f, 2f, octagon: false, corridor: true),
		new RoomData(9f, -20f, 3f, 6f, octagon: false, corridor: true)
	};

	private static readonly RoomData[] FungleRooms = new RoomData[16]
	{
		new RoomData(-8f, 11f, 6f, 5f, octagon: true),
		new RoomData(-20f, 2f, 7f, 6f, octagon: true),
		new RoomData(-10f, -2f, 6f, 5f),
		new RoomData(-18f, -7f, 5f, 4f),
		new RoomData(1f, 2f, 5f, 4f),
		new RoomData(9f, 7f, 6f, 5f),
		new RoomData(6f, 0f, 5f, 4f),
		new RoomData(22f, -2f, 6f, 6f, octagon: true),
		new RoomData(17f, 7f, 6f, 5f),
		new RoomData(-5f, -10f, 7f, 5f),
		new RoomData(10f, -12f, 5f, 4f),
		new RoomData(21f, -12f, 5f, 4f),
		new RoomData(-30f, 0f, 5f, 4f, octagon: true),
		new RoomData(-12f, 5f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(14f, 4f, 3f, 2f, octagon: false, corridor: true),
		new RoomData(5f, -6f, 2f, 4f, octagon: false, corridor: true)
	};

	private static readonly RoomData[] AirshipRooms = new RoomData[21]
	{
		new RoomData(-20f, 3f, 6f, 5f, octagon: true),
		new RoomData(-18f, -4f, 7f, 5f),
		new RoomData(-11f, -7f, 5f, 4f),
		new RoomData(-6f, 3f, 6f, 5f),
		new RoomData(5f, 0f, 10f, 8f, octagon: true),
		new RoomData(7f, -7f, 4f, 4f),
		new RoomData(18f, 0f, 5f, 5f),
		new RoomData(28f, -2f, 6f, 5f),
		new RoomData(35f, -3f, 7f, 7f),
		new RoomData(22f, 10f, 6f, 5f),
		new RoomData(-8f, -10f, 5f, 4f),
		new RoomData(0f, -10f, 5f, 4f),
		new RoomData(-2f, 12f, 5f, 4f),
		new RoomData(15f, 14f, 6f, 5f, octagon: true),
		new RoomData(30f, 5f, 5f, 4f),
		new RoomData(40f, 6f, 6f, 5f),
		new RoomData(21f, -10f, 5f, 4f),
		new RoomData(-12f, 0f, 6f, 2f, octagon: false, corridor: true),
		new RoomData(14f, 0f, 4f, 2f, octagon: false, corridor: true),
		new RoomData(23f, 4f, 2f, 6f, octagon: false, corridor: true),
		new RoomData(8f, 8f, 2f, 4f, octagon: false, corridor: true)
	};

	private static Texture2D _texRoomFill;

	private static Texture2D _texRoomBorder;

	private static Texture2D _texCorridorFill;

	private static GUIStyle _sRoomFill;

	private static GUIStyle _sRoomBorder;

	private static GUIStyle _sCorridorFill;

	private static bool _radarInitialized = false;

	private static Texture2D _texWindowBg;

	private static Texture2D _texHeaderBg;

	private static Texture2D _texMapBg;

	private static Texture2D _texBorderAccent;

	private static Texture2D _texBorderDim;

	private static Texture2D _texImpostorGlow;

	private static Texture2D _texImpostorDot;

	private static Texture2D _texGhostDot;

	private static Texture2D _texDeadBodyBg;

	private static Texture2D _texVisor;

	private static Texture2D _texShadow;

	private static Texture2D _texMapOverlay;

	private static Texture2D _texVent;

	private static Texture2D _texTracerLine;

	private static Texture2D _texGridDim;

	private static Texture2D _texGridBright;

	private static Texture2D _texVentStyle;

	private static Texture2D _texScanLine;

	private static Texture2D[] _texPlayers;

	private static GUIStyle _sWindowBg;

	private static GUIStyle _sHeaderBg;

	private static GUIStyle _sMapBg;

	private static GUIStyle _sBorderAccent;

	private static GUIStyle _sBorderDim;

	private static GUIStyle _sImpostorGlow;

	private static GUIStyle _sImpostorDot;

	private static GUIStyle _sGhostDot;

	private static GUIStyle _sDeadBodyBg;

	private static GUIStyle _sVisor;

	private static GUIStyle _sShadow;

	private static GUIStyle _sMapOverlay;

	private static GUIStyle _sGridDim;

	private static GUIStyle _sGridBright;

	private static GUIStyle _sVentStyle;

	private static GUIStyle _sScanLine;

	private static GUIStyle[] _sPlayers;

	private static GUIStyle _sLabel;

	private static GUIStyle _sTitle;

	private static GUIStyle _sSmall;

	private static GUIStyle _sButton;

	private static GUIStyle _sSkeldMapImage;

	private static readonly Color ThemeAccent = new Color(1f, 0.09f, 0.27f, 1f);

	private static readonly Color ThemeAccentSoft = new Color(1f, 0.25f, 0.4f, 1f);

	private static readonly Color ThemeAccentDim = new Color(0.5f, 0.02f, 0.1f, 1f);

	private static readonly Color ThemeVisor = new Color(0f, 0.9f, 1f, 1f);

	private static readonly Color ThemeImpostor = new Color(0.84f, 0f, 0f, 1f);

	private static readonly Color ThemeImpostorGlow = new Color(1f, 0f, 0.15f, 0.35f);

	private static readonly Color ThemeBgDark = new Color(0.04f, 0.04f, 0.06f, 0.97f);

	private static readonly Color ThemeBgMid = new Color(0.07f, 0.07f, 0.1f, 0.95f);

	private static readonly Color ThemeHeaderTop = new Color(0.12f, 0.02f, 0.05f, 0.98f);

	private static readonly Color ThemeHeaderBot = new Color(0.04f, 0.01f, 0.02f, 0.98f);

	private static readonly Color ThemeTextPrimary = new Color(0.96f, 0.96f, 0.97f, 1f);

	private static readonly Color ThemeTextMuted = new Color(0.54f, 0.54f, 0.6f, 1f);

	private static readonly Color ThemeMapBg = new Color(0.03f, 0.03f, 0.05f, 0.98f);

	private static readonly Color ThemeMapOverlay = new Color(1f, 0.09f, 0.27f, 0.06f);

	private static readonly Color[] PlayerColors = (Color[])(object)new Color[18]
	{
		new Color(0.77f, 0.07f, 0.07f),
		new Color(0.07f, 0.18f, 0.82f),
		new Color(0.07f, 0.5f, 0.18f),
		new Color(0.93f, 0.33f, 0.73f),
		new Color(0.94f, 0.49f, 0.05f),
		new Color(0.96f, 0.93f, 0.15f),
		new Color(0.24f, 0.24f, 0.24f),
		new Color(0.84f, 0.88f, 0.94f),
		new Color(0.42f, 0.18f, 0.73f),
		new Color(0.44f, 0.29f, 0.12f),
		new Color(0.22f, 0.99f, 0.86f),
		new Color(0.31f, 0.94f, 0.22f),
		new Color(0.43f, 0.15f, 0.15f),
		new Color(0.93f, 0.8f, 0.8f),
		new Color(1f, 0.72f, 0.4f),
		new Color(0.51f, 0.51f, 0.51f),
		new Color(0.56f, 0.44f, 0.34f),
		new Color(0.93f, 0.46f, 0.47f)
	};

	private static GUIStyle _cachedMapStyle;

	private static float _lastCacheUpdateTime = 0f;

	private const float CACHE_UPDATE_INTERVAL = 0.1f;

	internal static bool NoKillDistanceLimitEnabled = false;

	internal static bool SeeGhostsEnabled = false;

	internal static bool SeeDeadChatEnabled = false;

	internal static bool AlwaysShowChatEnabled = false;

	private static bool _killCooldownUIInitialized = false;

	private static GUIStyle _cooldownAboveHeadStyle;

	private static GUIStyle _cooldownShadowStyle;

	private static Texture2D _cooldownBgTex;

	private static float _killAlertEndTime = 0f;

	private static float _killAlertStartTime = 0f;

	private static string _killAlertKiller = "";

	private static string _killAlertVictim = "";

	private static string _killAlertLocation = "";

	private const float KILL_ALERT_DURATION = 5f;

	private const float KILL_ALERT_SLIDE_IN = 0.3f;

	private const float KILL_ALERT_FADE_OUT = 1.5f;

	private static GUIStyle _killAlertTitleStyle;

	private static GUIStyle _killAlertDetailStyle;

	private static GUIStyle _killAlertBgStyle;

	private static GUIStyle _killAlertAccentStyle;

	private static Texture2D _killAlertBgTex;

	private static Texture2D _killAlertAccentTex;

	private static Vector2 _eventLogScrollPos = Vector2.zero;

	private static Rect _eventLogRect = new Rect(10f, 250f, 380f, 300f);

	private static bool _eventLogDragging = false;

	private static Vector2 _eventLogDragOffset;

	private static GUIStyle _eventLogHeaderStyle;

	private static GUIStyle _eventLogEventStyle;

	private static GUIStyle _eventLogTimeStyle;

	private static Texture2D _eventLogBgTex;

	private static Texture2D _eventLogHeaderTex;

	private static System.Collections.Generic.HashSet<byte> _revealedVoters = new System.Collections.Generic.HashSet<byte>();

	public static int ForceVotesSelectedTargetIndex = 0;

	public static List<ForceVotesPlayerInfo> ForceVotesAlivePlayers = new List<ForceVotesPlayerInfo>();

	public const byte VOTE_HAS_NOT_VOTED = byte.MaxValue;

	public const byte VOTE_MISSED = 254;

	public const byte VOTE_SKIPPED = 253;

	public static bool AnimShields { get; set; } = false;

	public static bool AnimAsteroids { get; set; } = false;

	public static bool AnimEmptyGarbage { get; set; } = false;

	public static bool AnimCamsInUse { get; set; } = false;

	internal static bool IsZoomOutActive => zoomOutEnabled;

	public static bool HasProtectedPlayer => _protectedPlayerId.HasValue;

	public static bool IsGodModeActive
	{
		get
		{
			ConfigEntry<bool> godMode = CheatConfig.GodMode;
			if (godMode != null && godMode.Value)
			{
				AmongUsClient instance = AmongUsClient.Instance;
				if (instance == null)
				{
					return false;
				}
				return ((InnerNetClient)instance).AmHost;
			}
			return false;
		}
	}

	private static void LogCheat(string message)
	{
		Debug.Log(InteropFix.Cast("[Cheat] " + message));
	}

	private static string GenerateFakeToken(byte playerId = 0)
	{
		long ticks = DateTime.UtcNow.Ticks;
		AmongUsClient instance = AmongUsClient.Instance;
		string value = ((instance != null) ? ((InnerNetClient)instance).GameId.ToString() : null) ?? "0";
		string s = $"{playerId}-{ticks}-{value}";
		using SHA256 sHA = SHA256.Create();
		return Convert.ToBase64String(sHA.ComputeHash(Encoding.UTF8.GetBytes(s)));
	}

	private static string GenerateVerificationToken(byte playerId, byte scanCount, long timestamp)
	{
		return $"{playerId}-{scanCount}-{timestamp}-MODMENUCREW";
	}

	private static bool ValidateToken(string token, byte playerId, byte scanCount, long timestamp)
	{
		string text = GenerateVerificationToken(playerId, scanCount, timestamp);
		if (token == text)
		{
			return Math.Abs(DateTime.UtcNow.Ticks - timestamp) < 50000000;
		}
		return false;
	}

	internal static void CloseMeeting()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (InteropFix.Cast((Object)(object)MeetingHud.Instance))
			{
				((InnerNetObject)MeetingHud.Instance).DespawnOnDestroy = false;
				Object.Destroy((Object)(object)((Component)MeetingHud.Instance).gameObject);
				((MonoBehaviour)DestroyableSingleton<HudManager>.Instance).StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false));
				PlayerControl.LocalPlayer.SetKillTimer(GameManager.Instance.LogicOptions.GetKillCooldown());
				ShipStatus.Instance.EmergencyCooldown = GameManager.Instance.LogicOptions.GetEmergencyCooldown();
				((Component)Camera.main).GetComponent<FollowerCamera>().Locked = false;
				DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
				LogCheat("Meeting closed successfully.");
			}
			else if ((Object)(object)ExileController.Instance != (Object)null)
			{
				ExileController.Instance.ReEnableGameplay();
				ExileController.Instance.WrapUp();
				LogCheat("Exile ended.");
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error in CloseMeeting: {value}"));
		}
	}

	private static void ForcePlayAnimation(byte animationType)
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			return;
		}
		try
		{
			PlayerControl.LocalPlayer.PlayAnimation(animationType);
			PlayerControl.LocalPlayer.RpcPlayAnimation(animationType);
		}
		catch (Exception ex)
		{
			LogCheat($"[Animation] Error playing anim {animationType}: {ex.Message}");
		}
	}

	internal static void ProcessAnimationCheats()
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).IsGameStarted)
		{
			return;
		}
		try
		{
			GameOptionsManager instance = GameOptionsManager.Instance;
			byte? obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				IGameOptions currentGameOptions = instance.CurrentGameOptions;
				obj = ((currentGameOptions != null) ? new byte?(currentGameOptions.MapId) : ((byte?)null));
			}
			byte? b = obj;
			byte valueOrDefault = b.GetValueOrDefault();
			if (AnimShields)
			{
				if (valueOrDefault == 0 || valueOrDefault == 3)
				{
					ForcePlayAnimation(1);
				}
				AnimShields = false;
			}
			if (AnimAsteroids)
			{
				if (valueOrDefault == 0 || valueOrDefault == 2 || valueOrDefault == 3)
				{
					ForcePlayAnimation(6);
				}
				AnimAsteroids = false;
			}
			if (AnimEmptyGarbage)
			{
				if (valueOrDefault == 0 || valueOrDefault == 3)
				{
					ForcePlayAnimation(10);
				}
				AnimEmptyGarbage = false;
			}
			if (AnimCamsInUse && !_hasUsedCamsCheatBefore)
			{
				if (valueOrDefault != 1 && valueOrDefault != 5)
				{
					ShipStatus instance2 = ShipStatus.Instance;
					if (instance2 != null)
					{
						instance2.RpcUpdateSystem((SystemTypes)11, (byte)1);
					}
					_hasUsedCamsCheatBefore = true;
				}
				else
				{
					AnimCamsInUse = false;
				}
			}
			else if (!AnimCamsInUse && _hasUsedCamsCheatBefore)
			{
				ShipStatus instance3 = ShipStatus.Instance;
				if (instance3 != null)
				{
					instance3.RpcUpdateSystem((SystemTypes)11, (byte)0);
				}
				_hasUsedCamsCheatBefore = false;
			}
		}
		catch
		{
		}
	}

	internal static void RunCoroutine(IEnumerator routine)
	{
		HudManager instance = DestroyableSingleton<HudManager>.Instance;
		if (instance != null)
		{
			MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)instance, routine);
		}
	}

	internal static void CompleteAllTasks()
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)PlayerControl.LocalPlayer.Data == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
		{
			return;
		}
		try
		{
			var myTasks = PlayerControl.LocalPlayer.myTasks;
			if (myTasks != null && myTasks.Count != 0)
			{
				int num = 0;
				foreach (PlayerTask t in (Il2CppArrayBase<PlayerTask>)(object)myTasks.ToArray())
				{
					if ((Object)(object)t != (Object)null && !t.IsComplete)
					{
						num++;
					}
				}
				if (num != 0)
				{
					MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, CompleteAllTasksOptimized(num));
				}
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error completing tasks: {value}"));
		}
	}

	internal static IEnumerator CompleteAllTasksOptimized(int totalPending)
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)PlayerControl.LocalPlayer.Data == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
		{
			yield break;
		}
		bool isHost = ((InnerNetClient)AmongUsClient.Instance).AmHost;
		int completedCount = 0;
		int batchCount = 0;
		Il2CppArrayBase<PlayerTask> val = PlayerControl.LocalPlayer.myTasks.ToArray();
		float dynamicDelay = Mathf.Lerp(0.08f, 0.02f, Mathf.Clamp01((float)totalPending / 15f));
		foreach (PlayerTask item in val)
		{
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
			{
				yield break;
			}
			if ((Object)(object)item == (Object)null || item.IsComplete)
			{
				continue;
			}
			try
			{
				MessageWriter val2 = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)1, (SendOption)1, -1);
				val2.WritePacked(item.Id);
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val2);
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"Error sending RPC for task {item.Id}: {value}"));
				continue;
			}
			NormalPlayerTask val3 = (NormalPlayerTask)(object)((item is NormalPlayerTask) ? item : null);
			if (val3 != null)
			{
				val3.taskStep = val3.MaxStep;
			}
			item.Complete();
			try
			{
				if ((Object)(object)GameData.Instance != (Object)null)
				{
					GameData.Instance.CompleteTask(PlayerControl.LocalPlayer, item.Id);
				}
			}
			catch
			{
			}
			completedCount++;
			batchCount++;
				float num = (float)(new System.Random().NextDouble() * 0.02 - 0.01);
			if (batchCount >= 3)
			{
				batchCount = 0;
				yield return (object)new WaitForSeconds(0.05f);
				yield return null;
			}
			else
			{
				yield return (object)new WaitForSeconds(Mathf.Max(0.01f, dynamicDelay + num));
			}
		}
		try
		{
			PlayerControl.LocalPlayer.Data.MarkDirty();
		}
		catch
		{
		}
		LogCheat($"All {completedCount} tasks completed. (Host: {isHost})");
	}

	internal static IEnumerator CompleteAllTasksWithDelay(float perTaskDelay = 0.1f)
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)PlayerControl.LocalPlayer.Data == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
		{
			yield break;
		}
		bool isHost = ((InnerNetClient)AmongUsClient.Instance).AmHost;
		Il2CppArrayBase<PlayerTask> val = PlayerControl.LocalPlayer.myTasks.ToArray();
		foreach (PlayerTask task in val)
		{
			if ((Object)(object)task == (Object)null || task.IsComplete)
			{
				continue;
			}
			MessageWriter val2 = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)1, (SendOption)1, -1);
			val2.WritePacked(task.Id);
			((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val2);
			PlayerTask obj = task;
			NormalPlayerTask val3 = (NormalPlayerTask)(object)((obj is NormalPlayerTask) ? obj : null);
			if (val3 != null)
			{
				val3.taskStep = val3.MaxStep;
			}
			task.Complete();
			try
			{
				NetworkedPlayerInfo.TaskInfo val4 = null;
				foreach (NetworkedPlayerInfo.TaskInfo t in (Il2CppArrayBase<NetworkedPlayerInfo.TaskInfo>)(object)PlayerControl.LocalPlayer.Data.Tasks.ToArray())
				{
					if (t != null && t.Id == task.Id)
					{
						val4 = t;
						break;
					}
				}
				if (val4 != null)
				{
					val4.Complete = true;
				}
			}
			catch
			{
			}
			LogCheat($"Task {task.Id} completed. (Host: {isHost})");
			if (perTaskDelay > 0f)
			{
				yield return (object)new WaitForSeconds(perTaskDelay);
			}
			else
			{
				yield return null;
			}
		}
		PlayerControl.LocalPlayer.Data.MarkDirty();
		LogCheat(isHost ? "All tasks completed and synced." : "All tasks completed locally. Sync may vary.");
	}

	internal static void TeleportToCursor()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)Camera.main == (Object)null)
			{
				return;
			}
			Vector3 mousePosition = Input.mousePosition;
			Vector3 val = Camera.main.ScreenToWorldPoint(mousePosition);
			val.z = ((Component)PlayerControl.LocalPlayer).transform.position.z;
			if (PlayerControl.LocalPlayer.inVent)
			{
				try
				{
					PlayerPhysics myPhysics = PlayerControl.LocalPlayer.MyPhysics;
					if (myPhysics != null)
					{
						myPhysics.ExitAllVents();
					}
				}
				catch
				{
				}
			}
			if (PlayerControl.LocalPlayer.onLadder)
			{
				try
				{
					PlayerPhysics myPhysics2 = PlayerControl.LocalPlayer.MyPhysics;
					if (myPhysics2 != null)
					{
						myPhysics2.ResetMoveState(true);
					}
				}
				catch
				{
				}
			}
			Collider2D collider = PlayerControl.LocalPlayer.Collider;
			bool enabled = collider != null && ((Behaviour)collider).enabled;
			if ((Object)(object)collider != (Object)null)
			{
				((Behaviour)collider).enabled = false;
			}
			((Component)PlayerControl.LocalPlayer).transform.position = val;
			if ((Object)(object)PlayerControl.LocalPlayer.NetTransform != (Object)null)
			{
				PlayerControl.LocalPlayer.NetTransform.SnapTo(InteropFix.Cast(val));
			}
			if ((Object)(object)collider != (Object)null)
			{
				((Behaviour)collider).enabled = enabled;
			}
			if ((Object)(object)AmongUsClient.Instance != (Object)null)
			{
				MessageWriter val2 = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)21, (SendOption)1, -1);
				val2.Write(val.x);
				val2.Write(val.y);
				CustomNetworkTransform netTransform = PlayerControl.LocalPlayer.NetTransform;
				val2.Write((ushort)((netTransform != null) ? netTransform.lastSequenceId : 0));
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val2);
			}
			LogCheat($"Teleported to ({val.x:F1}, {val.y:F1})");
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[TeleportToCursor] Error: " + ex.Message));
		}
	}

	internal static void CheckTeleportInput()
	{
		if (!TeleportToCursorEnabled || !Input.GetMouseButtonDown(1))
		{
			return;
		}
		try
		{
			if ((Object)(object)MeetingHud.Instance != (Object)null)
			{
				return;
			}
		}
		catch
		{
		}
		TeleportToCursor();
	}

	internal static void KickAllFromVents()
	{
		if ((Object)(object)ShipStatus.Instance == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("ShipStatus not initialized."));
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("You must be host to use this cheat."));
			return;
		}
		try
		{
			foreach (Vent item in (Il2CppArrayBase<Vent>)(object)ShipStatus.Instance.AllVents)
			{
				VentilationSystem.Update((VentilationSystem.Operation)5, item.Id);
			}
			LogCheat("All players kicked from vents.");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error in KickAllFromVents: {value}"));
		}
	}

	internal static void BypassScanner(bool value)
	{
		try
		{
			if (!((Object)(object)PlayerControl.LocalPlayer == (Object)null) && !((Object)(object)AmongUsClient.Instance == (Object)null))
			{
				byte b = (byte)(PlayerControl.LocalPlayer.scannerCount + 1);
				PlayerControl.LocalPlayer.scannerCount = b;
				PlayerControl.LocalPlayer.SetScanner(value, b);
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)15, (SendOption)1, ((InnerNetObject)current).OwnerId);
					val.Write(value);
					val.Write(b);
					((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("BypassScanner error: " + ex.Message));
		}
	}

	internal static void HandleScannerRPC(PlayerControl instance, byte callId, MessageReader reader)
	{
		if (callId != 15)
		{
			return;
		}
		try
		{
			bool flag = reader.ReadBoolean();
			byte b = reader.ReadByte();
			instance.SetScanner(flag, b);
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("HandleScannerRPC error: " + ex.Message));
		}
	}

	internal static IEnumerator BypassScannerWithTimeout(float duration)
	{
		BypassScanner(value: true);
		yield return (object)new WaitForSeconds(duration);
		BypassScanner(value: false);
	}

	internal static void UpdateScannerBypass()
	{
		if (ScannerBypassEnabled && _scannerAutoDisableTime > 0f && Time.time >= _scannerAutoDisableTime)
		{
			SetScannerBypass(enabled: false);
		}
	}

	internal static void SetScannerBypass(bool enabled)
	{
		if (ScannerBypassEnabled != enabled)
		{
			ScannerBypassEnabled = enabled;
			BypassScanner(enabled);
			if (enabled)
			{
				_scannerAutoDisableTime = Time.time + 12f;
				LogCheat($"Scanner Bypass ON (auto-off in {12f}s)");
			}
			else
			{
				_scannerAutoDisableTime = 0f;
				LogCheat("Scanner Bypass OFF");
			}
		}
	}

	internal static float GetScannerBypassRemainingTime()
	{
		if (!ScannerBypassEnabled || _scannerAutoDisableTime <= 0f)
		{
			return 0f;
		}
		return Mathf.Max(0f, _scannerAutoDisableTime - Time.time);
	}

	internal static void LocalVisualScanForEveryone(float duration = 2f)
	{
		if (!((Object)(object)PlayerControl.LocalPlayer == (Object)null))
		{
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				LocalToggleScanner(enumerator.Current, on: true);
			}
			((MonoBehaviour)DestroyableSingleton<HudManager>.Instance).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(LocalDisableScansAfterDelay(duration)));
		}
	}

	private static void LocalToggleScanner(PlayerControl player, bool on)
	{
		try
		{
			byte b = (player.scannerCount += 1);
			player.SetScanner(on, b);
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"LocalToggleScanner error ({((player != null) ? ((Object)player).name : null)}): {value}"));
		}
	}

	private static IEnumerator LocalDisableScansAfterDelay(float delay)
	{
		yield return (object)new WaitForSeconds(delay);
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			LocalToggleScanner(enumerator.Current, on: false);
		}
	}

	internal static void KillAll(bool crewOnly = false, bool impostorsOnly = false)
	{
		if ((Object)(object)ShipStatus.Instance == (Object)null || (Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("ShipStatus or local player not found."));
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("AmongUsClient not found."));
			return;
		}
		try
		{
			bool flag = !((InnerNetClient)AmongUsClient.Instance).IsGameStarted;
			List<PlayerControl> list = GetValidKillTargets(crewOnly, impostorsOnly);
			float num = 0f;
			foreach (PlayerControl item in list)
			{
				if (flag)
				{
					ExecuteKillBypass(PlayerControl.LocalPlayer, item, num);
				}
				else
				{
					BroadcastKillBypass(PlayerControl.LocalPlayer, item, num);
				}
					num += (float)new System.Random().Next(100, 300) / 1000f;
			}
			LogCheat("Mass kill completed with bypass.");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error in KillAll: {value}"));
		}
	}

	private static bool IsValidKillTarget(PlayerControl target, bool crewOnly, bool impostorsOnly)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		if (!((Object)(object)target == (Object)null))
		{
			NetworkedPlayerInfo data = target.Data;
			if (!((Object)(object)((data != null) ? data.Role : null) == (Object)null) && !target.Data.IsDead)
			{
				if (crewOnly)
				{
					return (int)target.Data.Role.TeamType == 0;
				}
				if (impostorsOnly)
				{
					return (int)target.Data.Role.TeamType == 1;
				}
				return true;
			}
		}
		return false;
	}

	internal static void HostForceKillPlayer(PlayerControl target)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected I4, but got Unknown
		if ((Object)(object)target == (Object)null || (Object)(object)target.Data == (Object)null || target.Data.IsDead)
		{
			Debug.LogWarning(InteropFix.Cast("[HostKill] Invalid target or already dead."));
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostKill] Only the host can use this method."));
			return;
		}
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if ((Object)(object)localPlayer == (Object)null)
		{
			return;
		}
		try
		{
			MurderResultFlags val = (MurderResultFlags)9;
			localPlayer.MurderPlayer(target, val);
			if ((Object)(object)target.Data != (Object)null && !target.Data.IsDead)
			{
				target.Data.IsDead = true;
			}
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (!((Object)(object)current == (Object)null) && !((Object)(object)current == (Object)(object)PlayerControl.LocalPlayer))
				{
					MessageWriter val2 = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)localPlayer).NetId, (byte)12, (SendOption)1, ((InnerNetObject)current).OwnerId);
					MessageExtensions.WriteNetObject(val2, (InnerNetObject)(object)target);
					val2.Write((int)val);
					((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val2);
				}
			}
			if ((Object)(object)target.cosmetics != (Object)null)
			{
				((Component)target).gameObject.layer = LayerMask.NameToLayer("Ghost");
			}
			NetworkedPlayerInfo data = target.Data;
			LogCheat("[HostKill] " + ((data != null) ? data.PlayerName : null) + " killed by host successfully.");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[HostKill] Error killing player: {value}"));
		}
	}

	private static void ExecuteKillBypass(PlayerControl killer, PlayerControl target, float delay)
	{
		if ((Object)(object)target == (Object)null || (Object)(object)killer == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("Target or killer not found."));
			return;
		}
		if (delay > 0f)
		{
			MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, DelayedKillBypass(killer, target, delay));
			return;
		}
		killer.MurderPlayer(target, (MurderResultFlags)9);
		LogCheat($"[Bypass] {target.PlayerId} local kill attempt (awaiting server validation).");
	}

	private static void BroadcastKillBypass(PlayerControl killer, PlayerControl target, float delay)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || (Object)(object)killer == (Object)null || (Object)(object)target == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("AmongUsClient/killer/target not found."));
			return;
		}
		if (delay > 0f)
		{
			MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, DelayedKillBypass(killer, target, delay, broadcast: true));
			return;
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if (!((Object)(object)current == (Object)(object)PlayerControl.LocalPlayer))
			{
				MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)killer).NetId, (byte)12, (SendOption)1, ((InnerNetObject)current).OwnerId);
				MessageExtensions.WriteNetObject(val, (InnerNetObject)(object)target);
				val.Write(9);
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
			}
		}
		LogCheat($"[Bypass] Kill of {target.PlayerId} broadcast to all (awaiting server validation).");
	}

	private static IEnumerator DelayedKillBypass(PlayerControl killer, PlayerControl target, float delay, bool broadcast = false)
	{
		yield return (object)new WaitForSeconds(delay);
		if ((Object)(object)killer == (Object)null || (Object)(object)target == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("killer/target/AmongUsClient not found."));
			yield break;
		}
		if (broadcast)
		{
			BroadcastKillBypass(killer, target, 0f);
		}
		else
		{
			ExecuteKillBypass(killer, target, 0f);
		}
		LogCheat($"[Bypass] Kill of {target.PlayerId} broadcast after {delay}s delay.");
	}

	internal static void RevealImpostors()
	{
		try
		{
			AmongUsClient instance = AmongUsClient.Instance;
			if (instance == null || !((InnerNetClient)instance).IsGameStarted)
			{
				LogCheat("Reveal Sus: Only works during a game!");
				return;
			}
			IsRevealSusActive = !IsRevealSusActive;
			if (IsRevealSusActive)
			{
				LogCheat("Reveal Sus ON! (Names will update constantly)");
				return;
			}
			LogCheat("Reveal Sus OFF!");
			try
			{
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					if ((Object)(object)((current != null) ? current.Data : null) != (Object)null)
					{
						CosmeticsLayer cosmetics = current.cosmetics;
						if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) != (Object)null)
						{
							((TMP_Text)current.cosmetics.nameText).text = current.Data.PlayerName;
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				MMCIdentification.ReapplyAllMMCTags();
			}
			catch
			{
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"RevealImpostors Error: {value}"));
		}
	}

	internal unsafe static string GetSpecificRoleName(RoleBehaviour role)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected I4, but got Unknown
		if ((Object)(object)role == (Object)null)
		{
			return "UNKNOWN";
		}
		try
		{
			RoleTypes role2 = role.Role;
			return (int)role2 switch
			{
				1 => "IMPOSTOR", 
				5 => "SHAPESHIFTER", 
				9 => "PHANTOM", 
				18 => "VIPER", 
				0 => "CREWMATE", 
				3 => "ENGINEER", 
				2 => "SCIENTIST", 
				8 => "NOISEMAKER", 
				10 => "TRACKER", 
				4 => "GUARDIAN ANGEL", 
				6 => "CREWMATE GHOST", 
				7 => "IMPOSTOR GHOST", 
				_ => ((object)(*(RoleTypes*)(&role2))/*cast due to .constrained prefix*/).ToString().ToUpper(), 
			};
		}
		catch
		{
			try
			{
				return role.NiceName?.ToUpper() ?? "UNKNOWN";
			}
			catch
			{
				return "UNKNOWN";
			}
		}
	}

	internal static void UpdateRevealSus()
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		if (!IsRevealSusActive || PlayerControl.AllPlayerControls == null)
		{
			return;
		}
		bool flag = !(CheatConfig.HideMMCStar?.Value ?? false);
		bool flag2 = CheatConfig.ShowKillCooldowns?.Value ?? false;
		try
		{
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
				if ((Object)obj == (Object)null)
				{
					continue;
				}
				CosmeticsLayer cosmetics = current.cosmetics;
				if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) == (Object)null)
				{
					continue;
				}
				bool isImpostor = current.Data.Role.IsImpostor;
				string playerName = current.Data.PlayerName;
				string specificRoleName = GetSpecificRoleName(current.Data.Role);
				bool num = flag && MMCIdentification.IsMMCPlayer(current.PlayerId);
				byte playerId = current.PlayerId;
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				bool flag3 = playerId == ((localPlayer != null) ? new byte?(localPlayer.PlayerId) : ((byte?)null));
				string value = (isImpostor ? "#FF3333" : "#99CCFF");
				_revealSusSb.Clear();
				if (num)
				{
					string value2 = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(Time.time * 0.5f % 1f, 0.7f, 1f));
					if (flag3)
					{
						_revealSusSb.Append("<color=#").Append(value2).Append(">★</color> ");
					}
					else
					{
						_revealSusSb.Append("<color=#").Append(value2).Append("><b>★ MMC ★</b></color> ");
					}
				}
				_revealSusSb.Append("<color=").Append(value).Append(">")
					.Append(playerName)
					.Append("</color>");
				_revealSusSb.Append("\n<size=60%><color=").Append(value).Append("><b>")
					.Append(specificRoleName)
					.Append("</b></color></size>");
				if (flag2 && isImpostor && !current.Data.IsDead)
				{
					float killTimer = current.killTimer;
					string value3;
					string value4;
					if (killTimer <= 0.1f)
					{
						value3 = "FF2222";
						value4 = "⚠ CAN KILL!";
					}
					else if (killTimer < 2f)
					{
						value3 = "FF4444";
						value4 = $"{killTimer:F1}s";
					}
					else if (killTimer < 5f)
					{
						value3 = "FF9922";
						value4 = $"{killTimer:F1}s";
					}
					else
					{
						value3 = "AAAAAA";
						value4 = $"{killTimer:F1}s";
					}
					_revealSusSb.Append("\n<size=50%><color=#").Append(value3).Append("><b>")
						.Append(value4)
						.Append("</b></color></size>");
				}
				string text = _revealSusSb.ToString();
				if (((TMP_Text)current.cosmetics.nameText).text != text)
				{
					((TMP_Text)current.cosmetics.nameText).text = text;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast("[RevealSus] Frame error: " + ex.Message));
		}
	}

	internal static void ChangePlayerName(PlayerControl player, string newName)
	{
		if ((Object)(object)player == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("Player not found."));
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("You must be host to use this cheat."));
			return;
		}
		try
		{
			player.Data.PlayerName = newName;
			player.Data.MarkDirty();
			MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)player).NetId, (byte)6, (SendOption)1, -1);
			val.Write(newName);
			val.Write(GenerateFakeToken(player.PlayerId));
			((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
			LogCheat($"Player {player.PlayerId} name changed to {newName} and synced to all.");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error changing name: {value}"));
		}
	}

	internal static void ToggleInvisibility(bool enable)
	{
		if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("Invisibility not enabled: local player not found."));
			return;
		}
		try
		{
			if (enable)
			{
				if (invisibilityCoroutine != null)
				{
					((MonoBehaviour)DestroyableSingleton<HudManager>.Instance).StopCoroutine(invisibilityCoroutine);
				}
				invisibilityCoroutine = MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, SendFakePositionCoroutine());
				((Component)PlayerControl.LocalPlayer.cosmetics.nameText).gameObject.SetActive(false);
				((Component)PlayerControl.LocalPlayer.cosmetics.hat).gameObject.SetActive(false);
				((Component)PlayerControl.LocalPlayer.cosmetics.skin).gameObject.SetActive(false);
				Debug.Log(InteropFix.Cast("Invisibility enabled (local only, no host)."));
			}
			else
			{
				if (invisibilityCoroutine != null)
				{
					((MonoBehaviour)DestroyableSingleton<HudManager>.Instance).StopCoroutine(invisibilityCoroutine);
					invisibilityCoroutine = null;
				}
				((Component)PlayerControl.LocalPlayer.cosmetics.nameText).gameObject.SetActive(true);
				((Component)PlayerControl.LocalPlayer.cosmetics.hat).gameObject.SetActive(true);
				((Component)PlayerControl.LocalPlayer.cosmetics.skin).gameObject.SetActive(true);
				Debug.Log(InteropFix.Cast("Invisibility disabled (local only, no host)."));
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error toggling invisibility: {value}"));
		}
	}

	private static IEnumerator SendFakePositionCoroutine()
	{
		Vector2 val = default(Vector2);
		while (true)
		{
			val._002Ector(1000f, 1000f);
			PlayerControl.LocalPlayer.NetTransform.SnapTo(val);
			yield return (object)new WaitForSeconds(0.2f);
		}
	}

	internal static void IncreaseVision(float multiplier = 3f)
	{
		zoomOutEnabled = true;
		customZoomValue = multiplier;
		Debug.Log(InteropFix.Cast($"[Cheat] Zoom Out enabled: {customZoomValue}"));
	}

	internal static void ResetSatelliteIfNotInGame()
	{
		try
		{
			if (_endGameActive && (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted)
			{
				_endGameActive = false;
			}
		}
		catch
		{
		}
	}

	internal static void ClearEndGameState()
	{
		_endGameActive = false;
	}

	internal static void ResetVision()
	{
		zoomOutEnabled = false;
		customZoomValue = _defaultOrthoSize;
		if ((Object)(object)Camera.main != (Object)null)
		{
			Camera.main.orthographicSize = _defaultOrthoSize;
		}
		Debug.Log(InteropFix.Cast("[Cheat] Zoom Out disabled"));
	}

	internal static void ForceUIRecalculation()
	{
		try
		{
			if ((Delegate)(object)ResolutionManager.ResolutionChanged != (Delegate)null)
			{
				ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / (float)Screen.height, Screen.width, Screen.height, Screen.fullScreen);
			}
		}
		catch
		{
		}
	}

	private static bool IsInBlockingUI()
	{
		try
		{
			if ((Object)(object)MeetingHud.Instance != (Object)null)
			{
				return true;
			}
			if ((Object)(object)ExileController.Instance != (Object)null)
			{
				return true;
			}
			if ((Object)(object)Minigame.Instance != (Object)null)
			{
				return true;
			}
			if ((Object)(object)MapBehaviour.Instance != (Object)null && MapBehaviour.Instance.IsOpen)
			{
				return true;
			}
			if ((Object)(object)PlayerCustomizationMenu.Instance != (Object)null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static void SafeMassKill()
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("You must be host to use this cheat."));
			return;
		}
		MonoBehaviourExtensions.StartCoroutine((MonoBehaviour)(object)DestroyableSingleton<HudManager>.Instance, KillAllCoroutine(crewOnly: true));
		LogCheat("Safe mass kill of crewmates started.");
	}

	private static IEnumerator KillAllCoroutine(bool crewOnly = false, bool impostorsOnly = false)
	{
		if ((Object)(object)ShipStatus.Instance == (Object)null || (Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).IsGameStarted || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			yield break;
		}
		List<PlayerControl> list = GetValidKillTargets(crewOnly, impostorsOnly);
		float baseDelay = 0f;
		foreach (PlayerControl target in list)
		{
			yield return (object)new WaitForSeconds(baseDelay);
			BroadcastKillBypass(PlayerControl.LocalPlayer, target, 0f);
				baseDelay += (float)new System.Random().Next(100, 300) / 1000f;
		}
	}

	private static bool ValidatePlayer(byte playerId)
	{
		return FindPlayerById(playerId) != null;
	}

	internal static void ForceStartGame()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		if ((Object)(object)AmongUsClient.Instance == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("[ForceStart] AmongUsClient not available."));
			return;
		}
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[ForceStart] Only the host can force start the game."));
			return;
		}
		if ((int)((InnerNetClient)AmongUsClient.Instance).GameState == 2)
		{
			Debug.LogWarning(InteropFix.Cast("[ForceStart] Game already in progress."));
			return;
		}
		try
		{
			int count = PlayerControl.AllPlayerControls.Count;
			if (count < 1)
			{
				Debug.LogWarning(InteropFix.Cast("[ForceStart] Not enough players."));
				return;
			}
			GameStartManager instance = DestroyableSingleton<GameStartManager>.Instance;
			if (instance != null)
			{
				PassiveButton startButton = instance.StartButton;
				if (startButton != null)
				{
					var onClick = startButton.OnClick;
					onClick?.Invoke();
				}
			}
			LogCheat($"Game force-started with {count} players!");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ForceStart] Error: {value}"));
		}
	}

	internal static void InstantStartGame()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Invalid comparison between Unknown and I4
		if (Time.time - _lastInstantStartTime < 3f || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost || (int)((InnerNetClient)AmongUsClient.Instance).GameState == 2)
		{
			return;
		}
		try
		{
			_lastInstantStartTime = Time.time;
			AmongUsClient.Instance.KickNotJoinedPlayers();
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)((instance != null) ? instance.GameMenu : null) != (Object)null && DestroyableSingleton<HudManager>.Instance.GameMenu.IsOpen)
			{
				DestroyableSingleton<HudManager>.Instance.GameMenu.Close();
			}
			AmongUsClient.Instance.StartGame();
			GameStartManager instance2 = DestroyableSingleton<GameStartManager>.Instance;
			if ((Object)(object)instance2 != (Object)null)
			{
				AmongUsClient.Instance.DisconnectHandlers.Remove(((Il2CppObjectBase)instance2).Cast<IDisconnectHandler>());
				Object.Destroy((Object)(object)((Component)instance2).gameObject);
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[InstantStart] Error: {value}"));
		}
	}

	internal static void KickPlayer(byte playerId, bool ban = false)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[Kick] Only the host can kick players."));
			return;
		}
		try
		{
			PlayerControl val = FindPlayerById(playerId);
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogWarning(InteropFix.Cast($"[Kick] Player {playerId} not found."));
				return;
			}
			if ((Object)(object)val == (Object)(object)PlayerControl.LocalPlayer)
			{
				Debug.LogWarning(InteropFix.Cast("[Kick] You cannot kick yourself."));
				return;
			}
			int ownerId = ((InnerNetObject)val).OwnerId;
			((InnerNetClient)AmongUsClient.Instance).KickPlayer(ownerId, ban);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Player ");
			NetworkedPlayerInfo data = val.Data;
			defaultInterpolatedStringHandler.AppendFormatted(((data != null) ? data.PlayerName : null) ?? playerId.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" was ");
			defaultInterpolatedStringHandler.AppendFormatted(ban ? "banned" : "kicked");
			defaultInterpolatedStringHandler.AppendLiteral("!");
			LogCheat(defaultInterpolatedStringHandler.ToStringAndClear());
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[Kick] Error: {value}"));
		}
	}

	internal static void KickPlayerByName(string playerName, bool ban = false)
	{
		PlayerControl val = FindPlayerByName(playerName);
		if ((Object)(object)val != (Object)null)
		{
			KickPlayer(val.PlayerId, ban);
		}
		else
		{
			Debug.LogWarning(InteropFix.Cast("[Kick] Player '" + playerName + "' not found."));
		}
	}

	internal static void KickPlayer(PlayerControl player, bool ban = false)
	{
		if (!((Object)(object)player == (Object)null))
		{
			KickPlayer(player.PlayerId, ban);
		}
	}

	internal static void ProtectPlayer(byte playerId, float duration = 10f)
	{
		if (!((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[ProtectPlayer] Only the HOST can protect players."));
			return;
		}
		if (!((Object)(object)ShipStatus.Instance != (Object)null) || InteropFix.Cast((Object)(object)MeetingHud.Instance))
		{
			Debug.LogWarning(InteropFix.Cast("[ProtectPlayer] Game must be in progress (not in lobby or meeting)."));
			return;
		}
		PlayerControl val = FindPlayerById(playerId);
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast($"[ProtectPlayer] Player {playerId} not found."));
			return;
		}
		NetworkedPlayerInfo data = val.Data;
		if (data != null && data.IsDead)
		{
			Debug.LogWarning(InteropFix.Cast("[ProtectPlayer] Cannot protect dead players."));
			return;
		}
		try
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			int? obj;
			if (localPlayer == null)
			{
				obj = null;
			}
			else
			{
				CosmeticsLayer cosmetics = localPlayer.cosmetics;
				obj = ((cosmetics != null) ? new int?(cosmetics.ColorId) : ((int?)null));
			}
			int? num = obj;
			int valueOrDefault = num.GetValueOrDefault();
			PlayerControl.LocalPlayer.RpcProtectPlayer(val, valueOrDefault);
			_protectedPlayerId = playerId;
			_protectionStartTime = Time.time;
			_protectionDuration = duration;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
			defaultInterpolatedStringHandler.AppendLiteral(" PROTECTION ON: ");
			NetworkedPlayerInfo data2 = val.Data;
			defaultInterpolatedStringHandler.AppendFormatted(((data2 != null) ? data2.PlayerName : null) ?? playerId.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" is protected for ");
			defaultInterpolatedStringHandler.AppendFormatted((duration > 0f) ? $"{duration}s" : "infinite");
			defaultInterpolatedStringHandler.AppendLiteral("!");
			LogCheat(defaultInterpolatedStringHandler.ToStringAndClear());
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ProtectPlayer] Error: {value}"));
		}
	}

	internal static void ProtectPlayerByName(string playerName, float duration = 10f)
	{
		PlayerControl val = FindPlayerByName(playerName);
		if ((Object)(object)val != (Object)null)
		{
			ProtectPlayer(val.PlayerId, duration);
		}
		else
		{
			Debug.LogWarning(InteropFix.Cast("[ProtectPlayer] Player '" + playerName + "' not found."));
		}
	}

	internal static void RemoveProtection()
	{
		if (!_protectedPlayerId.HasValue)
		{
			Debug.Log(InteropFix.Cast("[ProtectPlayer] No player is currently protected."));
			return;
		}
		PlayerControl val = FindPlayerById(_protectedPlayerId.Value);
		if ((Object)(object)val != (Object)null)
		{
			val.protectedByGuardianId = -1;
			NetworkedPlayerInfo data = val.Data;
			LogCheat("\ud83d\udee1\ufe0f PROTECTION REMOVED from " + ((data != null) ? data.PlayerName : null));
		}
		_protectedPlayerId = null;
	}

	internal static void UpdateProtection()
	{
		if (_protectedPlayerId.HasValue && !(_protectionDuration <= 0f) && Time.time - _protectionStartTime >= _protectionDuration)
		{
			RemoveProtection();
		}
	}

	internal static void ProtectSelf(float duration = 10f)
	{
		if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
		{
			ProtectPlayer(PlayerControl.LocalPlayer.PlayerId, duration);
		}
	}

	internal static PlayerControl GetProtectedPlayer()
	{
		if (!_protectedPlayerId.HasValue)
		{
			return null;
		}
		return FindPlayerById(_protectedPlayerId.Value);
	}

	private static PlayerControl FindPlayerById(byte playerId)
	{
		foreach (PlayerControl p in (Il2CppArrayBase<PlayerControl>)(object)PlayerControl.AllPlayerControls.ToArray())
		{
			if ((Object)(object)p != (Object)null && p.PlayerId == playerId)
			{
				return p;
			}
		}
		return null;
	}

	private static PlayerControl FindPlayerByName(string playerName)
	{
		if (string.IsNullOrWhiteSpace(playerName))
		{
			return null;
		}
		foreach (PlayerControl p in (Il2CppArrayBase<PlayerControl>)(object)PlayerControl.AllPlayerControls.ToArray())
		{
			if ((Object)(object)p == (Object)null || (Object)(object)p.Data == (Object)null)
			{
				continue;
			}
			string name = p.Data.PlayerName;
			if (!string.IsNullOrEmpty(name) && name.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
			{
				return p;
			}
		}
		return null;
	}

	private static List<PlayerControl> GetValidKillTargets(bool crewOnly, bool impostorsOnly)
	{
		List<PlayerControl> list = new List<PlayerControl>();
		foreach (PlayerControl target in (Il2CppArrayBase<PlayerControl>)(object)PlayerControl.AllPlayerControls.ToArray())
		{
			if ((Object)(object)target != (Object)(object)PlayerControl.LocalPlayer && IsValidKillTarget(target, crewOnly, impostorsOnly))
			{
				list.Add(target);
			}
		}
		return list;
	}

	internal static List<(byte PlayerId, string Name, bool IsImpostor)> GetAlivePlayersForProtection()
	{
		_alivePlayersBuffer.Clear();
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null) && !current.Data.IsDead)
			{
				List<(byte PlayerId, string Name, bool IsImpostor)> alivePlayersBuffer = _alivePlayersBuffer;
				byte playerId = current.PlayerId;
				string item = current.Data.PlayerName ?? $"Player {current.PlayerId}";
				RoleBehaviour role = current.Data.Role;
				alivePlayersBuffer.Add((playerId, item, role != null && role.IsImpostor));
			}
		}
		return _alivePlayersBuffer;
	}

	internal static void UpdateGodMode()
	{
		ConfigEntry<bool> godMode = CheatConfig.GodMode;
		if (godMode != null && godMode.Value && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmHost && !((Object)(object)ShipStatus.Instance == (Object)null) && !((Object)(object)MeetingHud.Instance != (Object)null))
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			if (!((Object)(object)localPlayer == (Object)null) && !((Object)(object)localPlayer.Data == (Object)null) && !localPlayer.Data.IsDead && Time.time - _lastGodModeProtectionTime >= 0.5f)
			{
				_lastGodModeProtectionTime = Time.time;
				ApplyGodModeProtection(localPlayer);
			}
		}
	}

	private static void ApplyGodModeProtection(PlayerControl target)
	{
		try
		{
			bool num = target.protectedByGuardianId >= 0;
			CosmeticsLayer cosmetics = target.cosmetics;
			int num2 = ((cosmetics != null) ? cosmetics.ColorId : 0);
			PlayerControl.LocalPlayer.RpcProtectPlayer(target, num2);
			if (!num)
			{
				NetworkedPlayerInfo data = target.Data;
				LogCheat("\ud83d\udd31 GOD MODE: Protection applied to " + ((data != null) ? data.PlayerName : null) + "!");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[GodMode] Error applying protection: " + ex.Message));
		}
	}

	internal static void UpdateMapEventRelay()
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		if (!MapEventRelayActive)
		{
			return;
		}
		if (Time.time - _mapEventRelayTime >= 3f)
		{
			MapEventRelayActive = false;
			PlayerPickMenu.TriggerRealtimeUpdate(force: true);
		}
		else
		{
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null || ((InnerNetClient)AmongUsClient.Instance).connection == null || !InteropFix.Cast((Object)(object)((InnerNetClient)AmongUsClient.Instance).GetHost().Character) || !((Object)(object)((InnerNetClient)AmongUsClient.Instance).GetHost().Character != (Object)(object)PlayerControl.LocalPlayer))
			{
				return;
			}
			try
			{
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					if (((InnerNetObject)current).AmOwner)
					{
						continue;
					}
					for (int i = 0; (float)i < 200f / ((float)((InnerNetClient)AmongUsClient.Instance).allClients.Count * 1.5f); i++)
					{
						MessageWriter val = MessageWriter.Get((SendOption)0);
						val.StartMessage((byte)6);
						val.Write(((InnerNetClient)AmongUsClient.Instance).GameId);
						val.WritePacked(((InnerNetObject)current).OwnerId);
						for (int j = 0; j < 10; j++)
						{
							val.StartMessage((byte)2);
							val.WritePacked(((InnerNetObject)PlayerControl.LocalPlayer).NetId);
							val.Write((byte)54);
							val.EndMessage();
						}
						val.EndMessage();
						((Connection)((InnerNetClient)AmongUsClient.Instance).connection).Send(val);
						val.Recycle();
					}
				}
			}
			catch
			{
			}
		}
	}

	internal static void ToggleMapEventRelay(bool enable)
	{
		MapEventRelayActive = enable;
		if (enable)
		{
			_mapEventRelayTime = Time.time;
		}
		LogCheat(enable ? "MER ON (3s)" : "MER OFF");
	}

	internal static float GetMapEventRelayRemaining()
	{
		if (!MapEventRelayActive)
		{
			return 0f;
		}
		float num = Time.time - _mapEventRelayTime;
		return Mathf.Max(0f, 3f - num);
	}

	internal static void UpdatePlayerEventRelays()
	{
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		if (_playerEventRelays.Count == 0)
		{
			return;
		}
		List<byte> list = new List<byte>();
		foreach (KeyValuePair<byte, float> playerEventRelay in _playerEventRelays)
		{
			if (Time.time - playerEventRelay.Value >= 3f)
			{
				list.Add(playerEventRelay.Key);
			}
		}
		if (list.Count > 0)
		{
			foreach (byte item in list)
			{
				_playerEventRelays.Remove(item);
			}
			PlayerPickMenu.TriggerRealtimeUpdate(force: true);
		}
		if (_playerEventRelays.Count == 0 || (Object)(object)PlayerControl.LocalPlayer == (Object)null || (Object)(object)AmongUsClient.Instance == (Object)null || ((InnerNetClient)AmongUsClient.Instance).connection == null)
		{
			return;
		}
		ClientData host = ((InnerNetClient)AmongUsClient.Instance).GetHost();
		if ((Object)(object)((host != null) ? host.Character : null) == (Object)null || (Object)(object)((InnerNetClient)AmongUsClient.Instance).GetHost().Character == (Object)(object)PlayerControl.LocalPlayer)
		{
			return;
		}
		foreach (KeyValuePair<byte, float> playerEventRelay2 in _playerEventRelays)
		{
			PlayerControl val = null;
			var enumerator3 = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator3.MoveNext())
			{
				PlayerControl current4 = enumerator3.Current;
				if (current4.PlayerId == playerEventRelay2.Key)
				{
					val = current4;
					break;
				}
			}
			if ((Object)(object)val == (Object)null || ((InnerNetObject)val).AmOwner)
			{
				continue;
			}
			try
			{
				for (int i = 0; i < 200; i++)
				{
					MessageWriter val2 = MessageWriter.Get((SendOption)0);
					val2.StartMessage((byte)6);
					val2.Write(((InnerNetClient)AmongUsClient.Instance).GameId);
					val2.WritePacked(((InnerNetObject)val).OwnerId);
					for (int j = 0; j < 10; j++)
					{
						val2.StartMessage((byte)2);
						val2.WritePacked(((InnerNetObject)PlayerControl.LocalPlayer).NetId);
						val2.Write((byte)54);
						val2.EndMessage();
					}
					val2.EndMessage();
					((Connection)((InnerNetClient)AmongUsClient.Instance).connection).Send(val2);
					val2.Recycle();
				}
			}
			catch
			{
			}
		}
	}

	internal static void TogglePlayerEventRelay(byte playerId)
	{
		if (_playerEventRelays.ContainsKey(playerId))
		{
			_playerEventRelays.Remove(playerId);
			LogCheat($"PER OFF {playerId}");
		}
		else
		{
			_playerEventRelays[playerId] = Time.time;
			LogCheat($"PER ON {playerId} (3s)");
		}
	}

	internal static bool IsPlayerEventRelayActive(byte playerId)
	{
		return _playerEventRelays.ContainsKey(playerId);
	}

	internal static float GetPlayerEventRelayRemaining(byte playerId)
	{
		if (!_playerEventRelays.TryGetValue(playerId, out var value))
		{
			return 0f;
		}
		float num = Time.time - value;
		return Mathf.Max(0f, 3f - num);
	}

	internal static Dictionary<byte, float> GetActivePlayerEventRelays()
	{
		Dictionary<byte, float> dictionary = new Dictionary<byte, float>();
		foreach (KeyValuePair<byte, float> playerEventRelay in _playerEventRelays)
		{
			float value = Mathf.Max(0f, 3f - (Time.time - playerEventRelay.Value));
			dictionary[playerEventRelay.Key] = value;
		}
		return dictionary;
	}

	internal static void HostSetPlayerColor(PlayerControl target, int colorId)
	{
		if (!ModKeyValidator.IsPremium || !ModKeyValidator.V())
		{
			NotifyUtils.Warning("★ Set Player Color is PREMIUM ONLY!");
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetColor] Only the HOST can use this cheat."));
			return;
		}
		if ((Object)(object)((target != null) ? target.Data : null) == (Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetColor] Invalid player."));
			return;
		}
		try
		{
			byte b = (byte)Math.Max(0, Math.Min(colorId, ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length - 1));
			target.RpcSetColor(b);
			LogCheat($"\ud83c\udfa8 Cor de {target.Data.PlayerName} mudada para {b} (sync broadcast)");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[HostSetColor] Erro: {value}"));
		}
	}

	internal static void HostSetAllPlayersColor(int colorId)
	{
		if (!ModKeyValidator.IsPremium || !ModKeyValidator.V())
		{
			NotifyUtils.Warning("★ Set All Colors is PREMIUM ONLY!");
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetAllColors] Only the HOST can use this."));
			return;
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)((current != null) ? current.Data : null) != (Object)null && !current.Data.Disconnected)
			{
				HostSetPlayerColorInternal(current, colorId);
			}
		}
		LogCheat($"\ud83c\udfa8 All players now have color {colorId}!");
	}

	private static void HostSetPlayerColorInternal(PlayerControl target, int colorId)
	{
		if ((Object)(object)((target != null) ? target.Data : null) == (Object)null)
		{
			return;
		}
		try
		{
			byte b = (byte)Math.Max(0, Math.Min(colorId, ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length - 1));
			target.RpcSetColor(b);
		}
		catch
		{
		}
	}

	internal static void HostRandomizeAllColors()
	{
		if (!ModKeyValidator.IsPremium || !ModKeyValidator.V())
		{
			NotifyUtils.Warning("★ System.Randomize Colors is PREMIUM ONLY!");
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostRandomizeColors] Only the HOST can use this."));
			return;
		}
		int length = ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length;
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)((current != null) ? current.Data : null) != (Object)null && !current.Data.Disconnected)
			{
					HostSetPlayerColorInternal(current, new System.Random().Next(0, length));
			}
		}
		LogCheat("\ud83c\udfb2 Cores de todos os jogadores System.Randomizadas!");
	}

	internal static void HostSetPlayerName(PlayerControl target, string newName)
	{
		Debug.LogWarning(InteropFix.Cast("[HostSetName] DISABLED - server validates names!"));
		LogCheat("⚠\ufe0f Set Name disabled - server reverts!");
	}

	internal static void HostSetAllNamesHacked()
	{
		Debug.LogWarning(InteropFix.Cast("[HostSetAllNames] DISABLED - server validates names!"));
		LogCheat("⚠\ufe0f Set All Names disabled - server reverts!");
	}

	internal static void HostSetPlayerHat(PlayerControl target, string hatId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetHat] Only the HOST can use this."));
		}
		else if (!((Object)(object)((target != null) ? target.Data : null) == (Object)null))
		{
			try
			{
				target.RpcSetHat(hatId);
				LogCheat($"\ud83c\udfa9 Hat de {target.Data.PlayerName} mudado para {hatId} (sync broadcast)");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HostSetHat] Erro: {value}"));
			}
		}
	}

	internal static void HostSetPlayerSkin(PlayerControl target, string skinId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetSkin] Only the HOST can use this."));
		}
		else if (!((Object)(object)((target != null) ? target.Data : null) == (Object)null))
		{
			try
			{
				target.RpcSetSkin(skinId);
				LogCheat($"\ud83d\udc55 Skin de {target.Data.PlayerName} mudada para {skinId} (sync broadcast)");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HostSetSkin] Erro: {value}"));
			}
		}
	}

	internal static void HostSetPlayerPet(PlayerControl target, string petId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetPet] Only the HOST can use this."));
		}
		else if (!((Object)(object)((target != null) ? target.Data : null) == (Object)null))
		{
			try
			{
				target.RpcSetPet(petId);
				LogCheat($"\ud83d\udc3e Pet de {target.Data.PlayerName} mudado para {petId} (sync broadcast)");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HostSetPet] Erro: {value}"));
			}
		}
	}

	internal static void HostSetPlayerVisor(PlayerControl target, string visorId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetVisor] Only the HOST can use this."));
		}
		else if (!((Object)(object)((target != null) ? target.Data : null) == (Object)null))
		{
			try
			{
				target.RpcSetVisor(visorId);
				LogCheat($"\ud83d\udc53 Visor de {target.Data.PlayerName} mudado para {visorId} (sync broadcast)");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HostSetVisor] Erro: {value}"));
			}
		}
	}

	internal static void HostSetPlayerNameplate(PlayerControl target, string nameplateId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.LogWarning(InteropFix.Cast("[HostSetNameplate] Only the HOST can use this."));
		}
		else if (!((Object)(object)((target != null) ? target.Data : null) == (Object)null))
		{
			try
			{
				target.Data.DefaultOutfit.NamePlateId = nameplateId;
				var defaultOutfit = target.Data.DefaultOutfit;
				byte namePlateSequenceId = defaultOutfit.NamePlateSequenceId;
				defaultOutfit.NamePlateSequenceId = (byte)(namePlateSequenceId + 1);
				target.Data.MarkDirty();
				LogCheat("\ud83c\udff7\ufe0f Nameplate de " + target.Data.PlayerName + " mudado para " + nameplateId);
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[HostSetNameplate] Erro: {value}"));
			}
		}
	}

	internal static void HostStripPlayerCosmetics(PlayerControl target)
	{
		Debug.LogWarning(InteropFix.Cast("[HostStripCosmetics] DISABLED - causes kick on online server!"));
		LogCheat("⚠\ufe0f Strip Cosmetics disabled - causes kick!");
	}

	internal static void HostStripAllCosmetics()
	{
		Debug.LogWarning(InteropFix.Cast("[HostStripAllCosmetics] DISABLED - causes kick on online server!"));
		LogCheat("⚠\ufe0f Strip All Cosmetics disabled - causes kick!");
	}

	internal static List<(int Id, string Name)> GetAvailableColors()
	{
		List<(int, string)> list = new List<(int, string)>();
		string[] array = new string[18]
		{
			"Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown",
			"Cyan", "Lime", "Maroon", "Rose", "Banana", "Gray", "Tan", "Coral"
		};
		for (int i = 0; i < Math.Min(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length, array.Length); i++)
		{
			list.Add((i, array[i]));
		}
		return list;
	}

	internal static void ToggleFreeCam(bool enable)
	{
		FreeCamEnabled = enable;
		LogCheat(enable ? "FreeCam ATIVADO - WASD Mover, Q/E Zoom" : "FreeCam DESATIVADO");
	}

	internal static void UpdateFreeCam()
	{
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Camera.main == (Object)null)
		{
			return;
		}
		FollowerCamera component = ((Component)Camera.main).gameObject.GetComponent<FollowerCamera>();
		if (FreeCamEnabled)
		{
			bool flag = false;
			try
			{
				flag = (Object)(object)MeetingHud.Instance != (Object)null || (Object)(object)ExileController.Instance != (Object)null;
			}
			catch
			{
			}
			if (flag)
			{
				if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}
				return;
			}
			if (!freecamActive)
			{
				if ((Object)(object)component != (Object)null)
				{
					((Behaviour)component).enabled = false;
					component.Target = null;
				}
				freecamActive = true;
				Debug.Log(InteropFix.Cast("[FreeCam] Ativado"));
			}
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)((instance != null) ? instance.ShadowQuad : null) != (Object)null && ((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.activeSelf)
			{
				((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(false);
			}
			bool flag2 = false;
			try
			{
				HudManager instance2 = DestroyableSingleton<HudManager>.Instance;
				flag2 = (Object)(object)((instance2 != null) ? instance2.Chat : null) != (Object)null && DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening;
			}
			catch
			{
			}
			if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				PlayerControl.LocalPlayer.moveable = flag2;
			}
			if (!flag2)
			{
				Vector3 val = default(Vector3);
				val._002Ector(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);
				float num = freeCamSpeed;
				if (Input.GetKey((KeyCode)304))
				{
					num *= 2.5f;
				}
				if (Input.GetKey((KeyCode)306))
				{
					num *= 0.3f;
				}
				((Component)Camera.main).transform.position = ((Component)Camera.main).transform.position + val * num * Time.deltaTime;
				if (Input.GetKey((KeyCode)113))
				{
					Camera main = Camera.main;
					main.orthographicSize += 5f * Time.deltaTime;
				}
				if (Input.GetKey((KeyCode)101))
				{
					Camera main2 = Camera.main;
					main2.orthographicSize -= 5f * Time.deltaTime;
				}
				float y = Input.mouseScrollDelta.y;
				if (y != 0f)
				{
					Camera main3 = Camera.main;
					main3.orthographicSize -= y * 2f;
				}
				Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 1f, 30f);
			}
		}
		else
		{
			if (!freecamActive)
			{
				return;
			}
			if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				PlayerControl.LocalPlayer.moveable = true;
			}
			if ((Object)(object)component != (Object)null)
			{
				((Behaviour)component).enabled = true;
				if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
				{
					component.SetTarget((MonoBehaviour)(object)PlayerControl.LocalPlayer);
				}
			}
			if ((Object)(object)Camera.main != (Object)null)
			{
				Camera.main.orthographicSize = 3f;
			}
			HudManager instance3 = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)((instance3 != null) ? instance3.UICamera : null) != (Object)null)
			{
				DestroyableSingleton<HudManager>.Instance.UICamera.orthographicSize = 3f;
			}
			HudManager instance4 = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)((instance4 != null) ? instance4.ShadowQuad : null) != (Object)null)
			{
				((Component)DestroyableSingleton<HudManager>.Instance.ShadowQuad).gameObject.SetActive(true);
			}
			freecamActive = false;
			Debug.Log(InteropFix.Cast("[FreeCam] Disabled - camera, movement and vision restored"));
		}
	}

	internal static void UpdateSatelliteScroll()
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Camera.main == (Object)null || FreeCamEnabled || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).IsGameStarted)
		{
			return;
		}
		try
		{
			if ((Object)(object)Minigame.Instance != (Object)null || (Object)(object)MeetingHud.Instance != (Object)null || (Object)(object)ExileController.Instance != (Object)null)
			{
				return;
			}
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)instance == (Object)null || ((Object)(object)instance.Chat != (Object)null && instance.Chat.IsOpenOrOpening) || (Object)(object)PlayerCustomizationMenu.Instance != (Object)null || ((Object)(object)MapBehaviour.Instance != (Object)null && MapBehaviour.Instance.IsOpen))
			{
				return;
			}
		}
		catch
		{
			return;
		}
		if (Input.GetMouseButtonDown(2))
		{
			CheatConfig.VisionMultiplier = _defaultOrthoSize;
			ServerData.SetSliderValueInternal("cheat_vision", _defaultOrthoSize);
			try
			{
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				return;
			}
			catch
			{
				return;
			}
		}
		float y = Input.mouseScrollDelta.y;
		if (y == 0f)
		{
			return;
		}
		float visionMultiplier = CheatConfig.VisionMultiplier;
		float num = visionMultiplier - y * 0.5f;
		num = Mathf.Clamp(num, 3f, 15f);
		if (Mathf.Abs(num - visionMultiplier) < 0.01f)
		{
			return;
		}
		CheatConfig.VisionMultiplier = num;
		ServerData.SetSliderValueInternal("cheat_vision", num);
		try
		{
			PlayerPickMenu.TriggerRealtimeUpdate(force: true);
		}
		catch
		{
		}
	}

	private static void AdjustResolution()
	{
		try
		{
			var resolutionChanged = ResolutionManager.ResolutionChanged;
			if (resolutionChanged != null)
			{
				resolutionChanged.Invoke((float)Screen.width / (float)Screen.height, Screen.width, Screen.height, Screen.fullScreen);
			}
		}
		catch
		{
		}
	}

	internal static void ToggleTracers(bool enable)
	{
		TracersEnabled = enable;
		if (enable)
		{
			InitializeTracers();
		}
		else
		{
			CleanupTracers();
		}
		LogCheat(enable ? "Tracers ENABLED (2026 Premium)" : "Tracers DISABLED");
	}

	private static void InitializeTracers()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		if ((Object)(object)_tracerContainer == (Object)null)
		{
			_tracerContainer = new GameObject("TracerContainer");
			Object.DontDestroyOnLoad((Object)(object)_tracerContainer);
		}
		if ((Object)(object)_tracerMaterial == (Object)null)
		{
			_tracerMaterial = new Material(Shader.Find("Sprites/Default"));
		}
		if ((Object)(object)_glowMaterial == (Object)null)
		{
			try
			{
				_glowMaterial = new Material(Shader.Find("Sprites/Default"));
			}
			catch
			{
				_glowMaterial = _tracerMaterial;
			}
		}
	}

	private static void CleanupTracers()
	{
		foreach (KeyValuePair<byte, LineRenderer> tracerLine in _tracerLines)
		{
			LineRenderer value = tracerLine.Value;
			if ((Object)(object)((value != null) ? ((Component)value).gameObject : null) != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)tracerLine.Value).gameObject);
			}
		}
		_tracerLines.Clear();
		foreach (KeyValuePair<byte, LineRenderer> item in _tracerGlow)
		{
			LineRenderer value2 = item.Value;
			if ((Object)(object)((value2 != null) ? ((Component)value2).gameObject : null) != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)item.Value).gameObject);
			}
		}
		_tracerGlow.Clear();
		foreach (KeyValuePair<byte, LineRenderer> impostorOutline in _impostorOutlines)
		{
			LineRenderer value3 = impostorOutline.Value;
			if ((Object)(object)((value3 != null) ? ((Component)value3).gameObject : null) != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)impostorOutline.Value).gameObject);
			}
		}
		_impostorOutlines.Clear();
		if ((Object)(object)_tracerContainer != (Object)null)
		{
			Object.Destroy((Object)(object)_tracerContainer);
			_tracerContainer = null;
		}
	}

	internal static void UpdateTracers()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0597: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (Object)(object)ShipStatus.Instance != (Object)null || (Object)(object)LobbyBehaviour.Instance != (Object)null;
		if (!TracersEnabled || !flag || (Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			if ((Object)(object)_tracerContainer != (Object)null)
			{
				CleanupTracers();
			}
			return;
		}
		if ((Object)(object)_tracerContainer == (Object)null)
		{
			InitializeTracers();
		}
		_tracerPulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
		Vector3 position = ((Component)PlayerControl.LocalPlayer).transform.position;
		Vector3 val = default(Vector3);
		val._002Ector(position.x, position.y, -1f);
		_tracerActiveIdsBuffer.Clear();
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		Color startColor = default(Color);
		Color endColor = default(Color);
		Color val3 = default(Color);
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current == (Object)null || (Object)(object)current == (Object)(object)PlayerControl.LocalPlayer || (Object)(object)current.Data == (Object)null || current.Data.IsDead)
			{
				continue;
			}
			byte playerId = current.PlayerId;
			_tracerActiveIdsBuffer.Add(playerId);
			Vector3 position2 = ((Component)current).transform.position;
			CosmeticsLayer cosmetics = current.cosmetics;
			int num = ((cosmetics != null) ? cosmetics.ColorId : 0);
			Color val2 = ((num >= 0 && num < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length) ? InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[num]) : Color.white);
			RoleBehaviour role = current.Data.Role;
			int num2;
			if (role == null)
			{
				num2 = 0;
			}
			else
			{
				num2 = (role.IsImpostor ? 1 : 0);
				if (num2 != 0)
				{
					startColor._002Ector(0f, 1f, 1f, 0.95f);
					endColor._002Ector(1f, 0.2f, 0.2f, 0.85f);
					val3._002Ector(1f, 0f, 0f, 0.25f + _tracerPulse * 0.15f);
					goto IL_024d;
				}
			}
			startColor._002Ector(1f, 1f, 1f, 0.8f);
			endColor._002Ector(val2.r, val2.g, val2.b, 0.7f);
			val3._002Ector(val2.r, val2.g, val2.b, 0.15f + _tracerPulse * 0.1f);
			goto IL_024d;
			IL_024d:
			if (!_tracerGlow.TryGetValue(playerId, out var value) || (Object)(object)value == (Object)null)
			{
				GameObject val4 = new GameObject($"TracerGlow_{playerId}");
				val4.transform.SetParent(_tracerContainer.transform);
				value = val4.AddComponent<LineRenderer>();
				((Renderer)value).material = _glowMaterial;
				value.positionCount = 2;
				((Renderer)value).sortingOrder = 3;
				value.numCapVertices = 4;
				_tracerGlow[playerId] = value;
			}
			value.SetPosition(0, new Vector3(val.x, val.y, -0.8f));
			value.SetPosition(1, new Vector3(position2.x, position2.y, -0.8f));
			value.startWidth = 0.15f;
			value.endWidth = 0.075f;
			value.startColor = val3;
			value.endColor = new Color(val3.r, val3.g, val3.b, val3.a * 0.3f);
			((Component)value).gameObject.SetActive(true);
			LineRenderer value3;
			if (num2 != 0)
			{
				if (!_impostorOutlines.TryGetValue(playerId, out var value2) || (Object)(object)value2 == (Object)null)
				{
					GameObject val5 = new GameObject($"ImpostorOutline_{playerId}");
					val5.transform.SetParent(_tracerContainer.transform);
					value2 = val5.AddComponent<LineRenderer>();
					((Renderer)value2).material = _tracerMaterial;
					value2.positionCount = 2;
					((Renderer)value2).sortingOrder = 4;
					value2.numCapVertices = 3;
					_impostorOutlines[playerId] = value2;
				}
				value2.SetPosition(0, new Vector3(val.x, val.y, -0.9f));
				value2.SetPosition(1, new Vector3(position2.x, position2.y, -0.9f));
				value2.startWidth = 0.12f;
				value2.endWidth = 0.072000004f;
				float num3 = 0.5f + _tracerPulse * 0.3f;
				value2.startColor = new Color(1f, 0f, 0f, num3);
				value2.endColor = new Color(0.8f, 0f, 0f, num3 * 0.5f);
				((Component)value2).gameObject.SetActive(true);
			}
			else if (_impostorOutlines.TryGetValue(playerId, out value3) && (Object)(object)value3 != (Object)null)
			{
				((Component)value3).gameObject.SetActive(false);
			}
			if (!_tracerLines.TryGetValue(playerId, out var value4) || (Object)(object)value4 == (Object)null)
			{
				GameObject val6 = new GameObject($"Tracer_{playerId}");
				val6.transform.SetParent(_tracerContainer.transform);
				value4 = val6.AddComponent<LineRenderer>();
				((Renderer)value4).material = _tracerMaterial;
				value4.positionCount = 2;
				((Renderer)value4).sortingOrder = 5;
				value4.numCapVertices = 4;
				_tracerLines[playerId] = value4;
			}
			value4.SetPosition(0, new Vector3(val.x, val.y, -1f));
			value4.SetPosition(1, new Vector3(position2.x, position2.y, -1f));
			value4.startWidth = 0.06f;
			value4.endWidth = 0.024f;
			value4.startColor = startColor;
			value4.endColor = endColor;
			((Component)value4).gameObject.SetActive(true);
		}
		_tracerRemoveBuffer.Clear();
		foreach (byte key in _tracerLines.Keys)
		{
			if (!_tracerActiveIdsBuffer.Contains(key))
			{
				LineRenderer obj = _tracerLines[key];
				if ((Object)(object)((obj != null) ? ((Component)obj).gameObject : null) != (Object)null)
				{
					((Component)_tracerLines[key]).gameObject.SetActive(false);
				}
				if (_tracerGlow.TryGetValue(key, out var value5) && (Object)(object)((value5 != null) ? ((Component)value5).gameObject : null) != (Object)null)
				{
					((Component)value5).gameObject.SetActive(false);
				}
				if (_impostorOutlines.TryGetValue(key, out var value6) && (Object)(object)((value6 != null) ? ((Component)value6).gameObject : null) != (Object)null)
				{
					((Component)value6).gameObject.SetActive(false);
				}
			}
		}
	}

	internal static void ToggleNoClipSmooth(bool enable)
	{
		NoClipSmoothEnabled = enable;
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if ((Object)(object)((localPlayer != null) ? localPlayer.Collider : null) != (Object)null)
		{
			((Behaviour)PlayerControl.LocalPlayer.Collider).enabled = !enable;
		}
		LogCheat(enable ? "NoClip Smooth ATIVADO - Use WASD para atravessar paredes" : "NoClip Smooth DESATIVADO");
	}

	internal static void UpdateNoClipSmooth()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		if (!NoClipSmoothEnabled || (Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			return;
		}
		try
		{
			if ((Object)(object)MeetingHud.Instance != (Object)null)
			{
				return;
			}
		}
		catch
		{
		}
		if ((Object)(object)PlayerControl.LocalPlayer.Collider != (Object)null && ((Behaviour)PlayerControl.LocalPlayer.Collider).enabled)
		{
			((Behaviour)PlayerControl.LocalPlayer.Collider).enabled = false;
		}
		Vector2 zero = Vector2.zero;
		if (Input.GetKey((KeyCode)119))
		{
			zero.y += 1f;
		}
		if (Input.GetKey((KeyCode)115))
		{
			zero.y -= 1f;
		}
		if (Input.GetKey((KeyCode)97))
		{
			zero.x -= 1f;
		}
		if (Input.GetKey((KeyCode)100))
		{
			zero.x += 1f;
		}
		if (!(zero == Vector2.zero))
		{
			float num = (Input.GetKey((KeyCode)304) ? (_noClipSpeed * 2f) : _noClipSpeed);
			Vector3 val = InteropFix.Cast(((Component)PlayerControl.LocalPlayer).transform.position);
			Vector2 val2 = zero.normalized * num * Time.deltaTime;
			val.x += val2.x;
			val.y += val2.y;
			((Component)PlayerControl.LocalPlayer).transform.position = new Vector3(val.x, val.y, ((Component)PlayerControl.LocalPlayer).transform.position.z);
			PlayerControl.LocalPlayer.NetTransform.SnapTo(new Vector2(val.x, val.y));
		}
	}

	public static void CleanupRadarTextures()
	{
		try
		{
			RadarEnabled = false;
			_radarInitialized = false;
			_mapTextureLoaded = false;
			if ((Object)(object)_texSkeldMap != (Object)null)
			{
				Object.Destroy((Object)(object)_texSkeldMap);
				_texSkeldMap = null;
			}
			if ((Object)(object)_texWindowBg != (Object)null)
			{
				Object.Destroy((Object)(object)_texWindowBg);
				_texWindowBg = null;
			}
			if ((Object)(object)_texHeaderBg != (Object)null)
			{
				Object.Destroy((Object)(object)_texHeaderBg);
				_texHeaderBg = null;
			}
			if ((Object)(object)_texMapBg != (Object)null)
			{
				Object.Destroy((Object)(object)_texMapBg);
				_texMapBg = null;
			}
			_sSkeldMapImage = null;
			_sWindowBg = null;
			_sHeaderBg = null;
			_sMapBg = null;
			_playerCacheCount = 0;
			_deadBodyCacheCount = 0;
			Debug.Log(InteropFix.Cast("[RADAR] FIX 2026: Textures cleaned up safely."));
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[RADAR] Error during cleanup: " + ex.Message));
		}
	}

	internal static void ToggleRadar(bool enable)
	{
		RadarEnabled = enable;
		LogCheat(enable ? "RADAR ATIVADO!" : "Radar desativado");
	}

	internal static void SetRadarScale(float scale)
	{
		SetRadarSize(Mathf.Lerp(200f, 500f, Mathf.InverseLerp(0.03f, 0.15f, scale)));
		LogCheat($"Radar scale: {scale:F2}");
	}

	internal static void SetRadarSize(float size)
	{
		size = Mathf.Clamp(size, 200f, 500f);
		_radarRect.width = size;
		_radarRect.height = size;
	}

	private static Texture2D MakeTex(Color c)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		Texture2D val = new Texture2D(1, 1);
		val.SetPixel(0, 0, c);
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		return val;
	}

	private static GUIStyle MakeStyle(Texture2D tex)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		GUIStyle val = new GUIStyle();
		val.normal.background = tex;
		return val;
	}

	private static Texture2D MakeGradientV(int h, Color top, Color bot)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(1, h);
		for (int i = 0; i < h; i++)
		{
			val.SetPixel(0, i, Color.Lerp(top, bot, (float)i / (float)(h - 1)));
		}
		val.Apply();
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		return val;
	}

	private static Texture2D MakeFrameTex(int w, int h, Color inner, Color border, int bw)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(w, h);
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				val.SetPixel(j, i, (j < bw || j >= w - bw || i < bw || i >= h - bw) ? border : inner);
			}
		}
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		return val;
	}

	private static void InitRadar()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Expected O, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Expected O, but got Unknown
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Expected O, but got Unknown
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Expected O, but got Unknown
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Expected O, but got Unknown
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		if (_radarInitialized)
		{
			return;
		}
		LoadMapTexture();
		_texWindowBg = MakeTex(ThemeBgDark);
		_texHeaderBg = MakeGradientV(24, ThemeHeaderTop, ThemeHeaderBot);
		_texMapBg = MakeTex(ThemeMapBg);
		_texBorderAccent = MakeTex(ThemeAccent);
		_texBorderDim = MakeTex(ThemeAccentDim);
		_texMapOverlay = MakeTex(ThemeMapOverlay);
		_texImpostorGlow = MakeTex(ThemeImpostorGlow);
		_texImpostorDot = MakeTex(ThemeImpostor);
		_texGhostDot = MakeTex(new Color(0.6f, 0.6f, 0.7f, 0.35f));
		_texDeadBodyBg = MakeTex(new Color(0.6f, 0.08f, 0.12f, 0.85f));
		_texVisor = MakeTex(ThemeVisor);
		_texShadow = MakeTex(new Color(0.01f, 0.01f, 0.02f, 0.92f));
		_texPlayers = (Texture2D[])(object)new Texture2D[18];
		for (int i = 0; i < 18; i++)
		{
			_texPlayers[i] = MakeTex(PlayerColors[i]);
		}
		_sWindowBg = MakeStyle(_texWindowBg);
		_sHeaderBg = MakeStyle(_texHeaderBg);
		_sMapBg = MakeStyle(_texMapBg);
		_sBorderAccent = MakeStyle(_texBorderAccent);
		_sBorderDim = MakeStyle(_texBorderDim);
		_sMapOverlay = MakeStyle(_texMapOverlay);
		_sImpostorGlow = MakeStyle(_texImpostorGlow);
		_sImpostorDot = MakeStyle(_texImpostorDot);
		_sGhostDot = MakeStyle(_texGhostDot);
		_sDeadBodyBg = MakeStyle(_texDeadBodyBg);
		_sVisor = MakeStyle(_texVisor);
		_sShadow = MakeStyle(_texShadow);
		_sPlayers = (GUIStyle[])(object)new GUIStyle[18];
		for (int j = 0; j < 18; j++)
		{
			_sPlayers[j] = MakeStyle(_texPlayers[j]);
		}
		_sLabel = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)4,
			fontStyle = (FontStyle)1,
			fontSize = 10
		};
		_sLabel.normal.textColor = ThemeTextPrimary;
		_sTitle = new GUIStyle(_sLabel)
		{
			fontSize = 12,
			fontStyle = (FontStyle)1
		};
		_sTitle.normal.textColor = ThemeAccent;
		_sSmall = new GUIStyle(_sLabel)
		{
			fontSize = 9,
			fontStyle = (FontStyle)0
		};
		_sSmall.normal.textColor = ThemeTextMuted;
		_sButton = new GUIStyle(GUI.skin.button)
		{
			fontSize = 11,
			alignment = (TextAnchor)4,
			fixedWidth = 20f,
			fixedHeight = 18f
		};
		_sButton.normal.background = MakeFrameTex(20, 18, new Color(0.06f, 0.06f, 0.08f, 0.98f), ThemeAccentDim, 1);
		_sButton.hover.background = MakeFrameTex(20, 18, new Color(0.12f, 0.03f, 0.06f, 0.98f), ThemeAccent, 1);
		_sButton.normal.textColor = ThemeTextPrimary;
		_sButton.hover.textColor = ThemeAccent;
		_texVent = MakeTex(new Color(0.2f, 0.8f, 0.6f, 0.9f));
		_texTracerLine = MakeTex(new Color(1f, 0.09f, 0.27f, 0.5f));
		_texGridDim = MakeTex(new Color(1f, 0.09f, 0.27f, 0.08f));
		_texGridBright = MakeTex(new Color(1f, 0.09f, 0.27f, 0.2f));
		_texVentStyle = MakeTex(new Color(0.2f, 0.8f, 0.6f, 0.9f));
		_texScanLine = MakeTex(new Color(1f, 0.09f, 0.27f, 0.18f));
		_sGridDim = MakeStyle(_texGridDim);
		_sGridBright = MakeStyle(_texGridBright);
		_sVentStyle = MakeStyle(_texVentStyle);
		_sScanLine = MakeStyle(_texScanLine);
		_texRoomFill = MakeTex(new Color(0.08f, 0.09f, 0.125f, 0.75f));
		_texRoomBorder = MakeTex(new Color(0.25f, 0.28f, 0.35f, 0.85f));
		_texCorridorFill = MakeTex(new Color(0.06f, 0.07f, 0.1f, 0.6f));
		_sRoomFill = MakeStyle(_texRoomFill);
		_sRoomBorder = MakeStyle(_texRoomBorder);
		_sCorridorFill = MakeStyle(_texCorridorFill);
		if ((Object)(object)_texSonarLine == (Object)null)
		{
			_texSonarLine = new Texture2D(2, 64, (TextureFormat)4, false);
			Color val = default(Color);
			for (int k = 0; k < 64; k++)
			{
				float num = 1f - (float)k / 64f;
				val._002Ector(0.2f, 1f, 0.4f, num * 0.6f);
				_texSonarLine.SetPixel(0, k, val);
				_texSonarLine.SetPixel(1, k, val);
			}
			_texSonarLine.Apply();
			((Texture)_texSonarLine).wrapMode = (TextureWrapMode)1;
			((Object)_texSonarLine).hideFlags = (HideFlags)61;
		}
		_sonarInitialized = true;
		_radarInitialized = true;
	}

	private static void LoadMapTexture()
	{
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		if (_mapTextureLoaded)
		{
			return;
		}
		_mapTextureLoaded = true;
		try
		{
			string skeldMapBase = RadarMapData.SkeldMapBase64;
			if (string.IsNullOrEmpty(skeldMapBase))
			{
				Debug.LogWarning(InteropFix.Cast("[RADAR] No map data found in RadarMapData"));
				_texSkeldMap = CreateFallbackTexture();
				return;
			}
			skeldMapBase = skeldMapBase.Trim().Replace("\r", "").Replace("\n", "");
			if (skeldMapBase.Length < 1000)
			{
				Debug.LogWarning(InteropFix.Cast($"[RADAR] Base64 string too short ({skeldMapBase.Length} chars), likely truncated. Expected ~19000 chars."));
				_texSkeldMap = CreateFallbackTexture();
				return;
			}
			byte[] array = Convert.FromBase64String(skeldMapBase);
			Debug.Log(InteropFix.Cast($"[RADAR] Map bytes decoded: {array.Length} bytes (from {skeldMapBase.Length} chars Base64)"));
			if (array.Length < 100)
			{
				Debug.LogWarning(InteropFix.Cast("[RADAR] Map data too small after decoding, likely corrupted"));
				_texSkeldMap = CreateFallbackTexture();
				return;
			}
			_texSkeldMap = new Texture2D(2, 2, (TextureFormat)4, false);
			if (!ImageConversion.LoadImage(_texSkeldMap, InteropFix.Cast(array)) || ((Texture)_texSkeldMap).width <= 4 || ((Texture)_texSkeldMap).height <= 4)
			{
				Debug.LogWarning(InteropFix.Cast($"[RADAR] Texture load failed or invalid size: {((Texture)_texSkeldMap).width}x{((Texture)_texSkeldMap).height}"));
				Object.Destroy((Object)(object)_texSkeldMap);
				_texSkeldMap = CreateFallbackTexture();
			}
			else
			{
				((Object)_texSkeldMap).hideFlags = (HideFlags)61;
				((Texture)_texSkeldMap).filterMode = (FilterMode)1;
				((Texture)_texSkeldMap).wrapMode = (TextureWrapMode)1;
				Debug.Log(InteropFix.Cast($"[RADAR] ✓ Skeld map texture loaded: {((Texture)_texSkeldMap).width}x{((Texture)_texSkeldMap).height}"));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[RADAR] Exception loading map texture: " + ex.GetType().Name + ": " + ex.Message));
			_texSkeldMap = CreateFallbackTexture();
		}
	}

	private static Texture2D CreateFallbackTexture()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Texture2D val = new Texture2D(64, 64, (TextureFormat)4, false);
			Color32 val2 = default(Color32);
			val2._002Ector((byte)40, (byte)40, (byte)50, (byte)220);
			Color32 val3 = default(Color32);
			val3._002Ector((byte)180, (byte)60, (byte)180, (byte)220);
			Color32[] array = (Color32[])(object)new Color32[4096];
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					bool flag = (j / 8 + i / 8) % 2 == 0;
					array[i * 64 + j] = (flag ? val2 : val3);
				}
			}
			val.SetPixels32(InteropFix.Cast(array));
			val.Apply();
			((Object)val).hideFlags = (HideFlags)61;
			((Texture)val).filterMode = (FilterMode)0;
			Debug.LogWarning(InteropFix.Cast("[RADAR] BLACKHAT FIX: Using checkerboard fallback texture - map asset corrupted or missing!"));
			return val;
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[RADAR] CreateFallbackTexture failed: " + ex.Message));
			return Texture2D.whiteTexture;
		}
	}

	private static void DrawMapImage(Rect mapArea)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_texSkeldMap == (Object)null))
		{
			if (_cachedMapStyle == null || (Object)(object)_cachedMapStyle.normal.background != (Object)(object)_texSkeldMap)
			{
				_cachedMapStyle = new GUIStyle();
				_cachedMapStyle.normal.background = _texSkeldMap;
			}
			float num = (float)((Texture)_texSkeldMap).width / (float)((Texture)_texSkeldMap).height;
			float num2 = mapArea.width / mapArea.height;
			Rect val = default(Rect);
			if (num > num2)
			{
				float num3 = mapArea.width / num;
				val._002Ector(mapArea.x, mapArea.y + (mapArea.height - num3) / 2f, mapArea.width, num3);
			}
			else
			{
				float num4 = mapArea.height * num;
				val._002Ector(mapArea.x + (mapArea.width - num4) / 2f, mapArea.y, num4, mapArea.height);
			}
			Color color = GUI.color;
			GUI.color = Color.white;
			try
			{
				GUI.Box(val, GUIContent.none, _cachedMapStyle);
			}
			catch
			{
			}
			GUI.color = color;
		}
	}

	private static void UpdateCache()
	{
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Invalid comparison between Unknown and I4
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		float time = Time.time;
		if (time - _lastRadarUpdate < 0.1f)
		{
			return;
		}
		_lastRadarUpdate = time;
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if ((Object)(object)localPlayer == (Object)null)
		{
			return;
		}
		Vector2 val = (_localPos = InteropFix.Cast(((Component)localPlayer).transform.position));
		UpdateMapInfo();
		_cachedAliveCount = 0;
		_playerCacheCount = 0;
		_nearestImpostorDist = float.MaxValue;
		try
		{
			var allPlayerControls = PlayerControl.AllPlayerControls;
			int count = allPlayerControls.Count;
			for (int i = 0; i < count; i++)
			{
				PlayerControl val2 = null;
				try
				{
					val2 = allPlayerControls[i];
				}
				catch
				{
					continue;
				}
				if ((Object)(object)val2 == (Object)null || (Object)(object)val2.Data == (Object)null || val2.PlayerId == localPlayer.PlayerId)
				{
					continue;
				}
				if (_playerCacheCount >= 15)
				{
					break;
				}
				bool flag = false;
				try
				{
					flag = val2.Data.IsDead;
				}
				catch
				{
					continue;
				}
				if (!flag)
				{
					_cachedAliveCount++;
				}
				if (flag && !RadarShowGhosts)
				{
					continue;
				}
				bool flag2 = false;
				bool flag3 = false;
				try
				{
					if ((Object)(object)val2.Data.Role != (Object)null)
					{
						flag2 = val2.Data.Role.IsImpostor;
						flag3 = true;
					}
				}
				catch
				{
				}
				if (!flag3)
				{
					try
					{
						if ((Object)(object)val2.Data.Role != (Object)null)
						{
							flag2 = (int)val2.Data.Role.TeamType == 1;
							flag3 = true;
						}
					}
					catch
					{
					}
				}
				if (!flag3)
				{
					try
					{
						flag2 = RoleManager.IsImpostorRole(val2.Data.RoleType);
					}
					catch
					{
					}
				}
				int num = 0;
				try
				{
					num = val2.Data.DefaultOutfit.ColorId;
				}
				catch
				{
				}
				Vector2 zero = Vector2.zero;
				try
				{
					zero = InteropFix.Cast(((Component)val2).transform.position);
				}
				catch
				{
					continue;
				}
				if (flag2 && !flag)
				{
					float num2 = zero.x - val.x;
					float num3 = zero.y - val.y;
					float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
					if (num4 < _nearestImpostorDist)
					{
						_nearestImpostorDist = num4;
					}
				}
				string name = "";
				try
				{
					name = ((val2.Data.PlayerName.Length > 6) ? val2.Data.PlayerName.Substring(0, 6) : val2.Data.PlayerName);
				}
				catch
				{
				}
				_playerCache[_playerCacheCount++] = new RadarPlayerData
				{
					x = zero.x,
					y = zero.y,
					isImpostor = flag2,
					isDead = flag,
					colorId = Mathf.Clamp(num, 0, 17),
					name = name,
					playerId = val2.PlayerId
				};
			}
			try
			{
				if (!localPlayer.Data.IsDead)
				{
					_cachedAliveCount++;
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
		_deadBodyCacheCount = 0;
		if (!RadarShowDeadBodies)
		{
			return;
		}
		try
		{
			var allPlayerControls2 = PlayerControl.AllPlayerControls;
			int count2 = allPlayerControls2.Count;
			for (int j = 0; j < count2; j++)
			{
				PlayerControl val3 = null;
				try
				{
					val3 = allPlayerControls2[j];
				}
				catch
				{
					continue;
				}
				if ((Object)(object)val3 == (Object)null || (Object)(object)val3.Data == (Object)null)
				{
					continue;
				}
				bool flag4 = false;
				try
				{
					flag4 = val3.Data.IsDead;
				}
				catch
				{
					continue;
				}
				if (flag4)
				{
					if (_deadBodyCacheCount >= 15)
					{
						break;
					}
					int num5 = 0;
					try
					{
						num5 = val3.Data.DefaultOutfit.ColorId;
					}
					catch
					{
					}
					string name2 = "";
					try
					{
						name2 = ((val3.Data.PlayerName.Length > 5) ? val3.Data.PlayerName.Substring(0, 5) : val3.Data.PlayerName);
					}
					catch
					{
					}
					Vector2 zero2 = Vector2.zero;
					try
					{
						zero2 = InteropFix.Cast(((Component)val3).transform.position);
					}
					catch
					{
						continue;
					}
					_deadBodyCache[_deadBodyCacheCount++] = new RadarDeadBodyData
					{
						x = zero2.x,
						y = zero2.y,
						colorId = Mathf.Clamp(num5, 0, 17),
						name = name2
					};
				}
			}
		}
		catch
		{
		}
	}

	private static void UpdateMapInfo()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)ShipStatus.Instance != (Object)null)
			{
				_currentMapType = (int)ShipStatus.Instance.Type;
				_cachedMapName = GetMapName(_currentMapType);
				return;
			}
			GameOptionsManager instance = GameOptionsManager.Instance;
			if (((instance != null) ? instance.CurrentGameOptions : null) != null)
			{
				_currentMapType = (int)GameOptionsManager.Instance.CurrentGameOptions.MapId;
				_cachedMapName = GetMapName(_currentMapType);
			}
			else if ((Object)(object)LobbyBehaviour.Instance != (Object)null)
			{
				_cachedMapName = "LOBBY";
			}
		}
		catch
		{
		}
	}

	private static string GetMapName(int mapId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		if (mapId == 0)
		{
			return "THE SKELD";
		}
		if (mapId == 1)
		{
			return "MIRA HQ";
		}
		if (mapId == 2)
		{
			return "POLUS";
		}
		if (mapId == 3)
		{
			return "THE FUNGLE";
		}
		if (mapId == 4)
		{
			return "AIRSHIP";
		}
		return "UNKNOWN";
	}

	private static Vector2 WorldToRadar(float worldX, float worldY, Rect mapArea)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		if (!MapInfos.TryGetValue(_currentMapType, out var value))
		{
			value = new MapInfo(0f, 0f, 4f, -20f, 20f, -15f, 10f);
		}
		float num = value.maxX - value.minX;
		float num2 = value.maxY - value.minY;
		float num3 = mapArea.width / num;
		float num4 = mapArea.height / num2;
		float num5 = Mathf.Min(num3, num4);
		float num6 = (mapArea.width - num * num5) * 0.5f;
		float num7 = (mapArea.height - num2 * num5) * 0.5f;
		float num8 = mapArea.x + num6 + (worldX - value.minX) * num5;
		float num9 = mapArea.y + num7 + (value.maxY - worldY) * num5;
		return new Vector2(num8, num9);
	}

	private static void HandleInput(float rx, float ry, float rw, float rh)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = InteropFix.Cast(Input.mousePosition);
		val.y = (float)Screen.height - val.y;
		Rect val2 = default(Rect);
		val2._002Ector(rx, ry, rw, rh);
		Rect val3 = default(Rect);
		val3._002Ector(rx, ry, rw - 44f, 24f);
		Rect val4 = default(Rect);
		val4._002Ector(rx + rw - 20f, ry + rh - 20f, 20f, 20f);
		Rect val5 = default(Rect);
		val5._002Ector(rx + 4f, ry + 24f + 2f, rw - 8f, rh - 24f - 18f - 4f);
		bool flag = false;
		try
		{
			flag = (Object)(object)MeetingHud.Instance != (Object)null;
		}
		catch
		{
		}
		try
		{
			if (!flag && val2.Contains(val) && (Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				if (PlayerControl.LocalPlayer.moveable)
				{
					PlayerControl.LocalPlayer.moveable = false;
					_radarBlockedMovement = true;
				}
			}
			else if (_radarBlockedMovement && !_isDragging && !_isResizing && (Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				PlayerControl.LocalPlayer.moveable = true;
				_radarBlockedMovement = false;
			}
		}
		catch
		{
			_radarBlockedMovement = false;
		}
		if (!RadarLocked && !RadarMinimized && val2.Contains(val))
		{
			float y = Input.mouseScrollDelta.y;
			if (y != 0f)
			{
				float num = 20f;
				float num2 = Mathf.Clamp(_radarRect.width + y * num, 200f, 500f);
				float width = _radarRect.width;
				float height = _radarRect.height;
				_radarRect.width = num2;
				_radarRect.height = num2;
				float num3 = (val.x - rx) / width;
				float num4 = (val.y - ry) / height;
				_radarRect.x = Mathf.Clamp(val.x - num3 * num2, 0f, (float)Screen.width - num2);
				_radarRect.y = Mathf.Clamp(val.y - num4 * num2, 0f, (float)Screen.height - num2);
			}
		}
		if (!RadarLocked)
		{
			if (Input.GetMouseButtonDown(0) && val4.Contains(val))
			{
				_isResizing = true;
				_resizeStart = val;
				_resizeStartRect = _radarRect;
			}
			if (_isResizing && Input.GetMouseButton(0))
			{
				Vector2 val6 = val - _resizeStart;
				float height2 = (_radarRect.width = Mathf.Clamp(_resizeStartRect.width + Mathf.Max(val6.x, val6.y), 200f, 500f));
				_radarRect.height = height2;
			}
			if (Input.GetMouseButtonUp(0))
			{
				_isResizing = false;
			}
		}
		if (!RadarLocked && !_isResizing)
		{
			if (Input.GetMouseButtonDown(0) && val3.Contains(val))
			{
				_isDragging = true;
				_dragOffset = val - new Vector2(rx, ry);
			}
			if (_isDragging && Input.GetMouseButton(0))
			{
				_radarRect.x = Mathf.Clamp(val.x - _dragOffset.x, 0f, (float)Screen.width - rw);
				_radarRect.y = Mathf.Clamp(val.y - _dragOffset.y, 0f, (float)Screen.height - rh);
			}
			if (Input.GetMouseButtonUp(0))
			{
				_isDragging = false;
			}
		}
		if (!RadarRightClickTP || RadarMinimized || flag || !Input.GetMouseButtonDown(1) || !val5.Contains(val) || Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)306) || !MapInfos.TryGetValue(_currentMapType, out var value))
		{
			return;
		}
		float num6 = (val.x - val5.x) / val5.width;
		float num7 = 1f - (val.y - val5.y) / val5.height;
		Vector2 val7 = default(Vector2);
		val7._002Ector(value.minX + num6 * (value.maxX - value.minX), value.minY + num7 * (value.maxY - value.minY));
		try
		{
			PlayerControl.LocalPlayer.NetTransform.SnapTo(val7);
			if ((Object)(object)AmongUsClient.Instance != (Object)null)
			{
				MessageWriter val8 = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)21, (SendOption)1, -1);
				NetHelpers.WriteVector2(val7, val8);
				val8.Write(PlayerControl.LocalPlayer.NetTransform.lastSequenceId);
				((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val8);
			}
		}
		catch
		{
		}
	}

	internal static void UpdateRadarLogic()
	{
		try
		{
			if (RadarEnabled && !((Object)(object)PlayerControl.LocalPlayer == (Object)null))
			{
				_radarFrame = (_radarFrame + 1) % 180;
				_pulseTime += Time.deltaTime;
				if (Time.time - _lastCacheUpdateTime >= 0.1f)
				{
					_lastCacheUpdateTime = Time.time;
					UpdateCache();
				}
			}
		}
		catch
		{
		}
	}

	internal static void DrawRadar()
	{
		//IL_0872: Unknown result type (might be due to invalid IL or missing references)
		//IL_0874: Unknown result type (might be due to invalid IL or missing references)
		//IL_0879: Unknown result type (might be due to invalid IL or missing references)
		//IL_087d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0889: Unknown result type (might be due to invalid IL or missing references)
		//IL_0896: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_090b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0915: Unknown result type (might be due to invalid IL or missing references)
		//IL_0922: Unknown result type (might be due to invalid IL or missing references)
		//IL_0939: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0986: Unknown result type (might be due to invalid IL or missing references)
		//IL_0988: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0991: Unknown result type (might be due to invalid IL or missing references)
		//IL_09be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0daa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f52: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f90: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1025: Unknown result type (might be due to invalid IL or missing references)
		//IL_1043: Unknown result type (might be due to invalid IL or missing references)
		//IL_1060: Unknown result type (might be due to invalid IL or missing references)
		//IL_1092: Unknown result type (might be due to invalid IL or missing references)
		//IL_10b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1104: Unknown result type (might be due to invalid IL or missing references)
		//IL_1122: Unknown result type (might be due to invalid IL or missing references)
		//IL_113f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1197: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a19: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b53: Unknown result type (might be due to invalid IL or missing references)
		//IL_1247: Unknown result type (might be due to invalid IL or missing references)
		//IL_1297: Unknown result type (might be due to invalid IL or missing references)
		//IL_12b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cab: Unknown result type (might be due to invalid IL or missing references)
		//IL_1336: Unknown result type (might be due to invalid IL or missing references)
		//IL_1368: Unknown result type (might be due to invalid IL or missing references)
		//IL_1391: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_05be: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e9: Invalid comparison between Unknown and I4
		//IL_05f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_081a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!RadarEnabled)
			{
				return;
			}
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null)
			{
				RadarEnabled = false;
				Debug.LogWarning(InteropFix.Cast("[RADAR] FIX 2026: Auto-disabled - LocalPlayer is null"));
				return;
			}
			if ((Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted && (Object)(object)ShipStatus.Instance == (Object)null)
			{
				RadarEnabled = false;
				Debug.LogWarning(InteropFix.Cast("[RADAR] FIX 2026: Auto-disabled - ShipStatus is null during game"));
				return;
			}
			InitRadar();
			float x = _radarRect.x;
			float y = _radarRect.y;
			float width = _radarRect.width;
			float num = (RadarMinimized ? 28f : _radarRect.height);
			HandleInput(x, y, width, _radarRect.height);
			GUI.Box(new Rect(x + 3f, y + 3f, width, num), GUIContent.none, _sShadow);
			GUI.Box(new Rect(x, y, width, num), GUIContent.none, _sWindowBg);
			if (RadarShowBorder)
			{
				float num2 = 0.6f + 0.4f * Mathf.Sin(_pulseTime * 3f);
				GUI.color = new Color(1f, 1f, 1f, num2);
				GUI.Box(new Rect(x, y, width, 2f), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x, y + num - 2f, width, 2f), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x, y, 2f, num), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x + width - 2f, y, 2f, num), GUIContent.none, _sBorderAccent);
				GUI.color = Color.white;
			}
			if (_nearestImpostorDist < 5f && RadarRevealRoles)
			{
				float num3 = Mathf.PingPong(Time.time * 4f, 1f);
				GUI.color = new Color(1f, 0.05f, 0.05f, 0.5f + num3 * 0.5f);
				GUI.Box(new Rect(x - 2f, y - 2f, width + 4f, 4f), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x - 2f, y + num - 2f, width + 4f, 4f), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x - 2f, y - 2f, 4f, num + 4f), GUIContent.none, _sBorderAccent);
				GUI.Box(new Rect(x + width - 2f, y - 2f, 4f, num + 4f), GUIContent.none, _sBorderAccent);
				GUI.color = Color.white;
			}
			GUI.Box(new Rect(x + 2f, y + 2f, width - 4f, 24f), GUIContent.none, _sHeaderBg);
			float num4 = 0.7f + 0.3f * Mathf.Sin(_pulseTime * 4f);
			GUI.color = new Color(1f, 1f, 1f, num4);
			GUI.Box(new Rect(x + 2f, y + 2f, width - 4f, 1f), GUIContent.none, _sBorderAccent);
			GUI.color = Color.white;
			_sTitle.alignment = (TextAnchor)3;
			_sTitle.normal.textColor = ThemeAccent;
			string text = ((int)(_pulseTime * 4f) % 4) switch
			{
				0 => "●", 
				1 => "◔", 
				2 => "◑", 
				3 => "◕", 
				_ => "●", 
			};
			GUI.Label(new Rect(x + 6f, y + 3f, 16f, 18f), text, _sTitle);
			_sTitle.normal.textColor = ThemeTextPrimary;
			_sTitle.fontSize = 12;
			GUI.Label(new Rect(x + 22f, y + 3f, 80f, 18f), "RADAR", _sTitle);
			_sSmall.alignment = (TextAnchor)5;
			_sSmall.normal.textColor = ThemeAccentSoft;
			_sSmall.fontSize = 9;
			GUI.Label(new Rect(x + width - 140f, y + 4f, 90f, 16f), _cachedMapName, _sSmall);
			if (GUI.Button(new Rect(x + width - 46f, y + 4f, 20f, 18f), RadarMinimized ? "□" : "−", _sButton))
			{
				RadarMinimized = !RadarMinimized;
			}
			if (GUI.Button(new Rect(x + width - 24f, y + 4f, 20f, 18f), "×", _sButton))
			{
				RadarEnabled = false;
			}
			GUI.Box(new Rect(x + 4f, y + 24f, width - 8f, 1f), GUIContent.none, _sBorderDim);
			if (RadarMinimized)
			{
				return;
			}
			Rect val = default(Rect);
			val._002Ector(x + 4f, y + 24f + 2f, width - 8f, num - 24f - 18f - 4f);
			GUI.Box(val, GUIContent.none, _sMapBg);
			if (RadarShowMapImage && (Object)(object)_texSkeldMap != (Object)null && (int)_currentMapType == 0)
			{
				DrawMapImage(val);
			}
			else
			{
				DrawMapRooms(val);
			}
			DrawMapGridSafe(val);
			float num5 = Time.time * 0.4f % 1f;
			GUI.Box(new Rect(val.x + num5 * val.width, val.y, 2f, val.height), GUIContent.none, _sScanLine);
			GUI.color = new Color(1f, 1f, 1f, 0.05f);
			for (int i = 0; (float)i < val.height; i += 10)
			{
				GUI.Box(new Rect(val.x, val.y + (float)i, val.width, 1f), GUIContent.none, _sScanLine);
			}
			GUI.color = Color.white;
			if (_sonarInitialized && (Object)(object)_texSonarLine != (Object)null)
			{
				_sonarAngle += Time.deltaTime * 180f;
				if (_sonarAngle >= 360f)
				{
					_sonarAngle -= 360f;
				}
				float num6 = val.x + val.width / 2f;
				float num7 = val.y + val.height / 2f;
				float num8 = Mathf.Min(val.width, val.height) / 2f - 4f;
				float num9 = _sonarAngle * ((float)System.Math.PI / 180f);
				float num10 = num6 + Mathf.Sin(num9) * num8;
				float num11 = num7 - Mathf.Cos(num9) * num8;
				int num12 = 8;
				for (int j = 0; j < num12; j++)
				{
					float num13 = (float)j / (float)num12;
					float num14 = Mathf.Lerp(num6, num10, num13);
					float num15 = Mathf.Lerp(num7, num11, num13);
					float num16 = 0.6f * (1f - num13);
					GUI.color = new Color(0.2f, 1f, 0.4f, num16);
					GUI.Box(new Rect(num14 - 1f, num15 - 1f, 3f, 3f), GUIContent.none, _sScanLine);
				}
				GUI.color = Color.white;
			}
			int num17 = 0;
			try
			{
				num17 = PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId;
			}
			catch
			{
			}
			num17 = Mathf.Clamp(num17, 0, 17);
			for (int k = 0; k < _deadBodyCacheCount; k++)
			{
				ref RadarDeadBodyData reference = ref _deadBodyCache[k];
				Vector2 val2 = WorldToRadar(reference.x, reference.y, val);
				if (val.Contains(val2))
				{
					GUI.Box(new Rect(val2.x - 7f, val2.y - 7f, 14f, 14f), GUIContent.none, _sDeadBodyBg);
					GUI.Box(new Rect(val2.x - 5f, val2.y - 5f, 10f, 10f), GUIContent.none, _sPlayers[reference.colorId]);
					_sLabel.normal.textColor = Color.white;
					GUI.Label(new Rect(val2.x - 7f, val2.y - 8f, 14f, 16f), "X", _sLabel);
				}
			}
			int num18 = 0;
			for (int l = 0; l < _playerCacheCount; l++)
			{
				ref RadarPlayerData reference2 = ref _playerCache[l];
				Vector2 val3 = WorldToRadar(reference2.x, reference2.y, val);
				val3.x = Mathf.Clamp(val3.x, val.xMin + 8f, val.xMax - 8f);
				val3.y = Mathf.Clamp(val3.y, val.yMin + 8f, val.yMax - 8f);
				if (reference2.isDead)
				{
					_ = GUI.color;
					GUI.color = new Color(1f, 1f, 1f, 0.35f);
					GUI.Box(new Rect(val3.x - 6f, val3.y - 6f, 12f, 12f), GUIContent.none, _sPlayers[reference2.colorId]);
					GUI.color = Color.white;
					_sLabel.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.9f);
					_sLabel.fontSize = 14;
					_sLabel.fontStyle = (FontStyle)1;
					GUI.Label(new Rect(val3.x - 7f, val3.y - 9f, 14f, 18f), "✖", _sLabel);
					_sLabel.fontSize = 10;
					_sLabel.fontStyle = (FontStyle)0;
					_sSmall.normal.textColor = new Color(0.65f, 0.65f, 0.7f, 0.55f);
					_sSmall.alignment = (TextAnchor)4;
					_sSmall.fontSize = 8;
					GUI.Label(new Rect(val3.x - 25f, val3.y + 8f, 50f, 10f), reference2.name, _sSmall);
					_sSmall.fontSize = 9;
				}
				else if (reference2.isImpostor && RadarRevealRoles)
				{
					num18++;
					GUI.Box(new Rect(val3.x - 12f, val3.y - 12f, 24f, 24f), GUIContent.none, _sImpostorGlow);
					DrawPlayerDotSafe(val3.x, val3.y, 0, isImp: true);
					string content = "[IMP] " + reference2.name;
					_sSmall.normal.textColor = Color.red;
					_sSmall.alignment = (TextAnchor)4;
					_sSmall.fontSize = 9;
					GuiStyles.DrawOutlinedLabel(new Rect(val3.x - 35f, val3.y + 8f, 70f, 12f), content, _sSmall);
					_sLabel.normal.textColor = Color.yellow;
					GUI.Label(new Rect(val3.x + 4f, val3.y - 12f, 12f, 12f), "!", _sLabel);
				}
				else
				{
					DrawPlayerDotSafe(val3.x, val3.y, reference2.colorId, isImp: false);
				}
			}
			Vector2 val4 = WorldToRadar(_localPos.x, _localPos.y, val);
			val4.x = Mathf.Clamp(val4.x, val.xMin + 8f, val.xMax - 8f);
			val4.y = Mathf.Clamp(val4.y, val.yMin + 8f, val.yMax - 8f);
			DrawPlayerDotSafe(val4.x, val4.y, num17, isImp: false);
			float num19 = 0.7f + 0.3f * Mathf.Sin(_pulseTime * 5f);
			GUI.color = new Color(1f, 1f, 1f, num19 * 0.5f);
			GUI.Box(new Rect(val4.x - 12f, val4.y - 12f, 24f, 24f), GUIContent.none, _sVisor);
			GUI.color = Color.white;
			_sLabel.normal.textColor = ThemeVisor;
			_sLabel.fontSize = 12;
			GUI.Label(new Rect(val4.x - 12f, val4.y - 12f, 24f, 24f), "◎", _sLabel);
			_sLabel.fontSize = 10;
			_sSmall.normal.textColor = ThemeVisor;
			_sSmall.alignment = (TextAnchor)4;
			_sSmall.fontSize = 9;
			_sSmall.fontStyle = (FontStyle)1;
			GUI.Label(new Rect(val4.x - 14f, val4.y + 12f, 28f, 12f), "YOU", _sSmall);
			_sSmall.fontStyle = (FontStyle)0;
			float num20 = y + num - 18f;
			GUI.Box(new Rect(x + 2f, num20, width - 4f, 16f), GUIContent.none, _sHeaderBg);
			GUI.Box(new Rect(x + 4f, num20, width - 8f, 1f), GUIContent.none, _sBorderDim);
			float num21 = num20 + 2f;
			_sSmall.alignment = (TextAnchor)3;
			_sSmall.fontSize = 8;
			GUI.Box(new Rect(x + 6f, num21 + 3f, 7f, 7f), GUIContent.none, _sPlayers[num17]);
			_sSmall.normal.textColor = ThemeAccent;
			GUI.Label(new Rect(x + 15f, num21, 18f, 12f), "EU", _sSmall);
			GUI.Box(new Rect(x + 38f, num21 + 3f, 7f, 7f), GUIContent.none, _sImpostorDot);
			_sSmall.normal.textColor = ThemeImpostor;
			GUI.Label(new Rect(x + 47f, num21, 20f, 12f), "IMP", _sSmall);
			GUI.Box(new Rect(x + 72f, num21 + 3f, 7f, 7f), GUIContent.none, _sVisor);
			_sSmall.normal.textColor = ThemeVisor;
			GUI.Label(new Rect(x + 81f, num21, 30f, 12f), "CREW", _sSmall);
			_sSmall.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.9f);
			GUI.Label(new Rect(x + 114f, num21 - 1f, 18f, 14f), "✖", _sSmall);
			_sSmall.normal.textColor = new Color(0.6f, 0.6f, 0.65f, 0.8f);
			GUI.Label(new Rect(x + 126f, num21, 25f, 12f), "\ud83d\udc80", _sSmall);
			_sSmall.normal.textColor = ThemeTextMuted;
			GUI.Label(new Rect(x + 150f, num21, 35f, 12f), $"[{_cachedAliveCount}]", _sSmall);
			_sSmall.alignment = (TextAnchor)4;
			_sSmall.normal.textColor = ThemeVisor;
			_sSmall.fontSize = 8;
			GUI.Label(new Rect(x + width / 2f - 50f, num21, 100f, 12f), $"X:{_localPos.x:F1} Y:{_localPos.y:F1}", _sSmall);
			if (num18 > 0)
			{
				_sSmall.alignment = (TextAnchor)5;
				_sSmall.normal.textColor = ThemeImpostor;
				_sSmall.fontSize = 9;
				_sSmall.fontStyle = (FontStyle)1;
				float num22 = 0.6f + 0.4f * Mathf.Sin(_pulseTime * 6f);
				GUI.color = new Color(1f, 1f, 1f, num22);
				GUI.Label(new Rect(x + width - 75f, num21, 65f, 12f), $"⚠ {num18} IMP", _sSmall);
				GUI.color = Color.white;
				_sSmall.fontStyle = (FontStyle)0;
			}
			_sSmall.fontSize = 9;
			if (!RadarLocked)
			{
				_sSmall.alignment = (TextAnchor)8;
				_sSmall.normal.textColor = ThemeAccentDim;
				_sSmall.fontSize = 14;
				GUI.Label(new Rect(x + width - 18f, y + num - 17f, 16f, 14f), "◢", _sSmall);
				_sSmall.alignment = (TextAnchor)6;
				_sSmall.normal.textColor = ThemeTextMuted;
				_sSmall.fontSize = 8;
				GUI.Label(new Rect(x + 4f, y + num - 14f, 90f, 12f), "↕ Scroll=Zoom", _sSmall);
			}
			_sSmall.fontSize = 9;
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[Radar] Error: " + ex.GetType().Name + ": " + ex.Message));
			if (ex.StackTrace != null && ex.StackTrace.Length > 0)
			{
				Debug.LogError(InteropFix.Cast("[Radar] Stack: " + ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length))));
			}
		}
	}

	private static void DrawPlayerDotSafe(float px, float py, int colorId, bool isImp)
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		colorId = Mathf.Clamp(colorId, 0, 17);
		if (RadarDrawIcons)
		{
			GUI.Box(new Rect(px - 6f, py - 7f, 12f, 14f), GUIContent.none, _sShadow);
			GUI.Box(new Rect(px - 5f, py - 6f, 10f, 12f), GUIContent.none, _sPlayers[colorId]);
			GUI.Box(new Rect(px - 1f, py - 5f, 4f, 3f), GUIContent.none, _sVisor);
			GUI.Box(new Rect(px - 7f, py - 1f, 3f, 5f), GUIContent.none, _sPlayers[colorId]);
		}
		else
		{
			GUI.Box(new Rect(px - 6f, py - 6f, 12f, 12f), GUIContent.none, _sShadow);
			GUI.Box(new Rect(px - 5f, py - 5f, 10f, 10f), GUIContent.none, _sPlayers[colorId]);
		}
	}

	private static void DrawMapGridSafe(Rect mapArea)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		int num = 8;
		float num2 = mapArea.width / (float)num;
		float num3 = mapArea.height / (float)num;
		for (int i = 1; i < num; i++)
		{
			float num4 = mapArea.x + (float)i * num2;
			bool flag = i == num / 2;
			GUI.Box(new Rect(num4, mapArea.y, (float)((!flag) ? 1 : 2), mapArea.height), GUIContent.none, flag ? _sGridBright : _sGridDim);
		}
		for (int j = 1; j < num; j++)
		{
			float num5 = mapArea.y + (float)j * num3;
			bool flag2 = j == num / 2;
			GUI.Box(new Rect(mapArea.x, num5, mapArea.width, (float)((!flag2) ? 1 : 2)), GUIContent.none, flag2 ? _sGridBright : _sGridDim);
		}
		GUI.Box(new Rect(mapArea.x, mapArea.y, mapArea.width, 1f), GUIContent.none, _sGridBright);
		GUI.Box(new Rect(mapArea.x, mapArea.yMax - 1f, mapArea.width, 1f), GUIContent.none, _sGridBright);
		GUI.Box(new Rect(mapArea.x, mapArea.y, 1f, mapArea.height), GUIContent.none, _sGridBright);
		GUI.Box(new Rect(mapArea.xMax - 1f, mapArea.y, 1f, mapArea.height), GUIContent.none, _sGridBright);
		_sSmall.alignment = (TextAnchor)4;
		_sSmall.normal.textColor = new Color(0.4f, 0.7f, 0.9f, 0.6f);
		_sSmall.fontSize = 9;
		float num6 = mapArea.x + mapArea.width * 0.5f;
		float num7 = mapArea.y + mapArea.height * 0.5f;
		GUI.Label(new Rect(num6 - 8f, mapArea.y + 2f, 16f, 12f), "N", _sSmall);
		GUI.Label(new Rect(num6 - 8f, mapArea.yMax - 14f, 16f, 12f), "S", _sSmall);
		GUI.Label(new Rect(mapArea.x + 2f, num7 - 6f, 12f, 12f), "W", _sSmall);
		GUI.Label(new Rect(mapArea.xMax - 14f, num7 - 6f, 12f, 12f), "E", _sSmall);
		_sSmall.fontSize = 8;
	}

	private static void DrawMapRooms(Rect mapArea)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		RoomData[] currentMapRooms = GetCurrentMapRooms();
		if (currentMapRooms == null || currentMapRooms.Length == 0)
		{
			return;
		}
		if (!MapInfos.TryGetValue(_currentMapType, out var value))
		{
			value = new MapInfo(0f, 0f, 4f, -20f, 20f, -15f, 10f);
		}
		RoomData[] array = currentMapRooms;
		Rect val3 = default(Rect);
		for (int i = 0; i < array.Length; i++)
		{
			RoomData roomData = array[i];
			float worldX = roomData.x - roomData.w / 2f;
			float worldY = roomData.y + roomData.h / 2f;
			float worldX2 = roomData.x + roomData.w / 2f;
			float worldY2 = roomData.y - roomData.h / 2f;
			Vector2 val = WorldToRadar(worldX, worldY, mapArea);
			Vector2 val2 = WorldToRadar(worldX2, worldY2, mapArea);
			val3._002Ector(val.x, val.y, val2.x - val.x, val2.y - val.y);
			if (!(val3.xMax < mapArea.x) && !(val3.x > mapArea.xMax) && !(val3.yMax < mapArea.y) && !(val3.y > mapArea.yMax))
			{
				switch (roomData.shapeType)
				{
				case 5:
					GUI.Box(val3, GUIContent.none, _sCorridorFill);
					break;
				case 4:
					DrawHexagonRoom(val3);
					break;
				case 2:
					DrawPentagonRoom(val3, pointLeft: true);
					break;
				case 3:
					DrawPentagonRoom(val3, pointLeft: false);
					break;
				case 1:
					DrawOctagonRoom(val3);
					break;
				default:
					DrawRectRoom(val3);
					break;
				}
			}
		}
	}

	private static void DrawOctagonRoom(Rect rect)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(rect.width, rect.height) * 0.15f;
		GUI.Box(new Rect(rect.x + num, rect.y, rect.width - num * 2f, rect.height), GUIContent.none, _sRoomFill);
		GUI.Box(new Rect(rect.x, rect.y + num, rect.width, rect.height - num * 2f), GUIContent.none, _sRoomFill);
		GUI.Box(new Rect(rect.x + num, rect.y, rect.width - num * 2f, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x + num, rect.yMax - 2f, rect.width - num * 2f, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.y + num, 2f, rect.height - num * 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - 2f, rect.y + num, 2f, rect.height - num * 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.y + num - 2f, num, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x + num - 2f, rect.y, 2f, num), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - num, rect.y + num - 2f, num, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - num, rect.y, 2f, num), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.yMax - num, num, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x + num - 2f, rect.yMax - num, 2f, num), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - num, rect.yMax - num, num, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - num, rect.yMax - num, 2f, num), GUIContent.none, _sRoomBorder);
	}

	private static void DrawRectRoom(Rect rect)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		GUI.Box(rect, GUIContent.none, _sRoomFill);
		GUI.Box(new Rect(rect.x, rect.y, rect.width, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.y, 2f, rect.height), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), GUIContent.none, _sRoomBorder);
	}

	private static void DrawHexagonRoom(Rect rect)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		float num = rect.width * 0.25f;
		GUI.Box(new Rect(rect.x, rect.y + rect.height * 0.3f, rect.width, rect.height * 0.7f), GUIContent.none, _sRoomFill);
		GUI.Box(new Rect(rect.x + num * 0.5f, rect.y, rect.width - num, rect.height * 0.4f), GUIContent.none, _sRoomFill);
		GUI.Box(new Rect(rect.x + num, rect.y, rect.width - num * 2f, 2f), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.y + num, 2f, rect.height - num), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.xMax - 2f, rect.y + num, 2f, rect.height - num), GUIContent.none, _sRoomBorder);
		GUI.Box(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), GUIContent.none, _sRoomBorder);
	}

	private static void DrawPentagonRoom(Rect rect, bool pointLeft)
	{
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		float num = rect.width * 0.3f;
		if (pointLeft)
		{
			GUI.Box(new Rect(rect.x + num * 0.5f, rect.y, rect.width - num * 0.5f, rect.height), GUIContent.none, _sRoomFill);
			float num2 = rect.y + rect.height * 0.5f;
			GUI.Box(new Rect(rect.x, num2 - rect.height * 0.25f, num, rect.height * 0.5f), GUIContent.none, _sRoomFill);
			GUI.Box(new Rect(rect.x + num, rect.y, rect.width - num, 2f), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.x + num, rect.yMax - 2f, rect.width - num, 2f), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.x, num2 - 1f, num, 2f), GUIContent.none, _sRoomBorder);
		}
		else
		{
			GUI.Box(new Rect(rect.x, rect.y, rect.width - num * 0.5f, rect.height), GUIContent.none, _sRoomFill);
			float num3 = rect.y + rect.height * 0.5f;
			GUI.Box(new Rect(rect.xMax - num, num3 - rect.height * 0.25f, num, rect.height * 0.5f), GUIContent.none, _sRoomFill);
			GUI.Box(new Rect(rect.x, rect.y, rect.width - num, 2f), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.x, rect.yMax - 2f, rect.width - num, 2f), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.x, rect.y, 2f, rect.height), GUIContent.none, _sRoomBorder);
			GUI.Box(new Rect(rect.xMax - num, num3 - 1f, num, 2f), GUIContent.none, _sRoomBorder);
		}
	}

	private static RoomData[] GetCurrentMapRooms()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		if ((int)_currentMapType == 0)
		{
			return SkeldRooms;
		}
		if ((int)_currentMapType == 1)
		{
			return MiraRooms;
		}
		if ((int)_currentMapType == 2)
		{
			return PolusRooms;
		}
		if ((int)_currentMapType == 3)
		{
			return FungleRooms;
		}
		if ((int)_currentMapType == 4)
		{
			return AirshipRooms;
		}
		return SkeldRooms;
	}

	internal static void ToggleNoKillDistanceLimit(bool enable)
	{
		NoKillDistanceLimitEnabled = enable;
		LogCheat(enable ? "No Kill Distance Limit ENABLED" : "No Kill Distance Limit DISABLED");
	}

	internal static void ToggleSeeGhosts(bool enable)
	{
		SeeGhostsEnabled = enable;
		if (enable)
		{
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null || !current.Data.IsDead)
				{
					continue;
				}
				try
				{
					current.Visible = true;
					((Component)current).gameObject.layer = LayerMask.NameToLayer("Players");
					CosmeticsLayer cosmetics = current.cosmetics;
					if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) != (Object)null)
					{
						((Component)current.cosmetics.nameText).gameObject.SetActive(true);
					}
				}
				catch
				{
				}
			}
			LogCheat("See Ghosts ON - You can see dead players!");
			return;
		}
		var enumerator4 = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator4.MoveNext())
		{
			PlayerControl current2 = enumerator4.Current;
			if ((Object)(object)current2 == (Object)null || (Object)(object)current2.Data == (Object)null || !current2.Data.IsDead || !((Object)(object)current2 != (Object)(object)PlayerControl.LocalPlayer))
			{
				continue;
			}
			try
			{
				if (!PlayerControl.LocalPlayer.Data.IsDead)
				{
					current2.Visible = false;
					((Component)current2).gameObject.layer = LayerMask.NameToLayer("Ghost");
				}
			}
			catch
			{
			}
		}
		LogCheat("Ver Fantasmas DESATIVADO");
	}

	internal static void ToggleSeeDeadChat(bool enable)
	{
		SeeDeadChatEnabled = enable;
		LogCheat(enable ? "Ver Chat dos Mortos ATIVADO" : "Ver Chat dos Mortos DESATIVADO");
	}

	private static void ForceAddGhostChat(PlayerControl sourcePlayer, string chatText)
	{
		HudManager instance = DestroyableSingleton<HudManager>.Instance;
		if ((Object)(object)((instance != null) ? instance.Chat : null) == (Object)null)
		{
			return;
		}
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if ((Object)(object)((localPlayer != null) ? localPlayer.Data : null) == (Object)null)
		{
			return;
		}
		try
		{
			bool num = !PlayerControl.LocalPlayer.Data.IsDead;
			if (num)
			{
				PlayerControl.LocalPlayer.Data.IsDead = true;
			}
			string text = "<color=#888888>[\ud83d\udc7b GHOST]</color> " + chatText;
			DestroyableSingleton<HudManager>.Instance.Chat.AddChat(sourcePlayer, text, false);
			if (num)
			{
				PlayerControl.LocalPlayer.Data.IsDead = false;
			}
			LogCheat("[GhostChat] " + sourcePlayer.Data.PlayerName + ": " + chatText);
		}
		catch (Exception ex)
		{
			PlayerControl localPlayer2 = PlayerControl.LocalPlayer;
			if ((Object)(object)((localPlayer2 != null) ? localPlayer2.Data : null) != (Object)null)
			{
				PlayerControl.LocalPlayer.Data.IsDead = false;
			}
			Debug.LogError(InteropFix.Cast("[GhostChat] Erro: " + ex.Message));
		}
	}

	internal static void ToggleAlwaysShowChat(bool enable)
	{
		AlwaysShowChatEnabled = enable;
		LogCheat(enable ? "Always Show Chat ENABLED (Read-Only)" : "Always Show Chat DISABLED");
	}

	private static void InitKillCooldownUI()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		if (!_killCooldownUIInitialized)
		{
			_cooldownBgTex = new Texture2D(1, 1);
			_cooldownBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
			_cooldownBgTex.Apply();
			GUIStyle val = new GUIStyle
			{
				fontSize = 13,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)4
			};
			val.normal.textColor = Color.white;
			_cooldownAboveHeadStyle = val;
			GUIStyle val2 = new GUIStyle
			{
				fontSize = 13,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)4
			};
			val2.normal.textColor = Color.black;
			_cooldownShadowStyle = val2;
			_killCooldownUIInitialized = true;
		}
	}

	private static Color GetKillCooldownColor(float killTimer)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (killTimer < 2f)
		{
			return new Color(1f, 0.2f, 0.2f);
		}
		if (killTimer < 5f)
		{
			return new Color(1f, 0.6f, 0.2f);
		}
		return Color.white;
	}

	internal static void DrawKillCooldowns()
	{
		ConfigEntry<bool> showKillCooldowns = CheatConfig.ShowKillCooldowns;
		if (showKillCooldowns == null || !showKillCooldowns.Value || (Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			return;
		}
		AmongUsClient instance = AmongUsClient.Instance;
		if (instance == null || !((InnerNetClient)instance).IsGameStarted || IsRevealSusActive)
		{
			return;
		}
		bool flag = !(CheatConfig.HideMMCStar?.Value ?? false);
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
			if ((Object)obj == (Object)null || !current.Data.Role.IsImpostor || current.Data.IsDead)
			{
				continue;
			}
			CosmeticsLayer cosmetics = current.cosmetics;
			if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) == (Object)null)
			{
				continue;
			}
			try
			{
				float killTimer = current.killTimer;
				string value;
				string value2;
				if (killTimer <= 0.1f)
				{
					value = "FF2222";
					value2 = "⚠ CAN KILL!";
				}
				else if (killTimer < 2f)
				{
					value = "FF4444";
					value2 = $"{killTimer:F1}s";
				}
				else if (killTimer < 5f)
				{
					value = "FF9922";
					value2 = $"{killTimer:F1}s";
				}
				else
				{
					value = "AAAAAA";
					value2 = $"{killTimer:F1}s";
				}
				string value3 = "";
				if (flag && MMCIdentification.IsMMCPlayer(current.PlayerId))
				{
					byte playerId = current.PlayerId;
					PlayerControl localPlayer = PlayerControl.LocalPlayer;
					bool num = playerId == ((localPlayer != null) ? new byte?(localPlayer.PlayerId) : ((byte?)null));
					string text = "FFD700";
					value3 = (num ? ("<color=#" + text + ">★</color> ") : ("<color=#" + text + "><b>★ MMC ★</b></color> "));
				}
				string text2 = $"{value3}{current.Data.PlayerName}\n<size=60%><color=#{value}>{value2}</color></size>";
				if (((TMP_Text)current.cosmetics.nameText).text != text2)
				{
					((TMP_Text)current.cosmetics.nameText).text = text2;
				}
			}
			catch
			{
			}
		}
	}

	private static void EnsureKillAlertStyles()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		if (_killAlertTitleStyle == null || !((Object)(object)_killAlertBgTex != (Object)null))
		{
			_killAlertBgTex = MakeKillAlertTex(2, 2, new Color(0.05f, 0.02f, 0.02f, 0.92f));
			_killAlertAccentTex = MakeKillAlertTex(2, 2, new Color(1f, 0.05f, 0.05f, 1f));
			GUIStyle val = new GUIStyle
			{
				fontSize = 20,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)4,
				richText = true
			};
			val.normal.textColor = Color.white;
			_killAlertTitleStyle = val;
			GUIStyle val2 = new GUIStyle
			{
				fontSize = 13,
				alignment = (TextAnchor)4,
				richText = true
			};
			val2.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
			_killAlertDetailStyle = val2;
			GUIStyle val3 = new GUIStyle();
			val3.normal.background = _killAlertBgTex;
			_killAlertBgStyle = val3;
			GUIStyle val4 = new GUIStyle();
			val4.normal.background = _killAlertAccentTex;
			_killAlertAccentStyle = val4;
		}
	}

	private static Texture2D MakeKillAlertTex(int w, int h, Color col)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(w, h);
		Color[] array = (Color[])(object)new Color[w * h];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = col;
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		return val;
	}

	public static void ShowKillAlert(PlayerControl killer, PlayerControl victim, string location)
	{
		ConfigEntry<bool> killAlertsEnabled = CheatConfig.KillAlertsEnabled;
		if (killAlertsEnabled != null && killAlertsEnabled.Value)
		{
			object obj;
			if (killer == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = killer.Data;
				obj = ((data != null) ? data.PlayerName : null);
			}
			if (obj == null)
			{
				obj = "???";
			}
			_killAlertKiller = (string)obj;
			object obj2;
			if (victim == null)
			{
				obj2 = null;
			}
			else
			{
				NetworkedPlayerInfo data2 = victim.Data;
				obj2 = ((data2 != null) ? data2.PlayerName : null);
			}
			if (obj2 == null)
			{
				obj2 = "???";
			}
			_killAlertVictim = (string)obj2;
			_killAlertLocation = location ?? "";
			_killAlertStartTime = Time.time;
			_killAlertEndTime = Time.time + 5f;
			LogCheat("☠ " + _killAlertKiller + " KILLED " + _killAlertVictim + (string.IsNullOrEmpty(location) ? "" : (" in " + location)));
		}
	}

	internal static void DrawKillAlert()
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		if (!(Time.time > _killAlertEndTime) && !string.IsNullOrEmpty(_killAlertKiller))
		{
			EnsureKillAlertStyles();
			float num = Time.time - _killAlertStartTime;
			float num2 = _killAlertEndTime - Time.time;
			float num3 = Mathf.Clamp01(num / 0.3f);
			float num4 = 1f - Mathf.Pow(1f - num3, 3f);
			float num5 = ((num2 < 1.5f) ? Mathf.Clamp01(num2 / 1.5f) : 1f);
			float num6 = Mathf.Clamp(Mathf.Pow((float)Screen.height / 1080f, 0.7f), 0.85f, 2.2f);
			float num7 = (float)Screen.width / num6;
			Matrix4x4 matrix = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(num6, num6, 1f));
			float num8 = 420f;
			float num9 = 64f;
			float num10 = (num7 - num8) / 2f;
			float num11 = 60f;
			float num12 = Mathf.Lerp(0f - num9 - 10f, num11, num4);
			float num13 = 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
			Color color = GUI.color;
			GUI.color = new Color(0f, 0f, 0f, num5 * 0.4f);
			GUI.Box(new Rect(num10 + 3f, num12 + 3f, num8, num9), GUIContent.none);
			GUI.color = new Color(1f, 1f, 1f, num5);
			GUI.Box(new Rect(num10, num12, num8, num9), GUIContent.none, _killAlertBgStyle);
			GUI.Box(new Rect(num10, num12, num8, 3f), GUIContent.none, _killAlertAccentStyle);
			GUI.Box(new Rect(num10, num12 + num9 - 2f, num8, 2f), GUIContent.none, _killAlertAccentStyle);
			GUI.color = new Color(1f, 0.05f, 0.05f, num5 * 0.6f);
			GUI.Box(new Rect(num10, num12 + 3f, 3f, num9 - 5f), GUIContent.none, _killAlertAccentStyle);
			GUI.Box(new Rect(num10 + num8 - 3f, num12 + 3f, 3f, num9 - 5f), GUIContent.none, _killAlertAccentStyle);
			GUI.color = new Color(1f, 1f, 1f, num5);
			_killAlertTitleStyle.normal.textColor = new Color(1f, 1f, 1f, num5);
			float num14 = num5 * num13;
			string text = "<color=#FF1A1A><size=24>☠</size></color>  <color=#FFFFFF>KILL DETECTED</color>  <color=#" + ColorUtility.ToHtmlStringRGBA(new Color(1f, 0.1f, 0.1f, num14)) + "><size=24>☠</size></color>";
			GUI.Label(new Rect(num10, num12 + 4f, num8, 32f), text, _killAlertTitleStyle);
			_killAlertDetailStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f, num5);
			string text2 = $"<color=#FF6666><b>{_killAlertKiller}</b></color> <color=#888888>→</color> <color=#AAFFAA><b>{_killAlertVictim}</b></color>";
			if (!string.IsNullOrEmpty(_killAlertLocation))
			{
				text2 = text2 + "  <color=#666666>(" + _killAlertLocation + ")</color>";
			}
			GUI.Label(new Rect(num10, num12 + 34f, num8, 24f), text2, _killAlertDetailStyle);
			GUI.color = color;
			GUI.matrix = matrix;
		}
	}

	private static void InitEventLoggerStyles()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		if ((Object)(object)_eventLogBgTex == (Object)null)
		{
			_eventLogBgTex = new Texture2D(1, 1);
			_eventLogBgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.95f));
			_eventLogBgTex.Apply();
			_eventLogHeaderTex = new Texture2D(1, 1);
			_eventLogHeaderTex.SetPixel(0, 0, new Color(0.15f, 0.1f, 0.18f, 1f));
			_eventLogHeaderTex.Apply();
		}
		if (_eventLogHeaderStyle == null)
		{
			GUIStyle val = new GUIStyle
			{
				fontSize = 14,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)3
			};
			val.normal.textColor = new Color(0.9f, 0.6f, 0.9f);
			_eventLogHeaderStyle = val;
			_eventLogHeaderStyle.padding.left = 10;
		}
		if (_eventLogEventStyle == null)
		{
			_eventLogEventStyle = new GUIStyle
			{
				fontSize = 12,
				alignment = (TextAnchor)3,
				wordWrap = false,
				clipping = (TextClipping)1
			};
			_eventLogEventStyle.padding.left = 5;
			_eventLogEventStyle.padding.right = 5;
			_eventLogEventStyle.padding.top = 2;
			_eventLogEventStyle.padding.bottom = 2;
		}
		if (_eventLogTimeStyle == null)
		{
			GUIStyle val2 = new GUIStyle
			{
				fontSize = 10,
				alignment = (TextAnchor)3
			};
			val2.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
			_eventLogTimeStyle = val2;
			_eventLogTimeStyle.padding.left = 5;
		}
	}

	internal static void DrawEventLoggerUI()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		ConfigEntry<bool> eventLoggerEnabled = CheatConfig.EventLoggerEnabled;
		if (eventLoggerEnabled == null || !eventLoggerEnabled.Value || !EventLogger.ShowUI)
		{
			return;
		}
		InitEventLoggerStyles();
		Event current = Event.current;
		Vector2 mousePosition = current.mousePosition;
		Rect val = default(Rect);
		val._002Ector(_eventLogRect.x, _eventLogRect.y, _eventLogRect.width - 70f, 28f);
		if ((int)current.type == 0 && val.Contains(mousePosition))
		{
			_eventLogDragging = true;
			_eventLogDragOffset = mousePosition - new Vector2(_eventLogRect.x, _eventLogRect.y);
		}
		else if ((int)current.type == 1)
		{
			_eventLogDragging = false;
		}
		else if ((int)current.type == 3 && _eventLogDragging)
		{
			_eventLogRect.x = mousePosition.x - _eventLogDragOffset.x;
			_eventLogRect.y = mousePosition.y - _eventLogDragOffset.y;
		}
		GUIStyle val2 = new GUIStyle();
		val2.normal.background = _eventLogBgTex;
		GUI.Box(_eventLogRect, GUIContent.none, val2);
		Rect val3 = new Rect(_eventLogRect.x, _eventLogRect.y, _eventLogRect.width, 28f);
		GUIStyle val4 = new GUIStyle();
		val4.normal.background = _eventLogHeaderTex;
		GUI.Box(val3, GUIContent.none, val4);
		GUI.Label(new Rect(_eventLogRect.x + 5f, _eventLogRect.y + 2f, 150f, 24f), "EVENT LOG", _eventLogHeaderStyle);
		List<GameEvent> filteredEvents = EventLogger.GetFilteredEvents();
		int count = filteredEvents.Count;
		_eventLogTimeStyle.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
		GUI.Label(new Rect(_eventLogRect.x + 120f, _eventLogRect.y + 6f, 100f, 16f), count.ToString(), _eventLogTimeStyle);
		GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
		if (GUI.Button(new Rect(_eventLogRect.xMax - 28f, _eventLogRect.y + 4f, 22f, 20f), "X"))
		{
			EventLogger.ShowUI = false;
		}
		GUI.backgroundColor = Color.white;
		GUI.backgroundColor = new Color(0.3f, 0.3f, 0.4f);
		if (GUI.Button(new Rect(_eventLogRect.xMax - 70f, _eventLogRect.y + 4f, 38f, 20f), "CLR"))
		{
			EventLogger.Clear();
		}
		GUI.backgroundColor = Color.white;
		float num = _eventLogRect.x + 3f;
		float num2 = _eventLogRect.y + 32f;
		float num3 = _eventLogRect.width - 6f;
		float num4 = _eventLogRect.height - 38f;
		float num5 = Mathf.Max((float)count * 24f, num4);
		float num6 = num3 - 18f;
		_eventLogScrollPos = GUI.BeginScrollView(new Rect(num, num2, num3, num4), _eventLogScrollPos, new Rect(0f, 0f, num6, num5));
		int num7 = Mathf.Max(0, Mathf.FloorToInt(_eventLogScrollPos.y / 24f) - 1);
		int num8 = Mathf.Min(count, num7 + Mathf.CeilToInt(num4 / 24f) + 2);
		for (int i = num7; i < num8; i++)
		{
			int num9 = count - 1 - i;
			if (num9 >= 0 && num9 < count)
			{
				GameEvent gameEvent = filteredEvents[num9];
				float num10 = (float)i * 24f;
				if (i % 2 == 0)
				{
					GUI.color = new Color(1f, 1f, 1f, 0.03f);
					GUI.Box(new Rect(0f, num10, num6, 24f), GUIContent.none);
					GUI.color = Color.white;
				}
				GUI.Label(new Rect(2f, num10, 40f, 24f), gameEvent.Time.ToString("HH:mm"), _eventLogTimeStyle);
				_eventLogEventStyle.normal.textColor = gameEvent.Color;
				GUI.Label(new Rect(45f, num10, num6 - 50f, 24f), GetEventIcon(gameEvent.Type) + gameEvent.Message, _eventLogEventStyle);
			}
		}
		GUI.EndScrollView();
	}

	private static string GetEventIcon(GameEventType type)
	{
		return type switch
		{
			GameEventType.Kill => "☠ ", 
			GameEventType.Vent => "\ud83d\udd32 ", 
			GameEventType.Report => "\ud83d\udce2 ", 
			GameEventType.Vote => "✋ ", 
			GameEventType.Sabotage => "⚠ ", 
			_ => "• ", 
		};
	}

	internal static void ToggleEventLoggerUI()
	{
		EventLogger.ShowUI = !EventLogger.ShowUI;
		LogCheat(EventLogger.ShowUI ? "Event Logger UI opened" : "Event Logger UI closed");
	}

	internal static void OpenSabotageMap()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		try
		{
			if ((Object)(object)DestroyableSingleton<HudManager>.Instance == (Object)null)
			{
				Debug.LogWarning(InteropFix.Cast("[OpenSabotageMap] HudManager.Instance is null"));
				return;
			}
			if ((Object)(object)ShipStatus.Instance == (Object)null)
			{
				Debug.LogWarning(InteropFix.Cast("[OpenSabotageMap] ShipStatus.Instance is null - not in game"));
				return;
			}
			DestroyableSingleton<HudManager>.Instance.InitMap();
			MapOptions val = new MapOptions
			{
				Mode = (MapOptions.Modes)3
			};
			DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(val);
			LogCheat("Sabotage map opened!");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[OpenSabotageMap] Error: {value}"));
		}
	}

	private static void RevealAnonymousVoteColors(Transform targetTransform, int voterColorId)
	{
		try
		{
			VoteSpreader componentInChildren = ((Component)targetTransform).GetComponentInChildren<VoteSpreader>();
			if (((componentInChildren != null) ? componentInChildren.Votes : null) != null && componentInChildren.Votes.Count != 0)
			{
				SpriteRenderer val = componentInChildren.Votes[componentInChildren.Votes.Count - 1];
				if ((Object)(object)val != (Object)null)
				{
					PlayerMaterial.SetColors(voterColorId, (Renderer)(object)val);
				}
			}
		}
		catch
		{
		}
	}

	private static bool LocalPlayerCanKill()
	{
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		object obj;
		if (localPlayer == null)
		{
			obj = null;
		}
		else
		{
			NetworkedPlayerInfo data = localPlayer.Data;
			obj = ((data != null) ? data.Role : null);
		}
		if ((Object)obj == (Object)null)
		{
			return false;
		}
		if (localPlayer.Data.Role.IsImpostor)
		{
			return true;
		}
		try
		{
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			KillButton val = ((instance != null) ? instance.KillButton : null);
			if ((Object)(object)val != (Object)null && ((Behaviour)val).isActiveAndEnabled)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static void ForceVotesRefreshPlayerList()
	{
		ForceVotesAlivePlayers.Clear();
		MeetingHud instance = MeetingHud.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return;
		}
		foreach (PlayerVoteArea item in (Il2CppArrayBase<PlayerVoteArea>)(object)instance.playerStates)
		{
			if ((Object)(object)item == (Object)null || item.AmDead)
			{
				continue;
			}
			string name = "Player " + item.TargetPlayerId;
			var enumerator2 = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				PlayerControl current2 = enumerator2.Current;
				if ((Object)(object)current2 != (Object)null && current2.PlayerId == item.TargetPlayerId && (Object)(object)current2.Data != (Object)null)
				{
					name = current2.Data.PlayerName;
					break;
				}
			}
			ForceVotesAlivePlayers.Add(new ForceVotesPlayerInfo
			{
				PlayerId = item.TargetPlayerId,
				Name = name
			});
		}
		Debug.Log(InteropFix.Cast($"[ForceVotes] RefreshPlayerList: Found {ForceVotesAlivePlayers.Count} alive players"));
	}

	internal static void ForceAllVotesTo(byte targetPlayerId)
	{
		if ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost)
		{
			Debug.Log(InteropFix.Cast("[ForceVotes] Must be host to force votes"));
			return;
		}
		MeetingHud instance = MeetingHud.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			Debug.Log(InteropFix.Cast("[ForceVotes] No active meeting"));
			return;
		}
		Debug.Log(InteropFix.Cast($"[ForceVotes] Attempting to force votes to target {targetPlayerId}"));
		int num = 0;
		foreach (PlayerVoteArea item in (Il2CppArrayBase<PlayerVoteArea>)(object)instance.playerStates)
		{
			if ((Object)(object)item == (Object)null)
			{
				continue;
			}
			byte targetPlayerId2 = item.TargetPlayerId;
			bool amDead = item.AmDead;
			sbyte b = (sbyte)item.VotedFor;
			byte votedFor = item.VotedFor;
			Debug.Log(InteropFix.Cast($"[ForceVotes] Player {targetPlayerId2}: VotedFor(sbyte)={b}, VotedFor(byte)={votedFor}, AmDead={amDead}, DidVote={item.DidVote}"));
			bool flag = !item.DidVote || b == -1 || votedFor == byte.MaxValue;
			if (!(!amDead && flag))
			{
				continue;
			}
			Debug.Log(InteropFix.Cast($"[ForceVotes] Forcing vote from {targetPlayerId2} to {targetPlayerId}"));
			try
			{
				instance.CmdCastVote(targetPlayerId2, targetPlayerId);
				num++;
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ForceVotes] CmdCastVote failed: {value}"));
				try
				{
					ForceVoteViaRPC(instance, targetPlayerId2, targetPlayerId);
					num++;
				}
				catch (Exception value2)
				{
					Debug.LogError(InteropFix.Cast($"[ForceVotes] RPC fallback also failed: {value2}"));
				}
			}
		}
		Debug.Log(InteropFix.Cast($"[ForceVotes] Forced {num} votes to target {targetPlayerId}"));
	}

	internal static void ForceVoteViaRPC(MeetingHud meeting, byte voterId, byte targetPlayerId)
	{
		MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)meeting).NetId, (byte)24, (SendOption)1, -1);
		val.Write(voterId);
		val.Write(targetPlayerId);
		((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
	}

	internal static void ForceAllVotesToSkip()
	{
		ForceAllVotesTo(253);
	}

	internal static int ForceVotesCountRemaining()
	{
		MeetingHud instance = MeetingHud.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return 0;
		}
		int num = 0;
		foreach (PlayerVoteArea item in (Il2CppArrayBase<PlayerVoteArea>)(object)instance.playerStates)
		{
			if (!((Object)(object)item == (Object)null) && !item.AmDead && !item.DidVote)
			{
				num++;
			}
		}
		return num;
	}

	internal static string ForceVotesGetCurrentTargetName()
	{
		if (ForceVotesSelectedTargetIndex == 0)
		{
			return "Skip Vote";
		}
		if (ForceVotesSelectedTargetIndex - 1 < ForceVotesAlivePlayers.Count)
		{
			return ForceVotesAlivePlayers[ForceVotesSelectedTargetIndex - 1].Name;
		}
		ForceVotesSelectedTargetIndex = 0;
		return "Skip Vote";
	}

	internal static void ForceVotesExecute()
	{
		if (ForceVotesSelectedTargetIndex == 0)
		{
			ForceAllVotesToSkip();
			return;
		}
		int num = ForceVotesSelectedTargetIndex - 1;
		if (num < ForceVotesAlivePlayers.Count)
		{
			byte playerId = ForceVotesAlivePlayers[num].PlayerId;
			Debug.Log(InteropFix.Cast($"[ForceVotes] Forcing to player ID: {playerId}"));
			ForceAllVotesTo(playerId);
		}
	}

	internal static void ForceVotesSelectPrevious()
	{
		ForceVotesSelectedTargetIndex--;
		if (ForceVotesSelectedTargetIndex < 0)
		{
			ForceVotesSelectedTargetIndex = ForceVotesAlivePlayers.Count;
		}
	}

	internal static void ForceVotesSelectNext()
	{
		ForceVotesSelectedTargetIndex++;
		if (ForceVotesSelectedTargetIndex > ForceVotesAlivePlayers.Count)
		{
			ForceVotesSelectedTargetIndex = 0;
		}
	}
}














