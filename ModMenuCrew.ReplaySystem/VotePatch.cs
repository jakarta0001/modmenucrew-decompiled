using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(MeetingHud), "CastVote")]
public static class VotePatch
{
	public static void Postfix(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording)
		{
			ReplayRecorder.Instance.LogEvent(ReplayEventType.Vote, srcPlayerId, suspectPlayerId, Vector2.zero, $"Voted: {srcPlayerId} -> {suspectPlayerId}");
		}
	}
}
