using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(PlayerControl), "Shapeshift")]
public static class ShapeshiftPatch
{
	public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer, bool animate)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording && !((Object)(object)__instance == (Object)null) && !((Object)(object)targetPlayer == (Object)null))
		{
			ReplayRecorder instance = ReplayRecorder.Instance;
			byte playerId = __instance.PlayerId;
			byte playerId2 = targetPlayer.PlayerId;
			Vector2 position = InteropFix.Cast(((Component)__instance).transform.position);
			NetworkedPlayerInfo data = __instance.Data;
			string obj = ((data != null) ? data.PlayerName : null) ?? "?";
			NetworkedPlayerInfo data2 = targetPlayer.Data;
			instance.LogEvent(ReplayEventType.Shapeshift, playerId, playerId2, position, "Shapeshift: " + obj + " -> " + (((data2 != null) ? data2.PlayerName : null) ?? "?"));
		}
	}
}

