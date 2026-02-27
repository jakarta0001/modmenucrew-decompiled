using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Il2CppSystem;
using ModMenuCrew.Features;
using ModMenuCrew.Networking;
using UnityEngine;

namespace ModMenuCrew;

public static class ServerData
{
	public sealed class UISnapshot
	{
		public readonly byte[] LobbyBytecode;

		public readonly byte[] GameBytecode;

		public readonly byte[] PlayerPickBytecode;

		public readonly byte[] BanMenuBytecode;

		public readonly byte[] CheatsBytecode;

		public readonly byte[] SpoofingBytecode;

		public readonly long SessionToken;

		public readonly int BytecodeVersion;

		public readonly bool IsValid;

		public UISnapshot(byte[] lobby, byte[] game, byte[] playerPick, byte[] banMenu, byte[] cheats, byte[] spoofing, long token, int version)
		{
			LobbyBytecode = lobby;
			GameBytecode = game;
			PlayerPickBytecode = playerPick;
			BanMenuBytecode = banMenu;
			CheatsBytecode = cheats;
			SpoofingBytecode = spoofing;
			SessionToken = token;
			BytecodeVersion = version;
			IsValid = (lobby != null || game != null) && token > 0;
		}
	}

	public class EncryptedPayload
	{
		public string Ciphertext { get; set; }

		public string Iv { get; set; }

		public string Tag { get; set; }
	}

	private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;

	private static long _integrityHashXor = 5439832262788800600L;

	private const long INTEGRITY_HASH_SENTINEL = 5439832262788800600L;

	private static long _lastRealtimeUpdateMs = 0L;

	private const long REALTIME_PROTECTION_WINDOW_MS = 5000L;

	private static long _lastDenialTimestamp = 0L;

	private static int _isLoadedXor = -559038737;

	private static List<TabDefinition> _tabsInternal = new List<TabDefinition>();

	private static List<string> _premiumFeaturesInternal = new List<string>();

	private static System.Collections.Generic.HashSet<string> _validControlIds = new System.Collections.Generic.HashSet<string>();

	private static Dictionary<string, bool> _toggleStates = new Dictionary<string, bool>();

	private static Dictionary<string, float> _sliderValues = new Dictionary<string, float>();

	private static Dictionary<string, string> _actionIdReverseMap = new Dictionary<string, string>();

	private static float _delayedDenialTime = -1f;

	public static string SessionDecryptKey { get; private set; }

	public static long LastUpdate { get; private set; }

	public static long LastRealtimeUpdateMs => _lastRealtimeUpdateMs;

	public static bool IsLoaded => (_isLoadedXor ^ -559038737) == 1;

	public static IReadOnlyList<TabDefinition> Tabs => _tabsInternal.AsReadOnly();

	public static IReadOnlyList<string> PremiumFeatures => _premiumFeaturesInternal.AsReadOnly();

	public static UISnapshot CurrentSnapshot { get; private set; }

	public static byte[] LobbyBytecode => CurrentSnapshot?.LobbyBytecode;

	public static byte[] GameBytecode => CurrentSnapshot?.GameBytecode;

	public static int BytecodeVersion => CurrentSnapshot?.BytecodeVersion ?? 0;

	public static Vector2 RenderOffset { get; private set; } = Vector2.zero;

	internal static void SetLoaded(bool loaded)
	{
		_isLoadedXor = (loaded ? 1 : 0) ^ -559038737;
		LastUpdate = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	private static byte[] HexToBytes(string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return null;
		}
		int length = hex.Length;
		byte[] array = new byte[length / 2];
		for (int i = 0; i < length; i += 2)
		{
			array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		}
		return array;
	}

