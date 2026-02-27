using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Patches;

[HarmonyPatch]
public static class LobbyListingPatch
{
	public class CapturedLobby
	{
		public int GameId { get; set; }

		public string GameCode { get; set; }

		public string HostName { get; set; }

		public string TrueHostName { get; set; }

		public string IPAddress { get; set; }

		public ushort Port { get; set; }

		public byte PlayerCount { get; set; }

		public byte MaxPlayers { get; set; }

		public byte MapId { get; set; }

		public string MapName { get; set; }

		public Platforms Platform { get; set; }

		public string PlatformName { get; set; }

		public uint Language { get; set; }

		public int Age { get; set; }

		public QuickChatModes QuickChat { get; set; }

		public System.DateTime CapturedAt { get; set; }

		public IGameOptions Options { get; set; }

		public int NumImpostors { get; set; }

		public float KillCooldown { get; set; }

		public float PlayerSpeed { get; set; }

		public GameModes GameMode { get; set; }

		public override string ToString()
		{
			string value = ((NumImpostors > 0) ? $" | {NumImpostors}imp" : "");
			return $"[{GameCode}] {HostName} ({PlayerCount}/{MaxPlayers}) {MapName} | {PlatformName}{value}";
		}
	}

	public enum LobbySortMode
	{
		Default,
		PlayerCountDesc,
		PlayerCountAsc,
		AvailableSlots,
		Newest,
		MapName
	}

	public static class Maps
	{
		public const byte Skeld = 0;

		public const byte MiraHQ = 1;

		public const byte Polus = 2;

		public const byte Dleks = 3;

		public const byte Airship = 4;

		public const byte Fungle = 5;

		public static readonly (byte Id, string Name)[] AllMaps = new(byte, string)[5]
		{
			(0, "Skeld"),
			(1, "Mira"),
			(2, "Polus"),
			(4, "Airship"),
			(5, "Fungle")
		};
	}

	private static readonly Dictionary<int, int> _lobbyIndex = new Dictionary<int, int>();

	private static readonly object _lock = new object();

	public const int MAX_DISPLAY_LOBBIES = 30;

	private static bool _isJoining = false;

	private static float _lastJoinTime = 0f;

	private const float JOIN_COOLDOWN = 3f;

	public static List<CapturedLobby> CapturedLobbies { get; private set; } = new List<CapturedLobby>();

	public static System.DateTime LastUpdateTime { get; private set; } = System.DateTime.MinValue;

	public static int TotalGamesInMatchmaker { get; private set; } = 0;

	public static int MatchingGamesCount { get; private set; } = 0;

	public static bool CaptureEnabled { get; set; } = true;

	public static LobbySortMode CurrentSortMode { get; set; } = LobbySortMode.Default;

	public static byte? FilterMapId { get; set; } = null;

	public static Platforms? FilterPlatform { get; set; } = null;

	public static bool FilterFreeChatOnly { get; set; } = false;

	public static bool FilterHasSpace { get; set; } = false;

	public static int? FilterNumImpostors { get; set; } = null;

	public static event System.Action<List<CapturedLobby>> OnLobbiesCaptured;

/*
	[HarmonyPatch(typeof(FindAGameManager), "HandleList")]
	[HarmonyPrefix]
	public static void HandleListPrefix(TotalGameData totalGames, FindGamesListFilteredResponse response)
	{
		if (!CaptureEnabled)
		{
			return;
		}
		try
		{
			if (((response != null) ? response.Metadata : null) != null)
			{
				TotalGamesInMatchmaker = response.Metadata.AllGamesCount;
				MatchingGamesCount = response.Metadata.MatchingGamesCount;
			}
			LastUpdateTime = System.DateTime.Now;
			int num = 0;
			if (((response != null) ? response.Games : null) != null)
			{
				lock (_lock)
				{
					var enumerator = response.Games.GetEnumerator();
					while (enumerator.MoveNext())
					{
						GameListing current = enumerator.Current;
						try
						{
							if (_lobbyIndex.TryGetValue(current.GameId, out var value) && value < CapturedLobbies.Count)
							{
								CapturedLobby capturedLobby = ConvertToCapturedLobby(current);
								if (capturedLobby != null)
								{
									CapturedLobbies[value] = capturedLobby;
								}
								continue;
							}
							CapturedLobby capturedLobby2 = ConvertToCapturedLobby(current);
							if (capturedLobby2 != null)
							{
								_lobbyIndex[current.GameId] = CapturedLobbies.Count;
								CapturedLobbies.Add(capturedLobby2);
								num++;
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning(InteropFix.Cast("[LobbyListing] Error capturing lobby: " + ex.Message));
						}
					}
					if (CapturedLobbies.RemoveAll((CapturedLobby l) => (System.DateTime.Now - l.CapturedAt).TotalMinutes > 5.0) > 0)
					{
						RebuildLobbyIndex();
					}
				}
			}
			Debug.Log(InteropFix.Cast($"[LobbyListing] Added {num} new lobbies. Total captured: {CapturedLobbies.Count}. In matchmaker: {TotalGamesInMatchmaker}, Matching: {MatchingGamesCount}"));
			List<CapturedLobby> obj;
			lock (_lock)
			{
				obj = new List<CapturedLobby>(CapturedLobbies);
			}
			LobbyListingPatch.OnLobbiesCaptured?.Invoke(obj);
		}
		catch (Exception value2)
		{
			Debug.LogError(InteropFix.Cast($"[LobbyListing] HandleListPrefix error: {value2}"));
		}
	}
*/

