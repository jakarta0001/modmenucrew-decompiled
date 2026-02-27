using HarmonyLib;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(MeetingHud), "Start")]
public static class MeetingStartPatch
{
	public static void Postfix(MeetingHud __instance)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)ReplayRecorder.Instance == (Object)null) && ReplayRecorder.Instance.IsRecording)
		{
			ReplayRecorder.Instance.LogEvent(ReplayEventType.Meeting, byte.MaxValue, byte.MaxValue, Vector2.zero, "Meeting Started");
		}
	}
}
