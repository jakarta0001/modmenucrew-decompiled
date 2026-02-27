using HarmonyLib;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "StartMeeting")]
public static class MeetingLogPatch
{
	public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
	{
		try
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = __instance.Data;
				obj = ((data != null) ? data.PlayerName : null);
			}
			if (obj == null)
			{
				obj = "Unknown";
			}
			string text = (string)obj;
			string message = (((Object)(object)target == (Object)null) ? (text + " called an emergency meeting") : (text + " reported " + (target.PlayerName ?? "unknown") + "'s body"));
			EventLogger.Log(GameEventType.Meeting, message, text);
		}
		catch
		{
		}
	}
}
