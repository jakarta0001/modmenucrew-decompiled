using HarmonyLib;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "Shapeshift")]
public static class ShapeshiftLogPatch
{
	public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer, bool animate)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!((Object)(object)((__instance != null) ? __instance.Data : null) == (Object)null) && !((Object)(object)((targetPlayer != null) ? targetPlayer.Data : null) == (Object)null))
			{
				string text = __instance.Data.PlayerName ?? "Unknown";
				string text2 = targetPlayer.Data.PlayerName ?? "Unknown";
				string text3 = ((targetPlayer.PlayerId == __instance.PlayerId) ? "reverted to normal" : ("shapeshifted into " + text2));
				string message = text + " " + text3;
				RoleBehaviour role = __instance.Data.Role;
				EventLogger.Log(GameEventType.Shapeshift, message, text, ((role != null) ? ((object)role.Role/*cast due to .constrained prefix*/).ToString() : null) ?? "Shapeshifter");
			}
		}
		catch
		{
		}
	}
}
