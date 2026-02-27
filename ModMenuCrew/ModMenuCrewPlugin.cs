using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using Microsoft.Win32;
using ModMenuCrew.Features;
using ModMenuCrew.Monitoring;
using ModMenuCrew.Networking;
using ModMenuCrew.Patches;
using ModMenuCrew.ReplaySystem;
using ModMenuCrew.UI;
using ModMenuCrew.UI.Managers;
using ModMenuCrew.UI.Menus;
using ModMenuCrew.UI.Styles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModMenuCrew;

[BepInPlugin("CrewCore.online", "Among Us Mod Menu Crew", "6.0.8")]
[BepInProcess("Among Us.exe")]
public class ModMenuCrewPlugin : BasePlugin
{
	public class DebuggerComponent : MonoBehaviour
	{
		private class Star
		{
			public RectTransform Rect;

			public float Speed;

			public Image Image;

			public bool IsBright;
		}

		private class FloatingCrewmate
		{
			public GameObject Root;

			public RectTransform Rect;

			public Vector2 Velocity;

			public float RotSpeed;

			public float BobFrequency;

			public float BobAmplitude;

			public float TimeOffset;
		}

		private readonly struct PopupTheme
		{
			public readonly Color Accent;

			public readonly Color AccentSoft;

			public readonly Color BadgeText;

			public readonly Color PrimaryButtonTop;

			public readonly Color PrimaryButtonBottom;

			public readonly Color SecondaryButtonTop;

			public readonly Color SecondaryButtonBottom;

			public PopupTheme(Color accent, Color accentSoft, Color badgeText, Color primaryButtonTop, Color primaryButtonBottom, Color secondaryButtonTop, Color secondaryButtonBottom)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				//IL_0008: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				//IL_000f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
				//IL_0016: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				//IL_0026: Unknown result type (might be due to invalid IL or missing references)
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				Accent = accent;
				AccentSoft = accentSoft;
				BadgeText = badgeText;
				PrimaryButtonTop = primaryButtonTop;
				PrimaryButtonBottom = primaryButtonBottom;
				SecondaryButtonTop = secondaryButtonTop;
				SecondaryButtonBottom = secondaryButtonBottom;
			}
		}

		private enum SavedKeyLoadResult
		{
			NotFound,
			Loaded,
			Invalid,
			Error
		}

		private static DebuggerComponent _instance;

		public uint NetId;

		private float _customSpeed = -1f;

		private float _originalBaseSpeed = -1f;

		private bool _isCustomSpeedActive;

		private float _killCooldown = 10f;

		private long _lastLobbyTabBuildTime;

		private string _pendingSavedKey;

		private const int BanPointsPerClick = 10;

		private static byte[] _ghostUIBytecode = null;

		private static bool _useGhostUI = true;

		private static bool _ghostUILastContext = false;

		private TeleportManager teleportManager;

		private CheatManager cheatManager;

		private PlayerPickMenu playerPickMenu;

		private BanMenu banMenu;

		private SpoofingMenu spoofingMenu;

		private SettingsTab settingsTab;

		private bool _isModGloballyActivated;

		private float _lastSecurityPassTime;

		private const float SECURITY_GRACE_PERIOD = 30f;

		private float _lastZombieResetTime;

		private const float ZOMBIE_RESET_COOLDOWN = 30f;

		private string currentActivationStatusMessage = "Loading...";

		private bool isValidatingNow;

		private Task pendingValidationTask;

		private Canvas activationCanvasTMP;

		private TMP_InputField apiKeyInputFieldTMP;

		private TextMeshProUGUI statusMessageTextTMP;

		private Button validateButtonTMP;

		private TextMeshProUGUI validateButtonTextTMP;

		private Button getKeyButtonTMP;

		private Button _discordLoginBtnTMP;

		private TextMeshProUGUI _discordLoginTextTMP;

		private Button _copyLinkBtnTMP;

		private TextMeshProUGUI _copyLinkTextTMP;

		private GameObject activationPanelGO;

		private GameObject eventSystemGO;

		private GameObject canvasGO;

		private int inGameRoleGridIndex;

		private bool isLocalFakeImpostor;

		private Vector2 _lobbyRoleDropdownScroll = Vector2.zero;

		private bool _showBodyTypeDropdown;

		private Vector2 _bodyTypeDropdownScroll = Vector2.zero;

		private bool _showGameEndDropdown;

		private int _selectedGameEndReasonIndex;

		private bool _showRoleOverrideDropdown;

		private bool _showAnimationCheats;

		private bool hasAttemptedInitialActivationUIShow;

		private bool _wasServerDataLoaded;

		private const string SavedKeyFileName = "crewcore_key.txt";

		private bool _isAutoValidatingSavedKey;

		private GameObject successPopupGO;

		private TMP_FontAsset _cachedFont;

		private Texture2D _headerGradientTexture;

		private Sprite _headerGradientSprite;

		private Sprite _cachedValidateBtnSprite;

		private Sprite _cachedGetKeyBtnSprite;

		private Sprite _cachedOkBtnSprite;

		private Sprite _cachedSaveKeyBtnSprite;

		private Sprite _cachedWhiteSprite;

		private Texture2D _validateButtonGradientTexture;

		private Texture2D _getKeyButtonGradientTexture;

		private Texture2D _okButtonGradientTexture;

		private Texture2D _discordIconTexture;

		private Texture2D _successCircleTexture;

		private Sprite _successCircleSprite;

		private string _lastStatusMessage;

		private bool _lastValidatingState;

		private string _lastInputText;

		private CanvasGroup _panelCanvasGroup;

		private Image _validateBtnImage;

		private Outline _validateBtnOutline;

		private float _targetAlpha;

		private float _currentAlpha;

		private bool _shouldAutoFocus;

		private bool _bootSkipAllowed;

		private TextMeshProUGUI _bootSkipHintText;

		private float _bootSkipHintAlpha;

		private float _panelScaleCurrent = 0.92f;

		private const float PANEL_SCALE_TARGET = 1f;

		private const float PANEL_SCALE_SPEED = 6f;

		private float _revealStartTime = -1f;

		private bool _revealComplete;

		private List<CanvasGroup> _staggerElements = new List<CanvasGroup>();

		private static readonly float[] _staggerDelays = new float[9] { 0f, 0.08f, 0.16f, 0.24f, 0.32f, 0.4f, 0.48f, 0.56f, 0.64f };

		private Image _inputFieldBgImage;

		private Outline _inputFieldOutline;

		private float _inputGlowPhase;

		private bool _inputWasFocused;

		private Image _accentTopLine;

		private Image _accentBottomLine;

		private float _loadingDotsTimer;

		private int _loadingDotsCount;

		private float _breathePhase;

		private float _lastGameStateUpdateTime;

		private string _cachedTimeStr = "";

		private float _lastTimeStrUpdate;

		private RoleTypes[] _cachedRoles;

		private string[] _cachedRoleNames;

		private int _selectedHostColorIndex;

		private const int MAX_STARS = 100;

		private List<Star> _stars = new List<Star>(100);

		private bool _bootSequenceComplete;

		private float _bootStartTime;

		private int _bootLineIndex;

		private int _bootCharIndex;

		private float _lastBootCharTime;

		private float _bootCharDelay = 0.005f;

		private TextMeshProUGUI _bootConsoleText;

		private GameObject _bootConsoleGO;

		private static readonly string[] _bootLines = new string[24]
		{
			"<color=#33FFFF>> MODMENUCREW // BOOT SEQUENCE</color> <color=#666666>v6.0.7c</color>", "<color=#333333>----------------------------------------------</color>", " ", "<color=#33FFFF>[INIT]</color> Loading core framework...", "<color=#00FF55>[OK]</color> BepInEx Runtime <color=#555555>............</color> <color=#00FF55>LOADED</color>", "<color=#00FF55>[OK]</color> Harmony Patcher <color=#555555>............</color> <color=#00FF55>READY</color>", "<color=#00FF55>[OK]</color> Il2Cpp Interop <color=#555555>.............</color> <color=#00FF55>BOUND</color>", " ", "<color=#FFAA00>[SCAN]</color> Scanning host environment...", "<color=#00FF55>[OK]</color> Among Us <color=#555555>...................</color> <color=#00FF55>PID 0x1A4F</color>",
			"<color=#00FF55>[OK]</color> GameAssembly.dll <color=#555555>...........</color> <color=#00FF55>MAPPED</color>", " ", "<color=#FF6B6B>[HOOK]</color> Injecting runtime patches...", "<color=#00FF55>  |--</color> PlayerControl <color=#555555>.............</color> <color=#00FF55>PATCHED</color>", "<color=#00FF55>  |--</color> ShipStatus <color=#555555>................</color> <color=#00FF55>PATCHED</color>", "<color=#00FF55>  |--</color> MeetingHud <color=#555555>................</color> <color=#00FF55>PATCHED</color>", " ", "<color=#FF3333>[!!]</color> <color=#FFAA00>EasyAntiCheat DETECTED</color>", "<color=#FF6B6B>[EAC]</color> Evading signatures <color=#555555>........</color> <color=#FFAA00>EVADING</color>", "<color=#00FF55>[EAC]</color> Bypass status <color=#555555>.............</color> <color=#00FF55>CLEAN</color>",
			" ", "<color=#333333>----------------------------------------------</color>", "<color=#00FF55>[DONE]</color> All systems operational", "<color=#FF3333>[AUTH]</color> <color=#FFFFFF>Awaiting license key...</color>"
		};

		private string _bootDisplayedText = "";

		private Image _bootProgressFill;

		private TextMeshProUGUI _bootPhaseLabel;

		private TextMeshProUGUI _bootPercentLabel;

		private Image _bootScanlineOverlay;

		private CanvasGroup _bootConsoleCanvasGroup;

		private float _bootCursorBlinkTimer;

		private int _bootSpinnerIdx;

		private float _bootLastSpinnerTime;

		private static readonly char[] MATRIX_CHARS = "01".ToCharArray();

		private GameObject _matrixRainContainer;

		private GameObject _bootOverlay;

		private bool _isHoldingDecrypt;

		private bool _avatarLoadRequested;

		private float _discordAnimStartTime = -1f;

		private bool _discordAnimPlayed;

		private const float AVATAR_ANIM_DURATION = 0.5f;

		private const float NAME_ANIM_DELAY = 0.2f;

		private const float NAME_ANIM_DURATION = 0.3f;

		private RawImage _discordAvatarImage;

		private RectTransform _activationAvatarContainer;

		private Image _activationAvatarGlow;

		private float _activationAvatarAnimStart = -1f;

		private GUIStyle _cachedAvatarStyle;

		private Texture2D _cachedAvatarTexture;

		private static Material _glCircleMaterial;

		private float _holdStartTime;

		private float _holdProgress;

		private const float HOLD_DURATION = 2f;

		private Image _holdProgressBar;

		private GameObject _holdProgressBarGO;

		private TextMeshProUGUI _titleTextTMP;

		private RectTransform _scanLineRT;

		private float _glitchTimer;

		private bool _isGlitching;

		private string _originalTitle = "MODMENUCREW // ACCESS TERMINAL";

		private System.Collections.Generic.Dictionary<string, System.Action> _lobbyTabRegistry;

		private System.Collections.Generic.Dictionary<string, System.Action<long>> _tabDrawRegistry;

		private Vector2 _replayScroll;

		private List<FloatingCrewmate> _floatingCrewmates = new List<FloatingCrewmate>();

		private const int MAX_FLOATING_CREWMATES = 12;

		private float _spawnTimer;

		private Sprite _cachedCircleSprite;

		private List<Color> _availableCrewmateColors = new List<Color>();

		private float _pulseTimer;

		private List<Star> _popupStars = new List<Star>();

		private (bool success, string message, string username) _pendingValidationResult;

		private bool _hasPendingValidationResult;

		private string _lastWindowTitle = "";

		private float _lastTitleUpdateTime;

		private Vector2 _lobbyListScrollPosition = Vector2.zero;

		private int _selectedLobbyIndex = -1;

		private GUIStyle _lobbyEmptyIconStyle;

		private static readonly string[] _sortOptions = new string[6] { "Default", "Players ↓", "Players ↑", "Slots", "New", "Map" };

		private static readonly string[] _mapFilterOptions = new string[6] { "All", "Skeld", "Mira", "Polus", "Air", "Fungle" };

		private static readonly Color _colorWhite = Color.white;

		private static readonly Color _colorCyan = new Color(0f, 0.9f, 1f, 1f);

		private static readonly Color _colorGreen = new Color(0.3f, 1f, 0.4f, 1f);

		private static readonly Color _colorGray = new Color(0.6f, 0.6f, 0.6f, 1f);

		private static readonly Color _colorOrange = new Color(1f, 0.6f, 0.2f, 1f);

		private static readonly Color _colorRed = new Color(1f, 0.3f, 0.3f, 1f);

		private static readonly Color _colorDimGray = new Color(0.5f, 0.5f, 0.5f, 1f);

		private static readonly Color[] _mapColors = (Color[])(object)new Color[6]
		{
			new Color(0.4f, 0.7f, 1f, 1f),
			new Color(0.9f, 0.5f, 0.8f, 1f),
			new Color(0.4f, 0.9f, 0.6f, 1f),
			new Color(0.4f, 0.7f, 1f, 1f),
			new Color(1f, 0.7f, 0.3f, 1f),
			new Color(0.7f, 1f, 0.4f, 1f)
		};

		private System.Collections.Generic.List<LobbyListingPatch.CapturedLobby> _cachedLobbies;

		private float _lastLobbyRefresh;

		private const float LOBBY_CACHE_INTERVAL = 0.5f;

		private const int MAX_VISIBLE_LOBBIES = 15;

		private Vector2 _movementScrollPosition = Vector2.zero;

		private Rect _localMainWindowRect = new Rect(90f, 60f, 980f, 680f);

		private Vector2 _localMainScrollPosition = Vector2.zero;

		private Vector2 _playersTabScrollPosition = Vector2.zero;

		private Vector2 _banPointsTabScrollPosition = Vector2.zero;

		private int _localMainTabIndex;

		private bool _showLocalMainWindow = true;

		private static readonly string[] _localMainTabIds = new string[11]
		{
			"dashboard", "game", "movement", "sabotage", "impostor", "teleport", "players", "banpoints", "lobbies", "settings",
			"replay"
		};

		private static readonly string[] _localMainTabLabels = new string[11]
		{
			"Dashboard", "Game", "Movement", "Sabotage", "Impostor", "Teleport", "Players", "Ban Points", "Lobbies", "Settings",
			"Replay"
		};

		private bool _isBindingToggleKey;

		private float _securityCheckTimer;

		private TextMeshProUGUI _toggleKeyTextTMP;

		private float _lastRealtimeFrame;

		private ServerData.UISnapshot _safeSnapshot;

		private bool _safeIsLoaded;

		private bool _safeCanRender;

		public bool DisableGameEnd { get; set; }

		public bool ForceImpostor { get; set; }

		public bool IsNoclipping { get; set; }

		public static bool BypassSecurity { get; set; }

		public float PlayerSpeed
		{
			get
			{
				if ((!ModKeyValidator.V() && !IsOfflineLocalModeEnabled()) || !_isCustomSpeedActive || !(_customSpeed > 0f))
				{
					return LobbySpeedMod;
				}
				return _customSpeed;
			}
			set
			{
				if (!_isCustomSpeedActive)
				{
					EnableCustomSpeed();
				}
				_customSpeed = Math.Max(0.5f, Math.Min(6f, value));
			}
		}

		public float LobbySpeedMod
		{
			get
			{
				try
				{
					GameOptionsManager instance = GameOptionsManager.Instance;
					if (((instance != null) ? instance.CurrentGameOptions : null) != null)
					{
						return GameOptionsManager.Instance.CurrentGameOptions.GetFloat((FloatOptionNames)2);
					}
				}
				catch
				{
				}
				return 1f;
			}
		}

		public bool IsCustomSpeedActive
		{
			get
			{
				return _isCustomSpeedActive;
			}
			set
			{
				if (!value)
				{
					ResetPlayerSpeed();
				}
				else
				{
					EnableCustomSpeed();
				}
			}
		}

		public float KillCooldown
		{
			get
			{
				if (!ModKeyValidator.V() && !IsOfflineLocalModeEnabled())
				{
					return 10f;
				}
				return _killCooldown;
			}
			set
			{
				if (ModKeyValidator.V() || IsOfflineLocalModeEnabled())
				{
					_killCooldown = Math.Max(0f, value);
				}
			}
		}

		public bool NoKillCooldown
		{
			get
			{
				if (ModKeyValidator.V() || IsOfflineLocalModeEnabled())
				{
					return CheatConfig.NoKillCooldown?.Value ?? false;
				}
				return false;
			}
			set
			{
				if (CheatConfig.NoKillCooldown != null)
				{
					CheatConfig.NoKillCooldown.Value = value;
				}
			}
		}

		public bool InstantWin { get; set; }

		public static bool IsMenuOpen => _useGhostUI || (_instance != null && _instance._showLocalMainWindow);

		private bool IsOfflineLocalModeEnabled()
		{
			return CheatConfig.OfflineLocalMode?.Value ?? false;
		}

