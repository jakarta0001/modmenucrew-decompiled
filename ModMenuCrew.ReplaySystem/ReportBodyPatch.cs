using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(PlayerControl), "ReportDeadBody")]
public static class ReportBodyPatch
{
	public static void Prefix(PlayerControl __instance, NetworkedPlayerInfo target)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording)
		{
			byte targetId = (((Object)(object)target != (Object)null) ? target.PlayerId : byte.MaxValue);
			ReplayRecorder.Instance.LogEvent(ReplayEventType.Report, __instance.PlayerId, targetId, InteropFix.Cast(((Component)__instance).transform.position), "Report: " + __instance.Data.PlayerName + " found body");
		}
	}
}

