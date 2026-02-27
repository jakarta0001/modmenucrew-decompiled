using HarmonyLib;
using Hazel;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerPhysics), "HandleRpc")]
public static class RemoteVentLogPatch
{
	public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
	{
		try
		{
			if ((Object)(object)((__instance != null) ? __instance.myPlayer : null) == (Object)null || (Object)(object)__instance.myPlayer == (Object)(object)PlayerControl.LocalPlayer || (callId != 19 && callId != 20))
			{
				return;
			}
			int position = reader.Position;
			try
			{
				int ventId = reader.ReadPackedInt32();
				bool entering = callId == 19;
				EventLogger.LogVent(__instance.myPlayer, ventId, entering);
			}
			catch
			{
			}
			finally
			{
				reader.Position = position;
			}
		}
		catch
		{
		}
	}
}
