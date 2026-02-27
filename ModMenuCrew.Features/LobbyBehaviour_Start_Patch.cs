using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(LobbyBehaviour), "Start")]
public static class LobbyBehaviour_Start_Patch
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
