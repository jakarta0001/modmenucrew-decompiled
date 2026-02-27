using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using ModMenuCrew.UI.Managers;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class RoleCheats
{
	[HarmonyPatch(typeof(EngineerRole), "SetCooldown")]
	public static class EngineerSetCooldownPatch
	{
		public static bool Prefix()
		{
			return !(CheatManager.Instance?.NoVentCooldown ?? false);
		}
	}

	[HarmonyPatch(typeof(ShapeshifterRole), "SetCooldown")]
	public static class ShapeshifterSetCooldownPatch
	{
		public static bool Prefix()
		{
			return !(CheatManager.Instance?.NoShapeshiftCooldown ?? false);
		}
	}

	[HarmonyPatch(typeof(TrackerRole), "SetCooldown")]
	public static class TrackerSetCooldownPatch
	{
		public static bool Prefix()
		{
			return !(CheatManager.Instance?.NoTrackingCooldown ?? false);
		}
	}

	[HarmonyPatch(typeof(EngineerRole), "FixedUpdate")]
	public static class EngineerUpdatePatch
	{
		public static void Postfix(EngineerRole __instance)
		{
			CheatManager instance = CheatManager.Instance;
			if (instance == null)
			{
				return;
			}
			if (instance.EndlessVentTime)
			{
				__instance.inVentTimeRemaining = 30f;
			}
			if (instance.NoVentCooldown)
			{
				if (__instance.cooldownSecondsRemaining > 0f)
				{
					__instance.cooldownSecondsRemaining = 0.01f;
				}
				UpdateAbilityButton();
			}
		}
	}

	[HarmonyPatch(typeof(ShapeshifterRole), "FixedUpdate")]
	public static class ShapeshifterUpdatePatch
	{
		public static void Postfix(ShapeshifterRole __instance)
		{
			CheatManager instance = CheatManager.Instance;
			if (instance == null)
			{
				return;
			}
			if (instance.EndlessShapeshiftDuration)
			{
				__instance.durationSecondsRemaining = MAX_SAFE_VALUE;
			}
			if (instance.NoShapeshiftCooldown)
			{
				if (__instance.cooldownSecondsRemaining > 0f)
				{
					__instance.cooldownSecondsRemaining = 0.01f;
				}
				UpdateAbilityButton();
			}
		}
	}

	[HarmonyPatch(typeof(TrackerRole), "FixedUpdate")]
	public static class TrackerUpdatePatch
	{
		public static void Postfix(TrackerRole __instance)
		{
			CheatManager instance = CheatManager.Instance;
			if (instance == null)
			{
				return;
			}
			if (instance.EndlessTracking)
			{
				__instance.durationSecondsRemaining = MAX_SAFE_VALUE;
			}
			if (instance.NoTrackingCooldown)
			{
				if (__instance.cooldownSecondsRemaining > 0f)
				{
					__instance.cooldownSecondsRemaining = 0.01f;
				}
				__instance.delaySecondsRemaining = 0.01f;
				UpdateAbilityButton();
			}
		}
	}

	[HarmonyPatch(typeof(ScientistRole), "Update")]
	public static class ScientistUpdatePatch
	{
		public static void Postfix(ScientistRole __instance)
		{
			CheatManager instance = CheatManager.Instance;
			if (instance == null)
			{
				return;
			}
			if (instance.EndlessBattery)
			{
				__instance.currentCharge = MAX_SAFE_VALUE;
				HudManager cachedHud = CachedHud;
				AbilityButton val = ((cachedHud != null) ? cachedHud.AbilityButton : null);
				if ((Object)(object)val != (Object)null)
				{
					((ActionButton)val).canInteract = true;
				}
			}
			if (instance.NoVitalsCooldown && __instance.currentCooldown > 0f)
			{
				__instance.currentCooldown = 0.01f;
				UpdateAbilityButton();
			}
		}
	}

	[HarmonyPatch(typeof(Vent), "CanUse")]
	private class VentCanUsePatch
	{
		private static bool Prefix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)((pc != null) ? pc.Object : null) == (Object)(object)PlayerControl.LocalPlayer)
			{
				CheatManager instance = CheatManager.Instance;
				if (instance != null && instance.AllowVenting)
				{
					if (PlayerControl.LocalPlayer.Data.IsDead)
					{
						canUse = false;
						couldUse = false;
						__result = float.MaxValue;
						return false;
					}
					Vector2 val = InteropFix.Cast(((Component)__instance).transform.position);
					float num = Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), val);
					canUse = num < __instance.UsableDistance;
					couldUse = true;
					__result = num;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayerControl), "get_CanMove")]
	public static class AlwaysMovePatch
	{
		private static void Postfix(PlayerControl __instance, ref bool __result)
		{
			if ((Object)(object)__instance == (Object)(object)PlayerControl.LocalPlayer && __instance.inVent)
			{
				CheatManager instance = CheatManager.Instance;
				if (instance != null && instance.AllowVenting && (Object)(object)ShipStatus.Instance != (Object)null && __instance.moveable && !__instance.shapeshifting && !InteropFix.Cast((Object)(object)MeetingHud.Instance) && !InteropFix.Cast((Object)(object)ExileController.Instance) && !InteropFix.Cast((Object)(object)Minigame.Instance))
				{
					__result = true;
				}
			}
		}
	}

	public static readonly float MAX_SAFE_VALUE = 3600f;

	private static HudManager _cachedHud;

	private static HudManager CachedHud => _cachedHud ?? (_cachedHud = DestroyableSingleton<HudManager>.Instance);

	public static void EnableVentingForAll(HudManager hudManager)
	{
		if ((Object)(object)hudManager == (Object)null)
		{
			return;
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current != (Object)null && (Object)(object)current.Data != (Object)null && !current.Data.IsDead && (Object)(object)current.Data.Role != (Object)null && !current.Data.Role.CanVent)
			{
				current.Data.Role.CanVent = true;
				if ((Object)(object)current == (Object)(object)PlayerControl.LocalPlayer && (Object)(object)hudManager.ImpostorVentButton != (Object)null)
				{
					((Component)hudManager.ImpostorVentButton).gameObject.SetActive(true);
				}
			}
		}
	}

	public static void UpdateAbilityButton()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		HudManager cachedHud = CachedHud;
		AbilityButton val = ((cachedHud != null) ? cachedHud.AbilityButton : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		try
		{
			((ActionButton)val).SetCoolDown(0f, 1f);
			((ActionButton)val).canInteract = true;
			((Behaviour)val).enabled = true;
			if ((Object)(object)((ActionButton)val).graphic != (Object)null)
			{
				((ActionButton)val).graphic.color = Color.white;
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"Error in UpdateAbilityButton: {value}"));
		}
	}
}



