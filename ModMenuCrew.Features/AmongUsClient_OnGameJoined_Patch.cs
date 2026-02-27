using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(AmongUsClient), "OnGameJoined")]
public static class AmongUsClient_OnGameJoined_Patch
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