	[HarmonyPatch(typeof(MatchMakerGameButton), "SetGame")]
	[HarmonyPostfix]
	public static void SetGamePostfix(MatchMakerGameButton __instance, GameListing gameListing)
	{
		if (!CaptureEnabled)
		{
			return;
		}
		try
		{
			lock (_lock)
			{
				if (!_lobbyIndex.ContainsKey(gameListing.GameId))
				{
					CapturedLobby capturedLobby = ConvertToCapturedLobby(gameListing);
					if (capturedLobby != null)
					{
						_lobbyIndex[gameListing.GameId] = CapturedLobbies.Count;
						CapturedLobbies.Add(capturedLobby);
						Debug.Log(InteropFix.Cast($"[LobbyListing] Additional lobby captured: {capturedLobby}"));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] SetGamePostfix error: " + ex.Message));
		}
	}

	[HarmonyPatch(typeof(GameContainer), "SetGameListing")]
	[HarmonyPostfix]
	public static void SetGameListingPostfix(GameContainer __instance, GameListing gameL)
	{
		if (!CaptureEnabled)
		{
			return;
		}
		try
		{
			lock (_lock)
			{
				if (!_lobbyIndex.ContainsKey(gameL.GameId))
				{
					CapturedLobby capturedLobby = ConvertToCapturedLobby(gameL);
					if (capturedLobby != null)
					{
						_lobbyIndex[gameL.GameId] = CapturedLobbies.Count;
						CapturedLobbies.Add(capturedLobby);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] SetGameListingPostfix error: " + ex.Message));
		}
	}

	private static CapturedLobby ConvertToCapturedLobby(GameListing listing)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		if (listing.GameId == 0)
		{
			return null;
		}
		CapturedLobby capturedLobby = new CapturedLobby
		{
			GameId = listing.GameId,
			GameCode = GameCode.IntToGameName(listing.GameId),
			HostName = (listing.HostName ?? "Unknown"),
			TrueHostName = (listing.TrueHostName ?? listing.HostName ?? "Unknown"),
			IPAddress = (listing.IPString ?? "N/A"),
			Port = listing.Port,
			PlayerCount = listing.PlayerCount,
			MaxPlayers = (byte)listing.MaxPlayers,
			MapId = listing.MapId,
			MapName = GetMapName(listing.MapId),
			Platform = listing.Platform,
			PlatformName = FormatPlatform(listing.Platform),
			Language = listing.Language,
			Age = listing.Age,
			QuickChat = listing.QuickChat,
			CapturedAt = System.DateTime.Now
		};
		if (listing.Options != null)
		{
			try
			{
				capturedLobby.Options = listing.Options;
				capturedLobby.NumImpostors = listing.Options.NumImpostors;
				capturedLobby.GameMode = listing.Options.GameMode;
				capturedLobby.KillCooldown = listing.Options.GetFloat((FloatOptionNames)1);
				capturedLobby.PlayerSpeed = listing.Options.GetFloat((FloatOptionNames)2);
			}
			catch
			{
			}
		}
		return capturedLobby;
	}

	private static string GetMapName(byte mapId)
	{
		return mapId switch
		{
			0 => "The Skeld", 
			1 => "Mira HQ", 
			2 => "Polus", 
			3 => "Dleks", 
			4 => "Airship", 
			5 => "The Fungle", 
			_ => $"Map {mapId}", 
		};
	}

	private static string FormatPlatform(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		return ((int)platform - 1) switch
		{
			1 => "Steam", 
			0 => "Epic", 
			3 => "MS Store", 
			5 => "iOS", 
			6 => "Android", 
			7 => "Switch", 
			8 => "Xbox", 
			9 => "PlayStation", 
			_ => "PC", 
		};
	}

	public static List<CapturedLobby> GetFilteredAndSortedLobbies()
	{
		lock (_lock)
		{
			IEnumerable<CapturedLobby> enumerable = CapturedLobbies;
			if (FilterMapId.HasValue)
			{
				enumerable = enumerable.Where((CapturedLobby l) => l.MapId == FilterMapId.Value);
			}
			if (FilterPlatform.HasValue)
			{
				enumerable = enumerable.Where((CapturedLobby l) => l.Platform == FilterPlatform.Value);
			}
			if (FilterFreeChatOnly)
			{
				enumerable = enumerable.Where((CapturedLobby l) => (int)l.QuickChat == 1);
			}
			if (FilterHasSpace)
			{
				enumerable = enumerable.Where((CapturedLobby l) => l.PlayerCount < l.MaxPlayers);
			}
			if (FilterNumImpostors.HasValue)
			{
				enumerable = enumerable.Where((CapturedLobby l) => l.NumImpostors == FilterNumImpostors.Value);
			}
			return (CurrentSortMode switch
			{
				LobbySortMode.PlayerCountDesc => enumerable.OrderByDescending((CapturedLobby l) => l.PlayerCount), 
				LobbySortMode.PlayerCountAsc => enumerable.OrderBy((CapturedLobby l) => l.PlayerCount), 
				LobbySortMode.AvailableSlots => enumerable.OrderByDescending((CapturedLobby l) => l.MaxPlayers - l.PlayerCount), 
				LobbySortMode.Newest => enumerable.OrderBy((CapturedLobby l) => l.Age), 
				LobbySortMode.MapName => enumerable.OrderBy((CapturedLobby l) => l.MapName), 
				_ => enumerable, 
			}).Take(30).ToList();
		}
	}

	public static List<CapturedLobby> GetLobbiesByMap(byte mapId)
	{
		lock (_lock)
		{
			return CapturedLobbies.Where((CapturedLobby l) => l.MapId == mapId).ToList();
		}
	}

	public static List<CapturedLobby> GetAvailableLobbies()
	{
		lock (_lock)
		{
			return CapturedLobbies.Where((CapturedLobby l) => l.PlayerCount < l.MaxPlayers).ToList();
		}
	}

	public static List<CapturedLobby> GetLobbiesByPlatform(Platforms platform)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		lock (_lock)
		{
			return CapturedLobbies.Where((CapturedLobby l) => l.Platform == platform).ToList();
		}
	}

	public static List<CapturedLobby> GetLobbiesOrderedByPlayers(bool descending = true)
	{
		lock (_lock)
		{
			return descending ? CapturedLobbies.OrderByDescending((CapturedLobby l) => l.PlayerCount).ToList() : CapturedLobbies.OrderBy((CapturedLobby l) => l.PlayerCount).ToList();
		}
	}

	public static void ClearFilters()
	{
		FilterMapId = null;
		FilterPlatform = null;
		FilterFreeChatOnly = false;
		FilterHasSpace = false;
		FilterNumImpostors = null;
		CurrentSortMode = LobbySortMode.Default;
	}

	public static void ClearCapturedLobbies()
	{
		lock (_lock)
		{
			CapturedLobbies.Clear();
			_lobbyIndex.Clear();
			LastUpdateTime = System.DateTime.MinValue;
			TotalGamesInMatchmaker = 0;
			MatchingGamesCount = 0;
		}
	}

	private static void RebuildLobbyIndex()
	{
		_lobbyIndex.Clear();
		for (int i = 0; i < CapturedLobbies.Count; i++)
		{
			_lobbyIndex[CapturedLobbies[i].GameId] = i;
		}
	}

	public static void RefreshLobbyList()
	{
		try
		{
			FindAGameManager val = Object.FindObjectOfType<FindAGameManager>();
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				val.RefreshList();
				Debug.Log(InteropFix.Cast("[LobbyListing] Refresh requested."));
			}
			else
			{
				Debug.LogWarning(InteropFix.Cast("[LobbyListing] FindAGameManager not found. Are you in the Find Game menu?"));
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[LobbyListing] RefreshLobbyList error: {value}"));
		}
	}

	public static void JoinByCode(string gameCode)
	{
		if (string.IsNullOrEmpty(gameCode) || (UnityEngine.Object)(object)AmongUsClient.Instance == (UnityEngine.Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] Cannot join: Invalid code or client."));
			return;
		}
		if (((InnerNetClient)AmongUsClient.Instance).AmConnected || _isJoining || Time.time - _lastJoinTime < 3f)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] Cannot join: Already connected or join in progress."));
			return;
		}
		try
		{
			gameCode = gameCode.Trim().ToUpper();
			if (gameCode.Length != 6 && gameCode.Length != 4)
			{
				Debug.LogWarning(InteropFix.Cast($"[LobbyListing] Invalid game code length: {gameCode.Length}. Expected 4 or 6."));
				return;
			}
			int num = GameCode.GameNameToInt(gameCode);
			if (num == -1)
			{
				Debug.LogWarning(InteropFix.Cast("[LobbyListing] Invalid game code: " + gameCode));
				return;
			}
			Debug.Log(InteropFix.Cast($"[LobbyListing] Joining by code: {gameCode} (ID: {num})"));
			_isJoining = true;
			_lastJoinTime = Time.time;
			((MonoBehaviour)AmongUsClient.Instance).StartCoroutine(AmongUsClient.Instance.CoJoinOnlineGameFromCode(num, true));
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[LobbyListing] JoinByCode error: {value}"));
		}
	}

