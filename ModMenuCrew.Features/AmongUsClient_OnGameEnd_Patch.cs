using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(AmongUsClient), "OnGameEnd")]
public static class AmongUsClient_OnGameEnd_Patch
{
	public static void Postfix()
	{
		if (SpoofingService.EnableLevelSpoof)
		{
			SpoofingService.ApplyLevelSpoof();
		}
		if (SpoofingService.EnableFriendCodeSpoof)
		{
			SpoofingService.ApplyFriendCodeSpoof();
		}
	}
}
