using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(PlayerControl), "MurderPlayer")]
public static class MurderPlayerPatch
{
	public static void Prefix(PlayerControl __instance, PlayerControl target)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording && !((Object)(object)__instance == (Object)null) && !((Object)(object)target == (Object)null))
		{
			ReplayRecorder.Instance.LogEvent(ReplayEventType.Kill, __instance.PlayerId, target.PlayerId, InteropFix.Cast(((Component)target).transform.position), "Kill: " + __instance.Data.PlayerName + " -> " + target.Data.PlayerName);
		}
	}
}

