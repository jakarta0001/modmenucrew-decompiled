using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(ShipStatus), "Start")]
public static class GameStartPatch
{
	public static void Postfix()
	{
		MMCIdentification.SendHandshake();
		MMCRendererComponent.EnsureExists();
	}
}
