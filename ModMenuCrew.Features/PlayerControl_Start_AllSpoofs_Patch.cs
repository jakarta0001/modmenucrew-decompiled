using HarmonyLib;
using InnerNet;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "Start")]
public static class PlayerControl_Start_AllSpoofs_Patch
{
	public static void Prefix(PlayerControl __instance)
	{
		if (((InnerNetObject)__instance).AmOwner && SpoofingService.EnableLevelSpoof)
		{
			SpoofingService.ApplyLevelSpoof();
		}
	}

	public static void Postfix(PlayerControl __instance)
	{
		if (((InnerNetObject)__instance).AmOwner && SpoofingService.EnableFriendCodeSpoof)
		{
			SpoofingService.ApplyFriendCodeSpoof();
		}
	}
}
