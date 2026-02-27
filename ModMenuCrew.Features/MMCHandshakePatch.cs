using HarmonyLib;
using Hazel;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
public static class MMCHandshakePatch
{
	public static void Postfix(PlayerControl __instance, byte callId, MessageReader reader)
	{
		if (callId == 202)
		{
			MMCIdentification.HandleHandshake(reader, __instance.PlayerId);
		}
	}
}
