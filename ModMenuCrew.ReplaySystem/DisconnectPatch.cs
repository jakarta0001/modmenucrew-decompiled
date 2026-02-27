using System.Runtime.CompilerServices;
using HarmonyLib;
using InnerNet;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

[HarmonyPatch(typeof(AmongUsClient), "OnPlayerLeft")]
public static class DisconnectPatch
{
	public static void Postfix(AmongUsClient __instance, ClientData data, DisconnectReasons reason)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ReplayRecorder.Instance == (Object)null || !ReplayRecorder.Instance.IsRecording || data == null || (Object)(object)data.Character == (Object)null)
		{
			return;
		}
		try
		{
			ReplayRecorder instance = ReplayRecorder.Instance;
			byte playerId = data.Character.PlayerId;
			byte playerId2 = data.Character.PlayerId;
			Vector2 position = InteropFix.Cast(((Component)data.Character).transform.position);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Disconnect: ");
			NetworkedPlayerInfo data2 = data.Character.Data;
			defaultInterpolatedStringHandler.AppendFormatted(((data2 != null) ? data2.PlayerName : null) ?? "?");
			defaultInterpolatedStringHandler.AppendLiteral(" (");
			defaultInterpolatedStringHandler.AppendFormatted<DisconnectReasons>(reason);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			instance.LogEvent(ReplayEventType.Disconnect, playerId, playerId2, position, defaultInterpolatedStringHandler.ToStringAndClear());
		}
		catch
		{
		}
	}
}

