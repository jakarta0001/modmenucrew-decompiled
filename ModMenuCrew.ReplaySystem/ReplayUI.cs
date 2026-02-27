using System;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ModMenuCrew.Features;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

public class ReplayUI : MonoBehaviour
{
	public static ReplayUI Instance;

	private ReplayViewer viewer;

	private bool showUI = true;

	private bool showPlayerList = true;

	private bool showEventList;

	private bool showMinimap = true;

	private bool showRoutes = true;

	private bool showHeatmap;

	private GUIStyle boxStyle;

	private GUIStyle labelStyle;

	private GUIStyle buttonStyle;

	private GUIStyle timelineStyle;

	private GUIStyle eventStyle;

	private Texture2D bgTex;

	private Texture2D accentTex;

	private Texture2D darkTex;

	private bool stylesInit;

	private GUIStyle _centerLabelStyle;

	private GUIStyle _accentLineStyle;

	private GUIStyle _shortcutsLabelStyle;

	private static GUIStyle _tabCenterLabelStyle;

	private static GUIStyle _tabCenterPlainStyle;

	private static GUIStyle _tabWordWrapStyle;

	private Vector2 _eventScrollPos = Vector2.zero;

	private const float PANEL_WIDTH = 280f;

	private const float TIMELINE_HEIGHT = 60f;

	private const float CONTROLS_HEIGHT = 50f;

	private const float EVENT_PANEL_WIDTH = 220f;

	private static readonly Color BG_COLOR = new Color(0.05f, 0.05f, 0.05f, 0.94f);

	private static readonly Color BG_DARK = new Color(0.07f, 0.07f, 0.07f, 0.94f);

	private static readonly Color ACCENT = new Color(0.95f, 0.1f, 0.1f, 1f);

	private static readonly Color ACCENT_DARK = new Color(0.8f, 0f, 0f, 1f);

	private static readonly Color ACCENT_GLOW = new Color(0.95f, 0.1f, 0.1f, 0.15f);

	private static readonly Color TEXT_DIM = new Color(0.85f, 0.85f, 0.85f, 1f);

	private static readonly float[] SPEEDS = new float[6] { 0.25f, 0.5f, 1f, 2f, 4f, 8f };

	private int currentSpeedIndex = 2;

	public bool ShowRoutes => showRoutes;

	public bool ShowHeatmap => showHeatmap;

	public bool ShowMinimap => showMinimap;

	public ReplayUI(System.IntPtr ptr)
		: base(ptr)
	{
	}//IL_001d: Unknown result type (might be due to invalid IL or missing references)
	//IL_0022: Unknown result type (might be due to invalid IL or missing references)


	private void Awake()
	{
		Instance = this;
	}

	public void Initialize(ReplayViewer v)
	{
		viewer = v;
		showUI = true;
	}

