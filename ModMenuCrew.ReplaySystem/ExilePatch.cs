using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(ExileController), "BeginForGameplay")]
public static class ExilePatch
{
	public static void Postfix(ExileController __instance, NetworkedPlayerInfo player, bool voteTie)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ReplayRecorder.Instance == (Object)null || !ReplayRecorder.Instance.IsRecording)
		{
			return;
		}
		try
		{
			if (!((Object)(object)player == (Object)null))
			{
				ReplayRecorder.Instance.LogEvent(ReplayEventType.Exiled, player.PlayerId, player.PlayerId, Vector2.zero, "Exiled: " + player.PlayerName);
			}
		}
		catch
		{
		}
	}
}
