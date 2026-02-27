using HarmonyLib;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(LobbyBehaviour), "Start")]
public static class LobbyStartPatch
{
	private static float _lobbyJoinTime;

	private static bool _pendingHandshake;

	public static void Postfix()
	{
		MMCRendererComponent.EnsureExists();
		MMCIdentification.ClearMMCPlayers();
		_lobbyJoinTime = Time.time;
		_pendingHandshake = true;
	}

	public static void CheckPendingHandshake()
	{
		if (_pendingHandshake && Time.time - _lobbyJoinTime > 1.5f)
		{
			_pendingHandshake = false;
			MMCIdentification.SendHandshake();
		}
	}
}
