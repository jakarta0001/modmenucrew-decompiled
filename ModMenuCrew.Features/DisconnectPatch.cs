using HarmonyLib;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(AmongUsClient), "OnDisconnected")]
public static class DisconnectPatch
{
	public static void Postfix()
	{
		MMCIdentification.ClearMMCPlayers();
		try
		{
			GameCheats.CleanupRadarTextures();
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[DisconnectPatch] FIX 2026: Radar cleanup error: " + ex.Message));
		}
		try
		{
			SabotageService.ResetDoorLocks();
		}
		catch
		{
		}
	}
}


