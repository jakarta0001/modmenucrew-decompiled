using Hazel;
using Il2CppSystem;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew;

public static class SystemManager
{
	internal static void CloseDoorsOfType(SystemTypes type)
	{
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected I4, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (!InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			NotifyUtils.Error("ShipStatus not available!");
			return;
		}
		if (!IsDoorSystem(type))
		{
			NotifyUtils.Warning($"{type} does not have doors to close.");
			return;
		}
		try
		{
			MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)ShipStatus.Instance).NetId, (byte)27, (SendOption)1, ((InnerNetClient)AmongUsClient.Instance).HostId);
			val.Write((byte)(int)type);
			((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
			ShipStatus.Instance.RpcCloseDoorsOfType(type);
			NotifyUtils.Door($"{type} doors closed!");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[SystemManager] Error closing {type} doors: {value}"));
			NotifyUtils.Error($"Failed to close {type} doors!");
		}
	}

	private static bool IsDoorSystem(SystemTypes type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected I4, but got Unknown
		switch ((int)type - 1)
		{
		case 0:
		case 1:
		case 3:
		case 6:
		case 9:
		case 10:
		case 12:
			return true;
		default:
			return false;
		}
	}
}



