using Il2CppSystem.Collections.Generic;
using InnerNet;
using TMPro;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class PlayerInfoDisplay
{
	private const char MARKER = '\u200b';

	private static readonly string[] PlatformNames = new string[11]
	{
		"Unknown", "Epic", "Steam", "Mac", "Win10", "Itch", "iPhone", "Android", "Switch", "Xbox",
		"PS"
	};

	private static string GetPlatformName(int platformId)
	{
		if (platformId >= 0 && platformId < PlatformNames.Length)
		{
			return PlatformNames[platformId];
		}
		return "?";
	}

	public static void UpdatePlayerInfoTags()
	{
		if (!InteropFix.Cast((Object)(object)LobbyBehaviour.Instance) || GameCheats.IsRevealSusActive)
		{
			return;
		}
		bool flag = CheatConfig.ShowPlayerInfo?.Value ?? false;
		List<PlayerControl> allPlayerControls = PlayerControl.AllPlayerControls;
		if (allPlayerControls == null || allPlayerControls.Count == 0)
		{
			return;
		}
		for (int i = 0; i < allPlayerControls.Count; i++)
		{
			PlayerControl val = allPlayerControls[i];
			if ((Object)(object)((val != null) ? val.Data : null) == (Object)null)
			{
				continue;
			}
			CosmeticsLayer cosmetics = val.cosmetics;
			if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) == (Object)null)
			{
				continue;
			}
			string text = ((TMP_Text)val.cosmetics.nameText).text;
			if (text == null)
			{
				continue;
			}
			int num = text.IndexOf('\u200b');
			bool flag2 = num >= 0;
			if (flag)
			{
				if (!flag2)
				{
					string text2 = BuildInfoSuffix(val);
					if (text2 != null)
					{
						((TMP_Text)val.cosmetics.nameText).text = text + text2;
					}
				}
			}
			else if (flag2)
			{
				((TMP_Text)val.cosmetics.nameText).text = text.Substring(0, num);
			}
		}
	}

	private static string BuildInfoSuffix(PlayerControl player)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected I4, but got Unknown
		try
		{
			AmongUsClient instance = AmongUsClient.Instance;
			ClientData val = ((instance != null) ? ((InnerNetClient)instance).GetClient(((InnerNetObject)player).OwnerId) : null);
			if (val == null)
			{
				return null;
			}
			int platformId = 0;
			if (val.PlatformData != null)
			{
				platformId = (int)val.PlatformData.Platform;
			}
			string platformName = GetPlatformName(platformId);
			uint playerLevel = val.PlayerLevel;
			int id = val.Id;
			return $"{8203}\n<size=65%><color=#888888>[{platformName} Â· Lv{playerLevel} Â· #{id}]</color></size>";
		}
		catch
		{
			return null;
		}
	}

	public static void CleanupAll()
	{
		try
		{
			List<PlayerControl> allPlayerControls = PlayerControl.AllPlayerControls;
			if (allPlayerControls == null)
			{
				return;
			}
			for (int i = 0; i < allPlayerControls.Count; i++)
			{
				PlayerControl val = allPlayerControls[i];
				object obj;
				if (val == null)
				{
					obj = null;
				}
				else
				{
					CosmeticsLayer cosmetics = val.cosmetics;
					obj = ((cosmetics != null) ? cosmetics.nameText : null);
				}
				if ((Object)obj == (Object)null)
				{
					continue;
				}
				string text = ((TMP_Text)val.cosmetics.nameText).text;
				if (text != null)
				{
					int num = text.IndexOf('\u200b');
					if (num >= 0)
					{
						((TMP_Text)val.cosmetics.nameText).text = text.Substring(0, num);
					}
				}
			}
		}
		catch
		{
		}
	}
}