	private void InitStyles()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Expected O, but got Unknown
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Expected O, but got Unknown
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Expected O, but got Unknown
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Expected O, but got Unknown
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Expected O, but got Unknown
		if (!stylesInit || !((Object)(object)bgTex != (Object)null) || !((Object)(object)accentTex != (Object)null) || !((Object)(object)darkTex != (Object)null))
		{
			stylesInit = false;
			bgTex = MakeTex(2, 2, BG_COLOR);
			((Object)bgTex).hideFlags = (HideFlags)61;
			accentTex = MakeTex(2, 2, ACCENT);
			((Object)accentTex).hideFlags = (HideFlags)61;
			darkTex = MakeTex(2, 2, BG_DARK);
			((Object)darkTex).hideFlags = (HideFlags)61;
			boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.normal.background = bgTex;
			boxStyle.padding = CreateRectOffset(10, 10, 10, 10);
			boxStyle.margin = CreateRectOffset(5, 5, 5, 5);
			labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontSize = 13;
			labelStyle.normal.textColor = Color.white;
			labelStyle.alignment = (TextAnchor)3;
			labelStyle.richText = true;
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 13;
			buttonStyle.fontStyle = (FontStyle)1;
			buttonStyle.alignment = (TextAnchor)4;
			buttonStyle.padding = CreateRectOffset(8, 8, 5, 5);
			buttonStyle.fixedHeight = 28f;
			Color bg = default(Color);
			bg._002Ector(0.12f, 0.12f, 0.14f, 1f);
			buttonStyle.normal.background = MakeFrameTex(32, 28, bg, new Color(0.4f, 0.1f, 0.1f, 1f), 1);
			buttonStyle.normal.textColor = TEXT_DIM;
			Color bg2 = default(Color);
			bg2._002Ector(0.18f, 0.08f, 0.08f, 1f);
			buttonStyle.hover.background = MakeFrameTex(32, 28, bg2, ACCENT, 2);
			buttonStyle.hover.textColor = Color.white;
			buttonStyle.active.background = MakeTex(2, 2, ACCENT_DARK);
			buttonStyle.active.textColor = Color.white;
			timelineStyle = new GUIStyle(GUI.skin.box);
			timelineStyle.normal.background = darkTex;
			eventStyle = new GUIStyle(GUI.skin.label);
			eventStyle.fontSize = 11;
			eventStyle.normal.textColor = TEXT_DIM;
			eventStyle.hover.textColor = ACCENT;
			eventStyle.richText = true;
			_centerLabelStyle = new GUIStyle(labelStyle)
			{
				alignment = (TextAnchor)4
			};
			GUIStyle val = new GUIStyle();
			val.normal.background = accentTex;
			_accentLineStyle = val;
			_shortcutsLabelStyle = new GUIStyle(labelStyle)
			{
				alignment = (TextAnchor)4
			};
			stylesInit = true;
		}
	}

	private Texture2D MakeFrameTex(int w, int h, Color bg, Color border, int thickness)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false);
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				bool flag = j < thickness || j >= w - thickness || i < thickness || i >= h - thickness;
				val.SetPixel(j, i, flag ? border : bg);
			}
		}
		val.Apply();
		return val;
	}

	private RectOffset CreateRectOffset(int left, int right, int top, int bottom)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		return new RectOffset
		{
			left = left,
			right = right,
			top = top,
			bottom = bottom
		};
	}

	internal static void DrawTabContent()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		if (_tabCenterLabelStyle == null)
		{
			_tabCenterLabelStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)4,
				richText = true
			};
			_tabCenterPlainStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)4
			};
			_tabWordWrapStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)4,
				richText = true,
				wordWrap = true,
				fontSize = 12
			};
		}
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.ExpandWidth(true),
			GUILayout.ExpandHeight(true)
		});
		GUILayout.Label("<size=24><b>\ud83c\udfac CINEMATIC REPLAY SYSTEM</b></size>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Label("<size=14><color=#AAAAAA>2026 Professional Edition</color></size>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(400f) });
		GUILayout.BeginVertical(GUI.skin.box, System.Array.Empty<GUILayoutOption>());
		GUILayout.Label("<size=16><b>\ud83d\udcf9 RECORDER STATUS</b></size>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(10f);
		GuiStyles.DrawSeparator();
		GUILayout.Space(10f);
		if ((Object)(object)ReplayRecorder.Instance != (Object)null && ReplayRecorder.Instance.IsRecording)
		{
			GUILayout.Label("<color=#FF4444><size=18>â— RECORDING IN PROGRESS</size></color>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label($"{ReplayRecorder.Instance.FrameCount} frames captured", _tabCenterPlainStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(10f);
			if (GUILayout.Button("â¹\ufe0f STOP RECORDING", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(40f) }))
			{
				ReplayRecorder.Instance.StopRecording();
			}
		}
		else
		{
			GUILayout.Label("<color=#88FF88><size=18>â— READY TO RECORD</size></color>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("Auto-Record: " + (ReplayRecorder.AutoRecordEnabled ? "ON" : "OFF"), _tabCenterPlainStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(10f);
			if (GUILayout.Button("â–¶\ufe0f START RECORDING", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(40f) }) && (Object)(object)ReplayRecorder.Instance != (Object)null)
			{
				ReplayRecorder.Instance.StartRecording();
			}
		}
		GUILayout.Space(5f);
		if (GUILayout.Button(ReplayRecorder.AutoRecordEnabled ? "\ud83d\udd34 DISABLE AUTO-RECORD" : "\ud83d\udfe2 ENABLE AUTO-RECORD", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
		{
			ReplayRecorder.AutoRecordEnabled = !ReplayRecorder.AutoRecordEnabled;
		}
		GUILayout.EndVertical();
		GUILayout.Space(20f);
		GUILayout.BeginVertical(GUI.skin.box, System.Array.Empty<GUILayoutOption>());
		GUILayout.Label("<size=16><b>âš¡ ACTIONS</b></size>", _tabCenterLabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(10f);
		GuiStyles.DrawSeparator();
		GUILayout.Space(10f);
		if (GUILayout.Button("\ud83d\udcc2 OPEN REPLAYS FOLDER", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			Application.OpenURL(Path.Combine(Paths.GameRootPath, "Replays"));
		}
		GUILayout.Space(10f);
		GUILayout.Label("<i>To watch replays, verify files, or use advanced playback controls, please use the <b>F9 Overlay Menu</b> in-game.</i>", _tabWordWrapStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.EndVertical();
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
	}

	private Texture2D MakeTex(int w, int h, Color col)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return NotifyUtils.MakeTex(w, h, col);
	}

	private void Update()
	{
		HandleInput();
	}

	private void HandleInput()
	{
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)viewer == (Object)null)
		{
			if (Time.frameCount % 300 == 0)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayUI] HandleInput: viewer is null!");
			}
			return;
		}
		if (Input.GetKeyDown((KeyCode)9))
		{
			showUI = !showUI;
		}
		if (Input.GetKeyDown((KeyCode)32))
		{
			viewer.TogglePause();
		}
		if (Input.GetKeyDown((KeyCode)276))
		{
			viewer.StepFrame(-1);
		}
		if (Input.GetKeyDown((KeyCode)275))
		{
			viewer.StepFrame(1);
		}
		if (Input.GetKeyDown((KeyCode)273) || Input.GetKeyDown((KeyCode)61) || Input.GetKeyDown((KeyCode)270))
		{
			ChangeSpeed(1);
		}
		if (Input.GetKeyDown((KeyCode)274) || Input.GetKeyDown((KeyCode)45) || Input.GetKeyDown((KeyCode)269))
		{
			ChangeSpeed(-1);
		}
		if (Input.GetKey((KeyCode)113))
		{
			viewer.AdjustZoom(0.98f);
		}
		if (Input.GetKey((KeyCode)101))
		{
			viewer.AdjustZoom(1.02f);
		}
		if (Input.GetKeyDown((KeyCode)114))
		{
			showRoutes = !showRoutes;
		}
		if (Input.GetKeyDown((KeyCode)109))
		{
			showMinimap = !showMinimap;
		}
		if (Input.GetKeyDown((KeyCode)104))
		{
			showHeatmap = !showHeatmap;
		}
		if (Input.GetKeyDown((KeyCode)112))
		{
			showPlayerList = !showPlayerList;
		}
		if (Input.GetKeyDown((KeyCode)108))
		{
			showEventList = !showEventList;
		}
		for (int i = 0; i < 9; i++)
		{
			if (Input.GetKeyDown((KeyCode)(49 + i)))
			{
				viewer.FollowPlayer((byte)i);
			}
		}
		if (Input.GetKeyDown((KeyCode)48))
		{
			viewer.StopFollowing();
		}
		float axis = Input.GetAxis("Mouse ScrollWheel");
		if (Mathf.Abs(axis) > 0.01f)
		{
			viewer.AdjustZoom((axis > 0f) ? 0.9f : 1.1f);
		}
		Vector3 zero = Vector3.zero;
		if (Input.GetKey((KeyCode)119))
		{
			zero.y += 1f;
		}
		if (Input.GetKey((KeyCode)115))
		{
			zero.y -= 1f;
		}
		if (Input.GetKey((KeyCode)97))
		{
			zero.x -= 1f;
		}
		if (Input.GetKey((KeyCode)100))
		{
			zero.x += 1f;
		}
		if (zero != Vector3.zero)
		{
			viewer.StopFollowing();
			viewer.MoveCamera(zero * Time.deltaTime * 5f);
		}
		if (Input.GetKeyDown((KeyCode)27))
		{
			viewer.Stop();
		}
	}

	private void ChangeSpeed(int delta)
	{
		currentSpeedIndex = Mathf.Clamp(currentSpeedIndex + delta, 0, SPEEDS.Length - 1);
		viewer.SetSpeed(SPEEDS[currentSpeedIndex]);
	}

	private void OnGUI()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)viewer == (Object)null) && viewer.IsActive && showUI && ServerData.IsLoaded)
		{
			InitStyles();
			float num = Mathf.Clamp(Mathf.Pow((float)Screen.height / 1080f, 0.7f), 0.85f, 2.2f);
			Matrix4x4 matrix = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(num, num, 1f));
			float num2 = (float)Screen.width / num;
			float h = (float)Screen.height / num;
			DrawTopBar(num2);
			DrawTimeline(num2, h);
			DrawControlsBar(num2, h);
			if (showPlayerList)
			{
				DrawPlayerPanel();
			}
			if (showEventList)
			{
				DrawEventPanel(num2, h);
			}
			if (showMinimap)
			{
				DrawMinimap(num2);
			}
			DrawShortcutsHint(num2);
			GUI.matrix = matrix;
		}
	}

	private void DrawTopBar(float w)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		GUI.Box(new Rect(0f, 0f, w, 40f), "", boxStyle);
		GUI.color = ACCENT;
		GUI.Box(new Rect(0f, 38f, w, 2f), "", _accentLineStyle);
		GUI.color = Color.white;
		GUI.Label(new Rect(15f, 8f, 350f, 24f), "<size=16><b><color=#FF4444>\ud83c\udfac REPLAY VIEWER</color></b></size>  <color=#888888>ModMenuCrew      Crewcore.online</color>", labelStyle);
		string text = FormatTime(viewer.CurrentTime) + " / " + FormatTime(viewer.TotalDuration);
		GUI.Label(new Rect(w / 2f - 80f, 8f, 160f, 24f), "<size=18><b>" + text + "</b></size>", _centerLabelStyle);
		string text2 = $"<color=#FF5555>{SPEEDS[currentSpeedIndex]}x</color>";
		GUI.Label(new Rect(w - 220f, 8f, 140f, 24f), "<size=14>â© Speed: <b>" + text2 + "</b></size>", labelStyle);
		GUI.backgroundColor = new Color(0.8f, 0.1f, 0.1f);
		if (GUI.Button(new Rect(w - 70f, 5f, 60f, 30f), "âœ– EXIT", buttonStyle))
		{
			viewer.Stop();
		}
		GUI.backgroundColor = Color.white;
	}

	private void DrawTimeline(float w, float h)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			float num = h - 60f - 50f;
			Rect val = default(Rect);
			val._002Ector(100f, num, w - 200f, 60f);
			GUI.Box(val, "", timelineStyle);
			float num2 = ((viewer.TotalDuration > 0f) ? (viewer.CurrentTime / viewer.TotalDuration) : 0f);
			GUI.Box(new Rect(val.x + 5f, val.y + 20f, (val.width - 10f) * num2, 20f), "", _accentLineStyle);
			if (viewer.Data != null && viewer.Data.Events != null)
			{
				foreach (ReplayEvent @event in viewer.Data.Events)
				{
					float num3 = ((viewer.TotalDuration > 0f) ? (@event.Time / viewer.TotalDuration) : 0f);
					float num4 = val.x + 5f + (val.width - 10f) * num3;
					GUI.color = @event.GetColor();
					GUI.Label(new Rect(num4 - 6f, val.y + 2f, 20f, 16f), @event.GetIcon());
					GUI.color = Color.white;
				}
			}
			if (Input.GetMouseButtonDown(0))
			{
				float num5 = Mathf.Clamp(Mathf.Pow((float)Screen.height / 1080f, 0.7f), 0.85f, 2.2f);
				Vector2 val2 = default(Vector2);
				val2._002Ector(Input.mousePosition.x / num5, ((float)Screen.height - Input.mousePosition.y) / num5);
				if (val.Contains(val2))
				{
					float num6 = (val2.x - val.x) / val.width;
					viewer.Seek(num6 * viewer.TotalDuration);
				}
			}
			for (int i = 0; i <= 10; i += 2)
			{
				float num7 = val.x + 5f + (val.width - 10f) * ((float)i / 10f);
				float seconds = viewer.TotalDuration * ((float)i / 10f);
				GUI.Label(new Rect(num7 - 20f, val.y + 42f, 40f, 16f), FormatTime(seconds), labelStyle);
			}
		}
		catch
		{
		}
	}

	private void DrawControlsBar(float w, float h)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		float num = h - 50f;
		GUI.Box(new Rect(0f, num, w, 50f), "", boxStyle);
		GUI.color = ACCENT;
		GUI.Box(new Rect(0f, num, w, 2f), "", _accentLineStyle);
		GUI.color = Color.white;
		float num2 = 50f;
		float num3 = 30f;
		float num4 = w / 2f - 150f;
		float num5 = num + 10f;
		if (GUI.Button(new Rect(num4, num5, num2, num3), "â®\ufe0f", buttonStyle))
		{
			viewer.Seek(0f);
		}
		if (GUI.Button(new Rect(num4 + 55f, num5, num2, num3), "â—€\ufe0f", buttonStyle))
		{
			viewer.StepFrame(-1);
		}
		string text = (viewer.IsPaused ? "â–¶\ufe0f" : "â¸\ufe0f");
		if (GUI.Button(new Rect(num4 + 110f, num5, num2, num3), text, buttonStyle))
		{
			viewer.TogglePause();
		}
		if (GUI.Button(new Rect(num4 + 165f, num5, num2, num3), "â–¶\ufe0f", buttonStyle))
		{
			viewer.StepFrame(1);
		}
		if (GUI.Button(new Rect(num4 + 220f, num5, num2, num3), "â­\ufe0f", buttonStyle))
		{
			viewer.Seek(viewer.TotalDuration - 0.1f);
		}
		if (GUI.Button(new Rect(num4 + 290f, num5, 35f, num3), "âˆ’", buttonStyle))
		{
			ChangeSpeed(-1);
		}
		GUI.Label(new Rect(num4 + 330f, num5, 50f, num3), $"<size=14><b>{SPEEDS[currentSpeedIndex]}x</b></size>", _centerLabelStyle);
		if (GUI.Button(new Rect(num4 + 385f, num5, 35f, num3), "+", buttonStyle))
		{
			ChangeSpeed(1);
		}
		float num6 = w - 350f;
		GUI.color = (showRoutes ? ACCENT : TEXT_DIM);
		if (GUI.Button(new Rect(num6, num5, 60f, num3), "\ud83d\udee4\ufe0f Routes", buttonStyle))
		{
			showRoutes = !showRoutes;
		}
		GUI.color = (showMinimap ? ACCENT : TEXT_DIM);
		if (GUI.Button(new Rect(num6 + 65f, num5, 60f, num3), "\ud83d\uddfa\ufe0f Map", buttonStyle))
		{
			showMinimap = !showMinimap;
		}
		GUI.color = (showHeatmap ? ACCENT : TEXT_DIM);
		if (GUI.Button(new Rect(num6 + 130f, num5, 60f, num3), "\ud83d\udd25 Heat", buttonStyle))
		{
			showHeatmap = !showHeatmap;
		}
		GUI.color = (showEventList ? ACCENT : TEXT_DIM);
		if (GUI.Button(new Rect(num6 + 195f, num5, 65f, num3), "\ud83d\udcdc Events", buttonStyle))
		{
			showEventList = !showEventList;
		}
		GUI.color = Color.white;
		if (GUI.Button(new Rect(w - 160f, num5, 70f, num3), "\ud83d\udcf7 Free", buttonStyle))
		{
			viewer.StopFollowing();
		}
		GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
		if (GUI.Button(new Rect(w - 80f, num5, 70f, num3), "âœ– EXIT", buttonStyle))
		{
			viewer.Stop();
		}
		GUI.backgroundColor = Color.white;
	}

	private void DrawPlayerPanel()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		float num = 10f;
		float num2 = 50f;
		float num3 = 30 + (viewer.Data?.Players?.Count).GetValueOrDefault() * 28;
		GUI.Box(new Rect(num, num2, 280f, num3), "", boxStyle);
		GUI.Label(new Rect(num + 10f, num2 + 5f, 260f, 20f), "<size=13><b><color=#FF4444>\ud83d\udc65 PLAYERS</color></b></size>", labelStyle);
		GUI.color = ACCENT;
		GUI.Box(new Rect(num + 5f, num2 + 27f, 270f, 1f), "", _accentLineStyle);
		GUI.color = Color.white;
		if (viewer.Data == null)
		{
			return;
		}
		float num4 = num2 + 30f;
		foreach (ReplayPlayerInfo player in viewer.Data.Players)
		{
			GUI.color = ((((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length > player.ColorId) ? InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[player.ColorId]) : Color.white);
			GUI.Label(new Rect(num + 10f, num4, 20f, 20f), "â—");
			GUI.color = Color.white;
			string text = (player.IsImpostor ? "<color=#F44>\ud83d\udc7f</color>" : "<color=#5F5>\ud83d\udc64</color>");
			GUI.Label(new Rect(num + 30f, num4, 150f, 20f), text + " " + player.Name, labelStyle);
			bool flag = viewer.FollowingPlayerId == player.PlayerId;
			string text2 = (flag ? "\ud83d\udc41\ufe0f" : "â—‹");
			if (GUI.Button(new Rect(num + 200f, num4, 60f, 22f), text2, buttonStyle))
			{
				if (flag)
				{
					viewer.StopFollowing();
				}
				else
				{
					viewer.FollowPlayer(player.PlayerId);
				}
			}
			num4 += 28f;
		}
	}

	private void DrawEventPanel(float w, float h)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		float num = w - 220f - 10f;
		float num2 = 50f;
		float num3 = h - 170f;
		GUI.Box(new Rect(num, num2, 220f, num3), "", boxStyle);
		GUI.Label(new Rect(num + 10f, num2 + 5f, 200f, 20f), "<size=13><b><color=#FF4444>\ud83d\udcdc EVENTS</color></b></size>", labelStyle);
		GUI.color = ACCENT;
		GUI.Box(new Rect(num + 5f, num2 + 27f, 210f, 1f), "", _accentLineStyle);
		GUI.color = Color.white;
		if (viewer.Data == null || viewer.Data.Events == null)
		{
			return;
		}
		GUILayout.BeginArea(new Rect(num + 2f, num2 + 30f, 216f, num3 - 35f));
		_eventScrollPos = GUILayout.BeginScrollView(_eventScrollPos, false, true, System.Array.Empty<GUILayoutOption>());
		foreach (ReplayEvent @event in viewer.Data.Events)
		{
			if (Mathf.Abs(viewer.CurrentTime - @event.Time) < 0.5f)
			{
				GUI.color = ACCENT_GLOW;
				GUILayout.Box(GUIContent.none, _accentLineStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(2f) });
			}
			GUI.color = @event.GetColor();
			string value = FormatTime(@event.Time);
			if (GUILayout.Button($"{@event.GetIcon()} [{value}] {@event.Description}", eventStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(22f) }))
			{
				viewer.Seek(@event.Time);
			}
			GUI.color = Color.white;
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	private void DrawMinimap(float scaledW)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		if (!showMinimap)
		{
			return;
		}
		float num = 180f;
		float num2 = scaledW - num - 15f;
		float num3 = 50f;
		Rect val = default(Rect);
		val._002Ector(num2, num3, num, num);
		GUI.Box(val, "", boxStyle);
		if (viewer.Data == null)
		{
			return;
		}
		float num4 = -25f;
		float num5 = 25f;
		float num6 = -15f;
		float num7 = 15f;
		foreach (ReplayPuppet puppet in viewer.GetPuppets())
		{
			if (!((Object)(object)puppet == (Object)null))
			{
				Vector2 val2 = InteropFix.Cast(((Component)puppet).transform.position);
				float num8 = (val2.x - num4) / (num5 - num4);
				float num9 = 1f - (val2.y - num6) / (num7 - num6);
				float num10 = val.x + num8 * val.width;
				float num11 = val.y + num9 * val.height;
				GUI.color = ((((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length > puppet.Info.ColorId) ? InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[puppet.Info.ColorId]) : Color.white);
				GUI.Label(new Rect(num10 - 4f, num11 - 4f, 12f, 12f), "â—");
			}
		}
		GUI.color = Color.white;
	}

	private void DrawShortcutsHint(float scaledW)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		float num = 45f;
		float num2 = 620f;
		float num3 = scaledW / 2f - num2 / 2f;
		string text = "<size=10><color=#666>[Space] Pause | [â†â†’] Frame | [â†‘â†“] Speed | [1-9] Follow | [R] Routes | [M] Map | [H] Heat | [L] Events | [P] Players | [Tab] Hide UI | [Esc] Exit</color></size>";
		GUI.Label(new Rect(num3, num, num2, 20f), text, _shortcutsLabelStyle);
	}

	private string FormatTime(float seconds)
	{
		int value = (int)(seconds / 60f);
		int value2 = (int)(seconds % 60f);
		return $"{value}:{value2:D2}";
	}
}





