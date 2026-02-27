using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(MainMenuManager), "Start")]
public static class MainMenuStartPatch
{
	public static void Postfix()
	{
		MMCRendererComponent.EnsureExists();
	}
}
