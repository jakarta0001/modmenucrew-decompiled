using HarmonyLib;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "CmdReportDeadBody")]
public static class ReportLogPatch
{
	public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
	{
		try
		{
			if (!((Object)(object)((__instance != null) ? __instance.Data : null) == (Object)null))
			{
				EventLogger.Log(GameEventType.Report, __instance.Data.PlayerName + " reported " + (((target != null) ? target.PlayerName : null) ?? "emergency"), __instance.Data.PlayerName);
			}
		}
		catch
		{
		}
	}
}
