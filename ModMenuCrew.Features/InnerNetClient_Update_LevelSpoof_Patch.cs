using AmongUs.Data;
using AmongUs.Data.Player;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(InnerNetClient), "Update")]
public static class InnerNetClient_Update_LevelSpoof_Patch
{
	private static float _lastCheck;

	private const float CHECK_INTERVAL = 2f;

	public static void Postfix()
	{
		if (Time.time - _lastCheck < 2f)
		{
			return;
		}
		_lastCheck = Time.time;
		try
		{
			if (SpoofingService.EnableLevelSpoof)
			{
				PlayerData player = DataManager.Player;
				if (((player != null) ? player.Stats : null) != null)
				{
					uint effectiveLevel = SpoofingService.GetEffectiveLevel();
					if (DataManager.Player.Stats.Level != effectiveLevel)
					{
						DataManager.Player.Stats.Level = effectiveLevel;
						if (!(CheatConfig.OfflineLocalMode?.Value ?? false))
						{
							((AbstractSaveData)DataManager.Player).Save();
						}
					}
				}
			}
			if (SpoofingService.EnableFriendCodeSpoof && (Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				SpoofingService.ApplyFriendCodeSpoof();
			}
		}
		catch
		{
		}
	}
}
