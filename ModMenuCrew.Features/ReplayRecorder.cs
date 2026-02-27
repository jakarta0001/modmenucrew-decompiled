using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using ModMenuCrew.ReplaySystem;
using UnityEngine;

namespace ModMenuCrew.Features;

public class ReplayRecorder : MonoBehaviour
{
	public static ReplayRecorder Instance;

	private ReplayData currentReplay;

	private bool isRecording;

	private float recordingStartTime;

	private string replayDirectory;

	private System.Collections.Generic.HashSet<byte> deadPlayers = new System.Collections.Generic.HashSet<byte>();

	private bool wasInMeeting;

	internal static bool ReplayInProgress = false;

	private int currentGameId = -1;

	private bool hasRecordedThisGame;

	private InnerNet.InnerNetClient.GameStates lastGameState;

	internal static bool AutoRecordEnabled = true;

	private Dictionary<byte, AnimState> playerAnimStates = new Dictionary<byte, AnimState>();

	private float _lastRecordTime;

	private const float RECORD_INTERVAL = 0.05f;

	private bool showReplayMenu;

	private Vector2 replayListScroll = Vector2.zero;

	private string[] cachedReplayFiles = new string[0];

	private float lastFileRefresh;

	private static GUIStyle _replayCenterLabelStyle;

	private static GUIStyle _replayBoxStyle;

	private static GUIStyle _replayBtnStyle;

	private static Texture2D _replayBoxTex;

	private static Texture2D _replayBtnTex;

	private static Texture2D _replayBtnHoverTex;

	private static Texture2D _replayBtnActiveTex;

	public bool IsRecording => isRecording;

	public int FrameCount => (currentReplay?.Frames?.Count).GetValueOrDefault();

	public int EventCount => (currentReplay?.Events?.Count).GetValueOrDefault();

	public ReplayRecorder(System.IntPtr ptr)
		: base(ptr)
	{
	}//IL_001e: Unknown result type (might be due to invalid IL or missing references)
	//IL_0023: Unknown result type (might be due to invalid IL or missing references)


	private void Awake()
	{
		Instance = this;
		replayDirectory = Path.Combine(Paths.GameRootPath, "Replays");
		if (!Directory.Exists(replayDirectory))
		{
			Directory.CreateDirectory(replayDirectory);
		}
	}

