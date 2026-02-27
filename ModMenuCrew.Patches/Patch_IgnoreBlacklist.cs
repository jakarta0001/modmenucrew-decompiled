using HarmonyLib;

namespace ModMenuCrew.Patches;

[HarmonyPatch(typeof(HatManager), "CheckValidCosmetic")]
public static class Patch_IgnoreBlacklist
{
	public static bool Prefix(ref bool __result)
	{
		__result = true;
		return false;
	}
}

