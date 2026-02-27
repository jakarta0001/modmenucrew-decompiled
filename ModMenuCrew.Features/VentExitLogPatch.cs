using HarmonyLib;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerPhysics), "RpcExitVent")]
public static class VentExitLogPatch
{
	public static void Postfix(PlayerPhysics __instance, int ventId)
	{
		try
		{
			if (!((Object)(object)((__instance != null) ? __instance.myPlayer : null) == (Object)null))
			{
				EventLogger.LogVent(__instance.myPlayer, ventId, entering: false);
			}
		}
		catch
		{
		}
	}
}