	public void StartRecording()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected I4, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected I4, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		if (isRecording || !InteropFix.Cast((Object)(object)PlayerControl.LocalPlayer))
		{
			return;
		}
		if (ReplayInProgress)
		{
			((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayRecorder] Cannot start recording: Replay in progress");
			return;
		}
		currentReplay = new ReplayData();
		currentReplay.GameVersion = "2026.1.1";
		currentReplay.RecordedAt = System.DateTime.Now;
		if (InteropFix.Cast((Object)(object)ShipStatus.Instance))
		{
			currentReplay.MapId = (int)ShipStatus.Instance.Type;
			currentReplay.MapName = GetMapName((int)ShipStatus.Instance.Type);
		}
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null))
			{
				var defaultOutfit = current.Data.DefaultOutfit;
				ReplayPlayerInfo obj = new ReplayPlayerInfo
				{
					PlayerId = current.PlayerId,
					Name = current.Data.PlayerName,
					ColorId = defaultOutfit.ColorId,
					HatId = (defaultOutfit.HatId ?? ""),
					SkinId = (defaultOutfit.SkinId ?? ""),
					PetId = (defaultOutfit.PetId ?? "")
				};
				RoleBehaviour role = current.Data.Role;
				obj.IsImpostor = role != null && role.IsImpostor;
				obj.RealColor = GetRealColor(current);
				ReplayPlayerInfo item = obj;
				currentReplay.Players.Add(item);
			}
		}
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		AddEvent(ReplayEventType.GameStart, (byte)((localPlayer != null) ? localPlayer.PlayerId : 0), 0, Vector2.zero, "Game Started");
		deadPlayers.Clear();
		playerAnimStates.Clear();
		wasInMeeting = false;
		isRecording = true;
		recordingStartTime = Time.time;
		_lastRecordTime = -0.05f;
		((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayRecorder] Recording Started");
	}

	private void FixedUpdate()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Invalid comparison between Unknown and I4
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)AmongUsClient.Instance == (Object)null || ReplayInProgress)
		{
			return;
		}
		InnerNet.InnerNetClient.GameStates gameState = ((InnerNetClient)AmongUsClient.Instance).GameState;
		if (gameState != lastGameState)
		{
			if ((int)lastGameState == 2 && (int)gameState != 2)
			{
				if (isRecording)
				{
					StopRecording();
				}
				hasRecordedThisGame = false;
			}
			if ((int)gameState == 2)
			{
				int gameId = ((InnerNetClient)AmongUsClient.Instance).GameId;
				if (gameId != currentGameId)
				{
					currentGameId = gameId;
					hasRecordedThisGame = false;
					ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					bool flag = default(bool);
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(39, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayRecorder] New game detected: ID ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentGameId);
					}
					log.LogInfo(val);
				}
			}
			lastGameState = gameState;
		}
		if (!isRecording)
		{
			if ((int)gameState == 2 && !ReplayInProgress && !hasRecordedThisGame && AutoRecordEnabled)
			{
				StartRecording();
				hasRecordedThisGame = true;
			}
			return;
		}
		if ((int)gameState != 2)
		{
			StopRecording();
			return;
		}
		float num = Time.time - recordingStartTime;
		if (num - _lastRecordTime < 0.05f)
		{
			return;
		}
		_lastRecordTime = num;
		ReplayFrame replayFrame = new ReplayFrame
		{
			Time = num
		};
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null)
			{
				continue;
			}
			AnimState animState = AnimState.Idle;
			if (playerAnimStates.TryGetValue(current.PlayerId, out var value))
			{
				animState = value;
				if (animState == AnimState.VentEnter)
				{
					playerAnimStates[current.PlayerId] = AnimState.Idle;
				}
				if (animState == AnimState.VentExit)
				{
					playerAnimStates[current.PlayerId] = AnimState.Idle;
				}
			}
			else if (InteropFix.Cast((Object)(object)current.MyPhysics) && InteropFix.Cast((Object)(object)current.MyPhysics.body))
			{
				Vector2 velocity = current.MyPhysics.body.velocity;
				if (velocity.magnitude > 0.1f)
				{
					animState = AnimState.Run;
				}
			}
			if (current.Data.IsDead)
			{
				animState = AnimState.Ghost;
			}
			PlayerState playerState = new PlayerState
			{
				PlayerId = current.PlayerId,
				Position = InteropFix.Cast(((Component)current).transform.position),
				FaceRight = true,
				IsDead = current.Data.IsDead,
				IsInVent = current.inVent,
				AnimState = animState
			};
			try
			{
				CosmeticsLayer cosmetics = current.cosmetics;
				object obj;
				if (cosmetics == null)
				{
					obj = null;
				}
				else
				{
					PlayerBodySprite currentBodySprite = cosmetics.currentBodySprite;
					obj = ((currentBodySprite != null) ? currentBodySprite.BodySprite : null);
				}
				SpriteRenderer val2 = (SpriteRenderer)obj;
				if ((Object)(object)val2 != (Object)null)
				{
					playerState.FaceRight = !val2.flipX;
				}
				else
				{
					SpriteRenderer component = ((Component)current).GetComponent<SpriteRenderer>();
					if (InteropFix.Cast((Object)(object)component))
					{
						playerState.FaceRight = !component.flipX;
					}
				}
			}
			catch
			{
			}
			replayFrame.States.Add(playerState);
			if (current.Data.IsDead && !deadPlayers.Contains(current.PlayerId))
			{
				deadPlayers.Add(current.PlayerId);
				byte playerId = FindLikelyKiller(current);
				AddEvent(ReplayEventType.Kill, playerId, current.PlayerId, InteropFix.Cast(((Component)current).transform.position), GetPlayerName(playerId) + " killed " + current.Data.PlayerName);
			}
		}
		currentReplay.Frames.Add(replayFrame);
		bool flag2 = (Object)(object)MeetingHud.Instance != (Object)null;
		if (flag2 && !wasInMeeting)
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			AddEvent(ReplayEventType.Meeting, (byte)((localPlayer != null) ? localPlayer.PlayerId : 0), 0, Vector2.zero, "Meeting Called");
		}
		wasInMeeting = flag2;
	}

	private byte FindLikelyKiller(PlayerControl victim)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		byte result = byte.MaxValue;
		float num = float.MaxValue;
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null)
			{
				continue;
			}
			RoleBehaviour role = current.Data.Role;
			if (role != null && role.IsImpostor && !current.Data.IsDead)
			{
				float num2 = Vector2.Distance(InteropFix.Cast(((Component)current).transform.position), InteropFix.Cast(((Component)victim).transform.position));
				if (num2 < num)
				{
					num = num2;
					result = current.PlayerId;
				}
			}
		}
		return result;
	}

	private string GetPlayerName(byte playerId)
	{
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current != (Object)null && current.PlayerId == playerId && (Object)(object)current.Data != (Object)null)
			{
				return current.Data.PlayerName;
			}
		}
		return "Unknown";
	}

	private void AddEvent(ReplayEventType type, byte playerId, byte targetId, Vector2 position, string description)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (currentReplay != null)
		{
			float time = Time.time - recordingStartTime;
			currentReplay.Events.Add(new ReplayEvent
			{
				Time = time,
				Type = type,
				PlayerId = playerId,
				TargetId = targetId,
				Position = position,
				Description = description
			});
		}
	}

	private string GetMapName(int mapId)
	{
		return mapId switch
		{
			0 => "The Skeld", 
			1 => "MIRA HQ", 
			2 => "Polus", 
			3 => "Dleks", 
			4 => "Airship", 
			5 => "The Fungle", 
			_ => "Unknown", 
		};
	}

	private Color32 GetRealColor(PlayerControl p)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		int colorId = p.Data.DefaultOutfit.ColorId;
		if (colorId >= 0 && colorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
		{
			return ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[colorId];
		}
		SpriteRenderer component = ((Component)p).GetComponent<SpriteRenderer>();
		if ((Object)(object)component != (Object)null)
		{
			return InteropFix.Cast(component.color);
		}
		return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
	}

	public void StopRecording()
	{
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Expected O, but got Unknown
		if (!isRecording)
		{
			return;
		}
		isRecording = false;
		ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(38, 0, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayRecorder] Stopping recording...");
		}
		log.LogInfo(val);
		if (currentReplay != null && currentReplay.Frames.Count > 0)
		{
			currentReplay.TotalDuration = currentReplay.Frames[currentReplay.Frames.Count - 1].Time;
			currentReplay.Events.Add(new ReplayEvent
			{
				Time = currentReplay.TotalDuration,
				Type = ReplayEventType.GameEnd,
				PlayerId = 0,
				TargetId = 0,
				Position = Vector2.zero,
				Description = "Game Ended"
			});
			string path = $"Replay_{DateTime.Now:yyyyMMdd_HHmmss}_{currentReplay.Players.Count}p_{currentReplay.Frames.Count}f.mmc";
			string text = Path.Combine(replayDirectory, path);
			try
			{
				currentReplay.Save(text);
				ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				val = new BepInExInfoLogInterpolatedStringHandler(57, 4, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayRecorder] ✅ Saved: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" (");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentReplay.Frames.Count);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" frames, ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentReplay.Events.Count);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" events, ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<float>(currentReplay.TotalDuration, "F1");
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("s duration)");
				}
				log2.LogInfo(val);
			}
			catch (Exception ex)
			{
				ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayRecorder] Failed to save: ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex);
				}
				log3.LogError(val2);
			}
		}
		else
		{
			((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayRecorder] No frames recorded, skipping save");
		}
		currentReplay = null;
		deadPlayers.Clear();
	}

	public void ToggleRecording()
	{
		if (isRecording)
		{
			StopRecording();
		}
		else
		{
			StartRecording();
		}
	}

	public void LogEvent(ReplayEventType type, byte playerId, byte targetId, Vector2 position, string description)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!isRecording)
		{
			return;
		}
		switch (type)
		{
		case ReplayEventType.Kill:
			if (!deadPlayers.Contains(targetId))
			{
				deadPlayers.Add(targetId);
			}
			break;
		case ReplayEventType.Meeting:
			wasInMeeting = true;
			break;
		}
		AddEvent(type, playerId, targetId, position, description);
	}

	public void SetAnimState(byte playerId, AnimState state)
	{
		playerAnimStates[playerId] = state;
	}

	private void EnsureReplayGuiStyles()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		if (_replayCenterLabelStyle == null || !((Object)(object)_replayBoxTex != (Object)null) || !((Object)(object)_replayBtnTex != (Object)null) || !((Object)(object)_replayBtnHoverTex != (Object)null) || !((Object)(object)_replayBtnActiveTex != (Object)null))
		{
			_replayBoxTex = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.95f));
			_replayBtnTex = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.3f, 1f));
			_replayBtnHoverTex = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.5f, 1f));
			_replayBtnActiveTex = MakeTex(2, 2, new Color(0.4f, 0.4f, 0.7f, 1f));
			((Object)_replayBoxTex).hideFlags = (HideFlags)61;
			((Object)_replayBtnTex).hideFlags = (HideFlags)61;
			((Object)_replayBtnHoverTex).hideFlags = (HideFlags)61;
			((Object)_replayBtnActiveTex).hideFlags = (HideFlags)61;
			_replayCenterLabelStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)4,
				richText = true
			};
			_replayCenterLabelStyle.normal.textColor = Color.white;
			_replayBoxStyle = new GUIStyle(GUI.skin.box);
			_replayBoxStyle.normal.background = _replayBoxTex;
			_replayBtnStyle = new GUIStyle(GUI.skin.button);
			_replayBtnStyle.normal.background = _replayBtnTex;
			_replayBtnStyle.normal.textColor = Color.white;
			_replayBtnStyle.hover.background = _replayBtnHoverTex;
			_replayBtnStyle.hover.textColor = Color.white;
			_replayBtnStyle.active.background = _replayBtnActiveTex;
			_replayBtnStyle.active.textColor = Color.white;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown((KeyCode)290))
		{
			showReplayMenu = !showReplayMenu;
			if (showReplayMenu)
			{
				RefreshReplayList();
			}
		}
	}

	private void RefreshReplayList()
	{
		try
		{
			if (Directory.Exists(replayDirectory))
			{
				cachedReplayFiles = Directory.GetFiles(replayDirectory, "*.mmc");
				System.Array.Sort(cachedReplayFiles);
				System.Array.Reverse(cachedReplayFiles);
			}
		}
		catch
		{
			cachedReplayFiles = new string[0];
		}
		lastFileRefresh = Time.time;
	}

	private void OnGUI()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		if (!showReplayMenu || !ServerData.IsLoaded)
		{
			return;
		}
		EnsureReplayGuiStyles();
		float num = Mathf.Clamp(Mathf.Pow((float)Screen.height / 1080f, 0.7f), 0.85f, 2.2f);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(num, num, 1f));
		float num2 = 320f;
		float num3 = 400f;
		Rect val = new Rect((float)Screen.width / num - num2 - 20f, 100f, num2, num3);
		GUI.Box(val, "", _replayBoxStyle);
		GUILayout.BeginArea(val);
		GUILayout.Space(10f);
		GUILayout.Label("<size=18><b>\ud83c\udfac REPLAY SYSTEM</b></size>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(5f);
		GUILayout.Label("<color=#00FFFF>━━━ RECORDER ━━━</color>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		if (isRecording)
		{
			GUILayout.Label($"<color=#FF4444>● RECORDING</color> - {FrameCount} frames, {EventCount} events", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (GUILayout.Button("⏹\ufe0f STOP RECORDING", _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
			{
				StopRecording();
			}
		}
		else
		{
			GUILayout.Label("<color=#88FF88>● READY</color> - Auto-Record: " + (AutoRecordEnabled ? "ON" : "OFF"), _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			if (GUILayout.Button("▶\ufe0f START", _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
			{
				StartRecording();
			}
			if (GUILayout.Button(AutoRecordEnabled ? "\ud83d\udd34 AUTO:ON" : "⚪ AUTO:OFF", _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
			{
				AutoRecordEnabled = !AutoRecordEnabled;
			}
			GUILayout.EndHorizontal();
		}
		if (GUILayout.Button("\ud83d\udcc2 OPEN REPLAYS FOLDER", _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
		{
			Application.OpenURL(replayDirectory);
		}
		GUILayout.Space(10f);
		GUILayout.Label("<color=#FFFF00>━━━ SAVED REPLAYS ━━━</color>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		if (Time.time - lastFileRefresh > 5f)
		{
			RefreshReplayList();
		}
		replayListScroll = GUILayout.BeginScrollView(replayListScroll, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(180f) });
		if (cachedReplayFiles.Length == 0)
		{
			GUILayout.Label("<i>No replays found</i>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}
		else
		{
			string[] array = cachedReplayFiles;
			foreach (string path in array)
			{
				string text = Path.GetFileNameWithoutExtension(path);
				if (text.Length > 30)
				{
					text = text.Substring(0, 27) + "...";
				}
				if (GUILayout.Button("▶ " + text, _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
				{
					LoadAndPlayReplay(path);
				}
			}
		}
		GUILayout.EndScrollView();
		if ((Object)(object)ReplayViewer.Instance != (Object)null && ReplayViewer.Instance.IsActive)
		{
			GUILayout.Space(5f);
			GUILayout.Label("<color=#FF00FF>━━━ NOW PLAYING ━━━</color>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (GUILayout.Button("⏹\ufe0f STOP PLAYBACK", _replayBtnStyle, System.Array.Empty<GUILayoutOption>()))
			{
				ReplayViewer.Instance.Stop();
			}
		}
		GUILayout.Space(10f);
		GUILayout.Label("<size=10><color=#888888>Press F9 to close</color></size>", _replayCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.EndArea();
		GUI.matrix = matrix;
	}

	private void LoadAndPlayReplay(string path)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(44, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayRecorder] Requesting to load replay: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(path);
			}
			log.LogInfo(val);
			if ((Object)(object)ReplayViewer.Instance == (Object)null)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayRecorder] ReplayViewer instance is null. Creating new GameObject.");
				GameObject val2 = new GameObject("ReplayViewer");
				Object.DontDestroyOnLoad((Object)(object)val2);
				Component val3 = null;
				try
				{
					val3 = val2.AddComponent(Il2CppType.Of<ReplayViewer>());
				}
				catch (System.ArgumentException)
				{
					((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayRecorder] ReplayViewer type not registered in IL2CPP. Attempting lazy registration...");
					try
					{
						ClassInjector.RegisterTypeInIl2Cpp<ReplayViewer>();
						((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayRecorder] Lazy registration successful. Retrying AddComponent...");
						val3 = val2.AddComponent(Il2CppType.Of<ReplayViewer>());
					}
					catch (Exception ex2)
					{
						ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
						BepInExErrorLogInterpolatedStringHandler val4 = new BepInExErrorLogInterpolatedStringHandler(43, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayRecorder] Lazy registration FAILED: ");
							((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<Exception>(ex2);
						}
						log2.LogError(val4);
						return;
					}
				}
				if ((Object)(object)val3 == (Object)null)
				{
					((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[ReplayRecorder] FATAL: AddComponent<ReplayViewer> returned null!");
					return;
				}
				ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				val = new BepInExInfoLogInterpolatedStringHandler(34, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayRecorder] Component added: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(((Object)val3).ToString());
				}
				log3.LogInfo(val);
			}
			if ((Object)(object)ReplayViewer.Instance == (Object)null)
			{
				ReplayViewer.Instance = Object.FindObjectOfType<ReplayViewer>();
			}
			if ((Object)(object)ReplayViewer.Instance != (Object)null)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayRecorder] Starting viewer...");
				ReplayViewer.Instance.StartViewer(path);
				showReplayMenu = false;
			}
			else
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[ReplayRecorder] FATAL: ReplayViewer.Instance is STILL NULL after creation attempt.");
			}
		}
		catch (Exception ex3)
		{
			ManualLogSource log4 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExErrorLogInterpolatedStringHandler val4 = new BepInExErrorLogInterpolatedStringHandler(49, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayRecorder] Exception in LoadAndPlayReplay: ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<Exception>(ex3);
			}
			log4.LogError(val4);
		}
	}

	private Texture2D MakeTex(int width, int height, Color col)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return NotifyUtils.MakeTex(width, height, col);
	}
}










