using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(PlayerPhysics), "RpcExitVent")]
public static class VentExitPatch
{
	public static void Postfix(PlayerPhysics __instance, int ventId)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording)
		{
			ReplayRecorder.Instance.LogEvent(ReplayEventType.Vent, __instance.myPlayer.PlayerId, (byte)ventId, InteropFix.Cast(((Component)__instance).transform.position), "Exited Vent");
			ReplayRecorder.Instance.SetAnimState(__instance.myPlayer.PlayerId, AnimState.VentExit);
		}
	}
}

