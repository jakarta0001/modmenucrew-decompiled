using HarmonyLib;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "RpcSetLevel")]
public static class PlayerControl_RpcSetLevel_Patch
{
	public static void Prefix(uint level)
	{
		if (SpoofingService.EnableLevelSpoof)
		{
			uint effectiveLevel = SpoofingService.GetEffectiveLevel();
			Debug.Log(InteropFix.Cast($"[SpoofingService] RpcSetLevel intercepted: {level} -> {effectiveLevel}"));
			level = effectiveLevel;
		}
	}
}


