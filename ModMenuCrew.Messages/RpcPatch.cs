using HarmonyLib;
using Hazel;

namespace ModMenuCrew.Messages;

[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
public static class RpcPatch
{
	public static void Postfix(PlayerControl __instance, byte callId, MessageReader reader)
	{
		if (callId == 201)
		{
			CustomMessage.HandleBypass(reader);
		}
	}
}
