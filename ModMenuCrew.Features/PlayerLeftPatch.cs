using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(AmongUsClient), "OnPlayerLeft")]
public static class PlayerLeftPatch
{
	public static void Postfix(ClientData data)
	{
		if ((Object)(object)((data != null) ? data.Character : null) != (Object)null)
		{
			MMCIdentification.RemovePlayer(data.Character.PlayerId);
		}
	}
}
