using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(Constants), "GetPlatformData")]
public static class Constants_GetPlatformData_Patch
{
	public static void Postfix(PlatformSpecificData __result)
	{
		if (SpoofingService.EnablePlatformSpoof && __result != null)
		{
			SpoofingService.ApplyPlatformSpoof(__result);
		}
	}
}