		private bool isModGloballyActivated
		{
			get
			{
				return _isModGloballyActivated;
			}
			set
			{
				_isModGloballyActivated = value;
			}
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern System.IntPtr ShellExecuteW(System.IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

		private static bool IsImpostorRoleSafe(RoleTypes role)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Invalid comparison between Unknown and I4
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Invalid comparison between Unknown and I4
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			try
			{
				return RoleManager.IsImpostorRole(role);
			}
			catch
			{
				return (int)role == 1 || (int)role == 5 || (int)role == 9 || (int)role == 18;
			}
		}

		internal static bool OpenBrowser(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return false;
			}
			if (!System.Uri.TryCreate(url, System.UriKind.Absolute, out System.Uri result) || (result.Scheme != "https" && result.Scheme != "http"))
			{
				return false;
			}
			try
			{
				if (ShellExecuteW(System.IntPtr.Zero, "open", url, null, null, 1).ToInt64() > 32)
				{
					return true;
				}
			}
			catch
			{
			}
			bool flag = false;
			try
			{
				flag = ModKeyValidator.IsRunningUnderWine;
			}
			catch
			{
			}
			if (flag)
			{
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "winebrowser",
						Arguments = url,
						UseShellExecute = false,
						CreateNoWindow = true
					});
					return true;
				}
				catch
				{
				}
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "xdg-open",
						Arguments = url,
						UseShellExecute = false,
						CreateNoWindow = true
					});
					return true;
				}
				catch
				{
				}
				try
				{
					string fileName = (File.Exists("Z:\\usr\\bin\\open") ? "Z:\\usr\\bin\\open" : "open");
					Process.Start(new ProcessStartInfo
					{
						FileName = fileName,
						Arguments = url,
						UseShellExecute = false,
						CreateNoWindow = true
					});
					return true;
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = url,
						UseShellExecute = true
					});
					return true;
				}
				catch
				{
				}
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "cmd.exe",
						Arguments = "/c start \"\" \"" + url.Replace("&", "^&") + "\"",
						UseShellExecute = false,
						CreateNoWindow = true
					});
					return true;
				}
				catch
				{
				}
			}
			try
			{
				Application.OpenURL(url);
				return true;
			}
			catch
			{
			}
			return false;
		}

		public void EnableCustomSpeed()
		{
			if ((!ModKeyValidator.V() && !IsOfflineLocalModeEnabled()) || _isCustomSpeedActive)
			{
				return;
			}
			try
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.MyPhysics : null) != (Object)null)
				{
					_originalBaseSpeed = PlayerControl.LocalPlayer.MyPhysics.Speed;
				}
			}
			catch
			{
			}
			if (_originalBaseSpeed <= 0f)
			{
				_originalBaseSpeed = 2.5f;
			}
			_customSpeed = _originalBaseSpeed;
			_isCustomSpeedActive = true;
		}

		public void ResetPlayerSpeed()
		{
			if (!_isCustomSpeedActive)
			{
				return;
			}
			_isCustomSpeedActive = false;
			_customSpeed = -1f;
			try
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.MyPhysics : null) != (Object)null && _originalBaseSpeed > 0f)
				{
					PlayerControl.LocalPlayer.MyPhysics.Speed = _originalBaseSpeed;
				}
			}
			catch
			{
			}
			_originalBaseSpeed = -1f;
		}

		private void FixedUpdate()
		{
			if (PlayerControl.AllPlayerControls == null)
			{
				return;
			}
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (!((Object)(object)current == (Object)null) && !((Object)(object)current == (Object)(object)PlayerControl.LocalPlayer) && (Object)(object)current.Data != (Object)null && (Object)(object)current.Data.Role != (Object)null && current.Data.Role.IsImpostor && !current.Data.IsDead && current.killTimer > 0f)
				{
					current.killTimer -= Time.fixedDeltaTime;
					if (current.killTimer < 0f)
					{
						current.killTimer = 0f;
					}
				}
			}
		}

		private void ResetDiscordAnimation()
		{
			_discordAnimStartTime = -1f;
			_discordAnimPlayed = false;
		}

		private void UpdateGlitchText()
		{
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_titleTextTMP == (Object)null)
			{
				return;
			}
			_glitchTimer += Time.deltaTime;
			if (!_isGlitching)
			{
				if (_glitchTimer > UnityEngine.Random.Range(2f, 5f))
				{
					_isGlitching = true;
					_glitchTimer = 0f;
				}
			}
			else if (_glitchTimer < UnityEngine.Random.Range(0.1f, 0.3f))
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < _originalTitle.Length; i++)
				{
					char c = _originalTitle[i];
					if (char.IsLetterOrDigit(c) && UnityEngine.Random.value > 0.6f)
					{
						stringBuilder.Append(MATRIX_CHARS[UnityEngine.Random.Range(0, MATRIX_CHARS.Length)]);
					}
					else
					{
						stringBuilder.Append(c);
					}
				}
				((TMP_Text)_titleTextTMP).text = stringBuilder.ToString();
				if (UnityEngine.Random.value > 0.5f)
				{
					((Graphic)_titleTextTMP).color = new Color(0.2f, 0.8f, 1f);
				}
				else
				{
					((Graphic)_titleTextTMP).color = new Color(0.2f, 1f, 1f);
				}
			}
			else
			{
				_isGlitching = false;
				_glitchTimer = 0f;
				((TMP_Text)_titleTextTMP).text = _originalTitle;
				((Graphic)_titleTextTMP).color = new Color(1f, 0.4f, 0.5f);
			}
		}

		private void CreateBootConsole(RectTransform parent)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Expected O, but got Unknown
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a4: Expected O, but got Unknown
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_021a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Expected O, but got Unknown
			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
			//IL_025d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0268: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Expected O, but got Unknown
			//IL_0327: Unknown result type (might be due to invalid IL or missing references)
			//IL_0352: Unknown result type (might be due to invalid IL or missing references)
			//IL_0367: Unknown result type (might be due to invalid IL or missing references)
			//IL_0372: Unknown result type (might be due to invalid IL or missing references)
			//IL_037c: Unknown result type (might be due to invalid IL or missing references)
			//IL_038b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0392: Expected O, but got Unknown
			//IL_0404: Unknown result type (might be due to invalid IL or missing references)
			//IL_0430: Unknown result type (might be due to invalid IL or missing references)
			//IL_0445: Unknown result type (might be due to invalid IL or missing references)
			//IL_0450: Unknown result type (might be due to invalid IL or missing references)
			//IL_045a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0469: Unknown result type (might be due to invalid IL or missing references)
			//IL_0470: Expected O, but got Unknown
			//IL_049a: Unknown result type (might be due to invalid IL or missing references)
			//IL_04af: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0514: Unknown result type (might be due to invalid IL or missing references)
			//IL_0523: Unknown result type (might be due to invalid IL or missing references)
			//IL_052a: Expected O, but got Unknown
			//IL_059c: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_05dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0607: Unknown result type (might be due to invalid IL or missing references)
			//IL_061b: Unknown result type (might be due to invalid IL or missing references)
			_bootConsoleGO = new GameObject("BootConsole");
			_bootConsoleGO.transform.SetParent((Transform)(object)parent, false);
			RectTransform obj = _bootConsoleGO.AddComponent<RectTransform>();
			obj.anchorMin = Vector2.zero;
			obj.anchorMax = Vector2.one;
			obj.sizeDelta = new Vector2(-40f, -40f);
			obj.anchoredPosition = Vector2.zero;
			_bootConsoleCanvasGroup = _bootConsoleGO.AddComponent<CanvasGroup>();
			_bootConsoleCanvasGroup.alpha = 1f;
			((Graphic)_bootConsoleGO.AddComponent<Image>()).color = new Color(0.01f, 0.02f, 0.04f, 0.97f);
			GameObject val = new GameObject("BootConsoleText");
			val.transform.SetParent(_bootConsoleGO.transform, false);
			RectTransform obj2 = val.AddComponent<RectTransform>();
			obj2.anchorMin = new Vector2(0f, 0.13f);
			obj2.anchorMax = Vector2.one;
			obj2.sizeDelta = new Vector2(-24f, -16f);
			obj2.anchoredPosition = Vector2.zero;
			_bootConsoleText = val.AddComponent<TextMeshProUGUI>();
			((TMP_Text)_bootConsoleText).font = LoadGameFont();
			((TMP_Text)_bootConsoleText).fontSize = 13f;
			((TMP_Text)_bootConsoleText).alignment = (TextAlignmentOptions)257;
			((Graphic)_bootConsoleText).color = new Color(0f, 1f, 0.3f);
			((TMP_Text)_bootConsoleText).richText = true;
			((TMP_Text)_bootConsoleText).overflowMode = (TextOverflowModes)3;
			((TMP_Text)_bootConsoleText).text = "";
			GameObject val2 = new GameObject("BootProgressBG");
			val2.transform.SetParent(_bootConsoleGO.transform, false);
			RectTransform obj3 = val2.AddComponent<RectTransform>();
			obj3.anchorMin = new Vector2(0.03f, 0.04f);
			obj3.anchorMax = new Vector2(0.75f, 0.075f);
			obj3.sizeDelta = Vector2.zero;
			obj3.anchoredPosition = Vector2.zero;
			((Graphic)val2.AddComponent<Image>()).color = new Color(0.12f, 0.12f, 0.18f, 1f);
			GameObject val3 = new GameObject("BootProgressFill");
			val3.transform.SetParent(val2.transform, false);
			RectTransform obj4 = val3.AddComponent<RectTransform>();
			obj4.anchorMin = Vector2.zero;
			obj4.anchorMax = new Vector2(0f, 1f);
			obj4.sizeDelta = Vector2.zero;
			obj4.anchoredPosition = Vector2.zero;
			_bootProgressFill = val3.AddComponent<Image>();
			((Graphic)_bootProgressFill).color = new Color(0.2f, 1f, 0.85f, 0.9f);
			GameObject val4 = new GameObject("BootPhaseLabel");
			val4.transform.SetParent(_bootConsoleGO.transform, false);
			_bootPhaseLabel = val4.AddComponent<TextMeshProUGUI>();
			((TMP_Text)_bootPhaseLabel).font = LoadGameFont();
			((TMP_Text)_bootPhaseLabel).fontSize = 11f;
			((TMP_Text)_bootPhaseLabel).alignment = (TextAlignmentOptions)1025;
			((Graphic)_bootPhaseLabel).color = new Color(0.4f, 0.9f, 1f, 0.85f);
			((TMP_Text)_bootPhaseLabel).text = "<color=#33FFFF>[|]</color> INITIALIZING";
			RectTransform component = val4.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.03f, 0.075f);
			component.anchorMax = new Vector2(0.5f, 0.12f);
			component.sizeDelta = Vector2.zero;
			component.anchoredPosition = Vector2.zero;
			GameObject val5 = new GameObject("BootPercentLabel");
			val5.transform.SetParent(_bootConsoleGO.transform, false);
			_bootPercentLabel = val5.AddComponent<TextMeshProUGUI>();
			((TMP_Text)_bootPercentLabel).font = LoadGameFont();
			((TMP_Text)_bootPercentLabel).fontSize = 12f;
			((TMP_Text)_bootPercentLabel).alignment = (TextAlignmentOptions)4100;
			((Graphic)_bootPercentLabel).color = new Color(0.2f, 1f, 0.85f, 0.9f);
			((TMP_Text)_bootPercentLabel).text = "0%";
			RectTransform component2 = val5.GetComponent<RectTransform>();
			component2.anchorMin = new Vector2(0.76f, 0.03f);
			component2.anchorMax = new Vector2(0.97f, 0.09f);
			component2.sizeDelta = Vector2.zero;
			component2.anchoredPosition = Vector2.zero;
			GameObject val6 = new GameObject("BootScanline");
			val6.transform.SetParent(_bootConsoleGO.transform, false);
			RectTransform obj5 = val6.AddComponent<RectTransform>();
			obj5.anchorMin = new Vector2(0f, 0f);
			obj5.anchorMax = new Vector2(1f, 0f);
			obj5.pivot = new Vector2(0.5f, 0f);
			obj5.sizeDelta = new Vector2(0f, 3f);
			obj5.anchoredPosition = Vector2.zero;
			_bootScanlineOverlay = val6.AddComponent<Image>();
			((Graphic)_bootScanlineOverlay).color = new Color(0.2f, 1f, 0.9f, 0.05f);
			GameObject val7 = new GameObject("BootSkipHint");
			val7.transform.SetParent(_bootConsoleGO.transform, false);
			_bootSkipHintText = val7.AddComponent<TextMeshProUGUI>();
			((TMP_Text)_bootSkipHintText).font = LoadGameFont();
			((TMP_Text)_bootSkipHintText).fontSize = 10f;
			((TMP_Text)_bootSkipHintText).alignment = (TextAlignmentOptions)1028;
			((Graphic)_bootSkipHintText).color = new Color(0.4f, 0.4f, 0.5f, 0f);
			((TMP_Text)_bootSkipHintText).text = "[ CLICK or PRESS ANY KEY to skip ]";
			RectTransform component3 = val7.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0f, 0f);
			component3.anchorMax = new Vector2(1f, 0f);
			component3.pivot = new Vector2(1f, 0f);
			component3.sizeDelta = new Vector2(-20f, 24f);
			component3.anchoredPosition = new Vector2(0f, 6f);
			_bootSequenceComplete = false;
			_bootStartTime = Time.realtimeSinceStartup;
			_bootLineIndex = 0;
			_bootCharIndex = 0;
			_bootSkipAllowed = false;
			_bootSkipHintAlpha = 0f;
			_bootCursorBlinkTimer = 0f;
			_bootSpinnerIdx = 0;
			_bootLastSpinnerTime = 0f;
			_bootDisplayedText = "";
			int num = 0;
			string[] bootLines = _bootLines;
			foreach (string input in bootLines)
			{
				num += Regex.Replace(input, "<.*?>", "").Length;
			}
			_bootCharDelay = ((num > 0) ? (3.5f / (float)num) : 0.005f);
		}

		public DebuggerComponent(System.IntPtr ptr)
			: base(ptr)
		{
		}//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)


		private void DestroySpriteAndTexture(Sprite sprite)
		{
			if ((Object)(object)sprite == (Object)null)
			{
				return;
			}
			try
			{
				Texture2D texture = sprite.texture;
				Object.Destroy((Object)(object)sprite);
				sprite = null;
				if ((Object)(object)texture != (Object)null && (Object)(object)texture != (Object)(object)Texture2D.whiteTexture)
				{
					Object.Destroy((Object)(object)texture);
				}
			}
			catch
			{
				sprite = null;
			}
		}

		public void CleanupResources()
		{
			try
			{
				CleanupActivationUI();
				if ((Object)(object)successPopupGO != (Object)null)
				{
					Object.Destroy((Object)(object)successPopupGO);
					successPopupGO = null;
				}
				if ((Object)(object)_headerGradientSprite != (Object)null)
				{
					Object.Destroy((Object)(object)_headerGradientSprite);
					_headerGradientSprite = null;
				}
				if ((Object)(object)_headerGradientTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_headerGradientTexture);
					_headerGradientTexture = null;
				}
				DestroySpriteAndTexture(_cachedValidateBtnSprite);
				DestroySpriteAndTexture(_cachedGetKeyBtnSprite);
				DestroySpriteAndTexture(_cachedOkBtnSprite);
				DestroySpriteAndTexture(_cachedSaveKeyBtnSprite);
				DestroySpriteAndTexture(_cachedWhiteSprite);
				if ((Object)(object)_validateButtonGradientTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_validateButtonGradientTexture);
					_validateButtonGradientTexture = null;
				}
				if ((Object)(object)_getKeyButtonGradientTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_getKeyButtonGradientTexture);
					_getKeyButtonGradientTexture = null;
				}
				if ((Object)(object)_okButtonGradientTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_okButtonGradientTexture);
					_okButtonGradientTexture = null;
				}
				if ((Object)(object)_successCircleSprite != (Object)null)
				{
					Object.Destroy((Object)(object)_successCircleSprite);
					_successCircleSprite = null;
				}
				if ((Object)(object)_successCircleTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_successCircleTexture);
					_successCircleTexture = null;
				}
				if ((Object)(object)_discordIconTexture != (Object)null)
				{
					Object.Destroy((Object)(object)_discordIconTexture);
					_discordIconTexture = null;
				}
				if ((Object)(object)eventSystemGO != (Object)null)
				{
					Object.Destroy((Object)(object)eventSystemGO);
					eventSystemGO = null;
				}
				teleportManager = null;
				cheatManager = null;
				playerPickMenu = null;
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error resource cleanup: {value}"));
			}
		}

		private void OnDestroy()
		{
			CleanupResources();
		}

		private void Awake()
		{
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Expected O, but got Unknown
			//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cc: Expected O, but got Unknown
			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
			//IL_022a: Expected O, but got Unknown
			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Expected O, but got Unknown
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Expected O, but got Unknown
			_instance = this;
			try
			{
				((BasePlugin)Instance).Log.LogInfo((object)"DebuggerComponent: Awake started.");
				bool offlineLocalModeEnabled = IsOfflineLocalModeEnabled();
				isModGloballyActivated = offlineLocalModeEnabled;
				currentActivationStatusMessage = "Enter your activation key.";
				InitializeFeatureManagers();
				InitializeMainWindowIMGUI();
				InitializeTabsForGameIMGUI();
				IntegrityGuard.Initialize();
				_useGhostUI = !offlineLocalModeEnabled;
				_showLocalMainWindow = true;
				if (!offlineLocalModeEnabled)
				{
					DiscordAuthManager.TryRestoreSession();
					DiscordAuthManager.OnLoginComplete += OnDiscordLoginComplete;
				}
				else
				{
					currentActivationStatusMessage = "Ready.";
					try
					{
						CleanupActivationUI();
					}
					catch
					{
					}
				}
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val2;
				if (!isModGloballyActivated)
				{
					currentActivationStatusMessage = "Enter your activation key or get a new one.";
					SetupActivationUI_TMP();
					if (DiscordAuthManager.IsLoggedIn && !_avatarLoadRequested)
					{
						_avatarLoadRequested = true;
						((MonoBehaviour)this).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(LoadDiscordAvatarCoroutine()));
					}
					try
					{
						string key;
						string detail;
						SavedKeyLoadResult savedKeyLoadResult = TryLoadSavedKeyFromFile(out key, out detail);
						switch (savedKeyLoadResult)
						{
						case SavedKeyLoadResult.Loaded:
							if (DiscordAuthManager.IsLoggedIn)
							{
								_isAutoValidatingSavedKey = true;
								hasAttemptedInitialActivationUIShow = true;
								_pendingSavedKey = null;
								currentActivationStatusMessage = "Validating saved key, please wait...";
								isValidatingNow = true;
								pendingValidationTask = ValidateKeyAndSetState(key);
								if ((Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
								{
									((Component)activationCanvasTMP).gameObject.SetActive(false);
								}
								ManageActivationUIVisibility();
								ManualLogSource log2 = ((BasePlugin)Instance).Log;
								val2 = new BepInExInfoLogInterpolatedStringHandler(69, 1, out flag);
								if (flag)
								{
									((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ModMenuCrew] Discord session active. Auto-validating saved key from ");
									((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(GetSavedKeyFilePath());
								}
								log2.LogInfo(val2);
							}
							else
							{
								_pendingSavedKey = key;
								_isAutoValidatingSavedKey = false;
								hasAttemptedInitialActivationUIShow = false;
								currentActivationStatusMessage = "Saved key found. Please login to Discord to verify ownership.";
								if ((Object)(object)activationCanvasTMP != (Object)null && !((Component)activationCanvasTMP).gameObject.activeSelf)
								{
									((Component)activationCanvasTMP).gameObject.SetActive(true);
									_shouldAutoFocus = false;
								}
								ManageActivationUIVisibility();
								ManualLogSource log3 = ((BasePlugin)Instance).Log;
								val2 = new BepInExInfoLogInterpolatedStringHandler(94, 0, out flag);
								if (flag)
								{
									((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ModMenuCrew] Saved key found but Discord not logged in. Waiting for Discord authentication...");
								}
								log3.LogInfo(val2);
							}
							break;
						case SavedKeyLoadResult.Invalid:
						case SavedKeyLoadResult.Error:
							_isAutoValidatingSavedKey = false;
							_pendingSavedKey = null;
							currentActivationStatusMessage = "Saved key error. Please enter your key manually.";
							if (!string.IsNullOrWhiteSpace(detail))
							{
								ManualLogSource log = ((BasePlugin)Instance).Log;
								BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(39, 2, out flag);
								if (flag)
								{
									((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModMenuCrew] Saved key file issue (");
									((BepInExLogInterpolatedStringHandler)val).AppendFormatted<SavedKeyLoadResult>(savedKeyLoadResult);
									((BepInExLogInterpolatedStringHandler)val).AppendLiteral("): ");
									((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(detail);
								}
								log.LogWarning(val);
							}
							break;
						}
					}
					catch (Exception ex)
					{
						_isAutoValidatingSavedKey = false;
						_pendingSavedKey = null;
						currentActivationStatusMessage = "Saved key error. Please enter your key manually.";
						ManualLogSource log4 = ((BasePlugin)Instance).Log;
						BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(47, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModMenuCrew] Saved key auto-validation error: ");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
						}
						log4.LogWarning(val);
					}
				}
				ManualLogSource log5 = ((BasePlugin)Instance).Log;
				val2 = new BepInExInfoLogInterpolatedStringHandler(51, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("DebuggerComponent: Awake completed. Mod initially ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(isModGloballyActivated ? "activated" : "deactivated");
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
				}
				log5.LogInfo(val2);
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Critical error DebuggerComponent.Awake: {value}"));
			}
		}

		private void Start()
		{
		}

		private void OnDiscordLoginComplete(bool success, string message)
		{
			if ((Object)(object)_discordLoginTextTMP != (Object)null)
			{
				((TMP_Text)_discordLoginTextTMP).text = (success ? ("✓ " + DiscordAuthManager.DiscordUsername) : "\ud83c\udfae Login Discord");
			}
			if ((Object)(object)statusMessageTextTMP != (Object)null)
			{
				currentActivationStatusMessage = message;
				((TMP_Text)statusMessageTextTMP).text = message;
			}
			if (success && !_avatarLoadRequested)
			{
				_avatarLoadRequested = true;
				((MonoBehaviour)this).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(LoadDiscordAvatarCoroutine()));
			}
			if (success && !string.IsNullOrEmpty(_pendingSavedKey))
			{
				((BasePlugin)Instance).Log.LogInfo((object)"[ModMenuCrew] Discord authenticated. Validating pending saved key...");
				string pendingSavedKey = _pendingSavedKey;
				_pendingSavedKey = null;
				currentActivationStatusMessage = "Verifying key ownership...";
				if ((Object)(object)statusMessageTextTMP != (Object)null)
				{
					((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
				}
				_isAutoValidatingSavedKey = true;
				isValidatingNow = true;
				pendingValidationTask = ValidateKeyAndSetState(pendingSavedKey);
				if ((Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
				{
					((Component)activationCanvasTMP).gameObject.SetActive(false);
				}
				ManageActivationUIVisibility();
			}
		}

		[HideFromIl2Cpp]
		private IEnumerator LoadDiscordAvatarCoroutine()
		{
			string url = DiscordAuthManager.GetAvatarUrl();
			if (string.IsNullOrEmpty(url))
			{
				yield break;
			}
			byte[] imageData = null;
			bool downloadComplete = false;
			bool downloadSuccess = false;
			string downloadError = null;
			Task.Run(async delegate
			{
				try
				{
					imageData = await DiscordAuthManager.GetPinnedClient().GetByteArrayAsync(url);
					byte[] array = imageData;
					downloadSuccess = array != null && array.Length != 0;
				}
				catch (Exception ex2)
				{
					downloadError = ex2.Message;
				}
				finally
				{
					downloadComplete = true;
				}
			});
			float timeout = 15f;
			while (!downloadComplete && timeout > 0f)
			{
				yield return null;
				timeout -= Time.deltaTime;
			}
			if (!downloadComplete || !downloadSuccess || imageData == null)
			{
				if (downloadError != null)
				{
					Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] Avatar download failed: " + downloadError));
				}
				_avatarLoadRequested = false;
				yield break;
			}
			try
			{
				Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false);
				((Texture)val).wrapMode = (TextureWrapMode)1;
				((Texture)val).filterMode = (FilterMode)1;
				bool flag = false;
				try
				{
					TinyPngDecoder.Decode(imageData, val);
					flag = true;
				}
				catch
				{
					try
					{
						flag = ImageConversion.LoadImage(val, InteropFix.Cast(imageData));
					}
					catch
					{
					}
				}
				if (flag)
				{
					DiscordAuthManager.SetAvatarTexture(val);
					if ((Object)(object)_discordAvatarImage != (Object)null)
					{
						_discordAvatarImage.texture = (Texture)(object)val;
						if ((Object)(object)_activationAvatarContainer != (Object)null)
						{
							((Component)_activationAvatarContainer).gameObject.SetActive(true);
							if (_bootSequenceComplete)
							{
								((Transform)_activationAvatarContainer).localScale = Vector3.one;
								_activationAvatarAnimStart = -1f;
							}
							else
							{
								_activationAvatarAnimStart = Time.time;
								((Transform)_activationAvatarContainer).localScale = Vector3.zero;
							}
						}
						else
						{
							Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] _activationAvatarContainer is NULL - avatar won't show in activation UI!"));
						}
					}
					else
					{
						Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] _discordAvatarImage is NULL - avatar won't show!"));
					}
				}
				else
				{
					Object.Destroy((Object)(object)val);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] Avatar error: " + ex.Message));
			}
		}

		private void InitializeFeatureManagers()
		{
			teleportManager = new TeleportManager();
			cheatManager = new CheatManager();
			playerPickMenu = new PlayerPickMenu();
			banMenu = new BanMenu();
			spoofingMenu = new SpoofingMenu();
			settingsTab = new SettingsTab();
		}

		private void InitializeMainWindowIMGUI()
		{
		}

		private void RenderDiscordCardWithAnimations()
		{
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0400: Unknown result type (might be due to invalid IL or missing references)
			//IL_0255: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_020e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0468: Unknown result type (might be due to invalid IL or missing references)
			//IL_0481: Unknown result type (might be due to invalid IL or missing references)
			//IL_049f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Expected O, but got Unknown
			//IL_052b: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_0379: Unknown result type (might be due to invalid IL or missing references)
			//IL_039b: Unknown result type (might be due to invalid IL or missing references)
			if (DiscordAuthManager.IsAvatarLoaded && !_discordAnimPlayed)
			{
				_discordAnimStartTime = Time.time;
				_discordAnimPlayed = true;
			}
			float num = ((_discordAnimStartTime > 0f) ? (Time.time - _discordAnimStartTime) : 99f);
			float num2 = Mathf.Clamp01(num / 0.5f);
			float num3 = num2 - 1f;
			float num4 = num3 * num3;
			float num5 = num4 * num3;
			float num6 = ((num2 >= 1f) ? 1f : Mathf.Clamp(1f + 2.70158f * num5 + 1.70158f * num4, 0f, 1.08f));
			float num7 = Mathf.Clamp01(num2 * 1.3f);
			float num8 = 1f - num7;
			float num9 = 1f - num8 * num8 * num8;
			float num10 = Mathf.Clamp01((num - 0.2f) / 0.3f);
			float num11 = 1f - num10;
			float num12 = num11 * num11;
			float num13 = 1f - num12 * num12;
			GUILayout.BeginVertical(GuiStyles.DashboardCardStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) });
			GUILayout.Space(4f);
			Rect rect = GUILayoutUtility.GetRect(72f, 72f, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(72f),
				GUILayout.Height(72f)
			});
			float num14 = rect.x + 36f;
			float num15 = rect.y + 36f;
			if ((Object)(object)DiscordAuthManager.AvatarTexture != (Object)null)
			{
				if (num2 >= 0.2f)
				{
					float num16 = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f);
					float num17 = Mathf.Clamp01((num2 - 0.2f) * 2f) * (0.3f + num16 * 0.4f);
					float num18 = 76f + num16 * 6f;
					GUI.color = new Color(0.345f, 0.396f, 0.949f, num17);
					GUI.Box(new Rect(num14 - num18 * 0.5f, num15 - num18 * 0.5f, num18, num18), "", GUI.skin.box);
					GUI.color = Color.white;
				}
				float num19 = 64f * num6;
				Rect val = new Rect(num14 - num19 * 0.5f, num15 - num19 * 0.5f, num19, num19);
				Texture2D avatarTexture = DiscordAuthManager.AvatarTexture;
				if (_cachedAvatarStyle == null || (Object)(object)_cachedAvatarTexture != (Object)(object)avatarTexture)
				{
					GUIStyle val2 = new GUIStyle();
					val2.normal.background = avatarTexture;
					_cachedAvatarStyle = val2;
					_cachedAvatarTexture = avatarTexture;
				}
				GUI.color = new Color(1f, 1f, 1f, num9);
				GUI.Box(val, GUIContent.none, _cachedAvatarStyle);
				GUI.color = Color.white;
				if (num2 >= 1f)
				{
					float num20 = Time.time * 1.5f;
					for (int i = 0; i < 2; i++)
					{
						float num21 = num20 + (float)i * (float)System.Math.PI;
						float num22 = 42f;
						float num23 = num14 + Mathf.Cos(num21) * num22;
						float num24 = num15 + Mathf.Sin(num21) * num22;
						float num25 = 0.4f + 0.4f * Mathf.Sin(Time.time * 3f + (float)i * 2f);
						GUI.color = new Color(0.6f, 0.7f, 1f, num25);
						GUI.Label(new Rect(num23 - 4f, num24 - 4f, 12f, 12f), "✦", GuiStyles.LabelStyle);
					}
					GUI.color = Color.white;
				}
			}
			else
			{
				float num26 = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
				GUI.color = new Color(0.345f, 0.396f, 0.949f, num26);
				GUI.Box(rect, "", GUI.skin.box);
				GUI.color = Color.white;
				GUI.Label(rect, "<size=20>⟳</size>", GuiStyles.LabelStyle);
			}
			if (num2 >= 1f)
			{
				float num27 = 0.92f + 0.08f * Mathf.Sin(Time.time * 2f);
				float num28 = 12f * num27;
				Rect val3 = new Rect(rect.xMax - num28 - 4f, rect.yMax - num28 - 4f, num28, num28);
				GUI.color = new Color(0.34f, 0.97f, 0.53f, 1f);
				GUI.Box(val3, "", GUI.skin.box);
				GUI.color = Color.white;
			}
			GUILayout.EndVertical();
			GUILayout.Space(12f);
			GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(8f);
			GUI.color = new Color(1f, 1f, 1f, num13);
			GUILayout.Label("<color=#5865F2><b>DISCORD</b></color>", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("<size=16><b>" + DiscordAuthManager.DiscordUsername + "</b></size>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (num10 >= 1f)
			{
				GUILayout.Label("<color=#57F287>● Online</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			GUI.color = Color.white;
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			if (num10 >= 1f)
			{
				GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Space(20f);
				if (GUILayout.Button("Logout", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(70f) }))
				{
					DiscordAuthManager.Logout();
					DiscordAuthManager.SetAvatarTexture(null);
					_avatarLoadRequested = false;
					ResetDiscordAnimation();
					PlayerPickMenu.TriggerRealtimeUpdate(force: true);
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private void DrawDashboardTab()
		{
			//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Unknown result type (might be due to invalid IL or missing references)
			//IL_0304: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Invalid comparison between Unknown and I4
			if (!ServerData.IsTabEnabled("dashboard"))
			{
				return;
			}
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			string text = ((DiscordAuthManager.IsLoggedIn && !string.IsNullOrEmpty(DiscordAuthManager.DiscordUsername)) ? DiscordAuthManager.DiscordUsername : ModKeyValidator.ValidatedUsername);
			GUILayout.Label("<size=20><b>Welcome, " + text + "</b></size>", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndVertical();
			GUILayout.Space(10f);
			if (DiscordAuthManager.IsLoggedIn)
			{
				RenderDiscordCardWithAnimations();
				GUILayout.Space(10f);
			}
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical(GuiStyles.DashboardCardStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.Label("\ud83d\udc8e LICENSE INFO", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			string keyTypeDisplay = ModKeyValidator.GetKeyTypeDisplay();
			GUILayout.Label("PLAN: <color=#FFD700><b>" + keyTypeDisplay + "</b></color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (ModKeyValidator.IsPremium)
			{
				if (ModKeyValidator.ExpiresAt.HasValue)
				{
					System.TimeSpan timeSpan = ModKeyValidator.TimeRemaining ?? System.TimeSpan.Zero;
					string text2 = $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
					GUILayout.Label("EXPIRES: <color=#AAAAAA>" + text2 + "</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				}
				else
				{
					GUILayout.Label("EXPIRES: <color=#00FF00>NEVER (Lifetime)</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				}
			}
			else
			{
				GUILayout.Label("VALIDITY: <color=#FFAA00>SESSION ONLY</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("<size=10><color=#888888><i>(Key expires when game closes)</i></color></size>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			GUILayout.Label("KEY: <color=#888888>••••-••••-••••-HIDDEN</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndVertical();
			GUILayout.Space(10f);
			GUILayout.BeginVertical(GuiStyles.DashboardCardStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.Label("\ud83c\udfae GAME STATUS", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			string value = "Offline";
			string value2 = "#ff4444";
			try
			{
				if ((Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted)
				{
					value = "Playing";
					value2 = "#44ff44";
				}
				else if ((Object)(object)AmongUsClient.Instance != (Object)null && (int)((InnerNetClient)AmongUsClient.Instance).GameState == 1)
				{
					value = "In Lobby";
					value2 = "#ffaa44";
				}
				else
				{
					value = "In Menu";
					value2 = "#AAAAAA";
				}
			}
			catch
			{
			}
			GUILayout.Label($"STATUS: <color={value2}><b>{value}</b></color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			string text3 = "None";
			try
			{
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				object obj2;
				if (localPlayer == null)
				{
					obj2 = null;
				}
				else
				{
					NetworkedPlayerInfo data = localPlayer.Data;
					obj2 = ((data != null) ? data.Role : null);
				}
				if ((Object)obj2 != (Object)null)
				{
					text3 = GetEnglishRoleName(PlayerControl.LocalPlayer.Data.Role.Role);
				}
			}
			catch
			{
				try
				{
					PlayerControl localPlayer2 = PlayerControl.LocalPlayer;
					object obj4;
					if (localPlayer2 == null)
					{
						obj4 = null;
					}
					else
					{
						NetworkedPlayerInfo data2 = localPlayer2.Data;
						obj4 = ((data2 != null) ? data2.Role : null);
					}
					if ((Object)obj4 != (Object)null)
					{
						text3 = PlayerControl.LocalPlayer.Data.Role.NiceName;
					}
				}
				catch
				{
				}
			}
			GUILayout.Label("ROLE: <color=#CCCCCC>" + text3 + "</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
						GUILayout.Space(15f);
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("PLAYER BODY", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			bool flag = IsCurrentUserHost();
			GUI.color = (flag ? GuiStyles.Theme.Success : GuiStyles.Theme.Warning);
			GUILayout.Label(flag ? "Host only control: enabled" : "Host only control: disabled", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUI.color = Color.white;
			PlayerBodyTypes[] availableTypes = GameCheats.BodyTypeCheats.AvailableTypes;
			int num = ((availableTypes != null && availableTypes.Length > 0) ? Mathf.Clamp(GameCheats.BodyTypeCheats.CurrentTypeIndex, 0, availableTypes.Length - 1) : 0);
			string bodyTypeText = ((availableTypes != null && availableTypes.Length > 0) ? GameCheats.BodyTypeCheats.GetBodyTypeName(availableTypes[num]) : "Unavailable");
			bool enabled = GUI.enabled;
			GUI.enabled = flag;
			if (GUILayout.Button("Type: " + bodyTypeText + (_showBodyTypeDropdown ? " [OPEN]" : " [CLOSED]"), GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				_showBodyTypeDropdown = !_showBodyTypeDropdown;
			}
			if (_showBodyTypeDropdown && availableTypes != null && availableTypes.Length > 0)
			{
				_bodyTypeDropdownScroll = GUILayout.BeginScrollView(_bodyTypeDropdownScroll, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(110f) });
				for (int i = 0; i < availableTypes.Length; i++)
				{
					PlayerBodyTypes val = availableTypes[i];
					string bodyTypeName = GameCheats.BodyTypeCheats.GetBodyTypeName(val);
					bool selected = i == num;
					GUI.color = (selected ? GuiStyles.Theme.Accent : Color.white);
					if (GUILayout.Button((selected ? "[x] " : "[ ] ") + bodyTypeName, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						GameCheats.BodyTypeCheats.CurrentTypeIndex = i;
						GameCheats.BodyTypeCheats.SetAllPlayersBodyType(val);
						_showBodyTypeDropdown = false;
						ShowNotification("Player Body -> " + bodyTypeName);
					}
				}
				GUI.color = Color.white;
				GUILayout.EndScrollView();
			}
			GUI.enabled = enabled;
			if (!flag)
			{
				GUILayout.Label("<size=10><color=#888888>Only host can change all players body type.</color></size>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				_showBodyTypeDropdown = false;
			}
			GUILayout.EndVertical();
			GUILayout.Space(10f);
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("ℹ\ufe0f ABOUT", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			GUILayout.Label("VERSION: <color=#AAAAAA><b>v6.0.8</b></color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("BUILD: <color=#888888>Among Us Mod Menu • 2026</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			ConfigEntry<KeyCode> menuToggleKey = CheatConfig.MenuToggleKey;
			string text4 = ((object)(KeyCode)((menuToggleKey == null) ? 282 : ((int)menuToggleKey.Value))/*cast due to .constrained prefix*/).ToString();
			GUILayout.Label("<size=10><color=#666666>crewcore.online • Press " + text4 + " to toggle menu</color></size>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndVertical();
		}

		private unsafe string GetEnglishRoleName(RoleTypes role)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected I4, but got Unknown
			return (int)role switch
			{
				1 => "Impostor", 
				5 => "Shapeshifter", 
				9 => "Phantom", 
				18 => "Viper", 
				3 => "Engineer", 
				2 => "Scientist", 
				4 => "Guardian Angel", 
				8 => "Noisemaker", 
				10 => "Tracker", 
				0 => "Crewmate", 
				_ => ((object)(*(RoleTypes*)(&role))/*cast due to .constrained prefix*/).ToString(), 
			};
		}

		private string GetTooltipForTab(string id)
		{
			return id switch
			{
				"dashboard" => "Home & Status", 
				"ban_menu" => "Ban management and lobby settings", 
				"lobbies" => "Find and join game lobbies", 
				"spoofing" => "Level, Platform, Friend Code spoofing", 
				"settings" => "Configuration and preferences", 
				_ => "", 
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool ValidateRenderToken(long token)
		{
			return GhostUI.CheckToken(token);
		}

		private void InitializeActionRegistry()
		{
			_tabDrawRegistry = new System.Collections.Generic.Dictionary<string, System.Action<long>>
			{
				{
					"dashboard",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawDashboardTab();
						}
					}
				},
				{
					"ban_menu",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							global::ModMenuCrew.UI.Menus.BanMenu.Draw();
						}
					}
				},
				{
					"spoofing",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							spoofingMenu?.Draw();
						}
					}
				},
				{
					"game",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawGameTabIMGUI();
						}
					}
				},
				{
					"movement",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawMovementTabIMGUI();
						}
					}
				},
				{
					"sabotage",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawSabotageTabIMGUI();
						}
					}
				},
				{
					"impostor",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawImpostorTabIMGUI();
						}
					}
				},
				{
					"lobbies",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawLobbyListingTabIMGUI();
						}
					}
				},
				{
					"teleport",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							DrawTeleportTabIMGUI();
						}
					}
				},
				{
					"settings",
					delegate(long t)
					{
						if (ValidateRenderToken(t))
						{
							settingsTab?.DrawSettingsTab();
						}
					}
				}
			};
			if (cheatManager != null)
			{
				_tabDrawRegistry["cheats"] = delegate(long t)
				{
					if (ValidateRenderToken(t))
					{
						cheatManager.DrawCheatsTab();
					}
				};
			}
			if (playerPickMenu != null)
			{
				_tabDrawRegistry["playerpick"] = delegate(long t)
				{
					if (ValidateRenderToken(t))
					{
						playerPickMenu.Draw();
					}
				};
			}
			_tabDrawRegistry["replay"] = delegate(long t)
			{
				if (ValidateRenderToken(t))
				{
					ReplayUI.DrawTabContent();
				}
			};
		}

		private void InitializeTabsForGameIMGUI()
		{
			IconLoader.PreloadAll();
			if (_tabDrawRegistry == null)
			{
				InitializeActionRegistry();
			}
		}

		private void DrawTeleportTabFromServer()
		{
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("TELEPORT", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			TabDefinition tabDefinition = ServerData.Tabs?.FirstOrDefault((TabDefinition t) => t.Id == "teleport");
			if (tabDefinition != null)
			{
				foreach (TeleportLocation location in tabDefinition.Locations)
				{
					if (GUILayout.Button(location.Name, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						PlayerControl localPlayer = PlayerControl.LocalPlayer;
						if ((Object)(object)localPlayer != (Object)null)
						{
							localPlayer.NetTransform.RpcSnapTo(new Vector2(location.X, location.Y));
						}
					}
				}
			}
			GUILayout.EndVertical();
		}

		private void DrawSettingsTabFromServer()
		{
			if (!ServerData.IsTabEnabled("settings"))
			{
				return;
			}
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("SETTINGS", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			TabDefinition tabDefinition = ServerData.Tabs?.FirstOrDefault((TabDefinition t) => t.Id == "settings");
			if (tabDefinition != null)
			{
				foreach (SectionDefinition section in tabDefinition.Sections)
				{
					foreach (SliderDefinition slider in section.Sliders)
					{
						float sliderValue = ServerData.GetSliderValue(slider.Id);
						GUILayout.Label($"{slider.Label}: {sliderValue:F1}", (Il2CppReferenceArray<GUILayoutOption>)null);
						sliderValue = GUILayout.HorizontalSlider(sliderValue, slider.Min, slider.Max, System.Array.Empty<GUILayoutOption>());
						ServerData.SetSliderValue(slider.Id, sliderValue);
					}
				}
			}
			GUILayout.EndVertical();
		}

		private void DrawServerDefinedTab(string tabId)
		{
			if (!ServerData.IsTabEnabled(tabId))
			{
				return;
			}
			TabDefinition tabDefinition = ServerData.Tabs?.FirstOrDefault((TabDefinition t) => t.Id == tabId);
			if (tabDefinition == null)
			{
				return;
			}
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label(tabDefinition.Name.ToUpper(), GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			foreach (SectionDefinition section in tabDefinition.Sections)
			{
				if (!string.IsNullOrEmpty(section.VisibleWhen) && section.VisibleWhen == "is_host" && ((Object)(object)AmongUsClient.Instance == (Object)null || !((InnerNetClient)AmongUsClient.Instance).AmHost))
				{
					continue;
				}
				if (!string.IsNullOrEmpty(section.Name))
				{
					GUILayout.Label(section.Name, GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				}
				int num = 0;
				foreach (ButtonDefinition button in section.Buttons)
				{
					if (num % 2 == 0)
					{
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					}
					if (button.Type == "toggle")
					{
						bool toggleState = ServerData.GetToggleState(button.Id);
						bool flag = GUILayout.Toggle(toggleState, button.Label, GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						if (flag != toggleState && button.Enabled)
						{
							ServerData.SetToggleState(button.Id, flag);
						}
					}
					else if (GUILayout.Button(button.Label, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()) && button.Enabled)
					{
						ExecuteServerAction(button.Id);
					}
					num++;
					if (num % 2 == 0)
					{
						GUILayout.EndHorizontal();
					}
				}
				if (num % 2 != 0)
				{
					GUILayout.EndHorizontal();
				}
				foreach (SliderDefinition slider in section.Sliders)
				{
					float sliderValue = ServerData.GetSliderValue(slider.Id);
					GUILayout.Label($"{slider.Label}: {sliderValue:F1}", (Il2CppReferenceArray<GUILayoutOption>)null);
					sliderValue = GUILayout.HorizontalSlider(sliderValue, slider.Min, slider.Max, System.Array.Empty<GUILayoutOption>());
					ServerData.SetSliderValue(slider.Id, sliderValue);
				}
			}
			GUILayout.EndVertical();
		}

		private void ExecuteServerAction(string actionId)
		{
			switch (actionId)
			{
			case "auto_tasks":
				if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null)
				{
					((MonoBehaviour)DestroyableSingleton<HudManager>.Instance).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(GameCheats.CompleteAllTasksWithDelay(0.2f)));
				}
				else
				{
					GameCheats.CompleteAllTasks();
				}
				ShowNotification("Tasks completing...");
				break;
			case "skip_meeting":
				GameCheats.CloseMeeting();
				ShowNotification("Meeting closed!");
				break;
			case "reveal_sus":
				GameCheats.RevealImpostors();
				ShowNotification("Impostors revealed!");
				break;
			case "bypass_scanner":
			{
				HudManager instance = DestroyableSingleton<HudManager>.Instance;
				if (instance != null)
				{
					((MonoBehaviour)instance).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(GameCheats.BypassScannerWithTimeout(12f)));
				}
				ShowNotification("Scanner bypassed!");
				break;
			}
			case "godmode":
				if (CheatConfig.GodMode != null)
				{
					CheatConfig.GodMode.Value = !CheatConfig.GodMode.Value;
				}
				break;
			case "kill_all":
				GameCheats.KillAll();
				break;
			case "kill_crew":
				GameCheats.KillAll(crewOnly: true);
				break;
			case "kill_imps":
				GameCheats.KillAll(crewOnly: false, impostorsOnly: true);
				break;
			case "crew_vent":
				if (CheatConfig.AllowVenting != null)
				{
					CheatConfig.AllowVenting.Value = !CheatConfig.AllowVenting.Value;
				}
				break;
			case "click_tp":
				if (CheatConfig.TeleportWithCursor != null)
				{
					CheatConfig.TeleportWithCursor.Value = !CheatConfig.TeleportWithCursor.Value;
				}
				break;
			case "drone_cam":
				if (CheatConfig.FreeCamEnabled != null)
				{
					CheatConfig.FreeCamEnabled.Value = !CheatConfig.FreeCamEnabled.Value;
				}
				break;
			case "ghost_walk":
				if (CheatConfig.NoClipSmoothEnabled != null)
				{
					CheatConfig.NoClipSmoothEnabled.Value = !CheatConfig.NoClipSmoothEnabled.Value;
				}
				break;
			case "radar":
				if (CheatConfig.RadarEnabled != null)
				{
					CheatConfig.RadarEnabled.Value = !CheatConfig.RadarEnabled.Value;
				}
				break;
			case "tracers":
				if (CheatConfig.TracersEnabled != null)
				{
					CheatConfig.TracersEnabled.Value = !CheatConfig.TracersEnabled.Value;
				}
				break;
			case "see_ghosts":
				if (CheatConfig.SeeGhosts != null)
				{
					CheatConfig.SeeGhosts.Value = !CheatConfig.SeeGhosts.Value;
				}
				break;
			case "dead_chat":
				if (CheatConfig.SeeDeadChat != null)
				{
					CheatConfig.SeeDeadChat.Value = !CheatConfig.SeeDeadChat.Value;
				}
				break;
			case "kill_timer":
				if (CheatConfig.ShowKillCooldowns != null)
				{
					CheatConfig.ShowKillCooldowns.Value = !CheatConfig.ShowKillCooldowns.Value;
				}
				break;
			case "no_shadows":
				if (CheatConfig.NoShadows != null)
				{
					CheatConfig.NoShadows.Value = !CheatConfig.NoShadows.Value;
				}
				break;
			default:
				ShowNotification("Action: " + actionId);
				break;
			}
		}

		private void DrawReplayTabIMGUI()
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0323: Unknown result type (might be due to invalid IL or missing references)
			//IL_033b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0340: Unknown result type (might be due to invalid IL or missing references)
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("REPLAY SYSTEM (BETA)", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			ReplayRecorder instance = ReplayRecorder.Instance;
			ReplayPlayer instance2 = ReplayPlayer.Instance;
			if ((Object)(object)instance2 != (Object)null && instance2.IsPlaying)
			{
				GUI.color = Color.green;
				GUILayout.Label("▶ PLAYING REPLAY", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
				GUILayout.Label($"Time: {instance2.CurrentTime:F1}s / {instance2.TotalDuration:F1}s", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				float num = GUILayout.HorizontalSlider(instance2.CurrentTime, 0f, instance2.TotalDuration, System.Array.Empty<GUILayoutOption>());
				if (Math.Abs(num - instance2.CurrentTime) > 1f)
				{
					instance2.Seek(num);
				}
				GUILayout.Space(5f);
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("0.5x", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.SetSpeed(0.5f);
				}
				if (GUILayout.Button("1x", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.SetSpeed(1f);
				}
				if (GUILayout.Button("2x", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.SetSpeed(2f);
				}
				if (GUILayout.Button("4x", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.SetSpeed(4f);
				}
				if (GUILayout.Button("10x", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.SetSpeed(10f);
				}
				GUILayout.EndHorizontal();
				if (GUILayout.Button("STOP PLAYBACK", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					instance2.Cleanup();
				}
			}
			else if ((Object)(object)instance != (Object)null)
			{
				if (instance.IsRecording)
				{
					GUILayout.Label("<color=#FF0000>● RECORDING...</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.Label($"Captured: {instance.FrameCount} frames", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					if (GUILayout.Button("STOP RECORDING", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						instance.StopRecording();
					}
				}
				else
				{
					if (InteropFix.Cast((Object)(object)AmongUsClient.Instance) && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted)
					{
						GUILayout.Label("RECORDER READY", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						if (GUILayout.Button("START RECORDING", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
						{
							instance.StartRecording();
						}
					}
					else
					{
						GUILayout.Label("Recording available in-game only.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					GUILayout.Space(10f);
					GuiStyles.DrawSeparator();
					GUILayout.Label("SAVED REPLAYS:", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					string text = Path.Combine(Directory.GetCurrentDirectory(), "Replays");
					if (!Directory.Exists(text))
					{
						Directory.CreateDirectory(text);
					}
					if (GUILayout.Button("OPEN REPLAY FOLDER", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						Application.OpenURL(text);
					}
					_replayScroll = GUILayout.BeginScrollView(_replayScroll, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(200f) });
					string[] files = Directory.GetFiles(text, "*.mmc");
					if (files.Length == 0)
					{
						GUILayout.Label("No replays found. Play a match to record!", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					string[] array = files;
					foreach (string path in array)
					{
						string? fileName = Path.GetFileName(path);
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						GUILayout.Label(fileName, GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						if (GUILayout.Button("▶ LOAD", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(60f) }))
						{
							instance2.LoadAndPlay(path);
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndScrollView();
				}
			}
			else
			{
				GUILayout.Label("Replay System Unavailable.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			GUILayout.Space(10f);
			GUILayout.EndVertical();
		}

		private Texture2D CreateDiscordIcon()
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)_discordIconTexture != (Object)null)
				{
					return _discordIconTexture;
				}
				_discordIconTexture = new Texture2D(16, 16);
				Color val = default(Color);
				val._002Ector(0.345f, 0.396f, 0.949f, 1f);
				Color white = Color.white;
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						_discordIconTexture.SetPixel(j, i, val);
					}
				}
				for (int k = 5; k <= 10; k++)
				{
					for (int l = 2; l <= 6; l++)
					{
						_discordIconTexture.SetPixel(l, k, white);
					}
				}
				for (int m = 5; m <= 10; m++)
				{
					for (int n = 9; n <= 13; n++)
					{
						_discordIconTexture.SetPixel(n, m, white);
					}
				}
				_discordIconTexture.Apply();
				return _discordIconTexture;
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error creating Discord icon: {value}"));
				return null;
			}
		}

		private TMP_FontAsset LoadGameFont(string primaryName = null, string fallbackName = null)
		{
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Expected O, but got Unknown
			if ((Object)(object)_cachedFont != (Object)null)
			{
				return _cachedFont;
			}
			try
			{
				Il2CppReferenceArray<Object> val = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
				if (val != null && ((Il2CppArrayBase<Object>)(object)val).Length > 0)
				{
					TMP_FontAsset val2 = null;
					if (!string.IsNullOrWhiteSpace(primaryName))
					{
						foreach (Object item in (Il2CppArrayBase<Object>)(object)val)
						{
							TMP_FontAsset val3 = ((item != (Object)null) ? ((Il2CppObjectBase)item).TryCast<TMP_FontAsset>() : null);
							if ((Object)(object)val3 != (Object)null && ((Object)val3).name.IndexOf(primaryName, System.StringComparison.OrdinalIgnoreCase) >= 0)
							{
								val2 = val3;
								break;
							}
						}
						if ((Object)(object)val2 != (Object)null)
						{
							return _cachedFont = val2;
						}
					}
					if (!string.IsNullOrWhiteSpace(fallbackName))
					{
						foreach (Object item2 in (Il2CppArrayBase<Object>)(object)val)
						{
							TMP_FontAsset val4 = ((item2 != (Object)null) ? ((Il2CppObjectBase)item2).TryCast<TMP_FontAsset>() : null);
							if ((Object)(object)val4 != (Object)null && ((Object)val4).name.IndexOf(fallbackName, System.StringComparison.OrdinalIgnoreCase) >= 0)
							{
								val2 = val4;
								break;
							}
						}
						if ((Object)(object)val2 != (Object)null)
						{
							return _cachedFont = val2;
						}
					}
					foreach (Object item3 in (Il2CppArrayBase<Object>)(object)val)
					{
						TMP_FontAsset val5 = ((item3 != (Object)null) ? ((Il2CppObjectBase)item3).TryCast<TMP_FontAsset>() : null);
						if ((Object)(object)val5 != (Object)null)
						{
							return _cachedFont = val5;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModMenuCrewPlugin instance = Instance;
				if (instance != null)
				{
					ManualLogSource log = ((BasePlugin)instance).Log;
					if (log != null)
					{
						bool flag = default(bool);
						BepInExWarningLogInterpolatedStringHandler val6 = new BepInExWarningLogInterpolatedStringHandler(30, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("[UI] Could not load TMP font: ");
							((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(ex.Message);
						}
						log.LogWarning(val6);
					}
				}
			}
			return null;
		}

		private void SetupActivationUI_TMP(bool forceRebuild = false)
		{
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Expected O, but got Unknown
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Expected O, but got Unknown
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0205: Expected O, but got Unknown
			//IL_027a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0284: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_030c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0321: Unknown result type (might be due to invalid IL or missing references)
			//IL_0336: Unknown result type (might be due to invalid IL or missing references)
			//IL_034b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0356: Unknown result type (might be due to invalid IL or missing references)
			//IL_036c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0372: Expected O, but got Unknown
			//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0413: Unknown result type (might be due to invalid IL or missing references)
			//IL_0422: Unknown result type (might be due to invalid IL or missing references)
			//IL_0427: Unknown result type (might be due to invalid IL or missing references)
			//IL_0434: Unknown result type (might be due to invalid IL or missing references)
			//IL_044e: Unknown result type (might be due to invalid IL or missing references)
			//IL_046b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0481: Unknown result type (might be due to invalid IL or missing references)
			//IL_0497: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0519: Unknown result type (might be due to invalid IL or missing references)
			//IL_052e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0543: Unknown result type (might be due to invalid IL or missing references)
			//IL_0558: Unknown result type (might be due to invalid IL or missing references)
			//IL_0562: Unknown result type (might be due to invalid IL or missing references)
			//IL_0578: Unknown result type (might be due to invalid IL or missing references)
			//IL_0587: Unknown result type (might be due to invalid IL or missing references)
			//IL_0596: Unknown result type (might be due to invalid IL or missing references)
			//IL_05eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0609: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Expected O, but got Unknown
			//IL_0660: Unknown result type (might be due to invalid IL or missing references)
			//IL_066f: Unknown result type (might be due to invalid IL or missing references)
			//IL_067e: Unknown result type (might be due to invalid IL or missing references)
			//IL_06c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_06d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0728: Unknown result type (might be due to invalid IL or missing references)
			//IL_0742: Unknown result type (might be due to invalid IL or missing references)
			//IL_0780: Unknown result type (might be due to invalid IL or missing references)
			//IL_078f: Unknown result type (might be due to invalid IL or missing references)
			//IL_07d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0806: Unknown result type (might be due to invalid IL or missing references)
			//IL_080b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0818: Unknown result type (might be due to invalid IL or missing references)
			//IL_0832: Unknown result type (might be due to invalid IL or missing references)
			//IL_084c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0860: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0954: Unknown result type (might be due to invalid IL or missing references)
			//IL_0963: Unknown result type (might be due to invalid IL or missing references)
			//IL_0927: Unknown result type (might be due to invalid IL or missing references)
			//IL_09cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a11: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a49: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a58: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ad3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ada: Expected O, but got Unknown
			//IL_0afd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b13: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b29: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b3f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b55: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b64: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b69: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b7c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b9a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bb4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bc9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bde: Unknown result type (might be due to invalid IL or missing references)
			//IL_0be9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bfd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c14: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c1b: Expected O, but got Unknown
			//IL_0c42: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c5e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c73: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c88: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c93: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ca7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ccd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ce1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d13: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d22: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d31: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d67: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d76: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d85: Unknown result type (might be due to invalid IL or missing references)
			//IL_0da8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0daf: Expected O, but got Unknown
			//IL_0de4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e00: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e15: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e2a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e3f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e53: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e6a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e6f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e95: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ea9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ac4: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)activationCanvasTMP != (Object)null)
				{
					if (!forceRebuild)
					{
						((Component)activationCanvasTMP).gameObject.SetActive(false);
						return;
					}
					if ((Object)(object)canvasGO != (Object)null)
					{
						Object.Destroy((Object)(object)canvasGO);
					}
					activationCanvasTMP = null;
					activationPanelGO = null;
					statusMessageTextTMP = null;
					validateButtonTMP = null;
					getKeyButtonTMP = null;
					validateButtonTextTMP = null;
					apiKeyInputFieldTMP = null;
					_copyLinkBtnTMP = null;
					_copyLinkTextTMP = null;
				}
				((BasePlugin)Instance).Log.LogInfo((object)"Configuring Activation UI with CYBERPUNK Theme...");
				if ((Object)(object)Object.FindObjectOfType<EventSystem>() == (Object)null)
				{
					if ((Object)(object)eventSystemGO == (Object)null)
					{
						eventSystemGO = new GameObject("ModMenuCrew_EventSystem");
						eventSystemGO.AddComponent<EventSystem>();
						eventSystemGO.AddComponent<StandaloneInputModule>();
					}
				}
				else if ((Object)(object)eventSystemGO != (Object)null)
				{
					Object.Destroy((Object)(object)eventSystemGO);
					eventSystemGO = null;
				}
				canvasGO = new GameObject("ModMenuCrew_ActivationCanvas");
				Object.DontDestroyOnLoad((Object)(object)canvasGO);
				activationCanvasTMP = canvasGO.AddComponent<Canvas>();
				activationCanvasTMP.renderMode = (RenderMode)0;
				activationCanvasTMP.sortingOrder = 32767;
				CanvasScaler obj = canvasGO.AddComponent<CanvasScaler>();
				obj.uiScaleMode = (CanvasScaler.ScaleMode)1;
				obj.referenceResolution = new Vector2(1920f, 1080f);
				canvasGO.AddComponent<GraphicRaycaster>();
				GameObject val = new GameObject("DarkOverlay");
				val.transform.SetParent(((Component)activationCanvasTMP).transform, false);
				((Graphic)val.AddComponent<Image>()).color = new Color(0.02f, 0.02f, 0.04f, 0.3f);
				RectTransform component = val.GetComponent<RectTransform>();
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.sizeDelta = Vector2.zero;
				_bootOverlay = val;
				CreateBootConsole(component);
				CreateScanLine(component);
				activationPanelGO = new GameObject("ActivationPanel");
				activationPanelGO.transform.SetParent(((Component)activationCanvasTMP).transform, false);
				activationPanelGO.SetActive(false);
				_panelCanvasGroup = activationPanelGO.AddComponent<CanvasGroup>();
				_panelCanvasGroup.alpha = 0f;
				_currentAlpha = 0f;
				_targetAlpha = 1f;
				_panelScaleCurrent = 0.92f;
				activationPanelGO.transform.localScale = Vector3.one * 0.92f;
				((Graphic)activationPanelGO.AddComponent<Image>()).color = new Color(0.04f, 0.04f, 0.06f, 0.98f);
				Outline obj2 = activationPanelGO.AddComponent<Outline>();
				((Shadow)obj2).effectColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);
				((Shadow)obj2).effectDistance = new Vector2(1f, -1f);
				RectTransform component2 = activationPanelGO.GetComponent<RectTransform>();
				component2.anchorMin = new Vector2(0.5f, 0.5f);
				component2.anchorMax = new Vector2(0.5f, 0.5f);
				component2.pivot = new Vector2(0.5f, 0.5f);
				component2.sizeDelta = new Vector2(660f, 510f);
				component2.anchoredPosition = Vector2.zero;
				CreateTechCorners(component2);
				GameObject val2 = new GameObject("AccentTop");
				val2.transform.SetParent((Transform)(object)component2, false);
				_accentTopLine = val2.AddComponent<Image>();
				((Graphic)_accentTopLine).color = new Color(0.2f, 1f, 1f, 0.7f);
				RectTransform component3 = val2.GetComponent<RectTransform>();
				component3.anchorMin = new Vector2(0f, 1f);
				component3.anchorMax = new Vector2(1f, 1f);
				component3.pivot = new Vector2(0.5f, 1f);
				component3.sizeDelta = new Vector2(-10f, 2f);
				component3.anchoredPosition = new Vector2(0f, -1f);
				GameObject val3 = new GameObject("HeaderBar");
				val3.transform.SetParent((Transform)(object)component2, false);
				((Graphic)val3.AddComponent<Image>()).color = new Color(0.06f, 0.06f, 0.09f, 1f);
				RectTransform component4 = val3.GetComponent<RectTransform>();
				component4.anchorMin = new Vector2(0f, 1f);
				component4.anchorMax = new Vector2(1f, 1f);
				component4.pivot = new Vector2(0.5f, 1f);
				component4.sizeDelta = new Vector2(0f, 58f);
				component4.anchoredPosition = new Vector2(0f, -3f);
				GameObject val4 = new GameObject("HeaderBottomLine");
				val4.transform.SetParent((Transform)(object)component4, false);
				((Graphic)val4.AddComponent<Image>()).color = new Color(0.2f, 0.8f, 1f, 0.25f);
				RectTransform component5 = val4.GetComponent<RectTransform>();
				component5.anchorMin = new Vector2(0.05f, 0f);
				component5.anchorMax = new Vector2(0.95f, 0f);
				component5.pivot = new Vector2(0.5f, 0f);
				component5.sizeDelta = new Vector2(0f, 1f);
				component5.anchoredPosition = Vector2.zero;
				_titleTextTMP = CreateTMPText(component4, _originalTitle, 20, Color.white, new Vector2(0f, -6f), new Vector2(620f, 44f), (TextAlignmentOptions)514);
				((TMP_Text)_titleTextTMP).fontStyle = (FontStyles)1;
				((TMP_Text)_titleTextTMP).characterSpacing = 3f;
				_staggerElements.Clear();
				statusMessageTextTMP = CreateTMPText(component2, currentActivationStatusMessage, 14, new Color(0.9f, 0.9f, 0.9f), new Vector2(0f, 148f), new Vector2(540f, 30f), (TextAlignmentOptions)514);
				if ((Object)(object)statusMessageTextTMP != (Object)null)
				{
					((TMP_Text)statusMessageTextTMP).enableWordWrapping = true;
					WrapInStaggerGroup(((Component)statusMessageTextTMP).gameObject);
				}
				TextMeshProUGUI val5 = CreateTMPText(component2, "LICENSE KEY", 10, new Color(0.2f, 0.8f, 1f), new Vector2(0f, 115f), new Vector2(500f, 22f), (TextAlignmentOptions)514);
				if ((Object)(object)val5 != (Object)null)
				{
					((TMP_Text)val5).characterSpacing = 6f;
				}
				WrapInStaggerGroup(((Component)val5).gameObject);
				apiKeyInputFieldTMP = CreateTMPInputField(component2, "", "Paste your license key here...", new Vector2(0f, 78f), new Vector2(500f, 55f));
				_inputFieldBgImage = ((Component)apiKeyInputFieldTMP).GetComponent<Image>();
				_inputFieldOutline = ((Component)apiKeyInputFieldTMP).gameObject.AddComponent<Outline>();
				((Shadow)_inputFieldOutline).effectColor = new Color(0.2f, 0.7f, 1f, 0f);
				((Shadow)_inputFieldOutline).effectDistance = new Vector2(2f, -2f);
				WrapInStaggerGroup(((Component)apiKeyInputFieldTMP).gameObject);
				validateButtonTMP = CreateTMPButton(component2, "ACTIVATE", InteropFix.Cast((Action)delegate
				{
					if (!isValidatingNow)
					{
						if (!DiscordAuthManager.IsLoggedIn)
						{
							currentActivationStatusMessage = "Login with Discord first!";
							if (InteropFix.Cast((Object)(object)statusMessageTextTMP))
							{
								((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
							}
						}
						else if ((Object)(object)apiKeyInputFieldTMP != (Object)null)
						{
							ProcessApiKeyValidation(apiKeyInputFieldTMP.text);
						}
					}
				}), new Vector2(-130f, 15f), new Vector2(240f, 48f));
				WrapInStaggerGroup(((Component)validateButtonTMP).gameObject);
				getKeyButtonTMP = CreateTMPButton(component2, "GET FREE KEY", InteropFix.Cast((Action)delegate
				{
					if (!OpenBrowser("https://crewcore.online/"))
					{
						currentActivationStatusMessage = "Could not open browser. Visit crewcore.online manually.";
						if (InteropFix.Cast((Object)(object)statusMessageTextTMP))
						{
							((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
						}
						ManageActivationUIVisibility();
					}
				}), new Vector2(130f, 15f), new Vector2(240f, 48f));
				WrapInStaggerGroup(((Component)getKeyButtonTMP).gameObject);
				GameObject val6 = new GameObject("SectionDivider");
				val6.transform.SetParent((Transform)(object)component2, false);
				((Graphic)val6.AddComponent<Image>()).color = new Color(0.2f, 0.2f, 0.25f, 0.35f);
				RectTransform component6 = val6.GetComponent<RectTransform>();
				component6.anchoredPosition = new Vector2(0f, -22f);
				component6.sizeDelta = new Vector2(560f, 1f);
				Button val7 = CreateTMPButton(component2, DiscordAuthManager.IsLoggedIn ? ("Discord: " + DiscordAuthManager.DiscordUsername) : "Login with Discord", InteropFix.Cast((Action)delegate
				{
					if (!isValidatingNow)
					{
						if (DiscordAuthManager.IsLoggedIn)
						{
							DiscordAuthManager.Logout();
							_avatarLoadRequested = false;
							if ((Object)(object)_discordAvatarImage != (Object)null)
							{
								((Component)_discordAvatarImage).gameObject.SetActive(false);
							}
							currentActivationStatusMessage = "Logged out from Discord.";
							if (InteropFix.Cast((Object)(object)statusMessageTextTMP))
							{
								((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
							}
							if (InteropFix.Cast((Object)(object)_discordLoginTextTMP))
							{
								((TMP_Text)_discordLoginTextTMP).text = "Login with Discord";
							}
						}
						else if (!DiscordAuthManager.IsLoggingIn)
						{
							currentActivationStatusMessage = "Opening browser for Discord login...";
							if (InteropFix.Cast((Object)(object)statusMessageTextTMP))
							{
								((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
							}
							DiscordAuthManager.StartLoginAsync();
						}
					}
				}), new Vector2(-100f, -60f), new Vector2(200f, 36f));
				WrapInStaggerGroup(((Component)val7).gameObject);
				_discordLoginBtnTMP = val7;
				_discordLoginTextTMP = ((Component)val7).GetComponentInChildren<TextMeshProUGUI>();
				if ((Object)(object)_discordLoginTextTMP != (Object)null)
				{
					((TMP_Text)_discordLoginTextTMP).fontSize = 12f;
				}
				Image component7 = ((Component)val7).GetComponent<Image>();
				if (InteropFix.Cast((Object)(object)component7))
				{
					((Graphic)component7).color = new Color(0.34f, 0.4f, 0.95f, 0.9f);
				}
				_copyLinkBtnTMP = CreateTMPButton(component2, "Copy Login Link", InteropFix.Cast((Action)delegate
				{
					//IL_0057: Unknown result type (might be due to invalid IL or missing references)
					string lastAuthUrl = DiscordAuthManager.LastAuthUrl;
					if (!string.IsNullOrEmpty(lastAuthUrl))
					{
						try
						{
							GUIUtility.systemCopyBuffer = lastAuthUrl;
						}
						catch
						{
						}
						currentActivationStatusMessage = "Link copied! Paste it in your browser.";
						if (InteropFix.Cast((Object)(object)statusMessageTextTMP))
						{
							((TMP_Text)statusMessageTextTMP).text = currentActivationStatusMessage;
							((Graphic)statusMessageTextTMP).color = new Color(0.3f, 1f, 0.3f);
						}
						if ((Object)(object)_copyLinkTextTMP != (Object)null)
						{
							((TMP_Text)_copyLinkTextTMP).text = "Copied!";
						}
					}
				}), new Vector2(0f, -100f), new Vector2(280f, 32f));
				_copyLinkTextTMP = ((Component)_copyLinkBtnTMP).GetComponentInChildren<TextMeshProUGUI>();
				if ((Object)(object)_copyLinkTextTMP != (Object)null)
				{
					((TMP_Text)_copyLinkTextTMP).fontSize = 11f;
				}
				Image component8 = ((Component)_copyLinkBtnTMP).GetComponent<Image>();
				if (InteropFix.Cast((Object)(object)component8))
				{
					((Graphic)component8).color = new Color(0.1f, 0.6f, 0.6f, 0.9f);
				}
				((Component)_copyLinkBtnTMP).gameObject.SetActive(false);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Key: [");
				ConfigEntry<KeyCode> menuToggleKey = CheatConfig.MenuToggleKey;
				defaultInterpolatedStringHandler.AppendFormatted<KeyCode>((KeyCode)((menuToggleKey == null) ? 282 : ((int)menuToggleKey.Value)));
				defaultInterpolatedStringHandler.AppendLiteral("]");
				Button val8 = CreateTMPButton(component2, defaultInterpolatedStringHandler.ToStringAndClear(), InteropFix.Cast((Action)delegate
				{
					if (!isValidatingNow)
					{
						_isBindingToggleKey = true;
						if ((Object)(object)_toggleKeyTextTMP != (Object)null)
						{
							((TMP_Text)_toggleKeyTextTMP).text = "Press Any Key...";
						}
					}
				}), new Vector2(130f, -60f), new Vector2(180f, 36f));
				WrapInStaggerGroup(((Component)val8).gameObject);
				_toggleKeyTextTMP = ((Component)val8).GetComponentInChildren<TextMeshProUGUI>();
				if ((Object)(object)_toggleKeyTextTMP != (Object)null)
				{
					((TMP_Text)_toggleKeyTextTMP).fontSize = 12f;
				}
				Image component9 = ((Component)val8).GetComponent<Image>();
				if (InteropFix.Cast((Object)(object)component9))
				{
					((Graphic)component9).color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
				}
				GameObject val9 = new GameObject("DiscordAvatarContainer");
				val9.transform.SetParent((Transform)(object)component2, false);
				RectTransform val10 = val9.AddComponent<RectTransform>();
				val10.anchorMin = new Vector2(0.5f, 0.5f);
				val10.anchorMax = new Vector2(0.5f, 0.5f);
				val10.pivot = new Vector2(0.5f, 0.5f);
				val10.anchoredPosition = new Vector2(-230f, -60f);
				val10.sizeDelta = new Vector2(40f, 40f);
				GameObject val11 = new GameObject("AvatarGlow");
				val11.transform.SetParent(val9.transform, false);
				Image val12 = val11.AddComponent<Image>();
				((Graphic)val12).color = new Color(0.345f, 0.396f, 0.949f, 0.5f);
				RectTransform component10 = val11.GetComponent<RectTransform>();
				component10.anchorMin = new Vector2(0.5f, 0.5f);
				component10.anchorMax = new Vector2(0.5f, 0.5f);
				component10.pivot = new Vector2(0.5f, 0.5f);
				component10.anchoredPosition = Vector2.zero;
				component10.sizeDelta = new Vector2(46f, 46f);
				_activationAvatarGlow = val12;
				GameObject val13 = new GameObject("DiscordAvatar");
				val13.transform.SetParent(val9.transform, false);
				_discordAvatarImage = val13.AddComponent<RawImage>();
				((Graphic)_discordAvatarImage).color = Color.white;
				RectTransform component11 = val13.GetComponent<RectTransform>();
				component11.anchorMin = new Vector2(0.5f, 0.5f);
				component11.anchorMax = new Vector2(0.5f, 0.5f);
				component11.pivot = new Vector2(0.5f, 0.5f);
				component11.anchoredPosition = Vector2.zero;
				component11.sizeDelta = new Vector2(36f, 36f);
				Outline obj3 = val13.AddComponent<Outline>();
				((Shadow)obj3).effectColor = new Color(0.345f, 0.396f, 0.949f, 0.8f);
				((Shadow)obj3).effectDistance = new Vector2(2f, -2f);
				val9.SetActive(false);
				_activationAvatarContainer = val10;
				TextMeshProUGUI val14 = CreateTMPText(component2, ">> FREE KEYS @ crewcore.online <<", 10, new Color(0.3f, 0.65f, 0.85f), new Vector2(0f, -108f), new Vector2(500f, 22f), (TextAlignmentOptions)514);
				WrapInStaggerGroup(((Component)val14).gameObject);
				TextMeshProUGUI val15 = CreateTMPText(component2, "MODMENUCREW v6.0.8 // CREWCORE.ONLINE", 9, new Color(0.4f, 0.4f, 0.5f), new Vector2(0f, -135f), new Vector2(450f, 20f), (TextAlignmentOptions)514);
				WrapInStaggerGroup(((Component)val15).gameObject);
				GameObject val16 = new GameObject("AccentBottom");
				val16.transform.SetParent((Transform)(object)component2, false);
				_accentBottomLine = val16.AddComponent<Image>();
				((Graphic)_accentBottomLine).color = new Color(0.2f, 1f, 1f, 0.7f);
				RectTransform component12 = val16.GetComponent<RectTransform>();
				component12.anchorMin = new Vector2(0f, 0f);
				component12.anchorMax = new Vector2(1f, 0f);
				component12.pivot = new Vector2(0.5f, 0f);
				component12.sizeDelta = new Vector2(-10f, 2f);
				component12.anchoredPosition = new Vector2(0f, 1f);
				StyleButton(validateButtonTMP, ref _cachedValidateBtnSprite, GuiStyles.Theme.Accent, GuiStyles.Theme.Secondary);
				StyleButton(getKeyButtonTMP, ref _cachedGetKeyBtnSprite, new Color(0.18f, 0.18f, 0.28f), new Color(0.08f, 0.08f, 0.16f));
				validateButtonTextTMP = ((Component)validateButtonTMP).GetComponentInChildren<TextMeshProUGUI>();
				if ((Object)(object)validateButtonTMP != (Object)null)
				{
					_validateBtnImage = ((Component)validateButtonTMP).GetComponent<Image>();
					_validateBtnOutline = ((Component)validateButtonTMP).GetComponent<Outline>();
				}
				((Component)activationCanvasTMP).gameObject.SetActive(false);
				((BasePlugin)Instance).Log.LogInfo((object)"Activation UI TMP created (Premium Redesign) + Cached Components.");
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error SetupActivationUI_TMP: {value}"));
			}
		}

		private void UpdateToggleKeyText()
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)_toggleKeyTextTMP != (Object)null)
				{
					TextMeshProUGUI toggleKeyTextTMP = _toggleKeyTextTMP;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Menu Key: [");
					ConfigEntry<KeyCode> menuToggleKey = CheatConfig.MenuToggleKey;
					defaultInterpolatedStringHandler.AppendFormatted<KeyCode>((KeyCode)((menuToggleKey == null) ? 282 : ((int)menuToggleKey.Value)));
					defaultInterpolatedStringHandler.AppendLiteral("]");
					((TMP_Text)toggleKeyTextTMP).text = defaultInterpolatedStringHandler.ToStringAndClear();
				}
			}
			catch
			{
			}
		}

		private Sprite GetWhiteSprite()
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_cachedWhiteSprite == (Object)null)
			{
				_cachedWhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 100f, 0u, (SpriteMeshType)0);
			}
			return _cachedWhiteSprite;
		}

		private TextMeshProUGUI CreateTMPText(RectTransform parent, string text, int fontSize, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment = (TextAlignmentOptions)513)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Expected O, but got Unknown
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("TMPText_" + Guid.NewGuid().ToString().Substring(0, 8));
			val.transform.SetParent((Transform)(object)parent, false);
			TextMeshProUGUI obj = val.AddComponent<TextMeshProUGUI>();
			((TMP_Text)obj).text = text;
			((TMP_Text)obj).fontSize = fontSize;
			((Graphic)obj).color = color;
			((TMP_Text)obj).alignment = alignment;
			((TMP_Text)obj).font = LoadGameFont();
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchoredPosition = anchoredPosition;
			component.sizeDelta = sizeDelta;
			return obj;
		}

		private TMP_InputField CreateTMPInputField(RectTransform parent, string initialText, string placeholderText, Vector2 anchoredPosition, Vector2 sizeDelta)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_016f: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("TMP_InputField_Activation");
			val.transform.SetParent((Transform)(object)parent, false);
			Image obj = val.AddComponent<Image>();
			((Graphic)obj).color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
			obj.sprite = GetWhiteSprite();
			TMP_InputField val2 = val.AddComponent<TMP_InputField>();
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchoredPosition = anchoredPosition;
			component.sizeDelta = sizeDelta;
			GameObject val3 = new GameObject("Text Area");
			val3.transform.SetParent((Transform)(object)component, false);
			RectTransform val4 = val3.AddComponent<RectTransform>();
			val4.anchorMin = Vector2.zero;
			val4.anchorMax = Vector2.one;
			val4.offsetMin = new Vector2(10f, 5f);
			val4.offsetMax = new Vector2(-10f, -5f);
			val3.AddComponent<RectMask2D>();
			TMP_FontAsset font = LoadGameFont();
			GameObject val5 = new GameObject("Placeholder");
			val5.transform.SetParent((Transform)(object)val4, false);
			TextMeshProUGUI val6 = val5.AddComponent<TextMeshProUGUI>();
			((TMP_Text)val6).text = placeholderText;
			((TMP_Text)val6).fontSize = 16f;
			((TMP_Text)val6).fontStyle = (FontStyles)2;
			((Graphic)val6).color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
			((TMP_Text)val6).alignment = (TextAlignmentOptions)513;
			((TMP_Text)val6).font = font;
			RectTransform component2 = val5.GetComponent<RectTransform>();
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.sizeDelta = Vector2.zero;
			GameObject val7 = new GameObject("Text");
			val7.transform.SetParent((Transform)(object)val4, false);
			TextMeshProUGUI val8 = val7.AddComponent<TextMeshProUGUI>();
			((TMP_Text)val8).text = initialText;
			((TMP_Text)val8).fontSize = 18f;
			((Graphic)val8).color = Color.white;
			((TMP_Text)val8).alignment = (TextAlignmentOptions)513;
			((TMP_Text)val8).font = font;
			RectTransform component3 = val7.GetComponent<RectTransform>();
			component3.anchorMin = Vector2.zero;
			component3.anchorMax = Vector2.one;
			component3.sizeDelta = Vector2.zero;
			val2.textViewport = val4;
			val2.textComponent = (TMP_Text)(object)val8;
			val2.placeholder = (Graphic)(object)val6;
			return val2;
		}

		private Button CreateTMPButton(RectTransform parent, string buttonText, UnityAction onClickAction, Vector2 anchoredPosition, Vector2 sizeDelta)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("TMPButton_" + buttonText.Replace(" ", ""));
			val.transform.SetParent((Transform)(object)parent, false);
			Image val2 = val.AddComponent<Image>();
			val2.sprite = GetWhiteSprite();
			Button val3 = val.AddComponent<Button>();
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchoredPosition = anchoredPosition;
			component.sizeDelta = sizeDelta;
			GameObject val4 = new GameObject("Text (TMP)");
			val4.transform.SetParent((Transform)(object)component, false);
			TextMeshProUGUI obj = val4.AddComponent<TextMeshProUGUI>();
			((TMP_Text)obj).text = buttonText;
			((TMP_Text)obj).fontSize = 18f;
			((Graphic)obj).color = Color.white;
			((TMP_Text)obj).alignment = (TextAlignmentOptions)514;
			((TMP_Text)obj).font = LoadGameFont();
			RectTransform component2 = val4.GetComponent<RectTransform>();
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.sizeDelta = Vector2.zero;
			((Selectable)val3).targetGraphic = (Graphic)(object)val2;
			((UnityEvent)val3.onClick).AddListener(onClickAction);
			return val3;
		}

		private void WrapInStaggerGroup(GameObject go)
		{
			if (!((Object)(object)go == (Object)null))
			{
				CanvasGroup val = go.GetComponent<CanvasGroup>();
				if ((Object)(object)val == (Object)null)
				{
					val = go.AddComponent<CanvasGroup>();
				}
				val.alpha = 0f;
				_staggerElements.Add(val);
			}
		}

		private void CreateScanLine(RectTransform parent)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("ScanLine");
			val.transform.SetParent((Transform)(object)parent, false);
			_scanLineRT = val.AddComponent<RectTransform>();
			_scanLineRT.anchorMin = new Vector2(0f, 1f);
			_scanLineRT.anchorMax = new Vector2(1f, 1f);
			_scanLineRT.sizeDelta = new Vector2(0f, 2f);
			_scanLineRT.anchoredPosition = new Vector2(0f, 0f);
			((Graphic)val.AddComponent<Image>()).color = new Color(1f, 1f, 1f, 0.15f);
		}

		private void UpdateScanningLine()
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)_scanLineRT == (Object)null))
			{
				float num = 200f;
				float num2 = ((Screen.height > 0) ? ((float)Screen.height) : 600f);
				if ((Object)(object)activationCanvasTMP != (Object)null)
				{
					Rect rect = ((Component)activationCanvasTMP).GetComponent<RectTransform>().rect;
					num2 = rect.height;
				}
				Vector2 anchoredPosition = _scanLineRT.anchoredPosition;
				anchoredPosition.y -= num * Time.deltaTime;
				if (anchoredPosition.y < 0f - num2)
				{
					anchoredPosition.y = 0f;
				}
				_scanLineRT.anchoredPosition = anchoredPosition;
			}
		}

		private void FinishBootSequence()
		{
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			_bootSequenceComplete = true;
			if ((Object)(object)_bootConsoleGO != (Object)null)
			{
				_bootConsoleGO.SetActive(false);
			}
			if ((Object)(object)activationPanelGO != (Object)null)
			{
				activationPanelGO.SetActive(true);
			}
			if ((Object)(object)_discordAvatarImage != (Object)null && (Object)(object)_discordAvatarImage.texture != (Object)null && (Object)(object)_activationAvatarContainer != (Object)null)
			{
				((Component)_activationAvatarContainer).gameObject.SetActive(true);
				((Transform)_activationAvatarContainer).localScale = Vector3.one;
				_activationAvatarAnimStart = -1f;
			}
			_revealStartTime = Time.realtimeSinceStartup;
			_revealComplete = false;
			_panelScaleCurrent = 0.92f;
			_currentAlpha = 0f;
			if (InteropFix.Cast((Object)(object)_panelCanvasGroup))
			{
				_panelCanvasGroup.alpha = 0f;
			}
		}

		private void UpdateBootSequence()
		{
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_035d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0407: Unknown result type (might be due to invalid IL or missing references)
			if (_bootSequenceComplete || (Object)(object)_bootConsoleText == (Object)null)
			{
				return;
			}
			float num = Time.realtimeSinceStartup - _bootStartTime;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if ((Object)(object)_bootConsoleCanvasGroup != (Object)null)
			{
				float alpha = 1f - (Mathf.Sin(realtimeSinceStartup * 60f) * 0.008f + Mathf.Sin(realtimeSinceStartup * 127f) * 0.004f);
				_bootConsoleCanvasGroup.alpha = alpha;
			}
			if ((Object)(object)_bootScanlineOverlay != (Object)null)
			{
				RectTransform component = ((Component)_bootScanlineOverlay).GetComponent<RectTransform>();
				Transform parent = ((Component)component).transform.parent;
				RectTransform val = (RectTransform)(object)((parent is RectTransform) ? parent : null);
				if ((Object)(object)val != (Object)null)
				{
					Rect rect = val.rect;
					float height = rect.height;
					if (height > 0f)
					{
						float num2 = realtimeSinceStartup * 90f % height;
						component.anchoredPosition = new Vector2(0f, num2);
					}
				}
			}
			_bootCursorBlinkTimer += Time.deltaTime;
			string text = ((_bootCursorBlinkTimer % 0.6f < 0.35f) ? "<color=#00FFD0>_</color>" : " ");
			float num3 = ((_bootLines.Length != 0) ? ((float)_bootLineIndex / (float)_bootLines.Length) : 0f);
			if ((Object)(object)_bootProgressFill != (Object)null)
			{
				RectTransform component2 = ((Component)_bootProgressFill).GetComponent<RectTransform>();
				float x = component2.anchorMax.x;
				float num4 = Mathf.Clamp01(num3);
				float num5 = Mathf.Lerp(x, num4, Time.deltaTime * 8f);
				component2.anchorMax = new Vector2(num5, 1f);
				float num6 = 0.85f + Mathf.Sin(realtimeSinceStartup * 4f) * 0.15f;
				((Graphic)_bootProgressFill).color = new Color(0.2f * num6, 1f * num6, 0.85f * num6, 0.9f);
			}
			if ((Object)(object)_bootPercentLabel != (Object)null)
			{
				((TMP_Text)_bootPercentLabel).text = Mathf.RoundToInt(num3 * 100f) + "%";
			}
			if ((Object)(object)_bootPhaseLabel != (Object)null)
			{
				string text2 = ((_bootLineIndex < 7) ? "INITIALIZING" : ((_bootLineIndex < 11) ? "SCANNING" : ((_bootLineIndex < 16) ? "HOOKING" : ((_bootLineIndex >= 20) ? "FINALIZING" : "BYPASSING EAC"))));
				string[] array = new string[4] { "|", "/", "-", "\\" };
				if (realtimeSinceStartup - _bootLastSpinnerTime > 0.1f)
				{
					_bootSpinnerIdx = (_bootSpinnerIdx + 1) % array.Length;
					_bootLastSpinnerTime = realtimeSinceStartup;
				}
				if (num3 < 1f)
				{
					((TMP_Text)_bootPhaseLabel).text = "<color=#33FFFF>[" + array[_bootSpinnerIdx] + "]</color> " + text2;
				}
				else
				{
					((TMP_Text)_bootPhaseLabel).text = "<color=#00FF55>[*]</color> COMPLETE";
				}
			}
			if (num > 0.8f && (Object)(object)_bootSkipHintText != (Object)null)
			{
				_bootSkipAllowed = true;
				_bootSkipHintAlpha = Mathf.MoveTowards(_bootSkipHintAlpha, 1f, Time.deltaTime * 3f);
				((Graphic)_bootSkipHintText).color = new Color(0.4f, 0.4f, 0.5f, _bootSkipHintAlpha * 0.6f);
			}
			if (_bootSkipAllowed && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
			{
				while (_bootLineIndex < _bootLines.Length)
				{
					_bootDisplayedText = _bootDisplayedText + _bootLines[_bootLineIndex] + "\n";
					_bootLineIndex++;
				}
				if ((Object)(object)_bootConsoleText != (Object)null)
				{
					((TMP_Text)_bootConsoleText).text = _bootDisplayedText;
				}
				if ((Object)(object)_bootProgressFill != (Object)null)
				{
					((Component)_bootProgressFill).GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
				}
				if ((Object)(object)_bootPercentLabel != (Object)null)
				{
					((TMP_Text)_bootPercentLabel).text = "100%";
				}
				if ((Object)(object)_bootPhaseLabel != (Object)null)
				{
					((TMP_Text)_bootPhaseLabel).text = "<color=#00FF55>[*]</color> COMPLETE";
				}
				if ((Object)(object)_bootConsoleCanvasGroup != (Object)null)
				{
					_bootConsoleCanvasGroup.alpha = 1f;
				}
				FinishBootSequence();
				return;
			}
			while (realtimeSinceStartup - _lastBootCharTime >= _bootCharDelay)
			{
				_lastBootCharTime += _bootCharDelay;
				if (_bootLineIndex < _bootLines.Length)
				{
					string text3 = _bootLines[_bootLineIndex];
					string text4 = Regex.Replace(text3, "<.*?>", "");
					if (_bootCharIndex < text4.Length)
					{
						_bootCharIndex++;
						if (_bootCharIndex >= text4.Length)
						{
							_bootDisplayedText = _bootDisplayedText + text3 + "\n";
							((TMP_Text)_bootConsoleText).text = _bootDisplayedText + text;
							_bootLineIndex++;
							_bootCharIndex = 0;
						}
					}
					continue;
				}
				FinishBootSequence();
				break;
			}
			if (!_bootSequenceComplete && (Object)(object)_bootConsoleText != (Object)null && _bootLineIndex < _bootLines.Length)
			{
				((TMP_Text)_bootConsoleText).text = _bootDisplayedText + text;
			}
		}

		private void UpdateFloatingCrewmates()
		{
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_0187: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_bootOverlay == (Object)null)
			{
				return;
			}
			if (_floatingCrewmates.Count == 0)
			{
				for (int i = 0; i < 6; i++)
				{
					SpawnFloatingCrewmate(_bootOverlay.transform, spawnOnScreen: true);
				}
				_spawnTimer = 1f;
				return;
			}
			_spawnTimer -= Time.deltaTime;
			if (_floatingCrewmates.Count < 12 && _spawnTimer <= 0f)
			{
				SpawnFloatingCrewmate(_bootOverlay.transform);
				_spawnTimer = UnityEngine.Random.Range(1f, 2.5f);
			}
			for (int num = _floatingCrewmates.Count - 1; num >= 0; num--)
			{
				FloatingCrewmate floatingCrewmate = _floatingCrewmates[num];
				if ((Object)(object)floatingCrewmate.Root == (Object)null)
				{
					_floatingCrewmates.RemoveAt(num);
				}
				else
				{
					float deltaTime = Time.deltaTime;
					Vector3 val = InteropFix.Cast(floatingCrewmate.Rect.anchoredPosition);
					Vector2 val2 = floatingCrewmate.Velocity * deltaTime;
					val.x += val2.x;
					val.y += val2.y;
					float num2 = Mathf.Sin(Time.time * floatingCrewmate.BobFrequency + floatingCrewmate.TimeOffset) * floatingCrewmate.BobAmplitude * deltaTime;
					val.y += num2;
					floatingCrewmate.Rect.anchoredPosition = InteropFix.Cast(val);
					((Transform)floatingCrewmate.Rect).Rotate(0f, 0f, floatingCrewmate.RotSpeed * deltaTime);
					if (val.x > 1300f || val.x < -1300f || val.y > 800f || val.y < -800f)
					{
						Object.Destroy((Object)(object)floatingCrewmate.Root);
						_floatingCrewmates.RemoveAt(num);
					}
				}
			}
		}

		private void SpawnFloatingCrewmate(Transform parent, bool spawnOnScreen = false)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0231: Unknown result type (might be due to invalid IL or missing references)
			//IL_0282: Unknown result type (might be due to invalid IL or missing references)
			//IL_0284: Unknown result type (might be due to invalid IL or missing references)
			//IL_0286: Unknown result type (might be due to invalid IL or missing references)
			//IL_028d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_0297: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0300: Unknown result type (might be due to invalid IL or missing references)
			//IL_0305: Unknown result type (might be due to invalid IL or missing references)
			//IL_030a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0323: Unknown result type (might be due to invalid IL or missing references)
			//IL_0325: Unknown result type (might be due to invalid IL or missing references)
			//IL_0327: Unknown result type (might be due to invalid IL or missing references)
			//IL_036d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0372: Unknown result type (might be due to invalid IL or missing references)
			//IL_0374: Unknown result type (might be due to invalid IL or missing references)
			//IL_037b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0380: Unknown result type (might be due to invalid IL or missing references)
			//IL_0385: Unknown result type (might be due to invalid IL or missing references)
			//IL_03af: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_041a: Unknown result type (might be due to invalid IL or missing references)
			//IL_041f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0421: Unknown result type (might be due to invalid IL or missing references)
			//IL_043a: Unknown result type (might be due to invalid IL or missing references)
			//IL_043c: Unknown result type (might be due to invalid IL or missing references)
			//IL_043e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0486: Unknown result type (might be due to invalid IL or missing references)
			//IL_0488: Unknown result type (might be due to invalid IL or missing references)
			//IL_048a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0491: Unknown result type (might be due to invalid IL or missing references)
			//IL_0496: Unknown result type (might be due to invalid IL or missing references)
			//IL_049b: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_04df: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_050b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0510: Unknown result type (might be due to invalid IL or missing references)
			//IL_054b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("FloatingCrewmate");
			val.transform.SetParent(parent, false);
			RectTransform val2 = val.AddComponent<RectTransform>();
			float num;
			float num2;
			Vector2 val4;
			if (spawnOnScreen)
			{
				num = UnityEngine.Random.Range(-800f, 800f);
				num2 = UnityEngine.Random.Range(-400f, 400f);
				Vector2 val3 = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.5f, 0.5f));
				val4 = val3.normalized;
			}
			else
			{
				bool num3 = UnityEngine.Random.value > 0.5f;
				num = (num3 ? (-1000) : 1000);
				num2 = UnityEngine.Random.Range(-450, 450);
				val4 = (num3 ? Vector2.right : Vector2.left);
				val4 += new Vector2(0f, UnityEngine.Random.Range(-0.3f, 0.3f));
				val4.Normalize();
			}
			val2.anchoredPosition = new Vector2(num, num2);
			float num4 = UnityEngine.Random.Range(0.6f, 1.3f);
			((Transform)val2).localScale = Vector3.one * num4;
			float num5 = UnityEngine.Random.Range(50f, 120f) * (num4 * 0.7f + 0.3f);
			FloatingCrewmate item = new FloatingCrewmate
			{
				Root = val,
				Rect = val2,
				Velocity = val4 * num5,
				RotSpeed = UnityEngine.Random.Range(-25f, 25f),
				BobFrequency = UnityEngine.Random.Range(0.8f, 2.5f),
				BobAmplitude = UnityEngine.Random.Range(15f, 40f),
				TimeOffset = UnityEngine.Random.Range(0f, 10f)
			};
			Color randomCrewmateColor = GetRandomCrewmateColor();
			new Color(randomCrewmateColor.r * 0.7f, randomCrewmateColor.g * 0.7f, randomCrewmateColor.b * 0.7f);
			Color col = default(Color);
			col._002Ector(0f, 0f, 0f, 1f);
			Color col2 = default(Color);
			col2._002Ector(0.55f, 0.82f, 0.96f);
			Color col3 = default(Color);
			col3._002Ector(1f, 1f, 1f, 0.9f);
			new Color(0.25f, 0.45f, 0.55f);
			float num6 = 65f;
			float num7 = 60f;
			float num8 = 6f;
			Vector2 val5 = default(Vector2);
			val5._002Ector(num6 * 0.35f, num7 * 0.7f);
			Vector2 pos = default(Vector2);
			pos._002Ector((0f - num6) * 0.45f, (0f - num7) * 0.05f);
			CreatePart("BackpackOut", pos, val5 + Vector2.one * num8, col, val.transform, 1.5f);
			CreatePart("Backpack", pos, val5, randomCrewmateColor, val.transform, 1.5f);
			Vector2 val6 = default(Vector2);
			val6._002Ector(num6 * 0.85f, num7 * 0.95f);
			Vector2 pos2 = default(Vector2);
			pos2._002Ector(0f, 0f);
			CreatePart("BodyOut", pos2, val6 + Vector2.one * num8, col, val.transform, 0.5f);
			CreatePart("Body", pos2, val6, randomCrewmateColor, val.transform, 0.5f);
			Vector2 val7 = default(Vector2);
			val7._002Ector(num6 * 0.3f, num7 * 0.4f);
			float num9 = (0f - num7) * 0.5f;
			CreatePart("LegLOut", new Vector2((0f - num6) * 0.25f, num9), val7 + Vector2.one * num8, col, val.transform, 0.8f);
			CreatePart("LegL", new Vector2((0f - num6) * 0.25f, num9 + 2f), val7, randomCrewmateColor, val.transform, 0.8f);
			CreatePart("LegROut", new Vector2(num6 * 0.25f, num9), val7 + Vector2.one * num8, col, val.transform, 0.8f);
			CreatePart("LegR", new Vector2(num6 * 0.25f, num9 + 2f), val7, randomCrewmateColor, val.transform, 0.8f);
			GameObject val8 = CreatePart("BodyCover", pos2, val6, randomCrewmateColor, val.transform, 0.5f);
			Vector2 val9 = default(Vector2);
			val9._002Ector(num6 * 0.65f, num7 * 0.45f);
			Vector2 pos3 = default(Vector2);
			pos3._002Ector(num6 * 0.35f, num7 * 0.1f);
			CreatePart("VisorOut", pos3, val9 + Vector2.one * num8, col, val.transform, 0.6f);
			GameObject val10 = CreatePart("Visor", pos3, val9, col2, val.transform, 0.6f);
			CreatePart("VisorShine", new Vector2(val9.x * 0.25f, val9.y * 0.2f), new Vector2(val9.x * 0.4f, val9.y * 0.2f), col3, val10.transform);
			_floatingCrewmates.Add(item);
			if (UnityEngine.Random.value > 0.3f)
			{
				AddRandomHat(val.transform, val8.transform, randomCrewmateColor);
			}
			GameObject CreatePart(string name, Vector2 anchoredPosition, Vector2 size, Color color, Transform p, float pps = 1f)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				//IL_003c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0043: Unknown result type (might be due to invalid IL or missing references)
				//IL_0049: Unknown result type (might be due to invalid IL or missing references)
				//IL_0050: Expected O, but got Unknown
				GameObject val11 = new GameObject(name);
				val11.transform.SetParent(p, false);
				Image obj = val11.AddComponent<Image>();
				obj.sprite = GetCircleSprite();
				obj.type = (Image.Type)1;
				obj.pixelsPerUnitMultiplier = pps;
				((Graphic)obj).color = color;
				RectTransform component = val11.GetComponent<RectTransform>();
				component.anchoredPosition = anchoredPosition;
				component.sizeDelta = size;
				return val11;
			}
		}

		private Sprite GetCircleSprite()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Expected O, but got Unknown
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_cachedCircleSprite != (Object)null)
			{
				return _cachedCircleSprite;
			}
			int num = 64;
			Texture2D val = new Texture2D(num, num, (TextureFormat)4, false)
			{
				wrapMode = (TextureWrapMode)1,
				filterMode = (FilterMode)1,
				hideFlags = (HideFlags)61
			};
			Color[] array = (Color[])(object)new Color[num * num];
			float num2 = (float)num / 2f;
			Vector2 val2 = default(Vector2);
			val2._002Ector(num2, num2);
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					float num3 = Vector2.Distance(new Vector2((float)j, (float)i), val2) - num2;
					float num4 = 1f - Mathf.Clamp01(num3 + 1f);
					array[i * num + j] = new Color(1f, 1f, 1f, num4);
				}
			}
			val.SetPixels(InteropFix.Cast(array));
			val.Apply();
			float num5 = (float)num * 0.45f;
			Vector4 val3 = default(Vector4);
			val3._002Ector(num5, num5, num5, num5);
			_cachedCircleSprite = Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, val3);
			return _cachedCircleSprite;
		}

		private void AddRandomHat(Transform root, Transform body, Color bodyColor)
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			//IL_023b: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			//IL_027d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0282: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0305: Unknown result type (might be due to invalid IL or missing references)
			//IL_0314: Unknown result type (might be due to invalid IL or missing references)
			//IL_0319: Unknown result type (might be due to invalid IL or missing references)
			//IL_0348: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_035c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0375: Unknown result type (might be due to invalid IL or missing references)
			//IL_0384: Unknown result type (might be due to invalid IL or missing references)
			//IL_0389: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
			switch (UnityEngine.Random.Range(0, 6))
			{
			case 0:
				CreateImage(root, "Balloon", new Vector2(15f, 70f), new Vector2(24f, 28f), new Color(0.9f, 0.2f, 0.2f));
				CreateImage(root, "String", new Vector2(15f, 45f), new Vector2(2f, 25f), new Color(1f, 1f, 1f, 0.5f));
				break;
			case 1:
			{
				Color color4 = default(Color);
				color4._002Ector(1f, 0.85f, 0.1f);
				CreateImage(root, "Halo", new Vector2(12f, 50f), new Vector2(30f, 4f), color4);
				CreateImage(root, "HaloL", new Vector2(-2f, 50f), new Vector2(4f, 4f), color4);
				CreateImage(root, "HaloR", new Vector2(28f, 50f), new Vector2(4f, 4f), color4);
				break;
			}
			case 2:
			{
				Color color3 = default(Color);
				color3._002Ector(1f, 0.95f, 0.4f);
				CreateImage(body, "Dum", new Vector2(10f, 25f), new Vector2(20f, 15f), color3);
				break;
			}
			case 3:
				CreateImage(root, "CherryR", new Vector2(20f, 40f), new Vector2(12f, 12f), new Color(0.9f, 0.1f, 0.1f));
				CreateImage(root, "CherryL", new Vector2(8f, 38f), new Vector2(12f, 12f), new Color(0.8f, 0.1f, 0.1f));
				CreateImage(root, "Stem", new Vector2(15f, 48f), new Vector2(4f, 10f), new Color(0.1f, 0.7f, 0.1f));
				break;
			case 4:
			{
				Color color2 = default(Color);
				color2._002Ector(1f, 0.8f, 0f);
				GameObject val = CreateImage(root, "CrownBase", new Vector2(10f, 40f), new Vector2(30f, 8f), color2);
				CreateImage(val.transform, "Spike1", new Vector2(-10f, 8f), new Vector2(8f, 8f), color2);
				CreateImage(val.transform, "Spike2", new Vector2(0f, 10f), new Vector2(8f, 8f), color2);
				CreateImage(val.transform, "Spike3", new Vector2(10f, 8f), new Vector2(8f, 8f), color2);
				break;
			}
			case 5:
			{
				Color color = default(Color);
				color._002Ector(0.2f, 0.8f, 0.2f);
				CreateImage(root, "Stem", new Vector2(12f, 40f), new Vector2(4f, 12f), color);
				CreateImage(root, "LeafL", new Vector2(4f, 50f), new Vector2(10f, 6f), color);
				CreateImage(root, "LeafR", new Vector2(18f, 52f), new Vector2(10f, 6f), color);
				break;
			}
			}
		}

		private Color GetRandomCrewmateColor()
		{
			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			if (_availableCrewmateColors.Count == 0)
			{
				Color[] collection = (Color[])(object)new Color[12]
				{
					new Color(0.78f, 0.08f, 0.08f),
					new Color(0.07f, 0.18f, 0.85f),
					new Color(0.08f, 0.51f, 0.15f),
					new Color(0.93f, 0.35f, 0.73f),
					new Color(0.91f, 0.53f, 0.08f),
					new Color(0.97f, 0.96f, 0.33f),
					new Color(0.3f, 0.3f, 0.3f),
					new Color(0.92f, 0.92f, 0.92f),
					new Color(0.5f, 0.2f, 0.8f),
					new Color(0.55f, 0.35f, 0.15f),
					new Color(0.22f, 0.95f, 0.88f),
					new Color(0.35f, 0.95f, 0.3f)
				};
				foreach (Color color in collection)
				{
					_availableCrewmateColors.Add(color);
				}
				for (int num = _availableCrewmateColors.Count - 1; num > 0; num--)
				{
					int index = UnityEngine.Random.Range(0, num + 1);
					Color value = _availableCrewmateColors[num];
					_availableCrewmateColors[num] = _availableCrewmateColors[index];
					_availableCrewmateColors[index] = value;
				}
			}
			Color result = _availableCrewmateColors[_availableCrewmateColors.Count - 1];
			_availableCrewmateColors.RemoveAt(_availableCrewmateColors.Count - 1);
			return result;
		}

		private GameObject CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			GameObject val = new GameObject(name);
			val.transform.SetParent(parent, false);
			Image obj = val.AddComponent<Image>();
			obj.sprite = GetCircleSprite();
			((Graphic)obj).color = color;
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchoredPosition = pos;
			component.sizeDelta = size;
			return val;
		}

		private void ManageActivationUIVisibility()
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)activationCanvasTMP == (Object)null)
				{
					return;
				}
				if ((Object)(object)statusMessageTextTMP != (Object)null)
				{
					if (!string.Equals(_lastStatusMessage, currentActivationStatusMessage, System.StringComparison.Ordinal))
					{
						string text = "";
						Color white = Color.white;
						string text2 = currentActivationStatusMessage.ToLowerInvariant();
						if (text2.Contains("error") || text2.Contains("invalid") || text2.Contains("failed") || text2.Contains("timeout"))
						{
							white._002Ector(1f, 0.3f, 0.3f);
							text = "[!] ";
						}
						else if (text2.Contains("success") || text2.Contains("validated") || text2.Contains("activated"))
						{
							white._002Ector(0.3f, 1f, 0.3f);
							text = "[✓] ";
						}
						else if (text2.Contains("wait") || text2.Contains("validating"))
						{
							white._002Ector(1f, 0.8f, 0.2f);
							text = "[...] ";
						}
						((TMP_Text)statusMessageTextTMP).text = text + currentActivationStatusMessage;
						((Graphic)statusMessageTextTMP).color = white;
						_lastStatusMessage = currentActivationStatusMessage;
					}
				}
				else
				{
					((BasePlugin)Instance).Log.LogWarning((object)"ManageActivationUIVisibility: statusMessageTextTMP é nulo.");
				}
				if ((Object)(object)validateButtonTMP != (Object)null)
				{
					string text3 = (((Object)(object)apiKeyInputFieldTMP != (Object)null) ? apiKeyInputFieldTMP.text : null);
					bool flag = !isValidatingNow && !string.IsNullOrWhiteSpace(text3);
					if (((Selectable)validateButtonTMP).interactable != flag)
					{
						((Selectable)validateButtonTMP).interactable = flag;
					}
					if ((Object)(object)validateButtonTextTMP != (Object)null)
					{
						if (!isValidatingNow)
						{
							string text4 = "[ ACTIVATE ]";
							if (!string.Equals(((TMP_Text)validateButtonTextTMP).text, text4, System.StringComparison.Ordinal))
							{
								((TMP_Text)validateButtonTextTMP).text = text4;
								_loadingDotsTimer = 0f;
								_loadingDotsCount = 0;
							}
						}
					}
					else
					{
						((BasePlugin)Instance).Log.LogWarning((object)"ManageActivationUIVisibility: validateButtonTextTMP (texto do botão) é nulo.");
					}
					_lastValidatingState = isValidatingNow;
					_lastInputText = text3;
				}
				else
				{
					((BasePlugin)Instance).Log.LogWarning((object)"ManageActivationUIVisibility: validateButtonTMP é nulo.");
				}
				if (_shouldAutoFocus && (Object)(object)apiKeyInputFieldTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeInHierarchy)
				{
					_shouldAutoFocus = false;
					if ((Object)(object)EventSystem.current != (Object)null)
					{
						EventSystem.current.SetSelectedGameObject(((Component)apiKeyInputFieldTMP).gameObject);
						apiKeyInputFieldTMP.ActivateInputField();
					}
				}
			}
			catch
			{
			}
		}

		private void UpdateActivationUIAnimations()
		{
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_048b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0490: Unknown result type (might be due to invalid IL or missing references)
			//IL_0498: Unknown result type (might be due to invalid IL or missing references)
			//IL_049f: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_04af: Unknown result type (might be due to invalid IL or missing references)
			//IL_043a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0433: Unknown result type (might be due to invalid IL or missing references)
			//IL_043f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0447: Unknown result type (might be due to invalid IL or missing references)
			//IL_044e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0455: Unknown result type (might be due to invalid IL or missing references)
			//IL_0326: Unknown result type (might be due to invalid IL or missing references)
			//IL_034a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0300: Unknown result type (might be due to invalid IL or missing references)
			//IL_0507: Unknown result type (might be due to invalid IL or missing references)
			//IL_050e: Unknown result type (might be due to invalid IL or missing references)
			//IL_057e: Unknown result type (might be due to invalid IL or missing references)
			//IL_046d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0599: Unknown result type (might be due to invalid IL or missing references)
			//IL_05db: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_05fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_080a: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_061d: Unknown result type (might be due to invalid IL or missing references)
			//IL_063d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0642: Unknown result type (might be due to invalid IL or missing references)
			//IL_064d: Unknown result type (might be due to invalid IL or missing references)
			//IL_06f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0700: Unknown result type (might be due to invalid IL or missing references)
			//IL_0718: Unknown result type (might be due to invalid IL or missing references)
			//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_06c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_06df: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)activationCanvasTMP == (Object)null || !((Component)activationCanvasTMP).gameObject.activeSelf)
				{
					return;
				}
				float deltaTime = Time.deltaTime;
				UpdateBootSequence();
				UpdateFloatingCrewmates();
				UpdateGlitchText();
				UpdateScanningLine();
				if ((Object)(object)_panelCanvasGroup != (Object)null && Mathf.Abs(_currentAlpha - _targetAlpha) > 0.01f)
				{
					_currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, deltaTime * 8f);
					_panelCanvasGroup.alpha = _currentAlpha;
				}
				if ((Object)(object)activationPanelGO != (Object)null && _panelScaleCurrent < 0.999f)
				{
					_panelScaleCurrent = Mathf.Lerp(_panelScaleCurrent, 1f, deltaTime * 6f);
					activationPanelGO.transform.localScale = Vector3.one * _panelScaleCurrent;
				}
				else if ((Object)(object)activationPanelGO != (Object)null && _panelScaleCurrent < 1f)
				{
					_panelScaleCurrent = 1f;
					activationPanelGO.transform.localScale = Vector3.one;
				}
				if (_revealStartTime > 0f && !_revealComplete && _staggerElements.Count > 0)
				{
					float num = Time.realtimeSinceStartup - _revealStartTime;
					bool flag = true;
					for (int i = 0; i < _staggerElements.Count; i++)
					{
						CanvasGroup val = _staggerElements[i];
						if ((Object)(object)val == (Object)null)
						{
							continue;
						}
						float num2 = ((i < _staggerDelays.Length) ? _staggerDelays[i] : ((float)i * 0.08f));
						float num3 = num - num2;
						if (num3 < 0f)
						{
							flag = false;
							continue;
						}
						float num4 = Mathf.Clamp01(num3 / 0.25f);
						float alpha = 1f - (1f - num4) * (1f - num4);
						val.alpha = alpha;
						if (num4 < 1f)
						{
							flag = false;
						}
					}
					if (flag)
					{
						_revealComplete = true;
					}
				}
				if ((Object)(object)apiKeyInputFieldTMP != (Object)null && (Object)(object)_inputFieldOutline != (Object)null && (Object)(object)_inputFieldBgImage != (Object)null)
				{
					float num5 = (apiKeyInputFieldTMP.isFocused ? 1f : 0f);
					_inputGlowPhase = Mathf.MoveTowards(_inputGlowPhase, num5, deltaTime * 5f);
					if (_inputGlowPhase > 0.01f)
					{
						float num6 = 0.5f + Mathf.Sin(Time.time * 2.5f) * 0.3f;
						float num7 = _inputGlowPhase * num6;
						((Shadow)_inputFieldOutline).effectColor = new Color(0.2f, 0.7f, 1f, num7);
						float num8 = 0.1f + _inputGlowPhase * 0.04f;
						((Graphic)_inputFieldBgImage).color = new Color(num8, num8, num8 + 0.02f, 0.85f);
					}
					else
					{
						((Shadow)_inputFieldOutline).effectColor = new Color(0.2f, 0.7f, 1f, 0f);
						((Graphic)_inputFieldBgImage).color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
					}
				}
				if (isValidatingNow && (Object)(object)validateButtonTextTMP != (Object)null)
				{
					_loadingDotsTimer += deltaTime;
					if (_loadingDotsTimer > 0.4f)
					{
						_loadingDotsTimer = 0f;
						_loadingDotsCount = (_loadingDotsCount + 1) % 4;
					}
					string text = new string('.', _loadingDotsCount).PadRight(3);
					((TMP_Text)validateButtonTextTMP).text = "VALIDATING" + text;
				}
				if (!(_currentAlpha > 0.9f))
				{
					return;
				}
				_pulseTimer += deltaTime * 3f;
				float num9 = 0.7f + Mathf.Sin(_pulseTimer) * 0.3f;
				if ((Object)(object)_validateBtnImage != (Object)null)
				{
					Color val2 = (Color)(isValidatingNow ? Color.gray : new Color(1f, 1f, 1f, 1f));
					((Graphic)_validateBtnImage).color = new Color(val2.r, val2.g, val2.b, isValidatingNow ? 1f : num9);
				}
				if ((Object)(object)_validateBtnOutline != (Object)null)
				{
					Color effectColor = ((Shadow)_validateBtnOutline).effectColor;
					((Shadow)_validateBtnOutline).effectColor = new Color(effectColor.r, effectColor.g, effectColor.b, num9);
				}
				if (_revealComplete && (Object)(object)activationPanelGO != (Object)null)
				{
					_breathePhase += deltaTime * 1.2f;
					float num10 = 1f + Mathf.Sin(_breathePhase) * 0.003f;
					activationPanelGO.transform.localScale = Vector3.one * num10;
				}
				if ((Object)(object)_accentTopLine != (Object)null || (Object)(object)_accentBottomLine != (Object)null)
				{
					float num11 = 0.5f + Mathf.Sin(Time.time * 2f) * 0.3f;
					Color color = default(Color);
					color._002Ector(0.2f, 1f, 1f, num11);
					if ((Object)(object)_accentTopLine != (Object)null)
					{
						((Graphic)_accentTopLine).color = color;
					}
					if ((Object)(object)_accentBottomLine != (Object)null)
					{
						((Graphic)_accentBottomLine).color = color;
					}
				}
				if (_stars != null)
				{
					for (int j = 0; j < _stars.Count; j++)
					{
						Star star = _stars[j];
						if (!((Object)(object)star.Rect != (Object)null))
						{
							continue;
						}
						Vector2 anchoredPosition = star.Rect.anchoredPosition;
						anchoredPosition.x -= star.Speed * deltaTime * 60f;
						if (anchoredPosition.x < -1000f)
						{
							anchoredPosition.x = 1000f;
						}
						star.Rect.anchoredPosition = anchoredPosition;
						if ((Object)(object)star.Image != (Object)null)
						{
							Color color2 = ((Graphic)star.Image).color;
							float num12 = (star.IsBright ? (0.1f + Mathf.Sin(Time.time * star.Speed * 2f + (float)j) * 0.08f) : (color2.a + Mathf.Sin(Time.time * star.Speed * 3f + (float)j * 0.5f) * 0.1f));
							if (star.IsBright)
							{
								((Graphic)star.Image).color = new Color(color2.r, color2.g, color2.b, Mathf.Clamp(num12, 0.05f, 0.25f));
							}
							else
							{
								((Graphic)star.Image).color = new Color(color2.r, color2.g, color2.b, Mathf.Clamp(num12, 0.15f, 0.85f));
							}
						}
					}
				}
				_glitchTimer += deltaTime;
				if (!_isGlitching && UnityEngine.Random.Range(0, 500) == 0 && _glitchTimer > 2f)
				{
					_isGlitching = true;
					_glitchTimer = 0f;
					if ((Object)(object)_titleTextTMP != (Object)null)
					{
						((TMP_Text)_titleTextTMP).text = "IMPOSTOR DETECTED";
						((Graphic)_titleTextTMP).color = Color.red;
					}
				}
				else if (_isGlitching && _glitchTimer > 0.15f)
				{
					_isGlitching = false;
					_glitchTimer = 0f;
					if ((Object)(object)_titleTextTMP != (Object)null)
					{
						((TMP_Text)_titleTextTMP).text = _originalTitle;
						((Graphic)_titleTextTMP).color = new Color(1f, 0.4f, 0.5f);
					}
				}
			}
			catch
			{
			}
		}

		private void CreateStarfield(Transform parent)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_018f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Expected O, but got Unknown
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_0215: Unknown result type (might be due to invalid IL or missing references)
			//IL_0219: Unknown result type (might be due to invalid IL or missing references)
			//IL_0220: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			//IL_0233: Unknown result type (might be due to invalid IL or missing references)
			//IL_0252: Unknown result type (might be due to invalid IL or missing references)
			//IL_0268: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0310: Unknown result type (might be due to invalid IL or missing references)
			//IL_0325: Unknown result type (might be due to invalid IL or missing references)
			//IL_032f: Unknown result type (might be due to invalid IL or missing references)
			_stars.Clear();
			Color[] array = (Color[])(object)new Color[6]
			{
				new Color(1f, 1f, 1f),
				new Color(1f, 0.95f, 0.8f),
				new Color(0.8f, 0.9f, 1f),
				new Color(1f, 0.8f, 0.8f),
				new Color(0.9f, 1f, 0.95f),
				new Color(1f, 0.6f, 0.4f)
			};
			for (int i = 0; i < 80; i++)
			{
				GameObject val = new GameObject("Star");
				val.transform.SetParent(parent, false);
				Image val2 = val.AddComponent<Image>();
				Color val3 = array[UnityEngine.Random.Range(0, array.Length)];
				float num = UnityEngine.Random.Range(0.2f, 0.7f);
				((Graphic)val2).color = new Color(val3.r, val3.g, val3.b, num);
				RectTransform component = val.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0.5f);
				component.anchorMax = new Vector2(0.5f, 0.5f);
				float num2 = UnityEngine.Random.Range(1f, 4f);
				component.sizeDelta = new Vector2(num2, num2);
				component.anchoredPosition = new Vector2((float)UnityEngine.Random.Range(-960, 960), (float)UnityEngine.Random.Range(-540, 540));
				Star item = new Star
				{
					Rect = component,
					Speed = UnityEngine.Random.Range(0.3f, 1.5f),
					Image = val2
				};
				_stars.Add(item);
			}
			for (int j = 0; j < 10; j++)
			{
				GameObject val4 = new GameObject("BrightStar");
				val4.transform.SetParent(parent, false);
				Image val5 = val4.AddComponent<Image>();
				Color val6 = array[UnityEngine.Random.Range(0, 3)];
				((Graphic)val5).color = new Color(val6.r, val6.g, val6.b, 0.15f);
				RectTransform component2 = val4.GetComponent<RectTransform>();
				component2.anchorMin = new Vector2(0.5f, 0.5f);
				component2.anchorMax = new Vector2(0.5f, 0.5f);
				float num3 = UnityEngine.Random.Range(15f, 30f);
				component2.sizeDelta = new Vector2(num3, num3);
				component2.anchoredPosition = new Vector2((float)UnityEngine.Random.Range(-900, 900), (float)UnityEngine.Random.Range(-500, 500));
				GameObject val7 = new GameObject("Core");
				val7.transform.SetParent(val4.transform, false);
				((Graphic)val7.AddComponent<Image>()).color = new Color(1f, 1f, 1f, 0.9f);
				RectTransform component3 = val7.GetComponent<RectTransform>();
				component3.anchorMin = new Vector2(0.3f, 0.3f);
				component3.anchorMax = new Vector2(0.7f, 0.7f);
				component3.sizeDelta = Vector2.zero;
				Star item2 = new Star
				{
					Rect = component2,
					Speed = UnityEngine.Random.Range(1f, 3f),
					Image = val5,
					IsBright = true
				};
				_stars.Add(item2);
			}
		}

		private void CreateTechCorners(RectTransform parent)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			CreateTechCorners(parent, new Color(1f, 0.2f, 0.3f, 0.8f));
		}

		private void CreateTechCorners(RectTransform parent, Color color)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			float size = 30f;
			float thickness = 2f;
			CreateCorner(parent, new Vector2(0f, 1f), new Vector2(5f, -5f), size, thickness, color, 0f);
			CreateCorner(parent, new Vector2(1f, 1f), new Vector2(-5f, -5f), size, thickness, color, 90f);
			CreateCorner(parent, new Vector2(1f, 0f), new Vector2(-5f, 5f), size, thickness, color, 180f);
			CreateCorner(parent, new Vector2(0f, 0f), new Vector2(5f, 5f), size, thickness, color, 270f);
		}

		private void CreateCorner(RectTransform parent, Vector2 anchor, Vector2 offset, float size, float thickness, Color color, float rotation)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("Corner");
			val.transform.SetParent((Transform)(object)parent, false);
			RectTransform obj = val.AddComponent<RectTransform>();
			obj.anchorMin = anchor;
			obj.anchorMax = anchor;
			obj.pivot = anchor;
			obj.anchoredPosition = offset;
			obj.sizeDelta = new Vector2(size, size);
			((Transform)obj).localRotation = Quaternion.Euler(0f, 0f, 0f - rotation);
			GameObject val2 = new GameObject("V");
			val2.transform.SetParent(val.transform, false);
			((Graphic)val2.AddComponent<Image>()).color = color;
			RectTransform component = val2.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.zero;
			component.pivot = Vector2.zero;
			component.anchoredPosition = Vector2.zero;
			component.sizeDelta = new Vector2(thickness, size);
			GameObject val3 = new GameObject("H");
			val3.transform.SetParent(val.transform, false);
			((Graphic)val3.AddComponent<Image>()).color = color;
			RectTransform component2 = val3.GetComponent<RectTransform>();
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.zero;
			component2.pivot = Vector2.zero;
			component2.anchoredPosition = Vector2.zero;
			component2.sizeDelta = new Vector2(size, thickness);
		}

		private void StyleButton(Button btn, ref Sprite cachedSprite, Color topColor, Color bottomColor)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)btn == (Object)null)
			{
				return;
			}
			Image component = ((Component)btn).GetComponent<Image>();
			if (!((Object)(object)component != (Object)null))
			{
				return;
			}
			if ((Object)(object)cachedSprite == (Object)null)
			{
				Texture2D val = new Texture2D(1, 32, (TextureFormat)4, false)
				{
					wrapMode = (TextureWrapMode)1,
					filterMode = (FilterMode)1,
					hideFlags = (HideFlags)61
				};
				for (int i = 0; i < 32; i++)
				{
					float num = (float)i / 31f;
					Color val2 = Color.Lerp(topColor, bottomColor, num);
					val.SetPixel(0, i, val2);
				}
				val.Apply();
				cachedSprite = Sprite.Create(val, new Rect(0f, 0f, 1f, 32f), Vector2.one * 0.5f);
			}
			component.sprite = cachedSprite;
			Outline val3 = ((Component)btn).gameObject.GetComponent<Outline>();
			if ((Object)(object)val3 == (Object)null)
			{
				val3 = ((Component)btn).gameObject.AddComponent<Outline>();
			}
			((Shadow)val3).effectColor = new Color(topColor.r * 1.5f, topColor.g * 1.5f, topColor.b * 1.5f, 0.8f);
			((Shadow)val3).effectDistance = new Vector2(1f, -1f);
		}

		private static PopupTheme GetPopupTheme(bool isPremium)
		{
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			if (isPremium)
			{
				Color gold = GuiStyles.Theme.Gold;
				Color accentSoft = default(Color);
				accentSoft._002Ector(1f, 0.92f, 0.4f, 1f);
				return new PopupTheme(gold, accentSoft, new Color(0.12f, 0.09f, 0.02f, 1f), new Color(1f, 0.84f, 0.2f, 1f), new Color(0.75f, 0.52f, 0.05f, 1f), new Color(0.18f, 0.18f, 0.24f, 1f), new Color(0.1f, 0.1f, 0.14f, 1f));
			}
			return new PopupTheme(GuiStyles.Theme.Visor, new Color(0.2f, 1f, 1f, 1f), new Color(0.02f, 0.06f, 0.08f, 1f), new Color(0f, 0.6f, 0.6f, 1f), new Color(0f, 0.3f, 0.3f, 1f), new Color(0.18f, 0.18f, 0.24f, 1f), new Color(0.1f, 0.1f, 0.14f, 1f));
		}

		private static string NormalizeKeyTypeLabel(string keyType)
		{
			string text = (string.IsNullOrWhiteSpace(keyType) ? "" : keyType.Trim().ToUpperInvariant());
			if (string.IsNullOrEmpty(text) || text == "STANDARD")
			{
				return "PREMIUM";
			}
			return text;
		}

		private static string FormatRemainingShort(System.TimeSpan remaining)
		{
			if (remaining.TotalSeconds <= 0.0)
			{
				return "0s";
			}
			if (!(remaining.TotalDays >= 1.0))
			{
				if (!(remaining.TotalHours >= 1.0))
				{
					if (!(remaining.TotalMinutes >= 1.0))
					{
						return $"{remaining.Seconds}s";
					}
					return $"{remaining.Minutes}m {remaining.Seconds}s";
				}
				return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
			}
			return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
		}

		private static bool IsLifetimeAccess(bool isPremium, string keyType, System.TimeSpan? remaining)
		{
			if (!isPremium)
			{
				return false;
			}
			if (!string.IsNullOrWhiteSpace(keyType) && keyType.Trim().Equals("LIFETIME", System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (remaining.HasValue && remaining.Value.TotalDays >= 3650.0)
			{
				return true;
			}
			return false;
		}

		private void EnsureSuccessCircleSprite()
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected O, but got Unknown
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_successCircleSprite != (Object)null)
			{
				return;
			}
			if ((Object)(object)_successCircleTexture != (Object)null)
			{
				try
				{
					Object.Destroy((Object)(object)_successCircleTexture);
				}
				catch
				{
				}
				_successCircleTexture = null;
			}
			_successCircleTexture = new Texture2D(64, 64, (TextureFormat)4, false)
			{
				wrapMode = (TextureWrapMode)1,
				filterMode = (FilterMode)1,
				hideFlags = (HideFlags)61
			};
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					float num = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(31.5f, 31.5f));
					float num2 = 1f - Mathf.Clamp01((num - 10f) / 20f);
					_successCircleTexture.SetPixel(j, i, new Color(1f, 1f, 1f, num2));
				}
			}
			_successCircleTexture.Apply();
			_successCircleSprite = Sprite.Create(_successCircleTexture, new Rect(0f, 0f, 64f, 64f), Vector2.one * 0.5f, 100f, 0u, (SpriteMeshType)0);
		}

		private void ShowActivationSuccessPopup(string username, bool isPremium, string keyType, System.TimeSpan? remaining)
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Expected O, but got Unknown
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Expected O, but got Unknown
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_018d: Expected O, but got Unknown
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01da: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Expected O, but got Unknown
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_0244: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0270: Unknown result type (might be due to invalid IL or missing references)
			//IL_027c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_02de: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0302: Unknown result type (might be due to invalid IL or missing references)
			//IL_0311: Unknown result type (might be due to invalid IL or missing references)
			//IL_0320: Unknown result type (might be due to invalid IL or missing references)
			//IL_0325: Unknown result type (might be due to invalid IL or missing references)
			//IL_0338: Unknown result type (might be due to invalid IL or missing references)
			//IL_033f: Unknown result type (might be due to invalid IL or missing references)
			//IL_034a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0355: Unknown result type (might be due to invalid IL or missing references)
			//IL_0364: Unknown result type (might be due to invalid IL or missing references)
			//IL_037e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0393: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0410: Unknown result type (might be due to invalid IL or missing references)
			//IL_0421: Unknown result type (might be due to invalid IL or missing references)
			//IL_0436: Unknown result type (might be due to invalid IL or missing references)
			//IL_0450: Unknown result type (might be due to invalid IL or missing references)
			//IL_0465: Unknown result type (might be due to invalid IL or missing references)
			//IL_047a: Unknown result type (might be due to invalid IL or missing references)
			//IL_048f: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0510: Unknown result type (might be due to invalid IL or missing references)
			//IL_0525: Unknown result type (might be due to invalid IL or missing references)
			//IL_053a: Unknown result type (might be due to invalid IL or missing references)
			//IL_054f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0563: Unknown result type (might be due to invalid IL or missing references)
			//IL_0572: Unknown result type (might be due to invalid IL or missing references)
			//IL_0579: Expected O, but got Unknown
			//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0608: Unknown result type (might be due to invalid IL or missing references)
			//IL_061c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0638: Unknown result type (might be due to invalid IL or missing references)
			//IL_064d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0662: Unknown result type (might be due to invalid IL or missing references)
			//IL_0677: Unknown result type (might be due to invalid IL or missing references)
			//IL_068b: Unknown result type (might be due to invalid IL or missing references)
			//IL_069a: Unknown result type (might be due to invalid IL or missing references)
			//IL_069f: Unknown result type (might be due to invalid IL or missing references)
			//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_06f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_06ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_0705: Unknown result type (might be due to invalid IL or missing references)
			//IL_0710: Unknown result type (might be due to invalid IL or missing references)
			//IL_071a: Unknown result type (might be due to invalid IL or missing references)
			//IL_072f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0736: Expected O, but got Unknown
			//IL_075e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0769: Unknown result type (might be due to invalid IL or missing references)
			//IL_0774: Unknown result type (might be due to invalid IL or missing references)
			//IL_0783: Unknown result type (might be due to invalid IL or missing references)
			//IL_079f: Unknown result type (might be due to invalid IL or missing references)
			//IL_07b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_07de: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0801: Unknown result type (might be due to invalid IL or missing references)
			//IL_0808: Expected O, but got Unknown
			//IL_0821: Unknown result type (might be due to invalid IL or missing references)
			//IL_0826: Unknown result type (might be due to invalid IL or missing references)
			//IL_0839: Unknown result type (might be due to invalid IL or missing references)
			//IL_0840: Unknown result type (might be due to invalid IL or missing references)
			//IL_085a: Unknown result type (might be due to invalid IL or missing references)
			//IL_086f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0888: Unknown result type (might be due to invalid IL or missing references)
			//IL_0897: Unknown result type (might be due to invalid IL or missing references)
			//IL_089c: Unknown result type (might be due to invalid IL or missing references)
			//IL_08af: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_08fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_090d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0912: Unknown result type (might be due to invalid IL or missing references)
			//IL_0925: Unknown result type (might be due to invalid IL or missing references)
			//IL_096c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0964: Unknown result type (might be due to invalid IL or missing references)
			//IL_0990: Unknown result type (might be due to invalid IL or missing references)
			//IL_09a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_09cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_09e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_09f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_09f9: Expected O, but got Unknown
			//IL_0a22: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a38: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a4e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a64: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a7a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a96: Unknown result type (might be due to invalid IL or missing references)
			//IL_0aa9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ab0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0abb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ac6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ad5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ae5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0af0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0afa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b09: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b0e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b21: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b45: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b5a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b64: Unknown result type (might be due to invalid IL or missing references)
			//IL_0baa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0baf: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bc2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bf8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c23: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c38: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c4d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c62: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c76: Unknown result type (might be due to invalid IL or missing references)
			//IL_0dac: Unknown result type (might be due to invalid IL or missing references)
			//IL_0db1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0dc4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0dcb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0dd6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0de1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0df0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e0a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e1f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e34: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e49: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e5d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c91: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c96: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ca9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0cc9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d1f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d4a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d5f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d74: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d89: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d9d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e7d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e82: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e95: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ebd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0efa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f0f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f24: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f39: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f4d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f5c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f61: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f74: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f7b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f86: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f91: Unknown result type (might be due to invalid IL or missing references)
			//IL_0fa0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0fba: Unknown result type (might be due to invalid IL or missing references)
			//IL_0fcf: Unknown result type (might be due to invalid IL or missing references)
			//IL_0fe4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ff9: Unknown result type (might be due to invalid IL or missing references)
			//IL_100d: Unknown result type (might be due to invalid IL or missing references)
			//IL_101c: Unknown result type (might be due to invalid IL or missing references)
			//IL_1021: Unknown result type (might be due to invalid IL or missing references)
			//IL_1034: Unknown result type (might be due to invalid IL or missing references)
			//IL_104a: Unknown result type (might be due to invalid IL or missing references)
			//IL_104f: Unknown result type (might be due to invalid IL or missing references)
			//IL_1091: Unknown result type (might be due to invalid IL or missing references)
			//IL_10c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_10d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_10ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_1102: Unknown result type (might be due to invalid IL or missing references)
			//IL_1116: Unknown result type (might be due to invalid IL or missing references)
			//IL_1125: Unknown result type (might be due to invalid IL or missing references)
			//IL_112a: Unknown result type (might be due to invalid IL or missing references)
			//IL_113d: Unknown result type (might be due to invalid IL or missing references)
			//IL_1144: Unknown result type (might be due to invalid IL or missing references)
			//IL_114f: Unknown result type (might be due to invalid IL or missing references)
			//IL_115a: Unknown result type (might be due to invalid IL or missing references)
			//IL_1169: Unknown result type (might be due to invalid IL or missing references)
			//IL_1183: Unknown result type (might be due to invalid IL or missing references)
			//IL_1198: Unknown result type (might be due to invalid IL or missing references)
			//IL_11ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_11c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_11d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_13af: Unknown result type (might be due to invalid IL or missing references)
			//IL_13be: Unknown result type (might be due to invalid IL or missing references)
			//IL_13d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_13da: Unknown result type (might be due to invalid IL or missing references)
			//IL_121c: Unknown result type (might be due to invalid IL or missing references)
			//IL_122e: Unknown result type (might be due to invalid IL or missing references)
			//IL_123d: Unknown result type (might be due to invalid IL or missing references)
			//IL_12a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_12b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_12d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_12d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_132e: Unknown result type (might be due to invalid IL or missing references)
			//IL_133d: Unknown result type (might be due to invalid IL or missing references)
			//IL_1353: Unknown result type (might be due to invalid IL or missing references)
			//IL_1359: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)successPopupGO != (Object)null)
				{
					Object.Destroy((Object)(object)successPopupGO);
				}
				PopupTheme popupTheme = GetPopupTheme(isPremium);
				bool flag = IsLifetimeAccess(isPremium, keyType, remaining);
				string text = (string.IsNullOrWhiteSpace(username) ? "User" : username.Trim());
				string text2 = ((!isPremium) ? "◇ FREE" : (flag ? "∞ LIFETIME" : ("★ " + NormalizeKeyTypeLabel(keyType))));
				string value = "";
				if (isPremium)
				{
					if (flag)
					{
						value = "∞ Lifetime access";
					}
					else if (remaining.HasValue)
					{
						value = "Expires in " + FormatRemainingShort(remaining.Value);
					}
				}
				successPopupGO = new GameObject("ModMenuCrew_SuccessPopupCanvas");
				Object.DontDestroyOnLoad((Object)(object)successPopupGO);
				Canvas val = successPopupGO.AddComponent<Canvas>();
				val.renderMode = (RenderMode)0;
				val.sortingOrder = 32767;
				CanvasScaler obj = successPopupGO.AddComponent<CanvasScaler>();
				obj.uiScaleMode = (CanvasScaler.ScaleMode)1;
				obj.referenceResolution = new Vector2(1920f, 1080f);
				obj.matchWidthOrHeight = 0.5f;
				successPopupGO.AddComponent<GraphicRaycaster>();
				GameObject val2 = new GameObject("Overlay");
				val2.transform.SetParent(((Component)val).transform, false);
				((Graphic)val2.AddComponent<Image>()).color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
				RectTransform component = val2.GetComponent<RectTransform>();
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.sizeDelta = Vector2.zero;
				GameObject val3 = new GameObject("PopupStarfield");
				val3.transform.SetParent(val2.transform, false);
				RectTransform obj2 = val3.AddComponent<RectTransform>();
				obj2.anchorMin = Vector2.zero;
				obj2.anchorMax = Vector2.one;
				obj2.sizeDelta = Vector2.zero;
				CreatePopupStarfield(val3.transform);
				GameObject val4 = new GameObject("MainCard");
				val4.transform.SetParent(((Component)val).transform, false);
				CanvasGroup val5 = val4.AddComponent<CanvasGroup>();
				val5.alpha = 0f;
				((Graphic)val4.AddComponent<Image>()).color = new Color(0.05f, 0.05f, 0.08f, 0.99f);
				RectTransform component2 = val4.GetComponent<RectTransform>();
				component2.anchorMin = new Vector2(0.5f, 0.5f);
				component2.anchorMax = new Vector2(0.5f, 0.5f);
				component2.pivot = new Vector2(0.5f, 0.5f);
				component2.anchoredPosition = Vector2.zero;
				component2.sizeDelta = new Vector2(650f, 480f);
				Outline obj3 = val4.AddComponent<Outline>();
				((Shadow)obj3).effectColor = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.5f);
				((Shadow)obj3).effectDistance = new Vector2(1f, -1f);
				CreateTechCorners(component2, new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.85f));
				GameObject val6 = new GameObject("PopupAccentTop");
				val6.transform.SetParent(val4.transform, false);
				((Graphic)val6.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.8f);
				RectTransform component3 = val6.GetComponent<RectTransform>();
				component3.anchorMin = new Vector2(0f, 1f);
				component3.anchorMax = new Vector2(1f, 1f);
				component3.pivot = new Vector2(0.5f, 1f);
				component3.sizeDelta = new Vector2(-10f, 2f);
				component3.anchoredPosition = new Vector2(0f, -1f);
				GameObject val7 = new GameObject("HeaderBar");
				val7.transform.SetParent(val4.transform, false);
				((Graphic)val7.AddComponent<Image>()).color = new Color(popupTheme.Accent.r * 0.15f, popupTheme.Accent.g * 0.15f, popupTheme.Accent.b * 0.15f, 0.6f);
				RectTransform component4 = val7.GetComponent<RectTransform>();
				component4.anchorMin = new Vector2(0f, 1f);
				component4.anchorMax = new Vector2(1f, 1f);
				component4.pivot = new Vector2(0.5f, 1f);
				component4.sizeDelta = new Vector2(0f, 4f);
				component4.anchoredPosition = new Vector2(0f, -3f);
				GameObject val8 = new GameObject("HeaderGlow");
				val8.transform.SetParent(val4.transform, false);
				((Graphic)val8.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.2f);
				RectTransform component5 = val8.GetComponent<RectTransform>();
				component5.anchorMin = new Vector2(0.05f, 1f);
				component5.anchorMax = new Vector2(0.95f, 1f);
				component5.pivot = new Vector2(0.5f, 1f);
				component5.sizeDelta = new Vector2(0f, 1f);
				component5.anchoredPosition = new Vector2(0f, -7f);
				GameObject val9 = new GameObject("AccountBadge");
				val9.transform.SetParent(val4.transform, false);
				Image obj4 = val9.AddComponent<Image>();
				obj4.sprite = GetWhiteSprite();
				((Graphic)obj4).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, isPremium ? 0.95f : 0.35f);
				Outline obj5 = val9.AddComponent<Outline>();
				((Shadow)obj5).effectColor = new Color(popupTheme.AccentSoft.r, popupTheme.AccentSoft.g, popupTheme.AccentSoft.b, 0.8f);
				((Shadow)obj5).effectDistance = new Vector2(1f, -1f);
				RectTransform component6 = val9.GetComponent<RectTransform>();
				component6.anchorMin = new Vector2(1f, 1f);
				component6.anchorMax = new Vector2(1f, 1f);
				component6.pivot = new Vector2(1f, 1f);
				component6.anchoredPosition = new Vector2(-18f, -16f);
				component6.sizeDelta = new Vector2(180f, 28f);
				GameObject val10 = new GameObject("BadgeText");
				val10.transform.SetParent(val9.transform, false);
				TextMeshProUGUI obj6 = val10.AddComponent<TextMeshProUGUI>();
				((TMP_Text)obj6).text = text2;
				((TMP_Text)obj6).font = LoadGameFont();
				((TMP_Text)obj6).fontSize = 14f;
				((TMP_Text)obj6).fontStyle = (FontStyles)1;
				((TMP_Text)obj6).alignment = (TextAlignmentOptions)514;
				((Graphic)obj6).color = (isPremium ? popupTheme.BadgeText : Color.white);
				RectTransform component7 = val10.GetComponent<RectTransform>();
				component7.anchorMin = Vector2.zero;
				component7.anchorMax = Vector2.one;
				component7.sizeDelta = Vector2.zero;
				EnsureSuccessCircleSprite();
				GameObject val11 = new GameObject("IconBG");
				val11.transform.SetParent(val4.transform, false);
				Image obj7 = val11.AddComponent<Image>();
				obj7.sprite = _successCircleSprite;
				((Graphic)obj7).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.45f);
				RectTransform component8 = val11.GetComponent<RectTransform>();
				component8.anchorMin = new Vector2(0.5f, 0.5f);
				component8.anchorMax = new Vector2(0.5f, 0.5f);
				component8.pivot = new Vector2(0.5f, 0.5f);
				component8.anchoredPosition = new Vector2(0f, 135f);
				component8.sizeDelta = new Vector2(100f, 100f);
				GameObject val12 = new GameObject("Checkmark");
				val12.transform.SetParent(val11.transform, false);
				GameObject val13 = new GameObject("Line1");
				val13.transform.SetParent(val12.transform, false);
				((Graphic)val13.AddComponent<Image>()).color = popupTheme.AccentSoft;
				RectTransform component9 = val13.GetComponent<RectTransform>();
				component9.sizeDelta = new Vector2(10f, 30f);
				component9.anchoredPosition = new Vector2(-10f, -5f);
				((Transform)component9).localRotation = Quaternion.Euler(0f, 0f, 45f);
				GameObject val14 = new GameObject("Line2");
				val14.transform.SetParent(val12.transform, false);
				((Graphic)val14.AddComponent<Image>()).color = popupTheme.AccentSoft;
				RectTransform component10 = val14.GetComponent<RectTransform>();
				component10.sizeDelta = new Vector2(10f, 55f);
				component10.anchoredPosition = new Vector2(12f, 10f);
				((Transform)component10).localRotation = Quaternion.Euler(0f, 0f, -45f);
				GameObject val15 = new GameObject("TitleText");
				val15.transform.SetParent(val4.transform, false);
				TextMeshProUGUI obj8 = val15.AddComponent<TextMeshProUGUI>();
				((TMP_Text)obj8).text = (isPremium ? "PREMIUM ACCESS GRANTED" : "ACCESS GRANTED");
				((TMP_Text)obj8).font = LoadGameFont();
				((TMP_Text)obj8).fontSize = 26f;
				((TMP_Text)obj8).fontStyle = (FontStyles)1;
				((Graphic)obj8).color = (isPremium ? popupTheme.AccentSoft : Color.white);
				((TMP_Text)obj8).alignment = (TextAlignmentOptions)514;
				RectTransform component11 = val15.GetComponent<RectTransform>();
				component11.anchorMin = new Vector2(0.5f, 0.5f);
				component11.anchorMax = new Vector2(0.5f, 0.5f);
				component11.pivot = new Vector2(0.5f, 0.5f);
				component11.anchoredPosition = new Vector2(0f, 85f);
				component11.sizeDelta = new Vector2(560f, 36f);
				GameObject val16 = new GameObject("AvatarContainer");
				val16.transform.SetParent(val4.transform, false);
				RectTransform val17 = val16.AddComponent<RectTransform>();
				val17.anchorMin = new Vector2(0.5f, 0.5f);
				val17.anchorMax = new Vector2(0.5f, 0.5f);
				val17.pivot = new Vector2(0.5f, 0.5f);
				val17.anchoredPosition = new Vector2(-220f, 55f);
				val17.sizeDelta = new Vector2(64f, 64f);
				_activationAvatarContainer = val17;
				GameObject val18 = new GameObject("AvatarBG");
				val18.transform.SetParent(val16.transform, false);
				((Graphic)val18.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.3f);
				RectTransform component12 = val18.GetComponent<RectTransform>();
				component12.anchorMin = Vector2.zero;
				component12.anchorMax = Vector2.one;
				component12.sizeDelta = Vector2.zero;
				GameObject val19 = new GameObject("AvatarImage");
				val19.transform.SetParent(val16.transform, false);
				RawImage val20 = val19.AddComponent<RawImage>();
				((Graphic)val20).color = Color.white;
				RectTransform component13 = val19.GetComponent<RectTransform>();
				component13.anchorMin = new Vector2(0.1f, 0.1f);
				component13.anchorMax = new Vector2(0.9f, 0.9f);
				component13.sizeDelta = Vector2.zero;
				_discordAvatarImage = val20;
				if ((Object)(object)DiscordAuthManager.AvatarTexture != (Object)null)
				{
					_discordAvatarImage.texture = (Texture)(object)DiscordAuthManager.AvatarTexture;
					val16.SetActive(true);
				}
				else
				{
					val16.SetActive(false);
				}
				GameObject val21 = new GameObject("WelcomeText");
				val21.transform.SetParent(val4.transform, false);
				TextMeshProUGUI obj9 = val21.AddComponent<TextMeshProUGUI>();
				((TMP_Text)obj9).text = "Welcome, <b>" + text + "</b>";
				((TMP_Text)obj9).font = LoadGameFont();
				((TMP_Text)obj9).fontSize = 18f;
				((Graphic)obj9).color = Color.white;
				((TMP_Text)obj9).alignment = (TextAlignmentOptions)514;
				((TMP_Text)obj9).richText = true;
				RectTransform component14 = val21.GetComponent<RectTransform>();
				component14.anchorMin = new Vector2(0.5f, 0.5f);
				component14.anchorMax = new Vector2(0.5f, 0.5f);
				component14.pivot = new Vector2(0.5f, 0.5f);
				component14.anchoredPosition = new Vector2(0f, 55f);
				component14.sizeDelta = new Vector2(560f, 28f);
				if (!string.IsNullOrEmpty(value))
				{
					GameObject val22 = new GameObject("TimeRemainingText");
					val22.transform.SetParent(val4.transform, false);
					TextMeshProUGUI obj10 = val22.AddComponent<TextMeshProUGUI>();
					((TMP_Text)obj10).text = $"<color=#{ColorUtility.ToHtmlStringRGB(popupTheme.Accent)}>{value}</color>";
					((TMP_Text)obj10).font = LoadGameFont();
					((TMP_Text)obj10).fontSize = 14f;
					((Graphic)obj10).color = Color.white;
					((TMP_Text)obj10).alignment = (TextAlignmentOptions)514;
					((TMP_Text)obj10).richText = true;
					RectTransform component15 = val22.GetComponent<RectTransform>();
					component15.anchorMin = new Vector2(0.5f, 0.5f);
					component15.anchorMax = new Vector2(0.5f, 0.5f);
					component15.pivot = new Vector2(0.5f, 0.5f);
					component15.anchoredPosition = new Vector2(0f, 28f);
					component15.sizeDelta = new Vector2(560f, 22f);
				}
				GameObject val23 = new GameObject("Divider1");
				val23.transform.SetParent(val4.transform, false);
				((Graphic)val23.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.15f);
				RectTransform component16 = val23.GetComponent<RectTransform>();
				component16.anchorMin = new Vector2(0.5f, 0.5f);
				component16.anchorMax = new Vector2(0.5f, 0.5f);
				component16.pivot = new Vector2(0.5f, 0.5f);
				component16.anchoredPosition = new Vector2(0f, 8f);
				component16.sizeDelta = new Vector2(480f, 1f);
				string text3 = (isPremium ? "<size=16><color=#66FF88>✓</color> No ads / shortener links\n<color=#66FF88>✓</color> Unlimited premium access\n<color=#66FF88>✓</color> Priority support</size>" : "<size=16><color=#66FF88>✓</color> All features unlocked\n<color=#999999>•</color> Single-use key</size>");
				GameObject val24 = new GameObject("BenefitsText");
				val24.transform.SetParent(val4.transform, false);
				TextMeshProUGUI obj11 = val24.AddComponent<TextMeshProUGUI>();
				((TMP_Text)obj11).text = text3;
				((TMP_Text)obj11).font = LoadGameFont();
				((TMP_Text)obj11).fontSize = 16f;
				((Graphic)obj11).color = Color.white;
				((TMP_Text)obj11).alignment = (TextAlignmentOptions)514;
				((TMP_Text)obj11).enableWordWrapping = true;
				((TMP_Text)obj11).richText = true;
				((TMP_Text)obj11).lineSpacing = 6f;
				RectTransform component17 = val24.GetComponent<RectTransform>();
				component17.anchorMin = new Vector2(0.5f, 0.5f);
				component17.anchorMax = new Vector2(0.5f, 0.5f);
				component17.pivot = new Vector2(0.5f, 0.5f);
				component17.anchoredPosition = new Vector2(0f, -40f);
				component17.sizeDelta = new Vector2(520f, 140f);
				GameObject val25 = new GameObject("Divider2");
				val25.transform.SetParent(val4.transform, false);
				((Graphic)val25.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.12f);
				RectTransform component18 = val25.GetComponent<RectTransform>();
				component18.anchorMin = new Vector2(0.5f, 0.5f);
				component18.anchorMax = new Vector2(0.5f, 0.5f);
				component18.pivot = new Vector2(0.5f, 0.5f);
				component18.anchoredPosition = new Vector2(0f, -105f);
				component18.sizeDelta = new Vector2(480f, 1f);
				GameObject val26 = new GameObject("FooterText");
				val26.transform.SetParent(val4.transform, false);
				TextMeshProUGUI obj12 = val26.AddComponent<TextMeshProUGUI>();
				ConfigEntry<KeyCode> menuToggleKey = CheatConfig.MenuToggleKey;
				string text4 = ((object)(KeyCode)((menuToggleKey == null) ? 282 : ((int)menuToggleKey.Value))/*cast due to .constrained prefix*/).ToString();
				((TMP_Text)obj12).text = "<size=14><color=#AABBCC>Press  <b>[ " + text4 + " ]</b>  to open/close menu</color></size>\n<size=13><color=#00DDDD><u><link=\"https://crewcore.online\">crewcore.online</link></u></color></size>";
				((TMP_Text)obj12).font = LoadGameFont();
				((TMP_Text)obj12).fontSize = 14f;
				((Graphic)obj12).color = Color.white;
				((TMP_Text)obj12).alignment = (TextAlignmentOptions)514;
				((TMP_Text)obj12).enableWordWrapping = true;
				((TMP_Text)obj12).richText = true;
				RectTransform component19 = val26.GetComponent<RectTransform>();
				component19.anchorMin = new Vector2(0.5f, 0.5f);
				component19.anchorMax = new Vector2(0.5f, 0.5f);
				component19.pivot = new Vector2(0.5f, 0.5f);
				component19.anchoredPosition = new Vector2(0f, -135f);
				component19.sizeDelta = new Vector2(560f, 48f);
				GameObject val27 = new GameObject("PopupAccentBottom");
				val27.transform.SetParent(val4.transform, false);
				((Graphic)val27.AddComponent<Image>()).color = new Color(popupTheme.Accent.r, popupTheme.Accent.g, popupTheme.Accent.b, 0.8f);
				RectTransform component20 = val27.GetComponent<RectTransform>();
				component20.anchorMin = new Vector2(0f, 0f);
				component20.anchorMax = new Vector2(1f, 0f);
				component20.pivot = new Vector2(0.5f, 0f);
				component20.sizeDelta = new Vector2(-10f, 2f);
				component20.anchoredPosition = new Vector2(0f, 1f);
				float num = -195f;
				if (isPremium)
				{
					TextMeshProUGUI saveStatusTMP = CreateTMPText(component2, "", 12, new Color(0.75f, 0.85f, 0.95f, 1f), new Vector2(0f, num - 23f), new Vector2(560f, 24f), (TextAlignmentOptions)514);
					if ((Object)(object)saveStatusTMP != (Object)null)
					{
						((TMP_Text)saveStatusTMP).richText = true;
						((TMP_Text)saveStatusTMP).enableWordWrapping = true;
					}
					Button saveBtn = null;
					saveBtn = CreateTMPButton(component2, "SAVE KEY", InteropFix.Cast((Action)delegate
					{
						try
						{
							if (TrySaveKeyToFile(ModKeyValidator.CurrentKey, out var savedPath, out var error))
							{
								if ((Object)(object)saveStatusTMP != (Object)null)
								{
									((TMP_Text)saveStatusTMP).text = "<color=#66FF88>Saved!</color> " + savedPath;
								}
								if ((Object)(object)saveBtn != (Object)null)
								{
									((Selectable)saveBtn).interactable = false;
								}
								TextMeshProUGUI val30 = (((Object)(object)saveBtn != (Object)null) ? ((Component)saveBtn).GetComponentInChildren<TextMeshProUGUI>() : null);
								if ((Object)(object)val30 != (Object)null)
								{
									((TMP_Text)val30).text = "SAVED";
								}
							}
							else if ((Object)(object)saveStatusTMP != (Object)null)
							{
								((TMP_Text)saveStatusTMP).text = "<color=#FF6666>Save failed:</color> " + error;
							}
						}
						catch (Exception ex)
						{
							if ((Object)(object)saveStatusTMP != (Object)null)
							{
								((TMP_Text)saveStatusTMP).text = "<color=#FF6666>Save failed:</color> " + ex.Message;
							}
						}
					}), new Vector2(-110f, num), new Vector2(200f, 50f));
					StyleButton(saveBtn, ref _cachedSaveKeyBtnSprite, popupTheme.SecondaryButtonTop, popupTheme.SecondaryButtonBottom);
					TextMeshProUGUI componentInChildren = ((Component)saveBtn).GetComponentInChildren<TextMeshProUGUI>();
					if ((Object)(object)componentInChildren != (Object)null)
					{
						((TMP_Text)componentInChildren).fontStyle = (FontStyles)1;
						((TMP_Text)componentInChildren).fontSize = 18f;
					}
					Button val28 = CreateTMPButton(component2, "CONTINUE", InteropFix.Cast((Action)delegate
					{
						Object.Destroy((Object)(object)successPopupGO);
					}), new Vector2(110f, num), new Vector2(200f, 50f));
					StyleButton(val28, ref _cachedOkBtnSprite, popupTheme.PrimaryButtonTop, popupTheme.PrimaryButtonBottom);
					TextMeshProUGUI componentInChildren2 = ((Component)val28).GetComponentInChildren<TextMeshProUGUI>();
					if ((Object)(object)componentInChildren2 != (Object)null)
					{
						((TMP_Text)componentInChildren2).fontStyle = (FontStyles)1;
						((TMP_Text)componentInChildren2).fontSize = 18f;
					}
				}
				else
				{
					Button val29 = CreateTMPButton(component2, "CONTINUE", InteropFix.Cast((Action)delegate
					{
						Object.Destroy((Object)(object)successPopupGO);
					}), new Vector2(0f, num), new Vector2(220f, 50f));
					StyleButton(val29, ref _cachedOkBtnSprite, popupTheme.PrimaryButtonTop, popupTheme.PrimaryButtonBottom);
					TextMeshProUGUI componentInChildren3 = ((Component)val29).GetComponentInChildren<TextMeshProUGUI>();
					if ((Object)(object)componentInChildren3 != (Object)null)
					{
						((TMP_Text)componentInChildren3).fontStyle = (FontStyles)1;
						((TMP_Text)componentInChildren3).fontSize = 20f;
					}
				}
				((MonoBehaviour)this).StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(AnimatePopupContent(component2, val11.transform, val5)));
			}
			catch (Exception value2)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error showing popup: {value2}"));
			}
		}

		private void CreatePopupStarfield(Transform parent)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			_popupStars.Clear();
			for (int i = 0; i < 40; i++)
			{
				GameObject val = new GameObject("PopupStar");
				val.transform.SetParent(parent, false);
				Image val2 = val.AddComponent<Image>();
				((Graphic)val2).color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.2f, 0.6f));
				RectTransform component = val.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0.5f);
				component.anchorMax = new Vector2(0.5f, 0.5f);
				float num = UnityEngine.Random.Range(2f, 4f);
				component.sizeDelta = new Vector2(num, num);
				component.anchoredPosition = new Vector2((float)UnityEngine.Random.Range(-960, 960), (float)UnityEngine.Random.Range(-540, 540));
				_popupStars.Add(new Star
				{
					Rect = component,
					Speed = UnityEngine.Random.Range(0.2f, 1f),
					Image = val2
				});
			}
		}

		[HideFromIl2Cpp]
		private IEnumerator AnimatePopupContent(RectTransform content, Transform popup, CanvasGroup canvasGroup = null)
		{
			if ((Object)(object)content == (Object)null || (Object)(object)popup == (Object)null)
			{
				yield break;
			}
			try
			{
				((Transform)content).localScale = Vector3.zero;
			}
			catch
			{
				yield break;
			}
			try
			{
				popup.localScale = Vector3.zero;
			}
			catch
			{
			}
			if ((Object)(object)canvasGroup != (Object)null)
			{
				try
				{
					canvasGroup.alpha = 0f;
				}
				catch
				{
				}
			}
			float duration = 0.5f;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				if ((Object)(object)content == (Object)null)
				{
					yield break;
				}
				elapsed += Time.deltaTime;
				float num = elapsed / duration;
				float num2 = Mathf.Sin(-13f * (num + 1f) * (float)System.Math.PI * 0.5f) * Mathf.Pow(2f, -10f * num) + 1f;
				try
				{
					((Transform)content).localScale = Vector3.one * num2;
				}
				catch
				{
					yield break;
				}
				if (num > 0.3f && (Object)(object)popup != (Object)null)
				{
					try
					{
						popup.localScale = Vector3.one * Mathf.Clamp01((num - 0.3f) * 2f);
					}
					catch
					{
					}
				}
				if ((Object)(object)canvasGroup != (Object)null)
				{
					try
					{
						canvasGroup.alpha = Mathf.Clamp01(num * 2.5f);
					}
					catch
					{
					}
				}
				yield return null;
			}
			if ((Object)(object)content != (Object)null)
			{
				try
				{
					((Transform)content).localScale = Vector3.one;
				}
				catch
				{
				}
			}
			if ((Object)(object)popup != (Object)null)
			{
				try
				{
					popup.localScale = Vector3.one;
				}
				catch
				{
				}
			}
			if ((Object)(object)canvasGroup != (Object)null)
			{
				try
				{
					canvasGroup.alpha = 1f;
				}
				catch
				{
				}
			}
			float loopTime = 0f;
			while ((Object)(object)successPopupGO != (Object)null)
			{
				loopTime += Time.deltaTime;
				if (_popupStars != null)
				{
					for (int i = 0; i < _popupStars.Count; i++)
					{
						Star star = _popupStars[i];
						if ((Object)(object)star.Rect != (Object)null)
						{
							Vector2 anchoredPosition = star.Rect.anchoredPosition;
							anchoredPosition.x -= star.Speed * Time.deltaTime * 30f;
							if (anchoredPosition.x < -1000f)
							{
								anchoredPosition.x = 1000f;
							}
							star.Rect.anchoredPosition = anchoredPosition;
							if ((Object)(object)star.Image != (Object)null)
							{
								float num3 = 0.3f + Mathf.Sin(loopTime * star.Speed * 3f + (float)i * 1.7f) * 0.25f;
								Color color = ((Graphic)star.Image).color;
								((Graphic)star.Image).color = new Color(color.r, color.g, color.b, Mathf.Clamp(num3, 0.08f, 0.7f));
							}
						}
					}
				}
				yield return null;
			}
		}

		private static string GetGameRootPathSafe()
		{
			try
			{
				string gameRootPath = Paths.GameRootPath;
				if (!string.IsNullOrWhiteSpace(gameRootPath))
				{
					return gameRootPath;
				}
			}
			catch
			{
			}
			try
			{
				string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
				if (!string.IsNullOrWhiteSpace(baseDirectory))
				{
					return baseDirectory;
				}
			}
			catch
			{
			}
			try
			{
				string currentDirectory = Environment.CurrentDirectory;
				if (!string.IsNullOrWhiteSpace(currentDirectory))
				{
					return currentDirectory;
				}
			}
			catch
			{
			}
			return ".";
		}

		private string GetSavedKeyFilePath()
		{
			return Path.Combine(GetGameRootPathSafe(), "crewcore_key.txt");
		}

		private static bool IsKeyFormatValid(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			string input = key.Trim().ToUpperInvariant();
			return ModKeyValidator.KeyFormatRegex.IsMatch(input);
		}

		private SavedKeyLoadResult TryLoadSavedKeyFromFile(out string key, out string detail)
		{
			key = null;
			detail = null;
			string savedKeyFilePath = GetSavedKeyFilePath();
			try
			{
				if (!File.Exists(savedKeyFilePath))
				{
					return SavedKeyLoadResult.NotFound;
				}
				string text = File.ReadAllText(savedKeyFilePath);
				if (string.IsNullOrWhiteSpace(text))
				{
					detail = "Saved key file is empty.";
					return SavedKeyLoadResult.Invalid;
				}
				string text2 = text.Trim();
				if (text2.StartsWith("ENC2:"))
				{
					try
					{
						byte[] bytes = DecryptLocalKey(Convert.FromBase64String(text2.Substring(5).Trim()));
						text2 = Encoding.UTF8.GetString(bytes);
					}
					catch
					{
						detail = "Key file was saved on a different machine.";
						return SavedKeyLoadResult.Invalid;
					}
				}
				else if (text2.StartsWith("ENC:"))
				{
					try
					{
						byte[] bytes2 = DecryptLocalKeyLegacy(Convert.FromBase64String(text2.Substring(4).Trim()));
						text2 = Encoding.UTF8.GetString(bytes2);
					}
					catch
					{
						detail = "Key file was saved on a different machine.";
						return SavedKeyLoadResult.Invalid;
					}
				}
				else
				{
					string[] array = text.Split(new char[2] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						string text3 = array[i].Trim();
						if (text3.Length != 0)
						{
							text2 = text3;
							break;
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					detail = "Saved key file is empty.";
					return SavedKeyLoadResult.Invalid;
				}
				if (text2.StartsWith("KEY=", System.StringComparison.OrdinalIgnoreCase))
				{
					text2 = text2.Substring(4).Trim();
				}
				text2 = text2.Trim().ToUpperInvariant();
				if (!IsKeyFormatValid(text2))
				{
					detail = "Saved key has invalid format.";
					return SavedKeyLoadResult.Invalid;
				}
				key = text2;
				return SavedKeyLoadResult.Loaded;
			}
			catch (Exception ex)
			{
				detail = ex.Message;
				return SavedKeyLoadResult.Error;
			}
		}

		private bool TrySaveKeyToFile(string key, out string savedPath, out string error)
		{
			savedPath = GetSavedKeyFilePath();
			error = null;
			try
			{
				if (string.IsNullOrWhiteSpace(key))
				{
					error = "No key to save.";
					return false;
				}
				string text = key.Trim().ToUpperInvariant();
				if (!IsKeyFormatValid(text))
				{
					error = "Key has invalid format.";
					return false;
				}
				byte[] inArray = EncryptLocalKey(Encoding.UTF8.GetBytes(text));
				string text2 = "ENC2:" + Convert.ToBase64String(inArray);
				File.WriteAllText(savedPath, text2 + Environment.NewLine);
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		private static byte[] EncryptLocalKey(byte[] data)
		{
			byte[] array = new byte[16];
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(array);
			}
			byte[] key = DeriveKeyPBKDF2(GetMachinePassword(), array);
			using Aes aes = Aes.Create();
			aes.Key = key;
			aes.GenerateIV();
			using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
			byte[] array2 = cryptoTransform.TransformFinalBlock(data, 0, data.Length);
			byte[] array3 = new byte[16 + aes.IV.Length + array2.Length];
			System.Buffer.BlockCopy(array, 0, array3, 0, 16);
			System.Buffer.BlockCopy(aes.IV, 0, array3, 16, aes.IV.Length);
			System.Buffer.BlockCopy(array2, 0, array3, 32, array2.Length);
			return array3;
		}

		private static byte[] DecryptLocalKey(byte[] encryptedData)
		{
			byte[] array = new byte[16];
			System.Buffer.BlockCopy(encryptedData, 0, array, 0, 16);
			byte[] key = DeriveKeyPBKDF2(GetMachinePassword(), array);
			using Aes aes = Aes.Create();
			aes.Key = key;
			byte[] array2 = new byte[16];
			System.Buffer.BlockCopy(encryptedData, 16, array2, 0, 16);
			aes.IV = array2;
			byte[] array3 = new byte[encryptedData.Length - 32];
			System.Buffer.BlockCopy(encryptedData, 32, array3, 0, array3.Length);
			using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
			return cryptoTransform.TransformFinalBlock(array3, 0, array3.Length);
		}

		private static byte[] DecryptLocalKeyLegacy(byte[] encryptedData)
		{
			using Aes aes = Aes.Create();
			aes.Key = GetLegacyEncryptionKey();
			byte[] array = new byte[16];
			System.Buffer.BlockCopy(encryptedData, 0, array, 0, 16);
			aes.IV = array;
			byte[] array2 = new byte[encryptedData.Length - 16];
			System.Buffer.BlockCopy(encryptedData, 16, array2, 0, array2.Length);
			using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
			return cryptoTransform.TransformFinalBlock(array2, 0, array2.Length);
		}

		private static byte[] DeriveKeyPBKDF2(byte[] password, byte[] salt)
		{
			using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
			return rfc2898DeriveBytes.GetBytes(32);
		}

		private static byte[] GetMachinePassword()
		{
			string text = "";
			try
			{
				using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", writable: false);
				if (registryKey != null)
				{
					text = registryKey.GetValue("MachineGuid")?.ToString() ?? "";
				}
			}
			catch
			{
			}
			string s = string.Join("|", text, Environment.MachineName, Environment.UserName, SystemInfo.processorType, SystemInfo.processorCount.ToString(), Environment.OSVersion.ToString());
			return Encoding.UTF8.GetBytes(s);
		}

		private static byte[] GetLegacyEncryptionKey()
		{
			string s = Environment.MachineName + Environment.UserName + Environment.OSVersion.ToString();
			using SHA256 sHA = SHA256.Create();
			return sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
		}

		private void ProcessApiKeyValidation(string keyToValidate)
		{
			if (!isValidatingNow)
			{
				if (string.IsNullOrWhiteSpace(keyToValidate))
				{
					currentActivationStatusMessage = "Please enter a key.";
					ManageActivationUIVisibility();
					return;
				}
				keyToValidate = keyToValidate.Trim();
				isValidatingNow = true;
				currentActivationStatusMessage = "Validating your key, please wait...";
				ManageActivationUIVisibility();
				pendingValidationTask = ValidateKeyAndSetState(keyToValidate);
			}
		}

		[HideFromIl2Cpp]
		private async Task ValidateKeyAndSetState(string key)
		{
			_pendingValidationResult = await ModKeyValidator.ValidateKeyAsync(key);
			_hasPendingValidationResult = true;
		}

		private void CleanupActivationUI()
		{
			try
			{
				if ((Object)(object)canvasGO != (Object)null)
				{
					if ((Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeInHierarchy)
					{
						((Component)activationCanvasTMP).gameObject.SetActive(false);
					}
					activationPanelGO = null;
					apiKeyInputFieldTMP = null;
					statusMessageTextTMP = null;
					validateButtonTMP = null;
					getKeyButtonTMP = null;
					validateButtonTextTMP = null;
					_copyLinkBtnTMP = null;
					_copyLinkTextTMP = null;
					activationCanvasTMP = null;
					if (Application.isPlaying)
					{
						Object.Destroy((Object)(object)canvasGO);
					}
					canvasGO = null;
					((BasePlugin)Instance).Log.LogInfo((object)"Activation UI cleaned up.");
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error CleanupActivationUI: {value}"));
			}
		}

		private void HandleValidationComplete()
		{
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Expected O, but got Unknown
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Expected O, but got Unknown
			try
			{
				currentActivationStatusMessage = ModKeyValidator.LastValidationMessage;
				string text = ((DiscordAuthManager.IsLoggedIn && !string.IsNullOrEmpty(DiscordAuthManager.DiscordUsername)) ? DiscordAuthManager.DiscordUsername : (ModKeyValidator.ValidatedUsername ?? "User"));
				bool item = _pendingValidationResult.success;
				isModGloballyActivated = item;
				bool flag = default(bool);
				if (isModGloballyActivated)
				{
					ManualLogSource log = ((BasePlugin)Instance).Log;
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(40, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModMenuCrew] Validation Success. User: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
					}
					log.LogInfo(val);
				}
				else
				{
					ManualLogSource log2 = ((BasePlugin)Instance).Log;
					BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(38, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ModMenuCrew] Validation Failed. Msg: ");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(currentActivationStatusMessage);
					}
					log2.LogError(val2);
				}
				if (isModGloballyActivated)
				{
					_isAutoValidatingSavedKey = false;
					CleanupActivationUI();
					if (ServerData.IsLoaded)
					{
						bool flag2 = false;
						try
						{
							ServerData.UISnapshot currentSnapshot = ServerData.CurrentSnapshot;
							if (currentSnapshot != null)
							{
								byte[] array = currentSnapshot.LobbyBytecode ?? currentSnapshot.GameBytecode;
								if (array != null && array.Length >= 536 && array[0] == 80 && array[1] == 79 && array[2] == 76 && array[3] == 53)
								{
									long num = BitConverter.ToInt64(array, 528);
									long num2 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ModKeyValidator.ServerTimeOffsetMs;
									flag2 = Math.Abs(num - num2) < 300000;
								}
							}
						}
						catch
						{
						}
						if (flag2)
						{
							_useGhostUI = true;
							if (Instance != null)
							{
								((BasePlugin)Instance).Log.LogInfo((object)"[ModMenuCrew] Ghost UI Enabled.");
							}
						}
						else
						{
							_useGhostUI = true;
							if (Instance != null)
							{
								((BasePlugin)Instance).Log.LogWarning((object)"[ModMenuCrew] Ghost UI Enabled (bytecodes may be stale, requesting refresh).");
							}
							try
							{
								ModKeyValidator.ForceHeartbeatWakeup();
							}
							catch
							{
							}
						}
					}
					else if (Instance != null)
					{
						((BasePlugin)Instance).Log.LogWarning((object)"[ModMenuCrew] Validation success but ServerData NOT loaded. Ghost UI delayed.");
					}
					if (ModKeyValidator.IsPremium && !string.IsNullOrEmpty(ModKeyValidator.CurrentKey))
					{
						try
						{
							TrySaveKeyToFile(ModKeyValidator.CurrentKey, out var _, out var _);
						}
						catch
						{
						}
					}
					if (!ModKeyValidator._isHeartbeatRunning)
					{
						try
						{
							ModKeyValidator.StartHeartbeat();
						}
						catch
						{
						}
					}
					_lastWindowTitle = "";
					try
					{
						ShowActivationSuccessPopup(text, ModKeyValidator.IsPremium, ModKeyValidator.KeyType, ModKeyValidator.TimeRemaining);
						return;
					}
					catch (Exception value)
					{
						Debug.LogError(InteropFix.Cast($"[ModMenuCrew] CRASH in ShowActivationSuccessPopup: {value}"));
						return;
					}
				}
				string text2 = "Error: " + currentActivationStatusMessage;
				if (currentActivationStatusMessage.Contains("expired", System.StringComparison.OrdinalIgnoreCase))
				{
					text2 += "\nYour key has expired. Please generate a new one.";
				}
				else if (currentActivationStatusMessage.Contains("used", System.StringComparison.OrdinalIgnoreCase))
				{
					text2 += "\nThis key is already in use. Please generate a new one.";
				}
				ShowNotification(text2);
				if (_isAutoValidatingSavedKey)
				{
					_isAutoValidatingSavedKey = false;
					currentActivationStatusMessage = "Saved key validation failed. Please enter your key manually.";
					if ((Object)(object)activationCanvasTMP == (Object)null)
					{
						SetupActivationUI_TMP();
					}
					if ((Object)(object)activationCanvasTMP != (Object)null)
					{
						((Component)activationCanvasTMP).gameObject.SetActive(true);
						_shouldAutoFocus = true;
						_currentAlpha = 0f;
						_panelScaleCurrent = 0.92f;
						_revealStartTime = Time.realtimeSinceStartup;
						_revealComplete = false;
						if (InteropFix.Cast((Object)(object)_panelCanvasGroup))
						{
							_panelCanvasGroup.alpha = 0f;
						}
						if ((Object)(object)apiKeyInputFieldTMP != (Object)null)
						{
							apiKeyInputFieldTMP.text = "";
						}
						ManageActivationUIVisibility();
					}
					hasAttemptedInitialActivationUIShow = true;
				}
				if ((Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
				{
					ManageActivationUIVisibility();
				}
				if ((Object)(object)apiKeyInputFieldTMP != (Object)null && (string.IsNullOrEmpty(apiKeyInputFieldTMP.text) || currentActivationStatusMessage.Contains("empty")))
				{
					apiKeyInputFieldTMP.text = "";
				}
			}
			catch (Exception value2)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error HandleValidationComplete: {value2}"));
			}
			finally
			{
				isValidatingNow = false;
				if (!isModGloballyActivated && (Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
				{
					ManageActivationUIVisibility();
				}
			}
		}

		private void DrawMainModWindowIMGUI()
		{
			try
			{
				if (!isModGloballyActivated)
				{
					return;
				}
				if (!_showLocalMainWindow)
				{
					return;
				}
				UpdateWindowTitle();
				float num = Mathf.Max(520f, CheatConfig.MenuWidth?.Value ?? 980f);
				float num2 = Mathf.Max(460f, CheatConfig.MenuHeight?.Value ?? 680f);
				float num3 = Mathf.Max(40f, (float)Screen.width - num - 20f);
				float num4 = Mathf.Max(40f, (float)Screen.height - num2 - 20f);
				_localMainWindowRect.width = Mathf.Min(num, (float)Screen.width - 20f);
				_localMainWindowRect.height = Mathf.Min(num2, (float)Screen.height - 20f);
				_localMainWindowRect.x = Mathf.Clamp(_localMainWindowRect.x, 10f, num3);
				_localMainWindowRect.y = Mathf.Clamp(_localMainWindowRect.y, 10f, num4);
				GUILayout.BeginArea(_localMainWindowRect, GuiStyles.WindowStyle);
				DrawLocalMainWindowContents();
				GUILayout.EndArea();
			}
			catch (Exception ex)
			{
				GUILayout.Label("Menu Error: " + ex.Message, (Il2CppReferenceArray<GUILayoutOption>)null);
				if (Time.frameCount % 300 == 0)
				{
					Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Draw Error: {ex}"));
				}
			}
		}

		private void DrawLocalMainWindowContents()
		{
			GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical(GuiStyles.HeaderBackgroundStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(34f) });
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("Among Us Mod Menu Crew 6.0.8 | Decompiled by jakartanosia", GuiStyles.TitleLabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUI.color = GuiStyles.Theme.Success;
			GUILayout.Label("ONLINE", GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(90f) });
			GUI.color = Color.white;
			if (GUILayout.Button("x", GuiStyles.TitleBarButtonStyle, (Il2CppReferenceArray<GUILayoutOption>)null))
			{
				_showLocalMainWindow = false;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.Space(6f);
			DrawLocalTabBar();
			GuiStyles.DrawSeparator();
			_localMainScrollPosition = GUILayout.BeginScrollView(_localMainScrollPosition, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandHeight(true) });
			DrawSelectedLocalTab();
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

		private void DrawLocalTabBar()
		{
			int num = 5;
			int num2 = Mathf.CeilToInt((float)_localMainTabLabels.Length / (float)num);
			int num3 = 0;
			for (int i = 0; i < num2; i++)
			{
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				for (int j = 0; j < num; j++)
				{
					if (num3 >= _localMainTabLabels.Length)
					{
						GUILayout.FlexibleSpace();
					}
					else if (GuiStyles.DrawTab(_localMainTabLabels[num3], _localMainTabIndex == num3))
					{
						_localMainTabIndex = num3;
					}
					num3++;
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawSelectedLocalTab()
		{
			string text = _localMainTabIds[Mathf.Clamp(_localMainTabIndex, 0, _localMainTabIds.Length - 1)];
			switch (text)
			{
			case "dashboard":
				DrawDashboardTab();
				break;
			case "game":
				DrawGameTabIMGUI();
				break;
			case "movement":
				DrawMovementTabIMGUI();
				break;
			case "sabotage":
				DrawSabotageTabIMGUI();
				break;
			case "impostor":
				DrawImpostorTabIMGUI();
				break;
			case "teleport":
				DrawTeleportTabIMGUI();
				break;
			case "players":
				DrawPlayersTabIMGUI();
				break;
			case "banpoints":
				DrawBanPointsTabIMGUI();
				break;
			case "lobbies":
				DrawLobbyListingTabIMGUI();
				break;
			case "settings":
				settingsTab?.DrawSettingsTab();
				break;
			case "replay":
				DrawReplayTabIMGUI();
				break;
			default:
				GUILayout.Label("Tab not available.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				break;
			}
			if (!IsOfflineLocalModeEnabled())
			{
				GUILayout.Space(6f);
				GUILayout.Label("Press F1 to switch UI mode.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			else
			{
				GUILayout.Space(6f);
			}
		}

		private bool IsCurrentUserHost()
		{
			AmongUsClient instance = AmongUsClient.Instance;
			return instance != null && ((InnerNetClient)instance).AmHost;
		}

		private void AdjustLocalBanPoints(int delta, bool clear = false)
		{
			try
			{
				var player = global::AmongUs.Data.DataManager.Player;
				var ban = ((player != null) ? player.ban : null);
				if (ban == null)
				{
					ShowNotification("Ban data unavailable.");
					return;
				}
				if (clear)
				{
					ban.BanPoints = 0f;
					ban.PreviousGameStartDate = new Il2CppSystem.DateTime(global::System.DateTime.MinValue.Ticks);
				}
				else
				{
					ban.BanPoints = Mathf.Max(0f, ban.BanPoints + (float)delta);
					if (delta > 0)
					{
						ban.PreviousGameStartDate = new Il2CppSystem.DateTime(global::System.DateTime.UtcNow.Ticks);
					}
				}
				Il2CppSystem.Action onBanPointsChanged = ban.OnBanPointsChanged;
				if (onBanPointsChanged != null)
				{
					onBanPointsChanged.Invoke();
				}
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
			catch (Exception ex)
			{
				Debug.LogError(InteropFix.Cast("[BanPoints] Error: " + ex.Message));
			}
		}

		private static bool IsImpostorFamilyRole(RoleBehaviour role, string cachedRoleName = null)
		{
			try
			{
				if ((Object)(object)role != (Object)null)
				{
					if (role.IsImpostor)
					{
						return true;
					}
					RoleTypes role2 = role.Role;
					int num = (int)role2;
					if (num == 1 || num == 5 || num == 7 || num == 9 || num == 18)
					{
						return true;
					}
				}
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(cachedRoleName))
			{
				string text = cachedRoleName.ToUpperInvariant();
				if (text == "IMPOSTOR" || text == "IMPOSTOR GHOST" || text == "SHAPESHIFTER" || text == "PHANTOM" || text == "VIPER")
				{
					return true;
				}
			}
			return false;
		}

		private void DrawPlayersTabIMGUI()
		{
			try
			{
				bool flag = IsCurrentUserHost();
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("Player Pick", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				GUI.color = (flag ? GuiStyles.Theme.Success : GuiStyles.Theme.Warning);
				GUILayout.Label(flag ? "Host controls: enabled" : "Host controls: disabled", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
				bool value = CheatConfig.AllowVenting?.Value ?? false;
				bool flag2 = GuiStyles.DrawBetterToggle(value, "Crew Vent", "Allow all players to use vents.");
				if (flag2 != value && CheatConfig.AllowVenting != null)
				{
					CheatConfig.AllowVenting.Value = flag2;
					ShowNotification("Crew Vent: " + (flag2 ? "ON" : "OFF"));
				}
				GUILayout.Space(6f);
				if (PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0)
				{
					GUILayout.Label("No players available.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.EndVertical();
					return;
				}
				System.Collections.Generic.List<PlayerControl> list = new System.Collections.Generic.List<PlayerControl>();
				foreach (PlayerControl item in (Il2CppArrayBase<PlayerControl>)(object)PlayerControl.AllPlayerControls.ToArray())
				{
					if ((Object)(object)item != (Object)null && (Object)(object)item.Data != (Object)null)
					{
						list.Add(item);
					}
				}
				list.Sort(delegate(PlayerControl a, PlayerControl b)
				{
					string specificRoleName3 = GameCheats.GetSpecificRoleName(a.Data.Role);
					string specificRoleName4 = GameCheats.GetSpecificRoleName(b.Data.Role);
					bool flag4 = IsImpostorFamilyRole(a.Data.Role, specificRoleName3);
					bool flag5 = IsImpostorFamilyRole(b.Data.Role, specificRoleName4);
					if (flag4 != flag5)
					{
						return flag5.CompareTo(flag4);
					}
					return string.Compare(a.Data.PlayerName ?? "", b.Data.PlayerName ?? "", global::System.StringComparison.OrdinalIgnoreCase);
				});
				_playersTabScrollPosition = GUILayout.BeginScrollView(_playersTabScrollPosition, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandHeight(true) });
				foreach (PlayerControl item2 in list)
				{
					bool flag3 = (Object)(object)item2 == (Object)(object)PlayerControl.LocalPlayer;
					string text = item2.Data.PlayerName ?? ("Player " + item2.PlayerId);
					string text2 = (item2.Data.Disconnected ? "DISCONNECTED" : (item2.Data.IsDead ? "DEAD" : "ALIVE"));
					string specificRoleName = GameCheats.GetSpecificRoleName(item2.Data.Role);
					bool flag6 = IsImpostorFamilyRole(item2.Data.Role, specificRoleName);
					GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = (flag6 ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white);
					GUILayout.Label($"{text}  [ID {item2.PlayerId}]  {specificRoleName}", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUI.color = GuiStyles.Theme.TextMuted;
					GUILayout.Label(text2, GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = Color.white;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					if (teleportManager != null && GUILayout.Button("TP", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(70f) }))
					{
						teleportManager.TeleportToPlayer(item2);
						ShowNotification("Teleported to " + text);
					}
					if (flag && !flag3 && !item2.Data.Disconnected)
					{
						if ((Object)(object)ShipStatus.Instance != (Object)null && !item2.Data.IsDead && GUILayout.Button("KILL", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
						{
							GameCheats.HostForceKillPlayer(item2);
							ShowNotification("Killed " + text);
						}
						if (GUILayout.Button("KICK", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
						{
							GameCheats.KickPlayer(item2, ban: false);
							ShowNotification("Kicked " + text);
						}
						if (GUILayout.Button("BAN", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
						{
							GameCheats.KickPlayer(item2, ban: true);
							ShowNotification("Banned " + text);
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					GUILayout.Space(4f);
				}
				GUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			catch (Exception ex)
			{
				GUILayout.Label("Players error: " + ex.Message, GuiStyles.ErrorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
		}

		private void DrawBanPointsTabIMGUI()
		{
			try
			{
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("Ban Points", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				var player = global::AmongUs.Data.DataManager.Player;
				var ban = ((player != null) ? player.ban : null);
				if (ban == null)
				{
					GUILayout.Label("Ban data unavailable.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.EndVertical();
					return;
				}
				_banPointsTabScrollPosition = GUILayout.BeginScrollView(_banPointsTabScrollPosition, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandHeight(true) });
				GUI.color = GuiStyles.Theme.Accent;
				GUILayout.Label($"Current Ban Points: {ban.BanPoints:F1}", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label($"Ban Minutes Left: {ban.BanMinutesLeft}", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
				GUILayout.Space(6f);
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("+20 points", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(20);
					ShowNotification("+20 Ban Points");
				}
				if (GUILayout.Button("+50 points", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(50);
					ShowNotification("+50 Ban Points");
				}
				if (GUILayout.Button("+100 points", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(100);
					ShowNotification("+100 Ban Points");
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("-10 points", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(-10);
					ShowNotification("-10 Ban Points");
				}
				if (GUILayout.Button("-20 points", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(-20);
					ShowNotification("-20 Ban Points");
				}
				if (GUILayout.Button("Clear", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					AdjustLocalBanPoints(0, clear: true);
					ShowNotification("Ban Points cleared");
				}
				GUILayout.EndHorizontal();
				GUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			catch (Exception ex)
			{
				GUILayout.Label("Ban Points error: " + ex.Message, GuiStyles.ErrorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
		}

		private void UpdateWindowTitle()
		{
			if (Time.unscaledTime - _lastTitleUpdateTime < 1f)
			{
				return;
			}
			_lastTitleUpdateTime = Time.unscaledTime;
			string text = (ModKeyValidator.IsPremium ? "★" : "◇");
			string text2 = ((!ModKeyValidator.IsPremium) ? "FREE" : (ModKeyValidator.KeyType?.ToUpperInvariant() ?? "PREMIUM"));
			string text3 = "";
			if (ModKeyValidator.IsPremium)
			{
				if (text2 == "LIFETIME" || !ModKeyValidator.TimeRemaining.HasValue)
				{
					text3 = " ∞";
				}
				else if (ModKeyValidator.TimeRemaining.HasValue)
				{
					System.TimeSpan value = ModKeyValidator.TimeRemaining.Value;
					text3 = ((value.TotalDays >= 1.0) ? $" • {(int)value.TotalDays}d {value.Hours}h" : ((!(value.TotalHours >= 1.0)) ? $" • {value.Minutes}m" : $" • {(int)value.TotalHours}h {value.Minutes}m"));
				}
			}
			string text4 = ((!ModKeyValidator.IsPremium) ? ("◇ FREE • @" + ModKeyValidator.ValidatedUsername) : ((string.IsNullOrEmpty(ModKeyValidator.ValidatedUsername) || !(ModKeyValidator.ValidatedUsername != "⭐ Premium User") || !(ModKeyValidator.ValidatedUsername != "Premium User") || !(ModKeyValidator.ValidatedUsername != "User")) ? (text + " " + text2 + text3) : $"{text} {text2}{text3} • {ModKeyValidator.ValidatedUsername}"));
			string text5 = text4;
			if (_lastWindowTitle != text5)
			{
				_lastWindowTitle = text5;
			}
		}

		private void DrawLobbyUI_IMGUI()
		{
			if (!ServerData.IsLoaded)
			{
				GUILayout.Label("Waiting for server authorization...", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				return;
			}
			if (ServerData.Tabs == null || ServerData.Tabs.Count == 0)
			{
				GUILayout.Label("No UI data received from server.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				return;
			}
			if (_lobbyTabRegistry == null)
			{
				_lobbyTabRegistry = new System.Collections.Generic.Dictionary<string, System.Action>
				{
					{ "dashboard", DrawDashboardTab },
					{
						"ban_menu",
						delegate
						{
							global::ModMenuCrew.UI.Menus.BanMenu.Draw();
						}
					},
					{ "lobbies", DrawLobbyListingTabIMGUI },
					{
						"spoofing",
						delegate
						{
							spoofingMenu?.Draw();
						}
					},
					{ "settings", settingsTab.DrawSettingsTab }
				};
			}
			GUILayout.Label("Use Ghost UI mode for full functionality.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}

		private void DrawGameEndButton(GameOverReason reason, string label, Color color)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			GUI.color = color;
			if (GUILayout.Button(label, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				GameEndManager.ForceGameEnd(reason);
				string text = (GameEndManager.DidImpostorsWin(reason) ? "\ud83d\udc7f" : "\ud83d\udc65");
				ShowNotification(text + " " + label + "!");
				_showGameEndDropdown = false;
			}
			GUI.color = Color.white;
		}

		private void DrawTeleportTabIMGUI()
		{
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			if (!ServerData.IsTabEnabled("teleport"))
			{
				return;
			}
			try
			{
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83d\ude80 Teleport Manager", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				if (teleportManager != null)
				{
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.Label("Current Map: " + teleportManager.GetCurrentMapName() + " " + teleportManager.GetCurrentMapIcon(), GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("↻ Refresh", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
					{
						teleportManager.RefreshLocations();
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(10f);
					System.Collections.Generic.IReadOnlyDictionary<SystemTypes, Vector2> locations = teleportManager.Locations;
					if (locations.Count > 0)
					{
						int num = 0;
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						foreach (var item in locations)
						{
							if (num > 1)
							{
								GUILayout.EndHorizontal();
								GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
								num = 0;
							}
							if (GUILayout.Button(((object)item.Key/*cast due to .constrained prefix*/).ToString(), GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
							{
								teleportManager.TeleportToPosition(item.Value);
								ShowNotification($"Teleported to {item.Key}");
							}
							num++;
						}
						GUILayout.EndHorizontal();
					}
					else
					{
						GUILayout.Label("No locations available for this map.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
				}
				else
				{
					GUILayout.Label("Teleport Manager not initialized", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				}
				GUILayout.EndVertical();
			}
			catch (Exception ex)
			{
				GUILayout.Label("Teleport Error: " + ex.Message, GuiStyles.ErrorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				try
				{
					GUILayout.EndVertical();
				}
				catch
				{
				}
			}
		}

		private void DrawLobbyListingTabIMGUI()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0282: Unknown result type (might be due to invalid IL or missing references)
			//IL_0275: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0375: Unknown result type (might be due to invalid IL or missing references)
			//IL_0368: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0469: Unknown result type (might be due to invalid IL or missing references)
			//IL_045c: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0562: Unknown result type (might be due to invalid IL or missing references)
			//IL_0597: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_0629: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_068e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0727: Unknown result type (might be due to invalid IL or missing references)
			//IL_085a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0872: Unknown result type (might be due to invalid IL or missing references)
			//IL_0877: Unknown result type (might be due to invalid IL or missing references)
			//IL_077f: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_07eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_083e: Unknown result type (might be due to invalid IL or missing references)
			//IL_079c: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_07b5: Expected O, but got Unknown
			//IL_0b54: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ba7: Unknown result type (might be due to invalid IL or missing references)
			//IL_08fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0922: Unknown result type (might be due to invalid IL or missing references)
			//IL_092a: Unknown result type (might be due to invalid IL or missing references)
			//IL_092f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0931: Unknown result type (might be due to invalid IL or missing references)
			//IL_0934: Unknown result type (might be due to invalid IL or missing references)
			//IL_0962: Expected I4, but got Unknown
			//IL_0c31: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c36: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c38: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c3b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c69: Expected I4, but got Unknown
			//IL_0c9c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0cbd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d30: Unknown result type (might be due to invalid IL or missing references)
			//IL_0db0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ddc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a99: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a92: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a30: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a29: Unknown result type (might be due to invalid IL or missing references)
			//IL_0af3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a7c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f32: Unknown result type (might be due to invalid IL or missing references)
			//IL_0edc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f75: Unknown result type (might be due to invalid IL or missing references)
			if (!ServerData.IsTabEnabled("lobbies"))
			{
				return;
			}
			Color color = GUI.color;
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			try
			{
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("\ud83c\udf10 LOBBY BROWSER", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.FlexibleSpace();
				bool captureEnabled = LobbyListingPatch.CaptureEnabled;
				GUI.color = (captureEnabled ? GuiStyles.Theme.Success : GuiStyles.Theme.TextMuted);
				GUILayout.Label(captureEnabled ? "● LIVE" : "○ OFF", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.EndHorizontal();
				GUILayout.Space(6f);
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("\ud83d\udd04 Refresh", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.MinWidth(75f) }))
				{
					LobbyListingPatch.RefreshLobbyList();
					_cachedLobbies = null;
					ShowNotification("Refreshing...");
				}
				if (GUILayout.Button("\ud83d\uddd1 Clear", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.MinWidth(65f) }))
				{
					LobbyListingPatch.ClearCapturedLobbies();
					_selectedLobbyIndex = -1;
					_cachedLobbies = null;
				}
				if (GUILayout.Button("\ud83d\udccb Paste", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.MinWidth(65f) }))
				{
					try
					{
						string text = GUIUtility.systemCopyBuffer?.Trim().ToUpper();
						if (!string.IsNullOrEmpty(text) && text.Length >= 4 && text.Length <= 6)
						{
							LobbyListingPatch.JoinByCode(text);
							ShowNotification("Joining...");
						}
						else
						{
							ShowNotification("Copy a code first!");
						}
					}
					catch
					{
						ShowNotification("Invalid code!");
					}
				}
				GUILayout.FlexibleSpace();
				LobbyListingPatch.CaptureEnabled = GUILayout.Toggle(LobbyListingPatch.CaptureEnabled, "  Auto-Capture", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				GUILayout.Space(4f);
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				byte? filterMapId = LobbyListingPatch.FilterMapId;
				GUI.color = ((!filterMapId.HasValue) ? GuiStyles.Theme.Accent : _colorWhite);
				if (GUILayout.Button("All", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(35f) }))
				{
					LobbyListingPatch.FilterMapId = null;
					_cachedLobbies = null;
				}
				GUI.color = ((filterMapId == 0) ? _mapColors[0] : _colorWhite);
				if (GUILayout.Button("\ud83d\ude80", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(24f) }))
				{
					LobbyListingPatch.FilterMapId = 0;
					_cachedLobbies = null;
				}
				GUI.color = ((filterMapId == 1) ? _mapColors[1] : _colorWhite);
				if (GUILayout.Button("\ud83c\udfe2", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(24f) }))
				{
					LobbyListingPatch.FilterMapId = (byte)1;
					_cachedLobbies = null;
				}
				GUI.color = ((filterMapId == 2) ? _mapColors[2] : _colorWhite);
				if (GUILayout.Button("❄", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(24f) }))
				{
					LobbyListingPatch.FilterMapId = (byte)2;
					_cachedLobbies = null;
				}
				GUI.color = ((filterMapId == 4) ? _mapColors[4] : _colorWhite);
				if (GUILayout.Button("✈", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(24f) }))
				{
					LobbyListingPatch.FilterMapId = (byte)4;
					_cachedLobbies = null;
				}
				GUI.color = ((filterMapId == 5) ? _mapColors[5] : _colorWhite);
				if (GUILayout.Button("\ud83c\udf34", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(24f) }))
				{
					LobbyListingPatch.FilterMapId = (byte)5;
					_cachedLobbies = null;
				}
				GUI.color = color;
				GUILayout.Space(8f);
				bool filterHasSpace = LobbyListingPatch.FilterHasSpace;
				LobbyListingPatch.FilterHasSpace = GUILayout.Toggle(LobbyListingPatch.FilterHasSpace, "Space", GuiStyles.ToggleStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(60f) });
				if (filterHasSpace != LobbyListingPatch.FilterHasSpace)
				{
					_cachedLobbies = null;
				}
				bool filterFreeChatOnly = LobbyListingPatch.FilterFreeChatOnly;
				LobbyListingPatch.FilterFreeChatOnly = GUILayout.Toggle(LobbyListingPatch.FilterFreeChatOnly, "Chat", GuiStyles.ToggleStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(50f) });
				if (filterFreeChatOnly != LobbyListingPatch.FilterFreeChatOnly)
				{
					_cachedLobbies = null;
				}
				GUILayout.FlexibleSpace();
				int currentSortMode = (int)LobbyListingPatch.CurrentSortMode;
				if (filterMapId.HasValue || currentSortMode != 0 || LobbyListingPatch.FilterHasSpace || LobbyListingPatch.FilterFreeChatOnly)
				{
					GUI.color = GuiStyles.Theme.Error;
					if (GUILayout.Button("✕", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(22f) }))
					{
						LobbyListingPatch.ClearFilters();
						_cachedLobbies = null;
					}
					GUI.color = color;
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label("Sort:", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(30f) });
				GUI.color = color;
				for (int i = 0; i < _sortOptions.Length; i++)
				{
					GUI.color = ((currentSortMode == i) ? GuiStyles.Theme.Accent : _colorWhite);
					if (GUILayout.Button(_sortOptions[i], GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						LobbyListingPatch.CurrentSortMode = (LobbyListingPatch.LobbySortMode)i;
						_cachedLobbies = null;
					}
				}
				GUI.color = color;
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				GUILayout.Space(4f);
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (_cachedLobbies == null || realtimeSinceStartup - _lastLobbyRefresh > 0.5f)
				{
					_cachedLobbies = LobbyListingPatch.GetFilteredAndSortedLobbies();
					_lastLobbyRefresh = realtimeSinceStartup;
				}
				int count = LobbyListingPatch.CapturedLobbies.Count;
				int totalGamesInMatchmaker = LobbyListingPatch.TotalGamesInMatchmaker;
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label($"\ud83d\udcca {_cachedLobbies.Count}/{count} lobbies captured", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.FlexibleSpace();
				GUILayout.Label($"{totalGamesInMatchmaker} games online", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.EndHorizontal();
				GUILayout.Space(4f);
				if (_cachedLobbies.Count == 0)
				{
					GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
					GUILayout.Space(15f);
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUI.color = new Color(0.4f, 0.5f, 0.6f);
					if (_lobbyEmptyIconStyle == null)
					{
						_lobbyEmptyIconStyle = new GUIStyle(GUI.skin.label)
						{
							fontSize = 32,
							alignment = (TextAnchor)4
						};
					}
					GUILayout.Label("\ud83d\udd0d", _lobbyEmptyIconStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = color;
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(6f);
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUI.color = GuiStyles.Theme.TextMuted;
					GUILayout.Label("No lobbies captured yet", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(4f);
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUILayout.Label("Go to  Online → Find Game  to capture lobbies", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUI.color = color;
					GUILayout.Space(15f);
					GUILayout.EndVertical();
					return;
				}
				_lobbyListScrollPosition = GUILayout.BeginScrollView(_lobbyListScrollPosition, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(160f) });
				int num = Math.Min(_cachedLobbies.Count, 15);
				Platforms platform;
				string text2;
				for (int j = 0; j < num; j++)
				{
					LobbyListingPatch.CapturedLobby capturedLobby = _cachedLobbies[j];
					if (capturedLobby != null)
					{
						GUILayout.BeginHorizontal((j == _selectedLobbyIndex) ? GuiStyles.SelectedItemStyle : GuiStyles.ItemStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(28f) });
						byte b = (byte)((capturedLobby.MapId < 6) ? capturedLobby.MapId : 0);
						GUI.color = _mapColors[b];
						GUILayout.Label("●", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(14f) });
						GUI.color = color;
						platform = capturedLobby.Platform;
						switch ((int)platform - 1)
						{
						case 0:
						case 1:
						case 3:
							text2 = "\ud83d\udcbb";
							break;
						case 7:
						case 8:
						case 9:
							text2 = "\ud83c\udfae";
							break;
						case 5:
						case 6:
							text2 = "\ud83d\udcf1";
							break;
						default:
							text2 = "\ud83d\udda5";
							break;
						}
						GUILayout.Label(text2, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(18f) });
						string text3 = capturedLobby.GameCode ?? "?";
						string text4 = capturedLobby.HostName ?? "?";
						if (text4.Length > 10)
						{
							text4 = text4.Substring(0, 8) + "..";
						}
						if (GUILayout.Button(text3 + "  " + text4, GuiStyles.ListButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
						{
							_selectedLobbyIndex = j;
						}
						if (capturedLobby.NumImpostors > 0)
						{
							GUI.color = ((capturedLobby.NumImpostors >= 3) ? _colorRed : _colorOrange);
							GUILayout.Label($"{capturedLobby.NumImpostors}i", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(18f) });
							GUI.color = color;
						}
						GUI.color = ((capturedLobby.PlayerCount >= capturedLobby.MaxPlayers) ? GuiStyles.Theme.Error : GuiStyles.Theme.Success);
						GUILayout.Label($"{capturedLobby.PlayerCount}/{capturedLobby.MaxPlayers}", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(35f) });
						GUI.color = color;
						if (GUILayout.Button("▶", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(26f) }))
						{
							LobbyListingPatch.JoinCapturedLobby(capturedLobby);
							ShowNotification("Joining " + text3);
						}
						GUILayout.EndHorizontal();
					}
				}
				if (_cachedLobbies.Count > 15)
				{
					GUI.color = GuiStyles.Theme.TextMuted;
					GUILayout.Label($"+ {_cachedLobbies.Count - 15} more lobbies...", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = color;
				}
				GUILayout.EndScrollView();
				if (_selectedLobbyIndex < 0 || _selectedLobbyIndex >= _cachedLobbies.Count)
				{
					return;
				}
				LobbyListingPatch.CapturedLobby capturedLobby2 = _cachedLobbies[_selectedLobbyIndex];
				if (capturedLobby2 == null)
				{
					return;
				}
				GUILayout.Space(6f);
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				byte b2 = (byte)((capturedLobby2.MapId < 6) ? capturedLobby2.MapId : 0);
				string text5 = capturedLobby2.GameCode ?? "?";
				platform = capturedLobby2.Platform;
				switch ((int)platform - 1)
				{
				case 0:
				case 1:
				case 3:
					text2 = "\ud83d\udcbb";
					break;
				case 7:
				case 8:
				case 9:
					text2 = "\ud83c\udfae";
					break;
				case 5:
				case 6:
					text2 = "\ud83d\udcf1";
					break;
				default:
					text2 = "\ud83d\udda5";
					break;
				}
				string text6 = text2;
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = _mapColors[b2];
				GUILayout.Label("● " + text5, GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.FlexibleSpace();
				GUILayout.Label($"{capturedLobby2.MapName} | {capturedLobby2.PlayerCount}/{capturedLobby2.MaxPlayers}", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = GuiStyles.Theme.TextMuted;
				string text7 = capturedLobby2.TrueHostName ?? capturedLobby2.HostName ?? "Unknown";
				if (text7.Length > 15)
				{
					text7 = text7.Substring(0, 13) + "..";
				}
				GUILayout.Label("Host: " + text7, GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.FlexibleSpace();
				GUILayout.Label(text6 + " " + capturedLobby2.PlatformName, GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.EndHorizontal();
				if (capturedLobby2.NumImpostors > 0 || capturedLobby2.KillCooldown > 0f)
				{
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = _colorOrange;
					if (capturedLobby2.NumImpostors > 0)
					{
						GUILayout.Label($"\ud83d\udc7f {capturedLobby2.NumImpostors} Impostors", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					if (capturedLobby2.KillCooldown > 0f)
					{
						GUILayout.Label($"⏱ {capturedLobby2.KillCooldown:F0}s Kill CD", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					if (capturedLobby2.PlayerSpeed > 0f)
					{
						GUILayout.Label($"\ud83c\udfc3 {capturedLobby2.PlayerSpeed:F1}x Speed", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					GUI.color = color;
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				GUILayout.Space(4f);
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("\ud83d\udccb Copy", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.MinWidth(70f) }))
				{
					LobbyListingPatch.CopyCodeToClipboard(text5);
					ShowNotification("Code Copied!");
				}
				GUI.color = GuiStyles.Theme.Success;
				if (GUILayout.Button("\ud83d\ude80 JOIN LOBBY", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
				{
					LobbyListingPatch.JoinCapturedLobby(capturedLobby2);
					ShowNotification("Joining " + text5);
				}
				GUI.color = color;
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			}
			finally
			{
				GUILayout.EndVertical();
			}
		}

		private void DrawGameTabIMGUI()
		{
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01da: Unknown result type (might be due to invalid IL or missing references)
			//IL_0200: Unknown result type (might be due to invalid IL or missing references)
			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0316: Unknown result type (might be due to invalid IL or missing references)
			//IL_0336: Unknown result type (might be due to invalid IL or missing references)
			//IL_0356: Unknown result type (might be due to invalid IL or missing references)
			//IL_037e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0398: Unknown result type (might be due to invalid IL or missing references)
			//IL_03be: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
			//IL_0293: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0411: Unknown result type (might be due to invalid IL or missing references)
			//IL_0437: Unknown result type (might be due to invalid IL or missing references)
			//IL_0457: Unknown result type (might be due to invalid IL or missing references)
			//IL_047f: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_06cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e5: Unknown result type (might be due to invalid IL or missing references)
			if (!ServerData.IsTabEnabled("cheats"))
			{
				return;
			}
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("\ud83c\udfae Game Actions", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			string obj = (_showGameEndDropdown ? "▼ \ud83c\udfc1 Force Game End" : "► \ud83c\udfc1 Force Game End");
			bool flag = (GUI.enabled = GameEndManager.CanEndGame());
			if (flag)
			{
				GUI.backgroundColor = new Color(0.4f, 0.3f, 0.8f);
				GUI.color = new Color(1f, 1f, 1f);
			}
			else
			{
				GUI.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
				GUI.color = new Color(0.6f, 0.6f, 0.6f);
			}
			if (GUILayout.Button(obj, GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.ExpandWidth(true),
				GUILayout.Height(28f)
			}))
			{
				_showGameEndDropdown = !_showGameEndDropdown;
			}
			GUI.enabled = true;
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			GUILayout.EndHorizontal();
			if (flag)
			{
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = new Color(0.3f, 1f, 0.4f);
				if (GUILayout.Button("✓ My Team Wins", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					GameEndManager.ForceMyTeamWin();
					ShowNotification("\ud83c\udfc6 Your team wins!");
					_showGameEndDropdown = false;
				}
				GUI.color = new Color(1f, 0.5f, 0.3f);
				if (GUILayout.Button("✗ Enemy Wins", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					GameEndManager.ForceEnemyTeamWin();
					ShowNotification("\ud83d\udc80 Enemy team wins!");
					_showGameEndDropdown = false;
				}
				GUI.color = Color.white;
				GUILayout.EndHorizontal();
			}
			if (!flag)
			{
				GUI.color = GuiStyles.Theme.TextMuted;
				string cannotEndReason = GameEndManager.GetCannotEndReason();
				GUILayout.Label("  ⚠ Cannot end: " + cannotEndReason, GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
			}
			if (_showGameEndDropdown && flag)
			{
				GUILayout.Space(4f);
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				if (GameEndManager.IsHideAndSeekMode())
				{
					GUI.color = new Color(1f, 0.8f, 0.2f);
					GUILayout.Label("\ud83c\udfad HIDE AND SEEK MODE", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = Color.white;
					GUILayout.Space(4f);
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					DrawGameEndButton((GameOverReason)8, "\ud83d\udc7f Seeker Wins", new Color(1f, 0.4f, 0.4f));
					DrawGameEndButton((GameOverReason)7, "\ud83d\udc65 Hiders Win", new Color(0.4f, 0.9f, 0.6f));
					GUILayout.EndHorizontal();
				}
				else
				{
					GUI.color = new Color(1f, 0.3f, 0.3f);
					GUILayout.Label("\ud83d\udc7f IMPOSTOR VICTORIES", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = Color.white;
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					DrawGameEndButton((GameOverReason)3, "☠ Kill", new Color(1f, 0.35f, 0.35f));
					DrawGameEndButton((GameOverReason)4, "\ud83d\udca5 Sabotage", new Color(1f, 0.45f, 0.25f));
					DrawGameEndButton((GameOverReason)2, "\ud83d\uddf3 Vote", new Color(1f, 0.4f, 0.5f));
					GUILayout.EndHorizontal();
					GUILayout.Space(6f);
					GUI.color = new Color(0.4f, 0.8f, 1f);
					GUILayout.Label("\ud83d\udc65 CREWMATE VICTORIES", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = Color.white;
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					DrawGameEndButton((GameOverReason)0, "\ud83d\uddf3 Vote", new Color(0.4f, 0.7f, 1f));
					DrawGameEndButton((GameOverReason)1, "\ud83d\udccb Tasks", new Color(0.3f, 0.9f, 0.5f));
					GUILayout.EndHorizontal();
				}
				GUILayout.Space(6f);
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label("\ud83d\udce1 DISCONNECT", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				DrawGameEndButton((GameOverReason)5, "Imp DC → Crew", new Color(0.5f, 0.8f, 0.9f));
				DrawGameEndButton((GameOverReason)6, "Crew DC → Imp", new Color(0.9f, 0.5f, 0.5f));
				GUILayout.EndHorizontal();
				GUILayout.Space(4f);
				GUI.color = new Color(0.5f, 0.5f, 0.5f);
				if (GUILayout.Button("✕ Close", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(20f) }))
				{
					_showGameEndDropdown = false;
				}
				GUI.color = Color.white;
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
			GUILayout.EndVertical();
			GUILayout.Space(8f);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("\ud83d\udee1 Host & Protections", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			bool flag3 = CheatConfig.DisableGameEnd?.Value ?? DisableGameEndFallback;
			GUI.enabled = true;
			bool flag4 = GuiStyles.DrawBetterToggle(flag3, "\ud83d\udeab Disable Game End", "Prevents the game from ending (Host Only)");
			if (flag4 != flag3)
			{
				DisableGameEndFallback = flag4;
				if (CheatConfig.DisableGameEnd != null)
				{
					CheatConfig.DisableGameEnd.Value = flag4;
				}
				if (flag4)
				{
					ShowNotification("Game End Blocked! (Host Only)");
				}
				else
				{
					ShowNotification("Game End Allowed.");
				}
			}
			GUILayout.Space(4f);
			if (GUILayout.Button("\ud83d\udce2 Close Meeting", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				GameCheats.CloseMeeting();
				ShowNotification("Meeting Closed!");
			}
			GUILayout.Space(4f);
			if ((Object)(object)PlayerControl.LocalPlayer != (Object)null && GUILayout.Button("\ud83d\udce2 Call Emergency Meeting", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				PlayerControl.LocalPlayer.CmdReportDeadBody((NetworkedPlayerInfo)null);
				ShowNotification("Emergency meeting called!");
			}
			GUILayout.EndVertical();
			GUILayout.Space(8f);
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			_showAnimationCheats = GUILayout.Toggle(_showAnimationCheats, "\ud83c\udfad Fake Animations ▼", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (_showAnimationCheats)
			{
				GuiStyles.DrawSeparator();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("\ud83d\udee1\ufe0f Shields Anim", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					GameCheats.AnimShields = true;
				}
				if (GUILayout.Button("☄\ufe0f Weapons Anim", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					GameCheats.AnimAsteroids = true;
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				if (GUILayout.Button("\ud83d\uddd1\ufe0f Trash Anim", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
				{
					GameCheats.AnimEmptyGarbage = true;
				}
				bool animCamsInUse = GameCheats.AnimCamsInUse;
				if (GUILayout.Toggle(animCamsInUse, "\ud83d\udcf7 Cams Active", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null) != animCamsInUse)
				{
					GameCheats.AnimCamsInUse = !animCamsInUse;
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(4f);
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label("Animations respect map limits", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = Color.white;
			}
			GUILayout.EndVertical();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private void DrawMovementTabIMGUI()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_043b: Unknown result type (might be due to invalid IL or missing references)
			//IL_045c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0467: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0556: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0743: Unknown result type (might be due to invalid IL or missing references)
			//IL_075b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0760: Unknown result type (might be due to invalid IL or missing references)
			//IL_071c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0736: Unknown result type (might be due to invalid IL or missing references)
			//IL_078a: Unknown result type (might be due to invalid IL or missing references)
			//IL_07bb: Unknown result type (might be due to invalid IL or missing references)
			if (!ServerData.IsTabEnabled("teleport"))
			{
				return;
			}
			Color color = GUI.color;
			if ((Object)(object)ShipStatus.Instance == (Object)null)
			{
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83c\udfae Movement Controls", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label("⚠\ufe0f This tab is available during a game.", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("Join or start a match to use movement features.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.EndVertical();
				GUILayout.EndVertical();
				return;
			}
			try
			{
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83c\udfc3 Mobility", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
				{
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					IsNoclipping = GuiStyles.DrawBetterToggle(IsNoclipping, "\ud83d\udc7b Noclip", "Walk through walls");
					GUILayout.FlexibleSpace();
					GuiStyles.DrawStatusIndicator(IsNoclipping);
					GUILayout.EndHorizontal();
					if ((Object)(object)PlayerControl.LocalPlayer.Collider != (Object)null)
					{
						((Behaviour)PlayerControl.LocalPlayer.Collider).enabled = !IsNoclipping;
					}
				}
				GUILayout.EndVertical();
				GUILayout.Space(10f);
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("⚡ Speed", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.FlexibleSpace();
				GUI.color = GuiStyles.Theme.TextMuted;
				GUILayout.Label($"Base: {LobbySpeedMod:F1}x", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUI.color = color;
				GUILayout.EndHorizontal();
				GuiStyles.DrawSeparator();
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				bool isCustomSpeedActive = IsCustomSpeedActive;
				bool flag = GUILayout.Toggle(IsCustomSpeedActive, IsCustomSpeedActive ? " ✅ ON" : " ❌ OFF", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				if (isCustomSpeedActive != flag)
				{
					IsCustomSpeedActive = flag;
					if (!flag)
					{
						ShowNotification("Speed restored");
					}
				}
				GUILayout.FlexibleSpace();
				if (IsCustomSpeedActive && GUILayout.Button("\ud83d\udd04", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(28f) }))
				{
					ResetPlayerSpeed();
					ShowNotification("Reset");
				}
				GUILayout.EndHorizontal();
				if (IsCustomSpeedActive)
				{
					GUILayout.Space(4f);
					float num = ((_originalBaseSpeed > 0f) ? _originalBaseSpeed : 2.5f);
					float num2 = _customSpeed / num;
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = GuiStyles.Theme.Accent;
					GUILayout.Label($"{num2:F1}x", GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(35f) });
					GUI.color = color;
					if (GUILayout.Button("1x", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(32f) }))
					{
						_customSpeed = num;
					}
					if (GUILayout.Button("2x", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(32f) }))
					{
						_customSpeed = num * 2f;
					}
					if (GUILayout.Button("3x", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(32f) }))
					{
						_customSpeed = num * 3f;
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(2f);
					float num3 = GUILayout.HorizontalSlider(num2, 0.5f, 4f, GuiStyles.SliderStyle, GuiStyles.SliderThumbStyle, System.Array.Empty<GUILayoutOption>());
					if (Mathf.Abs(num3 - num2) > 0.01f)
					{
						_customSpeed = num * num3;
					}
				}
				GUILayout.EndVertical();
				GUILayout.Space(10f);
				if (teleportManager != null)
				{
					GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
					GUILayout.Label("\ud83d\uddfa\ufe0f Map Info", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GuiStyles.DrawSeparator();
					string currentMapIcon = teleportManager.GetCurrentMapIcon();
					string currentMapName = teleportManager.GetCurrentMapName();
					System.Collections.Generic.IReadOnlyDictionary<SystemTypes, Vector2> locationsForCurrentMap = teleportManager.GetLocationsForCurrentMap();
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = GuiStyles.Theme.Visor;
					GUILayout.Label(currentMapIcon + " " + currentMapName, GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = color;
					GUILayout.FlexibleSpace();
					GUI.color = GuiStyles.Theme.TextMuted;
					GUILayout.Label($"{locationsForCurrentMap.Count} locs", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = color;
					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					GUILayout.Space(10f);
					GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
					GUILayout.Label("\ud83d\udc65 Player Teleport", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GuiStyles.DrawSeparator();
					GUI.color = GuiStyles.Theme.Accent;
					if (GUILayout.Button("\ud83c\udfaf Nearest Player", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						PlayerControl closestPlayer = teleportManager.GetClosestPlayer();
						if ((Object)(object)closestPlayer != (Object)null)
						{
							teleportManager.TeleportToPlayer(closestPlayer);
							ShowNotification("Teleported to " + closestPlayer.Data.PlayerName + "!");
						}
						else
						{
							ShowNotification("No players found!");
						}
					}
					GUI.color = color;
					System.Collections.Generic.List<PlayerControl> playersByDistance = teleportManager.GetPlayersByDistance();
					if (playersByDistance.Count > 0)
					{
						GUILayout.Space(4f);
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						int num4 = Math.Min(3, playersByDistance.Count);
						for (int i = 0; i < num4; i++)
						{
							PlayerControl val = playersByDistance[i];
							NetworkedPlayerInfo data = val.Data;
							string text = ((data != null) ? data.PlayerName : null) ?? "???";
							if (text.Length > 8)
							{
								text = text.Substring(0, 6) + "..";
							}
							if (GUILayout.Button("→ " + text, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
							{
								teleportManager.TeleportToPlayer(val);
								NetworkedPlayerInfo data2 = val.Data;
								ShowNotification("Teleported to " + ((data2 != null) ? data2.PlayerName : null) + "!");
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
				}
				else
				{
					GUILayout.Label("⚠\ufe0f Teleport manager error.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				}
				GUILayout.EndVertical();
				GUILayout.Space(10f);
				GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
				if (teleportManager != null)
				{
					GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.Label("\ud83d\udccd Locations", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("\ud83d\udd04", (GUILayoutOption[])(object)new GUILayoutOption[2]
					{
						GUILayout.Width(30f),
						GUILayout.Height(20f)
					}))
					{
						teleportManager.RefreshLocations();
						ShowNotification("Locations refreshed!");
					}
					GUILayout.EndHorizontal();
					GuiStyles.DrawSeparator();
					System.Collections.Generic.IReadOnlyDictionary<SystemTypes, Vector2> locationsForCurrentMap2 = teleportManager.GetLocationsForCurrentMap();
					if (locationsForCurrentMap2.Count == 0)
					{
						GUI.color = GuiStyles.Theme.TextMuted;
						GUILayout.Label("No locations available.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						GUI.color = color;
					}
					else
					{
						_movementScrollPosition = GUILayout.BeginScrollView(_movementScrollPosition, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(280f) });
						System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<SystemTypes, Vector2>> list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<SystemTypes, Vector2>>(locationsForCurrentMap2);
						int count = list.Count;
						for (int j = 0; j < count; j++)
						{
							var keyValuePair = list[j];
							string text2 = FormatLocationName(keyValuePair.Key);
							if (GUILayout.Button("\ud83d\udccd " + text2, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
							{
								teleportManager.TeleportToLocation(keyValuePair.Key);
								ShowNotification("Teleported to " + text2 + "!");
							}
						}
						GUILayout.EndScrollView();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error in DrawMovementTabIMGUI: {value}"));
			}
		}

		private unsafe string FormatLocationName(SystemTypes location)
		{
			string? text = ((object)(*(SystemTypes*)(&location))/*cast due to .constrained prefix*/).ToString();
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = text;
			foreach (char c in text2)
			{
				if (char.IsUpper(c) && stringBuilder.Length > 0)
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(c);
			}
			return stringBuilder.ToString();
		}

		private void DrawSabotageTabIMGUI()
		{
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_0315: Unknown result type (might be due to invalid IL or missing references)
			//IL_0309: Unknown result type (might be due to invalid IL or missing references)
			if (!ServerData.IsTabEnabled("sabotage"))
			{
				return;
			}
			if ((Object)(object)ShipStatus.Instance == (Object)null)
			{
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83d\udca5 Sabotage Controls", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("⚠\ufe0f This tab is available during a game.", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("Join or start a match to control sabotages and doors.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.EndVertical();
				GUILayout.EndVertical();
				return;
			}
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("⚡ Systems", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			if (GUILayout.Button("☢\ufe0f Reactor", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.TriggerReactorMeltdown();
				ShowNotification("Reactor meltdown triggered!");
			}
			if (GUILayout.Button("\ud83e\udec1 Oxygen", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.TriggerOxygenDepletion();
				ShowNotification("Oxygen depletion triggered!");
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			if (GUILayout.Button("\ud83d\udca1 Lights", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.TriggerLightsOut();
				ShowNotification("Lights sabotaged!");
			}
			if (GUILayout.Button("\ud83d\udce1 Comms", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.TriggerSabotage((SystemTypes)14, "Sabotagem: Comms ativada!", 128);
				ShowNotification("Comms sabotaged!");
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
			GUI.color = new Color(1f, 0.4f, 0.4f);
			if (GUILayout.Button("\ud83d\udca5 TRIGGER ALL SABOTAGES", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.TriggerAllSabotages();
				ShowNotification("All sabotages triggered!");
			}
			GUI.color = Color.white;
			GUILayout.EndVertical();
			GUILayout.EndVertical();
			GUILayout.Space(8f);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("\ud83d\udeaa Doors", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			if (GUILayout.Button("\ud83d\udd12 Close All Doors", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.ToggleAllDoors();
				ShowNotification("All doors closed!");
			}
			GUILayout.Space(4f);
			GuiStyles.DrawSeparator();
			GUILayout.Label("\ud83d\udd10 Lock Individual Doors", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(4f);
			SystemTypes[] doorsForCurrentMap = SabotageService.GetDoorsForCurrentMap();
			if (doorsForCurrentMap.Length == 0)
			{
				GUILayout.Label("No doors detected on this map.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			else
			{
				for (int i = 0; i < doorsForCurrentMap.Length; i += 2)
				{
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					for (int j = 0; j < 2; j++)
					{
						int num = i + j;
						if (num < doorsForCurrentMap.Length)
						{
							SystemTypes room = doorsForCurrentMap[num];
							bool flag = SabotageService.IsDoorLocked(room);
							string doorRoomName = SabotageService.GetDoorRoomName(room);
							string obj = (flag ? ("\ud83d\udd12 " + doorRoomName) : doorRoomName);
							GUI.color = (Color)(flag ? new Color(0.9f, 0.25f, 0.25f) : Color.white);
							if (GUILayout.Button(obj, GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
							{
								SabotageService.SetDoorLocked(room, !flag);
							}
							GUI.color = Color.white;
						}
						else
						{
							GUILayout.Label("", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
						}
						if (j == 0)
						{
							GUILayout.Space(4f);
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(2f);
				}
			}
			GUILayout.EndVertical();
			GUILayout.Space(8f);
			GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
			GUILayout.Label("\ud83d\udd27 Repair", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GuiStyles.DrawSeparator();
			GUI.color = new Color(0.4f, 1f, 0.5f);
			if (GUILayout.Button("✅ REPAIR ALL SABOTAGES", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
			{
				SabotageService.RepairAllSabotages();
				ShowNotification("All sabotages repaired!");
			}
			GUI.color = Color.white;
			GUILayout.EndVertical();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private unsafe void DrawImpostorTabIMGUI()
		{
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_030b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0324: Unknown result type (might be due to invalid IL or missing references)
			//IL_043f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0497: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			try
			{
				if ((Object)(object)ShipStatus.Instance == (Object)null)
				{
					goto IL_002c;
				}
				PlayerControl localPlayer = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer != null) ? localPlayer.Data : null) == (Object)null)
				{
					goto IL_002c;
				}
				GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83c\udfad Role Select", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				PlayerControl localPlayer2 = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer2 != null) ? localPlayer2.Data : null) != (Object)null)
				{
					RoleTypes roleType = PlayerControl.LocalPlayer.Data.RoleType;
					bool flag = IsImpostorRoleSafe(roleType);
					GUILayout.Label("Select role (local):", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					if (_cachedRoles == null)
					{
						_cachedRoles = ImpostorForcer.GetSupportedRoles();
						_cachedRoleNames = _cachedRoles.Select((RoleTypes r) => ((object)(*(RoleTypes*)(&r))/*cast due to .constrained prefix*/).ToString()).ToArray();
					}
					RoleTypes[] cachedRoles = _cachedRoles;
					string[] cachedRoleNames = _cachedRoleNames;
					int num = System.Array.IndexOf(cachedRoles, roleType);
					if (num < 0)
					{
						num = 0;
					}
					if (inGameRoleGridIndex < 0)
					{
						inGameRoleGridIndex = num;
					}
					int num2 = DrawSimpleSelectionGrid(inGameRoleGridIndex, cachedRoleNames, 2);
					if (num2 != inGameRoleGridIndex)
					{
						inGameRoleGridIndex = num2;
					}
					GUILayout.Space(5f);
					GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
					if (GUILayout.Button("Apply (Local)", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						int num3 = (int)cachedRoles[Mathf.Clamp(inGameRoleGridIndex, 0, cachedRoles.Length - 1)];
						bool flag2 = IsImpostorRoleSafe((RoleTypes)num3);
						if (!flag && flag2)
						{
							isLocalFakeImpostor = true;
						}
						ImpostorForcer.TrySetLocalPlayerRole((RoleTypes)num3);
					}
					if (!flag && GUILayout.Button("Become Imp. (Local)", GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						isLocalFakeImpostor = true;
						ImpostorForcer.TrySetLocalPlayerAsImpostor();
					}
					GUILayout.EndHorizontal();
					if (!flag)
					{
						isLocalFakeImpostor = false;
					}
				}
				GUILayout.EndVertical();
				GUILayout.EndVertical();
				GUILayout.Space(10f);
				GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("ℹ\ufe0f Status & Cooldowns", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				PlayerControl localPlayer3 = PlayerControl.LocalPlayer;
				if ((Object)(object)((localPlayer3 != null) ? localPlayer3.Data : null) != (Object)null)
				{
					RoleTypes roleType2 = PlayerControl.LocalPlayer.Data.RoleType;
					bool flag3 = IsImpostorRoleSafe(roleType2);
					Color color = GUI.color;
					GUI.color = (flag3 ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.6f, 0.95f, 1f));
					GUILayout.Label($"Role: {roleType2}", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					GUI.color = color;
					if (flag3 && ((InnerNetClient)AmongUsClient.Instance).AmHost)
					{
						GUILayout.Space(10f);
						GUILayout.Label("Cooldowns (Host Only)", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						NoKillCooldown = GuiStyles.DrawBetterToggle(NoKillCooldown, "No Kill Cooldown", "Removes kill cooldown (HOST ONLY)");
						if (NoKillCooldown)
						{
							if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
							{
								PlayerControl.LocalPlayer.SetKillTimer(0f);
							}
							GUI.color = new Color(0.3f, 1f, 0.3f);
							GUILayout.Label("✓ Kill Cooldown: 0s (Active)", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
							GUI.color = color;
						}
						else
						{
							float num4 = GuiStyles.DrawCrewSlider(KillCooldown, 0f, 60f, "\ud83d\udde1 Kill Cooldown", "F1", "s", showMinMax: true);
							if (Math.Abs(num4 - KillCooldown) > 0.01f)
							{
								KillCooldown = num4;
								if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
								{
									PlayerControl.LocalPlayer.SetKillTimer(KillCooldown);
								}
							}
						}
					}
					else
					{
						GUILayout.Space(10f);
						GUI.color = GuiStyles.Theme.TextMuted;
						if (!((InnerNetClient)AmongUsClient.Instance).AmHost && flag3)
						{
							GUILayout.Label("⚠ No Kill Cooldown requires HOST", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						}
						else if (isLocalFakeImpostor)
						{
							GUILayout.Label("(Visual/Local Role Only)", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						}
						else
						{
							GUILayout.Label("(No cooldowns available)", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						}
						GUI.color = color;
					}
				}
				GUILayout.EndVertical();
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("⚡ Impostor Extras", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				bool flag4 = GuiStyles.DrawBetterToggle(GameCheats.NoKillDistanceLimitEnabled, "No Kill Distance Limit", "Kill any player on the map regardless of distance");
				if (flag4 != GameCheats.NoKillDistanceLimitEnabled)
				{
					GameCheats.ToggleNoKillDistanceLimit(flag4);
				}
				GUILayout.Space(5f);
				bool flag5 = GuiStyles.DrawBetterToggle(GameCheats.AlwaysShowChatEnabled, "Always Show Chat (Read-Only)", "Chat always visible — read only outside meetings, no send to prevent kicks");
				if (flag5 != GameCheats.AlwaysShowChatEnabled)
				{
					GameCheats.ToggleAlwaysShowChat(flag5);
				}
				GUILayout.EndVertical();
				GUILayout.EndVertical();
				goto end_IL_0006;
				IL_002c:
				GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("\ud83c\udfad Impostor Controls", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GuiStyles.DrawSeparator();
				GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
				GUILayout.Label("This tab is available during a game.", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.Label("Join or start a match to use impostor features.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
				GUILayout.EndVertical();
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				return;
				end_IL_0006:;
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error in DrawImpostorTabIMGUI: {value}"));
				GUILayout.Label("Error loading impostor tab.", GuiStyles.ErrorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			}
			GUILayout.EndHorizontal();
		}

		private void UpdateGameState()
		{
			if (Time.time - _lastGameStateUpdateTime < 0.2f)
			{
				return;
			}
			_lastGameStateUpdateTime = Time.time;
			if ((Object)(object)PlayerControl.LocalPlayer == (Object)null)
			{
				return;
			}
			try
			{
				if ((Object)(object)PlayerControl.LocalPlayer.MyPhysics != (Object)null && _isCustomSpeedActive && _customSpeed > 0f)
				{
					PlayerControl.LocalPlayer.MyPhysics.Speed = _customSpeed;
				}
				if (!((Object)(object)PlayerControl.LocalPlayer.Data != (Object)null) || !((Object)(object)PlayerControl.LocalPlayer.Data.Role != (Object)null) || !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
				{
					return;
				}
				if (NoKillCooldown && (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).AmHost)
				{
					PlayerControl.LocalPlayer.SetKillTimer(0f);
					return;
				}
				float killTimer = PlayerControl.LocalPlayer.killTimer;
				float killCooldown = KillCooldown;
				if (killTimer > killCooldown + 1f)
				{
					PlayerControl.LocalPlayer.SetKillTimer(killCooldown);
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error in UpdateGameState: {value}"));
			}
		}

		private void ShowNotification(string message)
		{
			NotifyUtils.Show(message);
		}

		[HideFromIl2Cpp]
		private int DrawSimpleSelectionGrid(int selectedIndex, string[] labels, int columns)
		{
			if (labels == null || labels.Length == 0)
			{
				return 0;
			}
			if (columns <= 0)
			{
				columns = 1;
			}
			int result = selectedIndex;
			int num = Mathf.CeilToInt((float)labels.Length / (float)columns);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
				for (int j = 0; j < columns; j++)
				{
					if (num2 >= labels.Length)
					{
						GUILayout.FlexibleSpace();
					}
					else
					{
						bool flag = num2 == selectedIndex;
						if (flag)
						{
							GUILayout.BeginVertical(GuiStyles.HighlightStyle, System.Array.Empty<GUILayoutOption>());
						}
						GUIStyle buttonStyle = GuiStyles.ButtonStyle;
						if (GUILayout.Button(flag ? ("[x] " + labels[num2]) : labels[num2], buttonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
						{
							result = num2;
						}
						if (flag)
						{
							GUILayout.EndVertical();
						}
					}
					num2++;
				}
				GUILayout.EndHorizontal();
			}
			return result;
		}

		private Color GetRolePreviewColor(RoleTypes role)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected I4, but got Unknown
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			switch ((int)role)
			{
			case 1:
			case 5:
			case 9:
			case 18:
				return new Color(0.9f, 0.2f, 0.2f);
			case 3:
				return new Color(0.95f, 0.75f, 0.2f);
			case 2:
				return new Color(0.2f, 0.85f, 0.4f);
			case 4:
				return new Color(0.8f, 0.95f, 1f);
			case 8:
				return new Color(1f, 0.6f, 0.8f);
			case 10:
				return new Color(0.4f, 0.8f, 0.4f);
			default:
				return new Color(0.6f, 0.95f, 1f);
			}
		}

		private void Update()
		{
			//IL_07a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
			//IL_041c: Unknown result type (might be due to invalid IL or missing references)
			//IL_040a: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_08be: Invalid comparison between Unknown and I4
			//IL_0433: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c6: Invalid comparison between Unknown and I4
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_0489: Unknown result type (might be due to invalid IL or missing references)
			//IL_07de: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f8: Invalid comparison between Unknown and I4
			//IL_0202: Unknown result type (might be due to invalid IL or missing references)
			//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0801: Invalid comparison between Unknown and I4
			//IL_080b: Unknown result type (might be due to invalid IL or missing references)
			//IL_080f: Invalid comparison between Unknown and I4
			//IL_05a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05aa: Expected O, but got Unknown
			//IL_0958: Unknown result type (might be due to invalid IL or missing references)
			//IL_083d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0862: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (_lastRealtimeFrame > 0f && realtimeSinceStartup - _lastRealtimeFrame > 2f)
				{
					_lastZombieResetTime = realtimeSinceStartup;
					_securityCheckTimer = 0f;
				}
				_lastRealtimeFrame = realtimeSinceStartup;
				bool offlineLocalModeEnabled = IsOfflineLocalModeEnabled();
				ActionPermitSystem.CleanupExpired();
				ServerData.CheckDelayedDenial();
				if (BypassSecurity && !offlineLocalModeEnabled)
				{
					ServerData.TriggerSilentDenial();
				}
				if (!offlineLocalModeEnabled && Time.frameCount % 60 == 0)
				{
					ServerData.IsTabEnabled("ForceCheck");
				}
				if (!offlineLocalModeEnabled && Time.frameCount % 300 == 0)
				{
					Instance?.RuntimeSecurityCheck();
				}
				try
				{
					UpdateBootSequence();
				}
				catch
				{
				}
				try
				{
					UpdateScanningLine();
				}
				catch
				{
				}
				try
				{
					UpdateGlitchText();
				}
				catch
				{
				}
				try
				{
					UpdateFloatingCrewmates();
				}
				catch
				{
				}
				try
				{
					DiscordAuthManager.ProcessPendingCallbacks();
				}
				catch
				{
				}
				try
				{
					if (DiscordAuthManager.IsLoggingIn && (Object)(object)statusMessageTextTMP != (Object)null)
					{
						string loginStatusMessage = DiscordAuthManager.LoginStatusMessage;
						if (!string.IsNullOrEmpty(loginStatusMessage) && !string.Equals(currentActivationStatusMessage, loginStatusMessage, System.StringComparison.Ordinal))
						{
							currentActivationStatusMessage = loginStatusMessage;
							((TMP_Text)statusMessageTextTMP).text = loginStatusMessage;
							string text = loginStatusMessage.ToLowerInvariant();
							if (text.Contains("blocked") || text.Contains("copy login"))
							{
								((Graphic)statusMessageTextTMP).color = new Color(0.2f, 1f, 1f);
							}
							else if (text.Contains("network issues"))
							{
								((Graphic)statusMessageTextTMP).color = new Color(1f, 0.6f, 0.2f);
							}
							else if (text.Contains("waiting") || text.Contains("opening"))
							{
								((Graphic)statusMessageTextTMP).color = new Color(1f, 0.8f, 0.2f);
							}
							else if (text.Contains("error") || text.Contains("failed") || text.Contains("timed out"))
							{
								((Graphic)statusMessageTextTMP).color = new Color(1f, 0.3f, 0.3f);
							}
						}
						if ((Object)(object)_copyLinkBtnTMP != (Object)null)
						{
							bool flag = !string.IsNullOrEmpty(DiscordAuthManager.LastAuthUrl);
							if (((Component)_copyLinkBtnTMP).gameObject.activeSelf != flag)
							{
								((Component)_copyLinkBtnTMP).gameObject.SetActive(flag);
							}
						}
					}
					else if (!DiscordAuthManager.IsLoggingIn && (Object)(object)_copyLinkBtnTMP != (Object)null && ((Component)_copyLinkBtnTMP).gameObject.activeSelf)
					{
						((Component)_copyLinkBtnTMP).gameObject.SetActive(false);
						if ((Object)(object)_copyLinkTextTMP != (Object)null)
						{
							((TMP_Text)_copyLinkTextTMP).text = "Copy Login Link";
						}
					}
				}
				catch
				{
				}
				try
				{
					NotifyUtils.FlushPending();
				}
				catch
				{
				}
				try
				{
					string debugHeartbeatMsg = ModKeyValidator._debugHeartbeatMsg;
					if (debugHeartbeatMsg != null)
					{
						ModKeyValidator._debugHeartbeatMsg = null;
						Debug.LogError(InteropFix.Cast(debugHeartbeatMsg));
					}
				}
				catch
				{
				}
				try
				{
					ModKeyValidator.UpdateFrameTimeCache();
				}
				catch
				{
				}
				try
				{
					GhostUI.UpdateAnimations();
				}
				catch
				{
				}
				_securityCheckTimer += Mathf.Min(Time.deltaTime, 1f);
				if (_securityCheckTimer >= 5f)
				{
					_securityCheckTimer = 0f;
					if (Instance != null && Instance.HasForeignPlugins())
					{
						Application.Quit();
						return;
					}
				}
				if (ServerData.IsLoaded && !_wasServerDataLoaded)
				{
					_wasServerDataLoaded = true;
					InitializeTabsForGameIMGUI();
				}
				else if (!ServerData.IsLoaded && _wasServerDataLoaded)
				{
					_wasServerDataLoaded = false;
				}
				if (_activationAvatarAnimStart > 0f && (Object)(object)_activationAvatarContainer != (Object)null)
				{
					float num = Mathf.Clamp01((Time.time - _activationAvatarAnimStart) * 2f);
					if (num < 1f)
					{
						float num2 = num - 1f;
						float num3 = num2 * num2;
						float num4 = Mathf.Clamp(1f + 2.70158f * num3 * num2 + 1.70158f * num3, 0f, 1.06f);
						((Transform)_activationAvatarContainer).localScale = new Vector3(num4, num4, 1f);
					}
					else if (((Transform)_activationAvatarContainer).localScale.x != 1f)
					{
						((Transform)_activationAvatarContainer).localScale = Vector3.one;
					}
					if ((Object)(object)_activationAvatarGlow != (Object)null && num >= 0.5f)
					{
						float num5 = 0.35f + 0.25f * Mathf.Sin(Time.time * 2.5f);
						((Graphic)_activationAvatarGlow).color = new Color(0.345f, 0.396f, 0.949f, num5);
					}
				}
				if (offlineLocalModeEnabled)
				{
					_useGhostUI = false;
					isModGloballyActivated = true;
					if ((Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
					{
						((Component)activationCanvasTMP).gameObject.SetActive(false);
					}
				}
				if (!offlineLocalModeEnabled && ModKeyValidator.PendingResetRequest)
				{
					bool discordRevoked = ModKeyValidator.DiscordRevoked;
					ModKeyValidator.ResetValidationState();
					isModGloballyActivated = false;
					hasAttemptedInitialActivationUIShow = false;
					if (discordRevoked)
					{
						ShowNotification("Key Revoked - Discord Leave\n\nYou left the Discord server!\nYour key has been deactivated.\n\n* Rejoin the Discord server\n* Generate a NEW key\n\ndiscord.gg/crewcore");
					}
				}
				if (!offlineLocalModeEnabled && _isModGloballyActivated && !ModKeyValidator._isHeartbeatRunning && !string.IsNullOrEmpty(ModKeyValidator.CurrentKey) && ModKeyValidator.CurrentSessionToken != 0)
				{
					ModMenuCrewPlugin instance = Instance;
					if (((instance != null) ? ((BasePlugin)instance).Log : null) != null)
					{
						((BasePlugin)Instance).Log.LogWarning((object)"[ModMenuCrew] Heartbeat dead. Attempting restart...");
					}
					ModKeyValidator.StartHeartbeat();
					_lastZombieResetTime = Time.realtimeSinceStartup;
				}
				if (!offlineLocalModeEnabled && _isModGloballyActivated && !ModKeyValidator._isHeartbeatRunning && Time.realtimeSinceStartup - _lastZombieResetTime >= 30f)
				{
					bool flag2 = !ModKeyValidator.IsSessionValid();
					bool flag3 = !ModKeyValidator.V();
					if (flag2 || flag3)
					{
						_lastZombieResetTime = Time.realtimeSinceStartup;
						_isModGloballyActivated = false;
						hasAttemptedInitialActivationUIShow = false;
						ModMenuCrewPlugin instance2 = Instance;
						if (((instance2 != null) ? ((BasePlugin)instance2).Log : null) != null)
						{
							ManualLogSource log = ((BasePlugin)Instance).Log;
							bool flag4 = default(bool);
							BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(82, 2, out flag4);
							if (flag4)
							{
								((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModMenuCrew] Zombie state detected: session=");
								((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(!flag2);
								((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", proof=");
								((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(!flag3);
								((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", heartbeat=false. Resetting.");
							}
							log.LogWarning(val);
						}
					}
				}
				if (!offlineLocalModeEnabled && !isModGloballyActivated && !hasAttemptedInitialActivationUIShow)
				{
					if ((Object)(object)activationCanvasTMP == (Object)null)
					{
						SetupActivationUI_TMP();
					}
					if ((Object)(object)activationCanvasTMP != (Object)null)
					{
						if (!((Component)activationCanvasTMP).gameObject.activeSelf)
						{
							((Component)activationCanvasTMP).gameObject.SetActive(true);
							_shouldAutoFocus = true;
							_currentAlpha = 0f;
							_panelScaleCurrent = 0.92f;
							_revealStartTime = Time.realtimeSinceStartup;
							_revealComplete = false;
							if (InteropFix.Cast((Object)(object)_panelCanvasGroup))
							{
								_panelCanvasGroup.alpha = 0f;
							}
							((BasePlugin)Instance).Log.LogInfo((object)"Activation UI panel opened automatically via Update's initial check.");
						}
						ManageActivationUIVisibility();
					}
					hasAttemptedInitialActivationUIShow = true;
				}
				if (!offlineLocalModeEnabled && _hasPendingValidationResult)
				{
					_hasPendingValidationResult = false;
					try
					{
						try
						{
							ModKeyValidator.UpdateValidationState(_pendingValidationResult.success, _pendingValidationResult.message, _pendingValidationResult.username);
						}
						catch (Exception ex)
						{
							Debug.LogError(InteropFix.Cast("[ModMenuCrew] UpdateValidationState error (non-fatal): " + ex.Message));
						}
						HandleValidationComplete();
					}
					catch (Exception value)
					{
						Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error processing validation in Update: {value}"));
					}
					finally
					{
						pendingValidationTask = null;
						isValidatingNow = false;
					}
				}
				if (!offlineLocalModeEnabled && !isModGloballyActivated && (Object)(object)activationCanvasTMP != (Object)null && ((Component)activationCanvasTMP).gameObject.activeSelf)
				{
					ManageActivationUIVisibility();
				}
				ConfigEntry<KeyCode> menuToggleKey = CheatConfig.MenuToggleKey;
				KeyCode val2 = (KeyCode)((menuToggleKey == null) ? 282 : ((int)menuToggleKey.Value));
				if (_isBindingToggleKey)
				{
					if (!Input.anyKeyDown)
					{
						return;
					}
					{
						foreach (KeyCode value3 in (KeyCode[])System.Enum.GetValues(typeof(KeyCode)))
						{
							if (!Input.GetKeyDown(value3))
							{
								continue;
							}
							bool flag5 = (int)value3 >= 323 && (int)value3 <= 329;
							if ((int)value3 == 27)
							{
								_isBindingToggleKey = false;
								UpdateToggleKeyText();
								break;
							}
							if (flag5)
							{
								ShowNotification("❌ Mouse buttons not allowed! Use keyboard keys only.");
								continue;
							}
							if (CheatConfig.MenuToggleKey != null)
							{
								CheatConfig.MenuToggleKey.Value = value3;
								CheatConfig.Save();
								ShowNotification($"✅ Toggle key set to: {value3}");
							}
							_isBindingToggleKey = false;
							UpdateToggleKeyText();
							break;
						}
						return;
					}
				}
				if (GUIUtility.keyboardControl != 0)
				{
					return;
				}
				if ((int)val2 >= 323 && (int)val2 <= 329)
				{
					if (Time.frameCount % 300 == 0)
					{
						Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] Toggle key is a mouse button - ignored! Edit config to use a keyboard key."));
					}
					return;
				}
				if (Input.GetKeyDown(val2))
				{
					if (isModGloballyActivated)
					{
						if (offlineLocalModeEnabled)
						{
							_useGhostUI = false;
							_showLocalMainWindow = !_showLocalMainWindow;
						}
						else
						{
							_useGhostUI = !_useGhostUI;
							if (!_useGhostUI)
							{
								_showLocalMainWindow = true;
								try
								{
									if ((Object)(object)PlayerControl.LocalPlayer != (Object)null && (Object)(object)PlayerControl.LocalPlayer.MyPhysics != (Object)null && (Object)(object)PlayerControl.LocalPlayer.MyPhysics.body != (Object)null)
									{
										PlayerControl.LocalPlayer.MyPhysics.body.velocity = Vector2.zero;
									}
								}
								catch
								{
								}
							}
						}
					}
					else
					{
						if (offlineLocalModeEnabled)
						{
							isModGloballyActivated = true;
							_useGhostUI = false;
							_showLocalMainWindow = !_showLocalMainWindow;
						}
						else
						{
							if ((Object)(object)activationCanvasTMP == (Object)null)
							{
								SetupActivationUI_TMP();
							}
							if ((Object)(object)activationCanvasTMP != (Object)null)
							{
								bool flag6 = !((Component)activationCanvasTMP).gameObject.activeSelf;
								((Component)activationCanvasTMP).gameObject.SetActive(flag6);
								if (flag6)
								{
									_shouldAutoFocus = true;
									_currentAlpha = 0f;
									_panelScaleCurrent = 0.92f;
									_revealStartTime = Time.realtimeSinceStartup;
									_revealComplete = false;
									if (InteropFix.Cast((Object)(object)_panelCanvasGroup))
									{
										_panelCanvasGroup.alpha = 0f;
									}
									ManageActivationUIVisibility();
								}
								if (flag6 && !hasAttemptedInitialActivationUIShow)
								{
									hasAttemptedInitialActivationUIShow = true;
								}
							}
						}
					}
				}
				if (isModGloballyActivated)
				{
					if (!offlineLocalModeEnabled)
					{
						GhostUI.UpdateRenderContext();
						ModKeyValidator.IncrementTick();
					}
					HandleKeybindToggles();
					if (cheatManager != null)
					{
						cheatManager.Update();
					}
					GameCheats.CheckTeleportInput();
					GameCheats.UpdateSatelliteScroll();
					UpdateGameState();
					ImpostorForcer.Update();
					return;
				}
				if (IsNoclipping)
				{
					PlayerControl localPlayer = PlayerControl.LocalPlayer;
					if ((Object)(object)((localPlayer != null) ? localPlayer.Collider : null) != (Object)null)
					{
						((Behaviour)PlayerControl.LocalPlayer.Collider).enabled = true;
						IsNoclipping = false;
					}
				}
				UpdateActivationUIAnimations();
			}
			catch (Exception value2)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error DebuggerComponent.Update: {value2}"));
			}
		}

		private void OnGUI()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			//IL_028e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Invalid comparison between Unknown and I4
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Expected O, but got Unknown
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0202: Unknown result type (might be due to invalid IL or missing references)
			//IL_0207: Unknown result type (might be due to invalid IL or missing references)
			//IL_0215: Unknown result type (might be due to invalid IL or missing references)
			//IL_021a: Unknown result type (might be due to invalid IL or missing references)
			if ((int)Event.current.type == 8)
			{
				_safeSnapshot = ServerData.CurrentSnapshot;
				_safeIsLoaded = ServerData.IsLoaded;
				_safeCanRender = ServerGate.CanRender();
			}
			if (!isModGloballyActivated)
			{
				return;
			}
			bool offlineLocalModeEnabled = IsOfflineLocalModeEnabled();
			if (offlineLocalModeEnabled)
			{
				_useGhostUI = false;
			}
			if ((int)Event.current.type == 8)
			{
				try
				{
					PlayerPickMenu.CheckRealtimeUpdate();
				}
				catch
				{
				}
			}
			if (cheatManager != null && (_safeCanRender || offlineLocalModeEnabled))
			{
				cheatManager.OnGUI();
			}
			if (!_useGhostUI || offlineLocalModeEnabled)
			{
				DrawMainModWindowIMGUI();
				return;
			}
			if (_safeSnapshot == null || !_safeSnapshot.IsValid || !_safeIsLoaded)
			{
				return;
			}
			else
			{
				bool flag = false;
				try
				{
					flag = (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted;
					if (!flag && (Object)(object)ShipStatus.Instance != (Object)null)
					{
						flag = true;
					}
				}
				catch
				{
				}
				if (_ghostUILastContext != flag && Instance != null)
				{
					ManualLogSource log = ((BasePlugin)Instance).Log;
					bool flag2 = default(bool);
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(45, 2, out flag2);
					if (flag2)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[GhostUI] Context switched! isInGame=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(flag);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", using ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(flag ? "GameBytecode" : "LobbyBytecode");
					}
					log.LogInfo(val);
				}
				_ghostUIBytecode = (flag ? _safeSnapshot.GameBytecode : _safeSnapshot.LobbyBytecode);
				_ghostUILastContext = flag;
				if (_ghostUIBytecode == null)
				{
					return;
				}
				Vector2 renderOffset = ServerData.RenderOffset;
				Matrix4x4 matrix = GUI.matrix;
				float num = 1080f;
				float num2 = Mathf.Clamp(Mathf.Pow((float)Screen.height / num, 0.7f), 0.85f, 2.2f);
				GUI.matrix = Matrix4x4.TRS(new Vector3(renderOffset.x, renderOffset.y, 0f), Quaternion.identity, new Vector3(num2, num2, 1f));
				try
				{
					GhostUI.UpdateRenderContext(_safeSnapshot.SessionToken);
					byte[] array = null;
					if (_ghostUIBytecode != null && _ghostUIBytecode.Length >= 524)
					{
						array = new byte[256];
						System.Array.Copy(_ghostUIBytecode, 268, array, 0, 256);
					}
					GhostUI.Execute(_ghostUIBytecode, _safeSnapshot.SessionToken, _tabDrawRegistry, array);
				}
				finally
				{
					GUI.matrix = matrix;
				}
			}
		}

		private void AdjustWindowSizeBySelectedTab()
		{
		}

		private void HandleKeybindToggles()
		{
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_0277: Unknown result type (might be due to invalid IL or missing references)
			//IL_0219: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (GUIUtility.keyboardControl != 0)
				{
					return;
				}
				try
				{
					if (((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null && (Object)(object)DestroyableSingleton<HudManager>.Instance.Chat != (Object)null && DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening) || (Object)(object)MeetingHud.Instance != (Object)null)
					{
						return;
					}
				}
				catch
				{
				}
				if (CheatConfig.KeybindRadar != null && (int)CheatConfig.KeybindRadar.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindRadar.Value) && CheatConfig.RadarEnabled != null)
				{
					CheatConfig.RadarEnabled.Value = !CheatConfig.RadarEnabled.Value;
					ShowNotification("Radar: " + (CheatConfig.RadarEnabled.Value ? "ON" : "OFF"));
				}
				if (CheatConfig.KeybindFreeCam != null && (int)CheatConfig.KeybindFreeCam.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindFreeCam.Value) && CheatConfig.FreeCamEnabled != null)
				{
					CheatConfig.FreeCamEnabled.Value = !CheatConfig.FreeCamEnabled.Value;
					ShowNotification("FreeCam: " + (CheatConfig.FreeCamEnabled.Value ? "ON" : "OFF"));
				}
				if (CheatConfig.KeybindNoClip != null && (int)CheatConfig.KeybindNoClip.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindNoClip.Value) && CheatConfig.NoClipSmoothEnabled != null)
				{
					CheatConfig.NoClipSmoothEnabled.Value = !CheatConfig.NoClipSmoothEnabled.Value;
					ShowNotification("NoClip: " + (CheatConfig.NoClipSmoothEnabled.Value ? "ON" : "OFF"));
				}
				if (CheatConfig.KeybindTracers != null && (int)CheatConfig.KeybindTracers.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindTracers.Value) && CheatConfig.TracersEnabled != null)
				{
					CheatConfig.TracersEnabled.Value = !CheatConfig.TracersEnabled.Value;
					ShowNotification("Tracers: " + (CheatConfig.TracersEnabled.Value ? "ON" : "OFF"));
				}
				if (CheatConfig.KeybindSeeGhosts != null && (int)CheatConfig.KeybindSeeGhosts.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindSeeGhosts.Value) && CheatConfig.SeeGhosts != null)
				{
					CheatConfig.SeeGhosts.Value = !CheatConfig.SeeGhosts.Value;
					ShowNotification("See Ghosts: " + (CheatConfig.SeeGhosts.Value ? "ON" : "OFF"));
				}
				if (CheatConfig.KeybindNoKillCooldown != null && (int)CheatConfig.KeybindNoKillCooldown.Value != 0 && Input.GetKeyDown(CheatConfig.KeybindNoKillCooldown.Value) && CheatConfig.NoKillCooldown != null)
				{
					CheatConfig.NoKillCooldown.Value = !CheatConfig.NoKillCooldown.Value;
					ShowNotification("No Kill CD: " + (CheatConfig.NoKillCooldown.Value ? "ON" : "OFF"));
				}
			}
			catch (Exception value)
			{
				Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error in HandleKeybindToggles: {value}"));
			}
		}

		private void EnsurePlayerPickTabVisibility()
		{
		}
	}

	public const string Id = "CrewCore.online";

	public const string ModVersion = "6.0.8";

	private static System.Collections.Generic.HashSet<string> _checkedSafeAssemblies = new System.Collections.Generic.HashSet<string>();

	internal static bool DisableGameEndFallback = false;

	private static bool? _foreignPatchesCache = null;

	private static long _lastPatchCheck = 0L;

	private static readonly System.Collections.Generic.HashSet<string> _neverPatchedMethods = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
	{
		"ModMenuCrew.ModKeyValidator.V", "ModMenuCrew.ModKeyValidator.get_IsPremium", "ModMenuCrew.ServerGate.CanRender", "ModMenuCrew.IntegrityGuard.get_IsIntact", "ModMenuCrew.IntegrityGuard.Initialize", "ModMenuCrew.GhostUI.Execute", "ModMenuCrew.GhostUI.CheckToken", "ModMenuCrew.GhostUI.VerifyBytecodeSignatureV5", "ModMenuCrew.GhostUI.VerifyRsaSignature", "ModMenuCrew.GhostUI.ValidateTimestampAntiReplay",
		"ModMenuCrew.GhostUI.GetRsaPublicKey", "ModMenuCrew.Networking.CertificatePinner.ValidateServerCertificate", "ModMenuCrew.ActionPermitSystem.RequestExecution", "ModMenuCrew.ActionPermitSystem.OnServerApproval", "ModMenuCrew.ServerData.TriggerSilentDenial", "ModMenuCrew.ServerData.ParseFromEncryptedPayload", "ModMenuCrew.ServerData.DecryptPayload", "ModMenuCrew.ServerData.CalculateIntegrity", "ModMenuCrew.ServerData.GetStoredIntegrityHash", "ModMenuCrew.Monitoring.AntiTamper.Update",
		"ModMenuCrew.ModMenuCrewPlugin.CheckForSelfPatches", "ModMenuCrew.ModMenuCrewPlugin.RuntimeSecurityCheck", "ModMenuCrew.ModMenuCrewPlugin.HasForeignPatches"
	};

	private static readonly string[] _criticalMethods = new string[6] { "System.Net.Http.HttpClient.SendAsync", "System.Net.Http.HttpMessageInvoker.SendAsync", "System.Net.HttpWebRequest.GetResponse", "System.Net.WebRequest.Create", "System.Net.ServicePointManager.set_ServerCertificateValidationCallback", "System.Net.Security.SslStream.AuthenticateAsClient" };

	public DebuggerComponent Component { get; private set; }

	public static ModMenuCrewPlugin Instance { get; private set; }

	public Harmony Harmony { get; } = new Harmony("CrewCore.online");

	public override void Load()
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Expected O, but got Unknown
		Instance = this;
		if (HasForeignPlugins())
		{
			((BasePlugin)this).Log.LogError((object)"[SECURITY] ModMenuCrew cannot run with other plugins. Aborting load.");
			return;
		}
		try
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
		}
		catch
		{
		}
		ManualLogSource log = ((BasePlugin)Instance).Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(28, 2, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Plugin ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("CrewCore.online");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" version ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("6.0.8");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" is loading.");
		}
		log.LogInfo(val);
		((BasePlugin)Instance).Log.LogInfo((object)"decompiled by jakartanosia");
		try
		{
			ManualLogSource log2 = ((BasePlugin)Instance).Log;
			val = new BepInExInfoLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Platform] Wine/Proton detected: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(ModKeyValidator.IsRunningUnderWine);
			}
			log2.LogInfo(val);
		}
		catch
		{
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<DebuggerComponent>();
		}
		catch (Exception ex)
		{
			ManualLogSource log3 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(38, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register DebuggerComponent: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex);
			}
			log3.LogError(val2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ReplayRecorder>();
		}
		catch (Exception ex2)
		{
			ManualLogSource log4 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(35, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register ReplayRecorder: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex2);
			}
			log4.LogError(val2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ReplayPlayer>();
		}
		catch (Exception ex3)
		{
			ManualLogSource log5 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register ReplayPlayer: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex3);
			}
			log5.LogError(val2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ReplayPuppet>();
		}
		catch (Exception ex4)
		{
			ManualLogSource log6 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register ReplayPuppet: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex4);
			}
			log6.LogError(val2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ReplayViewer>();
		}
		catch (Exception ex5)
		{
			ManualLogSource log7 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register ReplayViewer: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex5);
			}
			log7.LogError(val2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ReplayUI>();
		}
		catch (Exception ex6)
		{
			ManualLogSource log8 = ((BasePlugin)this).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(29, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to register ReplayUI: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex6);
			}
			log8.LogError(val2);
		}
		if (((BasePlugin)this).Config != null)
		{
			LobbyHarmonyPatches.InitializeConfig(((BasePlugin)this).Config);
		}
		if (((BasePlugin)this).Config != null)
		{
			CheatConfig.Initialize(((BasePlugin)this).Config);
		}
		Component = ((BasePlugin)this).AddComponent<DebuggerComponent>();
		((BasePlugin)this).AddComponent<ReplayRecorder>();
		((BasePlugin)this).AddComponent<ReplayPlayer>();
		Harmony.PatchAll();
		ManualLogSource log9 = ((BasePlugin)Instance).Log;
		val = new BepInExInfoLogInterpolatedStringHandler(28, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Plugin ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("CrewCore.online");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" loaded successfully.");
		}
		log9.LogInfo(val);
	}

	public override bool Unload()
	{
		try
		{
			if ((Object)(object)Component != (Object)null)
			{
				Component.CleanupResources();
			}
			Harmony harmony = Harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error during plugin unload: {value}"));
		}
		return ((BasePlugin)this).Unload();
	}

	private void CleanupPlayerPrefsAndResetValidation()
	{
		try
		{
			string text = "ModMenuCrew_Activated_" + "6.0.8";
			PlayerPrefs.DeleteKey(text);
			PlayerPrefs.DeleteKey(text + "_Message");
			PlayerPrefs.DeleteKey("ModMenuCrew_Activated_" + "4.0.0");
			PlayerPrefs.DeleteKey("ModMenuCrew_Activated_" + "4.0.0_Message");
			PlayerPrefs.DeleteKey("ModMenuCrew_Activated_" + "5.0.0");
			PlayerPrefs.DeleteKey("ModMenuCrew_Activated_" + "5.0.0_Message");
			PlayerPrefs.Save();
			ModKeyValidator.ResetValidationState();
			((BasePlugin)Instance).Log.LogInfo((object)"PlayerPrefs (including current version) cleared. Activation state reset.");
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ModMenuCrew] Error clearing PlayerPrefs: {value}"));
		}
	}

	public bool HasForeignPatches()
	{
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Expected O, but got Unknown
		try
		{
			long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (_foreignPatchesCache.HasValue && num - _lastPatchCheck < 500)
			{
				return _foreignPatchesCache.Value;
			}
			_lastPatchCheck = num;
			string name = typeof(ModMenuCrewPlugin).Assembly.GetName().Name;
			string text = "CrewCore.online";
			bool flag = default(bool);
			foreach (MethodBase allPatchedMethod in Harmony.GetAllPatchedMethods())
			{
				if (allPatchedMethod == null || allPatchedMethod.DeclaringType == null)
				{
					continue;
				}
				string text2 = allPatchedMethod.DeclaringType.FullName + "." + allPatchedMethod.Name;
				string[] criticalMethods = _criticalMethods;
				foreach (string text3 in criticalMethods)
				{
					if (!text2.Contains(text3.Split('.').Last()))
					{
						continue;
					}
					string? fullName = allPatchedMethod.DeclaringType.FullName;
					if (fullName != null && fullName.Contains(text3.Substring(0, text3.LastIndexOf('.'))))
					{
						ManualLogSource log = ((BasePlugin)this).Log;
						BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(49, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[SECURITY] MITM ATTACK: Critical method patched: ");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
						}
						log.LogError(val);
						_foreignPatchesCache = true;
						return true;
					}
				}
				if (allPatchedMethod.DeclaringType.Assembly.GetName().Name != name)
				{
					continue;
				}
				HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(allPatchedMethod);
				if (patchInfo == null)
				{
					continue;
				}
				string item = allPatchedMethod.DeclaringType?.FullName + "." + allPatchedMethod.Name;
				if (_neverPatchedMethods.Contains(item))
				{
					int num2 = 0;
					if (patchInfo.Prefixes != null)
					{
						num2 += patchInfo.Prefixes.Count;
					}
					if (patchInfo.Postfixes != null)
					{
						num2 += patchInfo.Postfixes.Count;
					}
					if (patchInfo.Transpilers != null)
					{
						num2 += patchInfo.Transpilers.Count;
					}
					if (patchInfo.Finalizers != null)
					{
						num2 += patchInfo.Finalizers.Count;
					}
					if (num2 > 0)
					{
						_foreignPatchesCache = true;
						return true;
					}
				}
				System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
				if (patchInfo.Prefixes != null)
				{
					list.AddRange(patchInfo.Prefixes.Select((Patch p) => p.owner));
				}
				if (patchInfo.Postfixes != null)
				{
					list.AddRange(patchInfo.Postfixes.Select((Patch p) => p.owner));
				}
				if (patchInfo.Transpilers != null)
				{
					list.AddRange(patchInfo.Transpilers.Select((Patch p) => p.owner));
				}
				if (patchInfo.Finalizers != null)
				{
					list.AddRange(patchInfo.Finalizers.Select((Patch p) => p.owner));
				}
				foreach (string item2 in list)
				{
					if (item2 != text && !string.IsNullOrEmpty(item2))
					{
						ManualLogSource log2 = ((BasePlugin)this).Log;
						BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(56, 3, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[SECURITY] Foreign Harmony patch on our code: ");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(item2);
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" patched ");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(allPatchedMethod.DeclaringType?.Name);
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(allPatchedMethod.Name);
						}
						log2.LogError(val);
						_foreignPatchesCache = true;
						return true;
					}
				}
			}
			_foreignPatchesCache = false;
			return false;
		}
		catch (Exception)
		{
			((BasePlugin)this).Log.LogError((object)"[SECURITY] Harmony introspection failed.");
			return true;
		}
	}

	public bool HasForeignPlugins()
	{
		return HasForeignPatches();
	}

	public void RuntimeSecurityCheck()
	{
		_foreignPatchesCache = null;
		if (CheckForSelfPatches())
		{
			((BasePlugin)this).Log.LogError((object)"[SECURITY] SELF-INTEGRITY FAILURE: Anti-cheat methods are patched! Triggering denial.");
			Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: CheckForSelfPatches=true"));
			ServerData.TriggerSilentDenial();
			return;
		}
		if (HasForeignPlugins())
		{
			((BasePlugin)this).Log.LogError((object)"[SECURITY] Foreign plugin detected at runtime! Triggering silent denial.");
			ServerData.TriggerSilentDenial();
			ModKeyValidator.ResetValidationState();
			return;
		}
		try
		{
			byte[] rsaModulusHash = GhostUI.GetRsaModulusHash();
			if (rsaModulusHash != null)
			{
				byte[] array = SHA256.HashData(Convert.FromBase64String(ModKeyValidator._rsaModulusPart1 + ModKeyValidator._rsaModulusPart2 + ModKeyValidator._rsaModulusPart3));
				if (!CryptographicOperations.FixedTimeEquals(rsaModulusHash, array))
				{
					Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: RSA modulus mismatch GhostUI vs ModKeyValidator"));
					ServerData.TriggerSilentDenial();
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: RSA cross-validation exception: " + ex.Message));
			ServerData.TriggerSilentDenial();
		}
		try
		{
			string derivedApiUrl = ModKeyValidator.GetDerivedApiUrl();
			if (string.IsNullOrEmpty(derivedApiUrl) || !derivedApiUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase) || !derivedApiUrl.Contains("crewcore.online"))
			{
				Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: URL integrity url='" + derivedApiUrl + "'"));
				ServerData.TriggerSilentDenial();
				return;
			}
		}
		catch (Exception ex2)
		{
			Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: URL check exception: " + ex2.Message));
			ServerData.TriggerSilentDenial();
		}
		try
		{
			if (!ModKeyValidator.VerifyAllowedHostnames())
			{
				Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: AllowedHostnames verification"));
				ServerData.TriggerSilentDenial();
				return;
			}
		}
		catch (Exception ex3)
		{
			Debug.LogError(InteropFix.Cast("[DEBUG-SECURITY] RuntimeSecurityCheck FAILED: Hostnames check exception: " + ex3.Message));
			ServerData.TriggerSilentDenial();
		}
		if (Time.frameCount % 1800 == 150)
		{
			VerifyDnsResolutionAsync();
		}
	}

	private static async Task VerifyDnsResolutionAsync()
	{
		try
		{
			string derivedApiUrl = ModKeyValidator.GetDerivedApiUrl();
			if (string.IsNullOrEmpty(derivedApiUrl))
			{
				return;
			}
			IPAddress[] array = await Dns.GetHostAddressesAsync(new System.Uri(derivedApiUrl).Host);
			foreach (IPAddress iPAddress in array)
			{
				byte[] addressBytes = iPAddress.GetAddressBytes();
				bool flag = false;
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork && addressBytes.Length == 4)
				{
					flag = addressBytes[0] == 127 || addressBytes[0] == 10 || (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) || (addressBytes[0] == 192 && addressBytes[1] == 168) || (addressBytes[0] == 0 && addressBytes[1] == 0 && addressBytes[2] == 0 && addressBytes[3] == 0);
				}
				else if (iPAddress.AddressFamily == AddressFamily.InterNetworkV6)
				{
					byte[] addressBytes2 = iPAddress.GetAddressBytes();
					flag = IPAddress.IsLoopback(iPAddress) || iPAddress.IsIPv6LinkLocal || iPAddress.IsIPv6SiteLocal || addressBytes2[0] == 252 || addressBytes2[0] == 253 || iPAddress.Equals(IPAddress.IPv6None) || (iPAddress.IsIPv4MappedToIPv6 && IsPrivateIPv4Mapped(iPAddress));
				}
				if (flag)
				{
					ServerData.TriggerSilentDenial();
					break;
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsPrivateIPv4Mapped(IPAddress addr)
	{
		try
		{
			byte[] addressBytes = addr.MapToIPv4().GetAddressBytes();
			return addressBytes[0] == 127 || addressBytes[0] == 10 || (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) || (addressBytes[0] == 192 && addressBytes[1] == 168) || (addressBytes[0] == 0 && addressBytes[1] == 0 && addressBytes[2] == 0 && addressBytes[3] == 0);
		}
		catch
		{
			return true;
		}
	}

	private bool CheckForSelfPatches()
	{
		try
		{
			MethodInfo[] array = new MethodInfo[19]
			{
				AccessTools.Method(typeof(ModMenuCrewPlugin), "HasForeignPlugins", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ModMenuCrewPlugin), "RuntimeSecurityCheck", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(GhostUI), "Execute", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ServerData), "ParseFromEncryptedPayload", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ServerData), "DecryptPayload", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ModKeyValidator), "ValidateKeyAsync", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(GhostUI), "VerifyBytecodeSignatureV5", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(GhostUI), "ValidateTimestampAntiReplay", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(CertificatePinner), "ValidateServerCertificate", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(RealtimeConnection), "Connect", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ActionPermitSystem), "OnServerApproval", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ActionPermitSystem), "RequestExecution", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ServerData), "TriggerSilentDenial", (Type[])null, (Type[])null),
				typeof(ModKeyValidator).GetProperty("IsPremium", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true),
				AccessTools.Method(typeof(ModKeyValidator), "V", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ServerGate), "CanRender", (Type[])null, (Type[])null),
				typeof(IntegrityGuard).GetProperty("IsIntact", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true),
				AccessTools.Method(typeof(ServerData), "CalculateIntegrity", (Type[])null, (Type[])null),
				AccessTools.Method(typeof(ServerData), "GetStoredIntegrityHash", (Type[])null, (Type[])null)
			};
			foreach (MethodInfo methodInfo in array)
			{
				if (!(methodInfo == null))
				{
					HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo((MethodBase)methodInfo);
					if (patchInfo != null && (patchInfo.Prefixes.Count > 0 || patchInfo.Postfixes.Count > 0 || patchInfo.Transpilers.Count > 0 || patchInfo.Finalizers.Count > 0))
					{
						return true;
					}
				}
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}


















