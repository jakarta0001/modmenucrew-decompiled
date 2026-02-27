using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlatformSpecificData), "Serialize")]
public static class PlatformSpecificData_Serialize_Patch
{
	public static void Prefix(PlatformSpecificData __instance)
	{
		SpoofingService.ApplyPlatformSpoof(__instance);
	}
}
