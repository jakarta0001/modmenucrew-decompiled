using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Features;

[HarmonyPatch]
public static class ChatCommandsPatch
{
	private static DateTime lastCommandTime = DateTime.MinValue;

	private const string PING_COMMAND = "/ping";

	private const float COMMAND_COOLDOWN_SECONDS = 3f;

	[HarmonyPatch(typeof(GameManager), "StartGame")]
	[HarmonyPostfix]
	public static void Postfix_StartGame()
	{
		lastCommandTime = DateTime.MinValue;
	}

	[HarmonyPatch(typeof(ChatController), "SendChat")]
	[HarmonyPrefix]
	public static bool Prefix_SendChat(ChatController __instance)
	{
		if (string.IsNullOrWhiteSpace(__instance.freeChatField.Text) || ((AbstractChatInputField)__instance.quickChatField).Visible)
		{
			return true;
		}
		if (__instance.freeChatField.Text.Split(' ')[0].Equals("/ping", StringComparison.OrdinalIgnoreCase))
		{
			((AbstractChatInputField)__instance.freeChatField).Clear();
			__instance.timeSinceLastMessage = 3f;
			HandlePingCommand();
			return false;
		}
		return true;
	}

	private static void HandlePingCommand()
	{
		if (!((InnerNetClient)AmongUsClient.Instance).AmConnected)
		{
			SendMessage("<color=#ff0000>Error: You are not connected to a lobby.</color>");
		}
		else if ((DateTime.UtcNow - lastCommandTime).TotalSeconds < 3.0)
		{
			int value = (int)Math.Ceiling(3.0 - (DateTime.UtcNow - lastCommandTime).TotalSeconds);
			SendMessage($"<color=#ffff00>Please wait {value}s before using this command again.</color>");
		}
		else
		{
			lastCommandTime = DateTime.UtcNow;
			SendMessage(BuildInfoMessage());
		}
	}

	private static string BuildInfoMessage()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<b><color=#00ffff>--- Room Status ---</color></b>");
		int ping = ((InnerNetClient)AmongUsClient.Instance).Ping;
		string pingColor = GetPingColor(ping);
		float fps = 1f / Time.smoothDeltaTime;
		string regionName = GetRegionName();
		string roomCode = "N/A";
		if (((InnerNetClient)AmongUsClient.Instance).GameId != 0)
		{
			roomCode = GameCode.IntToGameName(((InnerNetClient)AmongUsClient.Instance).GameId);
		}
		stringBuilder.AppendLine($"<b>Ping:</b> <color=#{pingColor}>{ping}ms</color> | <b>FPS:</b> {fps:0} | <b>Region:</b> {regionName}");
		stringBuilder.AppendLine($"<b>Room Code:</b> <color=#ffa500>{roomCode}</color>");
		stringBuilder.AppendLine("");
		if (PlayerControl.AllPlayerControls.Count > 0)
		{
			stringBuilder.AppendLine($"<b><color=#ffff00>Players ({PlayerControl.AllPlayerControls.Count}):</color></b>");
			Dictionary<byte, ClientData> dictionary = new Dictionary<byte, ClientData>();
			var enumerator = ((InnerNetClient)AmongUsClient.Instance).allClients.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ClientData current = enumerator.Current;
				if ((Object)(object)current.Character != (Object)null)
				{
					dictionary[current.Character.PlayerId] = current;
				}
			}
			List<PlayerControl> list = new List<PlayerControl>();
			var enumerator2 = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				PlayerControl current2 = enumerator2.Current;
				list.Add(current2);
			}
			list = list.OrderBy((PlayerControl p) => p.PlayerId).ToList();
			foreach (PlayerControl item in list)
			{
				if ((Object)(object)item.Data == (Object)null)
				{
					continue;
				}
				string playerName = item.Data.PlayerName;
				bool isLocal = (Object)(object)item == (Object)(object)PlayerControl.LocalPlayer;
				bool isHost = false;
				string platformName = "Unknown";
				if (dictionary.TryGetValue(item.PlayerId, out var value))
				{
					isHost = value.Id == ((InnerNetClient)AmongUsClient.Instance).HostId;
					platformName = GetPlatformName(value.PlatformData.Platform);
				}
				string hostPrefix = isHost ? "<color=#ff0000>[HOST]</color> " : "";
				string localSuffix = isLocal ? " <color=#00ff00>(You)</color>" : "";
				stringBuilder.AppendLine($"{hostPrefix}{playerName}{localSuffix} <size=80%><color=#cccccc>[{platformName}]</color></size>");
			}
		}
		else
		{
			stringBuilder.AppendLine("<i>No players found (empty lobby or error).</i>");
		}
		return stringBuilder.ToString();
	}
	private static string GetPingColor(int ping)
	{
		if (ping < 100)
		{
			return "00ff00";
		}
		if (ping < 200)
		{
			return "ffff00";
		}
		return "ff0000";
	}

	private static string GetRegionName()
	{
		if (DestroyableSingleton<ServerManager>.InstanceExists)
		{
			return DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Name;
		}
		return "Unknown";
	}

	private static string GetPlatformName(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		switch ((int)platform - 1)
		{
		case 5:
		case 6:
			return "Mobile";
		case 7:
			return "Switch";
		case 8:
			return "Xbox";
		case 9:
			return "PlayStation";
		case 1:
			return "PC (Steam)";
		case 0:
			return "PC (Epic)";
		case 4:
			return "PC (Itch)";
		case 3:
			return "PC (MS Store)";
		default:
			return "PC";
		}
	}

	private static void SendMessage(string message)
	{
		if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null && (Object)(object)DestroyableSingleton<HudManager>.Instance.Chat != (Object)null)
		{
			DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, message, false);
		}
	}
}