	public static void JoinCapturedLobby(CapturedLobby lobby)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		if (lobby == null || (UnityEngine.Object)(object)AmongUsClient.Instance == (UnityEngine.Object)null)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] Cannot join: Invalid lobby or client."));
			return;
		}
		if (((InnerNetClient)AmongUsClient.Instance).AmConnected || _isJoining || Time.time - _lastJoinTime < 3f)
		{
			Debug.LogWarning(InteropFix.Cast("[LobbyListing] Cannot join: Already connected or join in progress."));
			return;
		}
		try
		{
			if ((int)lobby.QuickChat == 1)
			{
				if (DestroyableSingleton<EOSManager>.Instance.IsFreechatAllowed())
				{
					DataManager.Settings.Multiplayer.ChatMode = (QuickChatModes)1;
				}
			}
			else
			{
				DataManager.Settings.Multiplayer.ChatMode = (QuickChatModes)2;
			}
			if (!string.IsNullOrEmpty(lobby.IPAddress) && lobby.IPAddress != "N/A" && lobby.Port > 0)
			{
				Debug.Log(InteropFix.Cast($"[LobbyListing] Direct join lobby: {lobby.GameCode} @ {lobby.IPAddress}:{lobby.Port}"));
				_isJoining = true;
				_lastJoinTime = Time.time;
				((MonoBehaviour)AmongUsClient.Instance).StartCoroutine(AmongUsClient.Instance.CoJoinOnlinePublicGame(lobby.GameId, lobby.IPAddress, lobby.Port, (AmongUsClient.MainMenuTarget)3));
			}
			else
			{
				Debug.Log(InteropFix.Cast("[LobbyListing] Code-based join lobby: " + lobby.GameCode + " (no IP available)"));
				_isJoining = true;
				_lastJoinTime = Time.time;
				((MonoBehaviour)AmongUsClient.Instance).StartCoroutine(AmongUsClient.Instance.CoJoinOnlineGameFromCode(lobby.GameId, false));
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[LobbyListing] JoinCapturedLobby error: {value}"));
		}
	}

	public static void CopyCodeToClipboard(string gameCode)
	{
		try
		{
			GUIUtility.systemCopyBuffer = gameCode;
			Debug.Log(InteropFix.Cast("[LobbyListing] Copied to clipboard: " + gameCode));
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[LobbyListing] CopyCodeToClipboard error: {value}"));
		}
	}

	public static string GenerateLobbyReport()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== LOBBY LISTING REPORT ===");
		stringBuilder.AppendLine($"Captured: {CapturedLobbies.Count} lobbies");
		stringBuilder.AppendLine($"Total in Matchmaker: {TotalGamesInMatchmaker}");
		stringBuilder.AppendLine($"Matching Filter: {MatchingGamesCount}");
		stringBuilder.AppendLine($"Last Update: {LastUpdateTime:HH:mm:ss}");
		stringBuilder.AppendLine();
		foreach (CapturedLobby item in CapturedLobbies.Take(30))
		{
			stringBuilder.AppendLine($"[{item.GameCode}] {item.TrueHostName}");
			stringBuilder.AppendLine($"  Players: {item.PlayerCount}/{item.MaxPlayers} | Map: {item.MapName}");
			stringBuilder.AppendLine($"  Platform: {item.PlatformName} | IP: {item.IPAddress}:{item.Port}");
			if (item.Options != null)
			{
				stringBuilder.AppendLine($"  Impostors: {item.NumImpostors} | Kill CD: {item.KillCooldown}s | Speed: {item.PlayerSpeed}x");
			}
			stringBuilder.AppendLine();
		}
		if (CapturedLobbies.Count > 30)
		{
			stringBuilder.AppendLine($"... and {CapturedLobbies.Count - 30} more lobbies");
		}
		return stringBuilder.ToString();
	}

	[HarmonyPatch(typeof(AmongUsClient), "OnGameJoined")]
	[HarmonyPostfix]
	public static void OnGameJoinedResetLock()
	{
		_isJoining = false;
	}

	[HarmonyPatch(typeof(AmongUsClient), "OnDisconnected")]
	[HarmonyPostfix]
	public static void OnDisconnectedResetLock()
	{
		_isJoining = false;
	}
}









