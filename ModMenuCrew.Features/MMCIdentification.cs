using System.Text;
using BepInEx.Configuration;
using Hazel;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using InnerNet;
using TMPro;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class MMCIdentification
{
	private const string MMC_SIGNATURE = "MMC_v5";

	private const float HANDSHAKE_INTERVAL = 5f;

	private const float TAG_OFFSET_Y = 0.4f;

	private static System.Collections.Generic.HashSet<byte> _mmcPlayers = new System.Collections.Generic.HashSet<byte>();

	private static System.Collections.Generic.HashSet<byte> _taggedPlayers = new System.Collections.Generic.HashSet<byte>();

	private static float _lastHandshakeTime = 0f;

	private static bool _handshakeSent = false;

	private static GUIStyle _tagStyle = null;

	private static float _hue = 0f;

	private static StringBuilder _sharedSb = new StringBuilder(256);

	private static readonly char STAR_CHAR = '*';

	private static readonly string MMC_TAG = "MMC";

	private static readonly string NAME_COLOR_START = "<color=#FFFFFF>";

	private static readonly string NAME_COLOR_END = "</color>";

	public static bool Enabled { get; set; } = true;

	public static int MMCPlayerCount => _mmcPlayers.Count;

	private static bool IsSystemDisabled => CheatConfig.HideMMCStar?.Value ?? false;

	public static bool IsMMCPlayer(byte playerId)
	{
		return _mmcPlayers.Contains(playerId);
	}

	public static System.Collections.Generic.List<string> GetMMCPlayerNames()
	{
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		if (PlayerControl.AllPlayerControls == null)
		{
			return list;
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current != (Object)null && _mmcPlayers.Contains(current.PlayerId))
			{
				NetworkedPlayerInfo data = current.Data;
				list.Add(((data != null) ? data.PlayerName : null) ?? "Unknown");
			}
		}
		return list;
	}

	public static void ClearMMCPlayers()
	{
		_mmcPlayers.Clear();
		_taggedPlayers.Clear();
		_handshakeSent = false;
		_lastHandshakeTime = 0f;
	}

	public static void ClearAppliedTags()
	{
		_taggedPlayers.Clear();
	}

	public static void ReapplyAllMMCTags()
	{
		if (!Enabled || IsSystemDisabled || PlayerControl.AllPlayerControls == null)
		{
			return;
		}
		_taggedPlayers.Clear();
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current != (Object)null && _mmcPlayers.Contains(current.PlayerId))
			{
				ApplyMMCTagToPlayer(current);
			}
		}
	}

	public static void SendHandshake()
	{
		if (!Enabled || IsSystemDisabled || (Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmConnected || (Object)(object)PlayerControl.LocalPlayer == (Object)null)
		{
			return;
		}
		try
		{
			MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)202, (SendOption)1, -1);
			val.Write("MMC_v5");
			val.Write(PlayerControl.LocalPlayer.PlayerId);
			val.Write("6.0.8");
			((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
			_handshakeSent = true;
			_lastHandshakeTime = Time.time;
			if (!_mmcPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
			{
				_mmcPlayers.Add(PlayerControl.LocalPlayer.PlayerId);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[MMCIdentification] Error sending handshake: " + ex.Message));
		}
	}

	public static void HandleHandshake(MessageReader reader, byte senderId)
	{
		if (!Enabled || IsSystemDisabled)
		{
			return;
		}
		try
		{
			string text = reader.ReadString();
			byte b = reader.ReadByte();
			string value = reader.ReadString();
			if (text != "MMC_v5")
			{
				Debug.LogWarning(InteropFix.Cast($"[MMCIdentification] Invalid signature from {b}"));
			}
			else
			{
				if (_mmcPlayers.Contains(b))
				{
					return;
				}
				_mmcPlayers.Add(b);
				string value2 = "Unknown";
				PlayerControl val = null;
				if (PlayerControl.AllPlayerControls != null)
				{
					var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
					while (enumerator.MoveNext())
					{
						PlayerControl current = enumerator.Current;
						if ((Object)(object)current != (Object)null && current.PlayerId == b)
						{
							NetworkedPlayerInfo data = current.Data;
							value2 = ((data != null) ? data.PlayerName : null) ?? "Unknown";
							val = current;
							break;
						}
					}
				}
				Debug.Log(InteropFix.Cast($"[MMCIdentification] New MMC player identified: {value2} (ID: {b}, v{value})"));
				if ((Object)(object)val != (Object)null && !GameCheats.IsRevealSusActive)
				{
					ApplyMMCTagToPlayer(val);
				}
				if (!_handshakeSent)
				{
					SendHandshake();
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[MMCIdentification] Error processing handshake: " + ex.Message));
		}
	}

	public static void Update()
	{
		if (Enabled && !IsSystemDisabled && !((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmConnected && !((Object)(object)PlayerControl.LocalPlayer == (Object)null))
		{
			LobbyStartPatch.CheckPendingHandshake();
			if (Time.time - _lastHandshakeTime > 5f)
			{
				SendHandshake();
			}
		}
	}

	private static string GetRainbowColorHex()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		_hue += Time.deltaTime * 0.5f;
		if (_hue > 1f)
		{
			_hue -= 1f;
		}
		return ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(_hue, 1f, 1f));
	}

	private static int GetPulsingSize()
	{
		return (int)((0.9f + Mathf.PingPong(Time.time * 2f, 0.2f)) * 100f);
	}

	public static void ApplyMMCTagToPlayer(PlayerControl player)
	{
		if ((Object)(object)((player != null) ? player.Data : null) == (Object)null)
		{
			return;
		}
		CosmeticsLayer cosmetics = player.cosmetics;
		if ((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) == (Object)null || IsSystemDisabled)
		{
			return;
		}
		ConfigEntry<bool> hideMMCStar = CheatConfig.HideMMCStar;
		if ((hideMMCStar != null && hideMMCStar.Value) || _taggedPlayers.Contains(player.PlayerId))
		{
			return;
		}
		try
		{
			string playerName = player.Data.PlayerName;
			byte playerId = player.PlayerId;
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			bool num = playerId == ((localPlayer != null) ? localPlayer.PlayerId : byte.MaxValue);
			string text = "FFD700";
			string text2 = (num ? ("<color=#" + text + ">â˜…</color> ") : ("<color=#" + text + "><b>â˜… MMC â˜…</b></color> ")) + "<color=#FFFFFF>" + playerName + "</color>";
			((TMP_Text)player.cosmetics.nameText).text = text2;
			_taggedPlayers.Add(player.PlayerId);
			Debug.Log(InteropFix.Cast($"[MMCIdentification] Tag applied to {playerName} (ID: {player.PlayerId})"));
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[MMCIdentification] Error applying tag: " + ex.Message));
		}
	}

	public static void UpdateMMCNameTags()
	{
		if (!Enabled || GameCheats.IsRevealSusActive)
		{
			return;
		}
		List<PlayerControl> allPlayerControls = PlayerControl.AllPlayerControls;
		if (allPlayerControls == null || allPlayerControls.Count == 0)
		{
			return;
		}
		ConfigEntry<bool> hideMMCStar = CheatConfig.HideMMCStar;
		if ((hideMMCStar != null && hideMMCStar.Value) || IsSystemDisabled)
		{
			for (int i = 0; i < allPlayerControls.Count; i++)
			{
				PlayerControl val = allPlayerControls[i];
				if ((Object)(object)((val != null) ? val.Data : null) == (Object)null)
				{
					continue;
				}
				CosmeticsLayer cosmetics = val.cosmetics;
				if (!((Object)(object)((cosmetics != null) ? cosmetics.nameText : null) == (Object)null))
				{
					string text = ((TMP_Text)val.cosmetics.nameText).text;
					if (text != null && (text.IndexOf(STAR_CHAR) >= 0 || text.IndexOf(MMC_TAG, System.StringComparison.Ordinal) >= 0))
					{
						((TMP_Text)val.cosmetics.nameText).text = val.Data.PlayerName;
					}
				}
			}
		}
		else
		{
			if (_mmcPlayers.Count == 0)
			{
				return;
			}
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			byte b = ((localPlayer != null) ? localPlayer.PlayerId : byte.MaxValue);
			string rainbowColorHex = GetRainbowColorHex();
			int pulsingSize = GetPulsingSize();
			_sharedSb.Clear();
			_sharedSb.Append("<size=").Append(pulsingSize).Append("%><color=#")
				.Append(rainbowColorHex)
				.Append("><b>â˜… MMC USER â˜…</b></color></size> ");
			string text2 = _sharedSb.ToString();
			_sharedSb.Clear();
			_sharedSb.Append("<size=").Append(pulsingSize).Append("%><color=#")
				.Append(rainbowColorHex)
				.Append(">â˜…</color></size> ");
			string text3 = _sharedSb.ToString();
			for (int j = 0; j < allPlayerControls.Count; j++)
			{
				PlayerControl val2 = allPlayerControls[j];
				if ((Object)(object)((val2 != null) ? val2.Data : null) == (Object)null)
				{
					continue;
				}
				CosmeticsLayer cosmetics2 = val2.cosmetics;
				if ((Object)(object)((cosmetics2 != null) ? cosmetics2.nameText : null) == (Object)null)
				{
					continue;
				}
				byte playerId = val2.PlayerId;
				if (!_mmcPlayers.Contains(playerId))
				{
					string text4 = ((TMP_Text)val2.cosmetics.nameText).text;
					if (text4 != null && (text4.IndexOf(STAR_CHAR) >= 0 || text4.IndexOf(MMC_TAG, System.StringComparison.Ordinal) >= 0 || text4.IndexOf('\n') >= 0))
					{
						((TMP_Text)val2.cosmetics.nameText).text = val2.Data.PlayerName;
					}
					continue;
				}
				string playerName = val2.Data.PlayerName;
				bool flag = playerId == b;
				_sharedSb.Clear();
				_sharedSb.Append(flag ? text3 : text2);
				_sharedSb.Append(NAME_COLOR_START).Append(playerName).Append(NAME_COLOR_END);
				string text5 = _sharedSb.ToString();
				if (((TMP_Text)val2.cosmetics.nameText).text != text5)
				{
					((TMP_Text)val2.cosmetics.nameText).text = text5;
				}
			}
		}
	}

	public static void RenderMMCTags()
	{
	}

	public static void RemovePlayer(byte playerId)
	{
		_mmcPlayers.Remove(playerId);
	}
}