	private static string DecryptPayload(EncryptedPayload payload, string sessionKeyHex)
	{
		if (payload == null || string.IsNullOrEmpty(payload.Ciphertext) || string.IsNullOrEmpty(payload.Iv) || string.IsNullOrEmpty(payload.Tag) || string.IsNullOrEmpty(sessionKeyHex))
		{
			return null;
		}
		try
		{
			byte[] key = HexToBytes(sessionKeyHex);
			byte[] nonce = HexToBytes(payload.Iv);
			byte[] tag = HexToBytes(payload.Tag);
			byte[] array = Convert.FromBase64String(payload.Ciphertext);
			byte[] array2 = new byte[array.Length];
			using (AesGcm aesGcm = new AesGcm(key))
			{
				aesGcm.Decrypt(nonce, array, tag, array2);
			}
			return Encoding.UTF8.GetString(array2);
		}
		catch
		{
			return null;
		}
	}

	internal static void ParseFromEncryptedPayload(EncryptedPayload encryptedPayload, string sessionKeyHex, long newSessionToken = 0L, bool isHeartbeat = false)
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = DecryptPayload(encryptedPayload, sessionKeyHex);
			if (string.IsNullOrEmpty(text))
			{
				if (isHeartbeat)
				{
					SetLoaded(loaded: false);
					return;
				}
				Debug.LogError(InteropFix.Cast("d0"));
				TriggerSilentDenial();
				return;
			}
			SessionDecryptKey = sessionKeyHex;
			LastUpdate = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			JsonElement rootElement = jsonDocument.RootElement;
			List<TabDefinition> list = new List<TabDefinition>();
			List<string> list2 = new List<string>();
			System.Collections.Generic.HashSet<string> hashSet = new System.Collections.Generic.HashSet<string>();
			byte[] array = null;
			byte[] array2 = null;
			int version = 0;
			if (rootElement.TryGetProperty("render_offset", out var value))
			{
				float num = 0f;
				float num2 = 0f;
				if (value.TryGetProperty("x", out var value2))
				{
					num = (float)value2.GetDouble();
				}
				if (value.TryGetProperty("y", out var value3))
				{
					num2 = (float)value3.GetDouble();
				}
				RenderOffset = new Vector2(num, num2);
			}
			else
			{
				RenderOffset = Vector2.zero;
			}
			rootElement.TryGetProperty("payload_signature", out var _);
			if (rootElement.TryGetProperty("premium_features", out var value5) && value5.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value5.EnumerateArray())
				{
					list2.Add(item.GetString() ?? "");
				}
				list2.Sort(StringComparer.Ordinal);
			}
			if (!rootElement.TryGetProperty("tabs", out var value6) || value6.ValueKind != JsonValueKind.Array)
			{
				return;
			}
			foreach (JsonElement item2 in value6.EnumerateArray())
			{
				JsonElement value7;
				JsonElement value8;
				JsonElement value9;
				JsonElement value10;
				JsonElement value11;
				TabDefinition tabDefinition = new TabDefinition
				{
					Id = (item2.TryGetProperty("id", out value7) ? (value7.GetString() ?? "") : ""),
					Name = (item2.TryGetProperty("name", out value8) ? (value8.GetString() ?? "") : ""),
					Icon = (item2.TryGetProperty("icon", out value9) ? (value9.GetString() ?? "") : ""),
					Context = (item2.TryGetProperty("context", out value10) ? (value10.GetString() ?? "both") : "both"),
					Enabled = (item2.TryGetProperty("enabled", out value11) && value11.GetBoolean())
				};
				if (item2.TryGetProperty("sections", out var value12) && value12.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item3 in value12.EnumerateArray())
					{
						JsonElement value13;
						JsonElement value14;
						JsonElement value15;
						JsonElement value16;
						SectionDefinition sectionDefinition = new SectionDefinition
						{
							Id = (item3.TryGetProperty("id", out value13) ? (value13.GetString() ?? "") : ""),
							Name = (item3.TryGetProperty("name", out value14) ? (value14.GetString() ?? "") : ""),
							Visible = (!item3.TryGetProperty("visible", out value15) || value15.GetBoolean()),
							VisibleWhen = (item3.TryGetProperty("visible_when", out value16) ? (value16.GetString() ?? "") : "")
						};
						if (item3.TryGetProperty("buttons", out var value17) && value17.ValueKind == JsonValueKind.Array)
						{
							foreach (JsonElement item4 in value17.EnumerateArray())
							{
								sectionDefinition.Buttons.Add(new ButtonDefinition
								{
									Id = (item4.TryGetProperty("id", out var value18) ? (value18.GetString() ?? "") : ""),
									Label = (item4.TryGetProperty("label", out var value19) ? (value19.GetString() ?? "") : ""),
									Type = (item4.TryGetProperty("type", out var value20) ? (value20.GetString() ?? "") : ""),
									Enabled = (!item4.TryGetProperty("enabled", out var value21) || value21.GetBoolean())
								});
							}
						}
						if (item3.TryGetProperty("sliders", out var value22) && value22.ValueKind == JsonValueKind.Array)
						{
							foreach (JsonElement item5 in value22.EnumerateArray())
							{
								JsonElement value23;
								JsonElement value24;
								JsonElement value25;
								JsonElement value26;
								JsonElement value27;
								JsonElement value28;
								SliderDefinition sliderDefinition = new SliderDefinition
								{
									Id = (item5.TryGetProperty("id", out value23) ? (value23.GetString() ?? "") : ""),
									Label = (item5.TryGetProperty("label", out value24) ? (value24.GetString() ?? "") : ""),
									Min = (item5.TryGetProperty("min", out value25) ? ((float)value25.GetDouble()) : 0f),
									Max = (item5.TryGetProperty("max", out value26) ? ((float)value26.GetDouble()) : 1f),
									Default = (item5.TryGetProperty("default", out value27) ? ((float)value27.GetDouble()) : 0f),
									Step = (item5.TryGetProperty("step", out value28) ? ((float)value28.GetDouble()) : 0.1f)
								};
								sectionDefinition.Sliders.Add(sliderDefinition);
								if (!_sliderValues.ContainsKey(sliderDefinition.Id))
								{
									_sliderValues[sliderDefinition.Id] = sliderDefinition.Default;
								}
							}
						}
						tabDefinition.Sections.Add(sectionDefinition);
					}
				}
				if (item2.TryGetProperty("locations", out var value29) && value29.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item6 in value29.EnumerateArray())
					{
						tabDefinition.Locations.Add(new TeleportLocation
						{
							Id = (item6.TryGetProperty("id", out var value30) ? (value30.GetString() ?? "") : ""),
							Name = (item6.TryGetProperty("name", out var value31) ? (value31.GetString() ?? "") : ""),
							X = (item6.TryGetProperty("x", out var value32) ? ((float)value32.GetDouble()) : 0f),
							Y = (item6.TryGetProperty("y", out var value33) ? ((float)value33.GetDouble()) : 0f)
						});
					}
				}
				if (item2.TryGetProperty("buttons", out var value34) && value34.ValueKind == JsonValueKind.Array)
				{
					SectionDefinition sectionDefinition2 = new SectionDefinition
					{
						Id = "default",
						Name = "",
						Visible = true
					};
					foreach (JsonElement item7 in value34.EnumerateArray())
					{
						sectionDefinition2.Buttons.Add(new ButtonDefinition
						{
							Id = (item7.TryGetProperty("id", out var value35) ? (value35.GetString() ?? "") : ""),
							Label = (item7.TryGetProperty("label", out var value36) ? (value36.GetString() ?? "") : ""),
							Type = (item7.TryGetProperty("type", out var value37) ? (value37.GetString() ?? "") : ""),
							Enabled = (!item7.TryGetProperty("enabled", out var value38) || value38.GetBoolean())
						});
					}
					if (sectionDefinition2.Buttons.Count > 0)
					{
						tabDefinition.Sections.Add(sectionDefinition2);
					}
				}
				if (item2.TryGetProperty("sliders", out var value39) && value39.ValueKind == JsonValueKind.Array)
				{
					SectionDefinition sectionDefinition3 = ((tabDefinition.Sections.Count > 0) ? tabDefinition.Sections[tabDefinition.Sections.Count - 1] : new SectionDefinition
					{
						Id = "default",
						Visible = true
					});
					foreach (JsonElement item8 in value39.EnumerateArray())
					{
						JsonElement value40;
						JsonElement value41;
						JsonElement value42;
						JsonElement value43;
						JsonElement value44;
						JsonElement value45;
						SliderDefinition sliderDefinition2 = new SliderDefinition
						{
							Id = (item8.TryGetProperty("id", out value40) ? (value40.GetString() ?? "") : ""),
							Label = (item8.TryGetProperty("label", out value41) ? (value41.GetString() ?? "") : ""),
							Min = (item8.TryGetProperty("min", out value42) ? ((float)value42.GetDouble()) : 0f),
							Max = (item8.TryGetProperty("max", out value43) ? ((float)value43.GetDouble()) : 1f),
							Default = (item8.TryGetProperty("default", out value44) ? ((float)value44.GetDouble()) : 0f),
							Step = (item8.TryGetProperty("step", out value45) ? ((float)value45.GetDouble()) : 0.1f)
						};
						sectionDefinition3.Sliders.Add(sliderDefinition2);
						if (!_sliderValues.ContainsKey(sliderDefinition2.Id))
						{
							_sliderValues[sliderDefinition2.Id] = sliderDefinition2.Default;
						}
					}
					if (!tabDefinition.Sections.Contains(sectionDefinition3))
					{
						tabDefinition.Sections.Add(sectionDefinition3);
					}
				}
				if (tabDefinition.Sections != null)
				{
					foreach (SectionDefinition section in tabDefinition.Sections)
					{
						if (section.Buttons != null)
						{
							foreach (ButtonDefinition button in section.Buttons)
							{
								if (!string.IsNullOrEmpty(button.Id))
								{
									hashSet.Add(button.Id);
								}
							}
						}
						if (section.Sliders == null)
						{
							continue;
						}
						foreach (SliderDefinition slider in section.Sliders)
						{
							if (!string.IsNullOrEmpty(slider.Id))
							{
								hashSet.Add(slider.Id);
							}
						}
					}
				}
				list.Add(tabDefinition);
			}
			if (rootElement.TryGetProperty("bytecode_version", out var value46))
			{
				version = value46.GetInt32();
			}
			if (rootElement.TryGetProperty("lobby_bytecode", out var value47))
			{
				string text2 = value47.GetString();
				if (!string.IsNullOrEmpty(text2))
				{
					try
					{
						array = Convert.FromBase64String(text2);
					}
					catch
					{
						array = null;
					}
				}
			}
			if (rootElement.TryGetProperty("game_bytecode", out var value48))
			{
				string text3 = value48.GetString();
				if (!string.IsNullOrEmpty(text3))
				{
					try
					{
						array2 = Convert.FromBase64String(text3);
					}
					catch
					{
						array2 = null;
					}
				}
			}
			byte[] array3 = null;
			if (rootElement.TryGetProperty("playerpick_bytecode", out var value49))
			{
				string text4 = value49.GetString();
				if (!string.IsNullOrEmpty(text4))
				{
					try
					{
						array3 = Convert.FromBase64String(text4);
					}
					catch
					{
						array3 = null;
					}
				}
			}
			byte[] array4 = null;
			if (rootElement.TryGetProperty("banmenu_bytecode", out var value50))
			{
				string text5 = value50.GetString();
				if (!string.IsNullOrEmpty(text5))
				{
					try
					{
						array4 = Convert.FromBase64String(text5);
					}
					catch
					{
						array4 = null;
					}
				}
			}
			long num3 = 0L;
			num3 = ExtractV5Token(array);
			if (num3 == 0L)
			{
				num3 = ExtractV5Token(array2);
			}
			if (num3 == 0L && newSessionToken > 0)
			{
				num3 = newSessionToken;
				if (array == null && array2 == null)
				{
					Debug.LogWarning(InteropFix.Cast("d1"));
				}
			}
			if (num3 == 0L)
			{
				num3 = ModKeyValidator.CurrentSessionToken;
				Debug.LogWarning(InteropFix.Cast("d2"));
			}
			byte[] array5 = null;
			if (rootElement.TryGetProperty("cheats_bytecode", out var value51))
			{
				string text6 = value51.GetString();
				if (!string.IsNullOrEmpty(text6))
				{
					try
					{
						array5 = Convert.FromBase64String(text6);
					}
					catch
					{
						array5 = null;
					}
				}
			}
			byte[] array6 = null;
			if (rootElement.TryGetProperty("spoofing_bytecode", out var value52))
			{
				string text7 = value52.GetString();
				if (!string.IsNullOrEmpty(text7))
				{
					try
					{
						array6 = Convert.FromBase64String(text7);
					}
					catch
					{
						array6 = null;
					}
				}
			}
			UISnapshot currentSnapshot = CurrentSnapshot;
			long num4 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (isHeartbeat && currentSnapshot != null)
			{
				byte[] array7 = currentSnapshot.GameBytecode ?? currentSnapshot.LobbyBytecode;
				byte[] array8 = array2 ?? array;
				if (array7 != null && array7.Length >= 536 && array8 != null && array8.Length >= 536 && array7[0] == 80 && array7[1] == 79 && array7[2] == 76 && array7[3] == 53 && array8[0] == 80 && array8[1] == 79 && array8[2] == 76 && array8[3] == 53)
				{
					long num5 = BitConverter.ToInt64(array7, 528);
					long num6 = BitConverter.ToInt64(array8, 528);
					if (num6 > 0 && num5 > 0 && num6 < num5)
					{
						return;
					}
				}
			}
			long serverNowMs;
			if (_lastRealtimeUpdateMs > 0 && num4 - _lastRealtimeUpdateMs < 5000 && currentSnapshot != null)
			{
				serverNowMs = num4 + ModKeyValidator.ServerTimeOffsetMs;
				if (array3 != null && currentSnapshot.PlayerPickBytecode != null && IsBytecodesFresh(currentSnapshot.PlayerPickBytecode))
				{
					array3 = currentSnapshot.PlayerPickBytecode;
				}
				if (array4 != null && currentSnapshot.BanMenuBytecode != null && IsBytecodesFresh(currentSnapshot.BanMenuBytecode))
				{
					array4 = currentSnapshot.BanMenuBytecode;
				}
				if (array5 != null && currentSnapshot.CheatsBytecode != null && IsBytecodesFresh(currentSnapshot.CheatsBytecode))
				{
					array5 = currentSnapshot.CheatsBytecode;
				}
				if (array6 != null && currentSnapshot.SpoofingBytecode != null && IsBytecodesFresh(currentSnapshot.SpoofingBytecode))
				{
					array6 = currentSnapshot.SpoofingBytecode;
				}
			}
			UISnapshot currentSnapshot2 = new UISnapshot(array, array2, array3, array4, array5, array6, num3, version);
			_tabsInternal = list;
			_premiumFeaturesInternal = list2;
			_validControlIds = hashSet;
			CurrentSnapshot = currentSnapshot2;
			if (newSessionToken > 0)
			{
				ModKeyValidator.SetSessionTokenInternal(newSessionToken);
				GhostUI.InvalidateHMACCache();
			}
			_integrityHashXor = CalculateIntegrity() ^ 0x4B7E2A9F1D3C6058L;
			SetLoaded(loaded: true);
			bool IsBytecodesFresh(byte[] bc)
			{
				if (bc != null && bc.Length >= 536 && bc[0] == 80 && bc[1] == 79 && bc[2] == 76 && bc[3] == 53)
				{
					long num7 = BitConverter.ToInt64(bc, 528);
					if (num7 > 0)
					{
						return serverNowMs - num7 < 300000;
					}
					return false;
				}
				return false;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast("[ServerData] ParseFromEncryptedPayload failed: " + ex.Message));
		}
		static long ExtractV5Token(byte[] bytes)
		{
			if (bytes != null && bytes.Length >= 268 && bytes[0] == 80 && bytes[1] == 79 && bytes[2] == 76 && bytes[3] == 53)
			{
				return BitConverter.ToInt64(bytes, 260);
			}
			return 0L;
		}
	}

	internal static void UpdateFromRealtime(byte[] lobbyBytecode, byte[] gameBytecode, byte[] playerPickBytecode, byte[] banMenuBytecode, byte[] cheatsBytecode, byte[] spoofingBytecode, long sessionToken)
	{
		try
		{
			if (lobbyBytecode != null || gameBytecode != null || playerPickBytecode != null || banMenuBytecode != null || cheatsBytecode != null || spoofingBytecode != null)
			{
				UISnapshot currentSnapshot = CurrentSnapshot;
				byte[] obj = lobbyBytecode ?? currentSnapshot?.LobbyBytecode;
				byte[] array = gameBytecode ?? currentSnapshot?.GameBytecode;
				byte[] playerPick = playerPickBytecode ?? currentSnapshot?.PlayerPickBytecode;
				byte[] banMenu = banMenuBytecode ?? currentSnapshot?.BanMenuBytecode;
				byte[] cheats = cheatsBytecode ?? currentSnapshot?.CheatsBytecode;
				byte[] spoofing = spoofingBytecode ?? currentSnapshot?.SpoofingBytecode;
				long num = 0L;
				if (lobbyBytecode != null)
				{
					num = ExtractV5Token(lobbyBytecode);
				}
				if (num == 0L && gameBytecode != null)
				{
					num = ExtractV5Token(gameBytecode);
				}
				long num2 = ExtractV5Token(obj);
				if (num2 == 0L)
				{
					num2 = ExtractV5Token(array);
				}
				long token = ((num > 0) ? ((sessionToken > 0) ? sessionToken : num) : ((num2 <= 0) ? ((sessionToken > 0) ? sessionToken : (currentSnapshot?.SessionToken ?? 0)) : num2));
				int version = currentSnapshot?.BytecodeVersion ?? 5;
				CurrentSnapshot = new UISnapshot(obj, array, playerPick, banMenu, cheats, spoofing, token, version);
				_lastRealtimeUpdateMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (sessionToken > 0)
				{
					ModKeyValidator.SetSessionTokenInternal(sessionToken);
					GhostUI.InvalidateHMACCache();
				}
				_integrityHashXor = CalculateIntegrity() ^ 0x4B7E2A9F1D3C6058L;
				if (!IsLoaded)
				{
					SetLoaded(loaded: true);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast(ex.Message));
		}
		static long ExtractV5Token(byte[] bc)
		{
			if (bc != null && bc.Length >= 268 && bc[0] == 80 && bc[1] == 79 && bc[2] == 76 && bc[3] == 53)
			{
				return BitConverter.ToInt64(bc, 260);
			}
			return 0L;
		}
	}

	internal static void RegisterControlId(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			_validControlIds.Add(id);
		}
	}

	internal static bool GetToggleState(string id)
	{
		bool value;
		return _toggleStates.TryGetValue(id, out value) && value;
	}

	internal static void SetToggleState(string id, bool value)
	{
		if (!_validControlIds.Contains(id))
		{
			if (IsLoaded)
			{
				TriggerSilentDenial();
			}
		}
		else
		{
			_toggleStates[id] = value;
		}
	}

	internal static void ToggleState(string id)
	{
		if (!_validControlIds.Contains(id))
		{
			if (IsLoaded)
			{
				TriggerSilentDenial();
			}
		}
		else
		{
			_toggleStates[id] = !GetToggleState(id);
		}
	}

	internal static float GetSliderValue(string id)
	{
		if (!_sliderValues.TryGetValue(id, out var value))
		{
			return 0f;
		}
		return value;
	}

	internal static void SetSliderValue(string id, float value)
	{
		if (!_validControlIds.Contains(id))
		{
			if (IsLoaded)
			{
				TriggerSilentDenial();
			}
		}
		else
		{
			_sliderValues[id] = value;
		}
	}

	internal static void SetSliderValueInternal(string id, float value)
	{
		_sliderValues[id] = value;
	}

	internal static void Clear()
	{
		SetLoaded(loaded: false);
		_tabsInternal.Clear();
		_premiumFeaturesInternal.Clear();
		_validControlIds.Clear();
		_toggleStates.Clear();
		_sliderValues.Clear();
		SessionDecryptKey = null;
		LastUpdate = 0L;
		_integrityHashXor = 5439832262788800600L;
		CurrentSnapshot = null;
	}

	private static long GetStableHash(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return 0L;
		}
		long num = 23L;
		foreach (char c in str)
		{
			num = num * 31 + c;
		}
		return num;
	}

	internal static long GetStoredIntegrityHash()
	{
		return _integrityHashXor ^ 0x4B7E2A9F1D3C6058L;
	}

	internal static long CalculateIntegrity()
	{
		long num = 3735928559L;
		if (PremiumFeatures != null)
		{
			foreach (string premiumFeature in PremiumFeatures)
			{
				num = (num * 397) ^ GetStableHash(premiumFeature);
			}
		}
		return num;
	}

	internal static void TriggerSilentDenial()
	{
		if (CheatConfig.OfflineLocalMode?.Value ?? false)
		{
			return;
		}
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		long num2 = System.Threading.Interlocked.Read(ref _lastDenialTimestamp);
		if (num - num2 < 2000)
		{
			return;
		}
		System.Threading.Interlocked.Exchange(ref _lastDenialTimestamp, num);
		if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
		{
			SessionDecryptKey = "CORRUPTED_SESSION_" + Guid.NewGuid();
			_isLoadedXor = 0;
			CurrentSnapshot = null;
			_integrityHashXor = 5439832262788800600L;
			ActionPermitSystem.EnqueueMainThread(delegate
			{
				TriggerSilentDenial();
			});
			return;
		}
		try
		{
			Debug.LogError(InteropFix.Cast($"[DEBUG-DENIAL] TriggerSilentDenial called! Stack:\n{new StackTrace()}"));
		}
		catch
		{
		}
		try
		{
			ModKeyValidator.ResetValidationState();
		}
		catch
		{
		}
		try
		{
			ActionPermitSystem.ClearRegistry();
		}
		catch
		{
		}
		try
		{
			RealtimeConnection.Disconnect();
		}
		catch
		{
		}
		SessionDecryptKey = "CORRUPTED_SESSION_" + Guid.NewGuid();
		_isLoadedXor = 0;
		_tabsInternal?.Clear();
		CurrentSnapshot = null;
		_integrityHashXor = 5439832262788800600L;
	}

	internal static void UpdateActionIdMap(JsonElement mapElement)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(_actionIdReverseMap);
		foreach (JsonProperty item in mapElement.EnumerateObject())
		{
			if (item.Value.ValueKind == JsonValueKind.String)
			{
				dictionary[item.Name] = item.Value.GetString();
			}
		}
		_actionIdReverseMap = dictionary;
	}

	internal static string DeobfuscateActionId(string obfuscatedId)
	{
		if (string.IsNullOrEmpty(obfuscatedId))
		{
			return obfuscatedId;
		}
		if (!_actionIdReverseMap.TryGetValue(obfuscatedId, out var value))
		{
			return obfuscatedId;
		}
		return value;
	}

	internal static void ScheduleDelayedDenial(float delaySeconds)
	{
		float num = Time.realtimeSinceStartup + delaySeconds;
		if (_delayedDenialTime < 0f || num < _delayedDenialTime)
		{
			_delayedDenialTime = num;
		}
	}

	internal static void CheckDelayedDenial()
	{
		if (CheatConfig.OfflineLocalMode?.Value ?? false)
		{
			_delayedDenialTime = -1f;
			return;
		}
		if (_delayedDenialTime > 0f && Time.realtimeSinceStartup >= _delayedDenialTime)
		{
			_delayedDenialTime = -1f;
			TriggerSilentDenial();
		}
	}

	internal static void CancelDelayedDenial()
	{
		_delayedDenialTime = -1f;
	}

	internal static bool IsTabEnabled(string tabId)
	{
		if (CheatConfig.OfflineLocalMode?.Value ?? false)
		{
			return true;
		}
		if (!IsLoaded || Tabs == null || Tabs.Count == 0)
		{
			return false;
		}
		foreach (TabDefinition tab in Tabs)
		{
			if (string.Equals(tab.Id, tabId, System.StringComparison.OrdinalIgnoreCase) && tab.Enabled)
			{
				return true;
			}
		}
		return false;
	}
}





