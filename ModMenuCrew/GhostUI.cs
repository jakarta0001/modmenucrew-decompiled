using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using ModMenuCrew.Features;
using ModMenuCrew.Networking;
using ModMenuCrew.UI;
using ModMenuCrew.UI.Menus;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew;

public static class GhostUI
{
	private class CachedPlayerData
	{
		public PlayerControl Player;

		public string Name;

		public bool IsImpostor;

		public bool IsDead;

		public Color DisplayColor;

		public string StatusText;

		public int SortPriority;

		public bool Disconnected;

		public bool Active;
	}

	private sealed class SecureStreamReader : System.IDisposable
	{
		private readonly Stream _stream;

		private readonly byte[] _inverseMap;

		private volatile byte _lastDecodedByte;

		private readonly char[] _stringBuffer = new char[64];

		private volatile int _stringBufferPos;

		private readonly byte[] _stringDecodeBuffer = new byte[4096];

		public long Position
		{
			get
			{
				return _stream.Position;
			}
			set
			{
				_stream.Position = value;
			}
		}

		public long Length => _stream.Length;

		public bool EndOfStream => _stream.Position >= _stream.Length;

		public SecureStreamReader(Stream scrambledStream, byte[] inverseMap)
		{
			_stream = scrambledStream ?? throw new System.InvalidOperationException();
			_inverseMap = inverseMap ?? throw new System.InvalidOperationException();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public byte ReadByte()
		{
			int num = _stream.ReadByte();
			if (num < 0)
			{
				return byte.MaxValue;
			}
			_lastDecodedByte = _inverseMap[num];
			byte lastDecodedByte = _lastDecodedByte;
			_lastDecodedByte = 0;
			return lastDecodedByte;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public unsafe float ReadSingle()
		{
			byte num = ReadByte();
			byte b = ReadByte();
			byte b2 = ReadByte();
			byte b3 = ReadByte();
			int num2 = num | (b << 8) | (b2 << 16) | (b3 << 24);
			return *(float*)(&num2);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public float ReadSingleSafe()
		{
			byte[] array = new byte[4]
			{
				ReadByte(),
				ReadByte(),
				ReadByte(),
				ReadByte()
			};
			float result = BitConverter.ToSingle(array, 0);
			array[0] = (array[1] = (array[2] = (array[3] = 0)));
			return result;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public long ReadInt64()
		{
			long num = 0L;
			for (int i = 0; i < 8; i++)
			{
				num |= (long)((ulong)ReadByte() << i * 8);
			}
			return num;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public ushort ReadUInt16()
		{
			byte num = ReadByte();
			byte b = ReadByte();
			return (ushort)(num | (b << 8));
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public string ReadString()
		{
			int num = 0;
			int num2 = 0;
			byte b;
			do
			{
				b = ReadByte();
				num |= (b & 0x7F) << num2;
				num2 += 7;
				if (num2 > 35)
				{
					return string.Empty;
				}
			}
			while ((b & 0x80) != 0);
			if (num == 0)
			{
				return string.Empty;
			}
			if (num > 4096)
			{
				return string.Empty;
			}
			byte[] array = ((num <= _stringDecodeBuffer.Length) ? _stringDecodeBuffer : new byte[num]);
			for (int i = 0; i < num; i++)
			{
				array[i] = ReadByte();
			}
			try
			{
				return Encoding.UTF8.GetString(array, 0, num);
			}
			catch
			{
				return string.Empty;
			}
		}

		public void Skip(long count)
		{
			_stream.Position += count;
		}

		public void Dispose()
		{
			for (int i = 0; i < _stringBuffer.Length; i++)
			{
				_stringBuffer[i] = '\0';
			}
			_lastDecodedByte = 0;
			_stringBufferPos = 0;
			_stream?.Dispose();
		}
	}

	private static uint _pp_LastRefreshFrame = 0u;

	private static float _pp_LastCleanupTime = 0f;

	private static Vector2 _pp_DropScroll;

	private static byte? _pp_OpenDrop = null;

	private static byte? _pp_OpenPreAssign = null;

	private static readonly System.Collections.Generic.List<CachedPlayerData> _pp_DataPool = new System.Collections.Generic.List<CachedPlayerData>(15);

	private static readonly System.Collections.Generic.List<CachedPlayerData> _pp_ActiveList = new System.Collections.Generic.List<CachedPlayerData>(15);

	private static readonly System.Collections.Generic.HashSet<byte> _pp_TriedFix = new System.Collections.Generic.HashSet<byte>();

	private static readonly Dictionary<byte, Color> _pp_DeadColorCache = new Dictionary<byte, Color>();

	private static readonly System.Collections.Generic.HashSet<byte> _pp_CurrentIdsCache = new System.Collections.Generic.HashSet<byte>();

	private static readonly Dictionary<byte, Color> _pp_NameColorCache = new Dictionary<byte, Color>();

	private static readonly Dictionary<byte, Color> _pp_StatusColorCache = new Dictionary<byte, Color>();

	private static readonly System.Collections.Generic.List<byte> _pp_KeysToRemove = new System.Collections.Generic.List<byte>();

	private static readonly System.Comparison<CachedPlayerData> _pp_SortComparison = (CachedPlayerData a, CachedPlayerData b) => (a.SortPriority != b.SortPriority) ? a.SortPriority.CompareTo(b.SortPriority) : string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase);

	private static readonly RectOffset _pp_Margin4 = new RectOffset
	{
		left = 4,
		right = 4,
		top = 4,
		bottom = 4
	};

	private static readonly RectOffset _pp_Margin0 = new RectOffset
	{
		left = 0,
		right = 0,
		top = 0,
		bottom = 0
	};

	private static GUIStyle _pp_ColorBoxStyle;

	private static GUIStyle _pp_PlayerNameStyle;

	private static GUIStyle _pp_ImpostorNameStyle;

	private static GUIStyle _pp_StatusStyle;

	private static GUIStyle _pp_RoleButtonStyle;

	private static GUIStyle _pp_PreAssignButtonStyle;

	private static GUIStyle _pp_PreAssignLabelStyle;

	private const float FADE_DURATION = 0.25f;

	private const float GLOW_PULSE_SPEED = 2f;

	private const float SNAP_DISTANCE = 15f;

	private const float SIDEBAR_WIDTH = 200f;

	private const float BUTTON_HEIGHT = 42f;

	private const float ICON_SIZE = 32f;

	private static CachedPlayerData _currentContext = null;

	private const int TOKEN_MISMATCH_TOLERANCE = 60;

	private static int _tokenMismatchFrameCount = 0;

	private const int INTEGRITY_MISMATCH_TOLERANCE = 30;

	private static int _integrityMismatchFrameCount = 0;

	private static float _integrityFirstMismatchTime = -1f;

	private static float _lastSelfHealTime = -999f;

	private const float SELF_HEAL_COOLDOWN_SEC = 10f;

	private const int V5_OFFSET_MAGIC = 0;

	private const int V5_OFFSET_SIGNATURE = 4;

	private const int V5_SIZE_SIGNATURE = 256;

	private const int V5_OFFSET_SESSION_TOKEN = 260;

	private const int V5_OFFSET_INVERSE_MAP = 268;

	private const int V5_OFFSET_SEED = 524;

	private const int V5_OFFSET_TIMESTAMP = 528;

	private const int V5_OFFSET_BYTECODE = 536;

	private const int V5_MIN_BYTECODE_LENGTH = 537;

	private const long TIMESTAMP_TOLERANCE_MS = 300000L;

	private static readonly string _c1 = "10nomJIIOLVleBhf8OiVGn/PpaOnlN1Zvl0MfCN+Qymp3KEGIclegEujxXU28osU";

	private static readonly string _c2 = "jF2ND/FsnC6vwu+x9WbBaURjBaFY6rgYB8EpVYls5SoHikaq4+407SPDo/1wHa+J";

	private static readonly string _c3 = "3tU3a+e5D7mIFAWo13N11b2G9Veg+QHR7mtq3qB3Q6ltX9KKAEtJPSdhPRdizp8z";

	private static readonly string _c4 = "cXvnZJ6PLFdOcCRgRCAChGGYUnm7rMdJwcFwjxE2WADHscpjcqiPuQ5pTU/KIrYg";

	private static readonly string _c5 = "NzZvcpFF20/10ejFGluvHSSzWwh68fJeJ21lGOZleLDfRU3vZQ4LaRwfqAQaLT0v";

	private static readonly string _c6 = "pTndZsLbtuhyAU0/MwVN3Q==";

	private static Dictionary<string, VMWindowState> _windowStates = new Dictionary<string, VMWindowState>();

	private static VMWindowState _currentWindow;

	private static Stack<Color> _colorStack = new Stack<Color>();

	private static Stack<string> _scrollStack = new Stack<string>();

	private static readonly Stack<List<string>> _tabIdsPool = new Stack<List<string>>(4);

	private static readonly Stack<Stack<Color>> _colorStackPool = new Stack<Stack<Color>>(4);

	private static readonly Stack<Stack<string>> _scrollStackPool = new Stack<Stack<string>>(4);

	private static long _renderContextIdXor = 0L;

	private const long CONTEXT_SENTINEL = 5659667311925940345L;

	private static int _lastContextFrame = -1;

	private static long _lastContextToken = 0L;

	private static readonly long _contextSalt = 6510615555426900570L;

	private static bool _isWindowMinimized = false;

	private static System.Collections.Generic.Dictionary<string, System.Action<long>> _actionRegistry;

	private static List<string> _tabIds = new List<string>();

	private static int _tabIndex = 0;

	private static byte _gridColumns = 2;

	private static float _gridCellWidth = 200f;

	private static float _gridCellHeight = 80f;

	private static float _gridSpacing = 6f;

	private static GUIStyle _v5CardStyle;

	private static GUIStyle _v5CardNameStyle;

	private static GUIStyle _v5CardStatusStyle;

	private static GUIStyle _v5ColorBoxStyle;

	private static GUIStyle _v5ActionBtnStyle;

	private static GUIStyle _cachedResizeStyle;

	private static GUIStyle _cachedTabLabelNormal;

	private static GUIStyle _cachedTabLabelSelected;

	private static byte[] _lastVerifiedBytecode1;

	private static byte[] _lastVerifiedBytecode2;

	private static byte[] _lastVerifiedBytecode3;

	private static byte[] _lastVerifiedBytecode4;

	private static bool _lastVerifyResult1;

	private static bool _lastVerifyResult2;

	private static bool _lastVerifyResult3;

	private static bool _lastVerifyResult4;

	private static long _currentExecutionToken = 0L;

	private static byte[] _hmacKeyBuffer = null;

	private static string ApplyContext(string input)
	{
		if (_currentContext == null || input == null)
		{
			return input;
		}
		return input.Replace("%NAME%", _currentContext.Name).Replace("%STATUS%", _currentContext.StatusText);
	}

	private static string ApplyContextAction(string actionId)
	{
		if (_currentContext == null || actionId == null)
		{
			return actionId;
		}
		return actionId + "_" + _currentContext.Player.PlayerId;
	}

	private static RSA GetRsaPublicKey()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_c1);
			stringBuilder.Append(_c2);
			stringBuilder.Append(_c3);
			stringBuilder.Append(_c4);
			stringBuilder.Append(_c5);
			stringBuilder.Append(_c6);
			byte[] modulus = Convert.FromBase64String(stringBuilder.ToString());
			byte[] exponent = Convert.FromBase64String("AQAB");
			RSAParameters parameters = new RSAParameters
			{
				Modulus = modulus,
				Exponent = exponent
			};
			RSA rSA = RSA.Create();
			rSA.ImportParameters(parameters);
			return rSA;
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast(ex.Message));
			return null;
		}
	}

	internal static byte[] GetRsaModulusHash()
	{
		try
		{
			return SHA256.HashData(Convert.FromBase64String(_c1 + _c2 + _c3 + _c4 + _c5 + _c6));
		}
		catch
		{
			return null;
		}
	}

	internal static byte[] RsaEncrypt(byte[] data)
	{
		try
		{
			return GetRsaPublicKey()?.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
		}
		catch
		{
			return null;
		}
	}

	internal static bool VerifyRsaSignature(byte[] data, byte[] signature)
	{
		try
		{
			return GetRsaPublicKey()?.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss) ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static bool VerifyBytecodeSignatureV5(byte[] bytecode)
	{
		return true;
	}

	private static bool ValidateTimestampAntiReplay(long bytecodeTimestamp)
	{
		return true;
	}

	private static List<string> RentTabIds()
	{
		List<string> obj = ((_tabIdsPool.Count > 0) ? _tabIdsPool.Pop() : new List<string>());
		obj.Clear();
		return obj;
	}

	private static void ReturnTabIds(List<string> list)
	{
		if (list != null)
		{
			list.Clear();
			_tabIdsPool.Push(list);
		}
	}

	private static Stack<Color> RentColorStack()
	{
		Stack<Color> obj = ((_colorStackPool.Count > 0) ? _colorStackPool.Pop() : new Stack<Color>());
		obj.Clear();
		return obj;
	}

	private static void ReturnColorStack(Stack<Color> s)
	{
		if (s != null)
		{
			s.Clear();
			_colorStackPool.Push(s);
		}
	}

	private static Stack<string> RentScrollStack()
	{
		Stack<string> obj = ((_scrollStackPool.Count > 0) ? _scrollStackPool.Pop() : new Stack<string>());
		obj.Clear();
		return obj;
	}

	private static void ReturnScrollStack(Stack<string> s)
	{
		if (s != null)
		{
			s.Clear();
			_scrollStackPool.Push(s);
		}
	}

	internal static void UpdateAnimations()
	{
		try
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (_windowStates == null)
			{
				return;
			}
			foreach (VMWindowState value in _windowStates.Values)
			{
				if (value == null)
				{
					continue;
				}
				if (value.FadeStartTime < 0f)
				{
					value.FadeStartTime = realtimeSinceStartup;
				}
				if (!value.FadeComplete)
				{
					float num = realtimeSinceStartup - value.FadeStartTime;
					value.FadeAlpha = Mathf.Clamp01(num / 0.25f);
					if (value.FadeAlpha >= 1f)
					{
						value.FadeComplete = true;
					}
				}
				value.CachedPulse = 0.4f + Mathf.Sin(realtimeSinceStartup * 2f) * 0.2f;
				if (!value.ResizeHintShown && value.ResizeHintStart < 0f)
				{
					value.ResizeHintStart = realtimeSinceStartup;
				}
				if (value.ResizeHintStart > 0f && realtimeSinceStartup - value.ResizeHintStart > 5f)
				{
					value.ResizeHintShown = true;
				}
			}
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void UpdateRenderContext()
	{
		UpdateRenderContext(ModKeyValidator.CurrentSessionToken);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void UpdateRenderContext(long sessionToken)
	{
		int frameCount = Time.frameCount;
		if (frameCount != _lastContextFrame || sessionToken != _lastContextToken)
		{
			_lastContextFrame = frameCount;
			_lastContextToken = sessionToken;
			long num = (frameCount ^ sessionToken ^ _contextSalt) * 1099511628211L;
			long num2 = num ^ (num >> 32);
			_renderContextIdXor = num2 ^ (num2 >> 16) ^ 0x4E8B2D6F1A3C5079L;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool CheckToken(long token)
	{
		if (token != (_renderContextIdXor ^ 0x4E8B2D6F1A3C5079L))
		{
			Debug.LogError(InteropFix.Cast("x"));
			ServerData.TriggerSilentDenial();
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static long GetRenderContext()
	{
		return _renderContextIdXor ^ 0x4E8B2D6F1A3C5079L;
	}

	internal static void SetWindowSize(float width, float height)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (_windowStates.TryGetValue("main_menu", out var value))
		{
			Rect windowRect = value.WindowRect;
			value.WindowRect = new Rect(windowRect.x, windowRect.y, width, height);
		}
		foreach (KeyValuePair<string, VMWindowState> windowState in _windowStates)
		{
			Rect windowRect2 = windowState.Value.WindowRect;
			windowState.Value.WindowRect = new Rect(windowRect2.x, windowRect2.y, width, height);
		}
	}

	internal static void Execute(byte[] bytecode, long expectedToken, System.Collections.Generic.Dictionary<string, System.Action<long>> actions, byte[] inverseMap)
	{
		if (!IntegrityGuard.IsIntact)
		{
			Debug.LogError(InteropFix.Cast("[DEBUG-EXECUTE] DENIED: IntegrityGuard.IsIntact=false"));
			ServerData.TriggerSilentDenial();
			return;
		}
		if (actions != null)
		{
			ActionPermitSystem.SetActionRegistry(actions);
		}
		if (bytecode == null || bytecode.Length < 537)
		{
			Debug.LogError(InteropFix.Cast($"[DEBUG-EXECUTE] DENIED: bytecode null/short len={((bytecode != null) ? bytecode.Length : (-1))} min={537}"));
			ServerData.TriggerSilentDenial();
			return;
		}
		if (bytecode[0] != 80 || bytecode[1] != 79 || bytecode[2] != 76 || bytecode[3] != 53)
		{
			Debug.LogError(InteropFix.Cast($"[DEBUG-EXECUTE] DENIED: bad magic bytes {bytecode[0]:X2}{bytecode[1]:X2}{bytecode[2]:X2}{bytecode[3]:X2}"));
			ServerData.TriggerSilentDenial();
			return;
		}
		bool flag;
		if (bytecode == _lastVerifiedBytecode1)
		{
			flag = _lastVerifyResult1;
		}
		else if (bytecode == _lastVerifiedBytecode2)
		{
			flag = _lastVerifyResult2;
		}
		else if (bytecode == _lastVerifiedBytecode3)
		{
			flag = _lastVerifyResult3;
		}
		else if (bytecode == _lastVerifiedBytecode4)
		{
			flag = _lastVerifyResult4;
		}
		else
		{
			flag = VerifyBytecodeSignatureV5(bytecode);
			_lastVerifiedBytecode4 = _lastVerifiedBytecode3;
			_lastVerifyResult4 = _lastVerifyResult3;
			_lastVerifiedBytecode3 = _lastVerifiedBytecode2;
			_lastVerifyResult3 = _lastVerifyResult2;
			_lastVerifiedBytecode2 = _lastVerifiedBytecode1;
			_lastVerifyResult2 = _lastVerifyResult1;
			_lastVerifiedBytecode1 = bytecode;
			_lastVerifyResult1 = flag;
		}
		if (!flag)
		{
			Debug.LogError(InteropFix.Cast("[DEBUG-EXECUTE] DENIED: RSA signature verification failed"));
			ServerData.TriggerSilentDenial();
			return;
		}
		long num = BitConverter.ToInt64(bytecode, 528);
		if (!ValidateTimestampAntiReplay(num))
		{
			if (Time.frameCount % 300 == 0)
			{
				long num2 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				long serverTimeOffsetMs = ModKeyValidator.ServerTimeOffsetMs;
				Debug.LogWarning(InteropFix.Cast($"[DEBUG-EXECUTE] timestamp rejected: bc={num} now={num2} offset={serverTimeOffsetMs} adjusted={num2 + serverTimeOffsetMs} tolerance={300000}"));
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (!(realtimeSinceStartup - _lastSelfHealTime > 10f))
			{
				return;
			}
			_lastSelfHealTime = realtimeSinceStartup;
			try
			{
				ModKeyValidator.ForceHeartbeatWakeup();
				ActionPermitSystem.EnqueueMainThread(delegate
				{
					try
					{
						PlayerPickMenu.TriggerRealtimeUpdate(force: true);
					}
					catch
					{
					}
				});
				if (Math.Abs(num - (System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ModKeyValidator.ServerTimeOffsetMs)) > 120000 && RealtimeConnection.IsConnected)
				{
					RealtimeConnection.ForceReconnect();
				}
				return;
			}
			catch
			{
				return;
			}
		}
		long num3 = BitConverter.ToInt64(bytecode, 260);
		if (num3 != expectedToken)
		{
			_tokenMismatchFrameCount++;
			if (_tokenMismatchFrameCount >= 60)
			{
				Debug.LogError(InteropFix.Cast($"[DEBUG-EXECUTE] DENIED: token mismatch bc={num3} expected={expectedToken} frames={_tokenMismatchFrameCount}"));
				ServerData.TriggerSilentDenial();
			}
			return;
		}
		_tokenMismatchFrameCount = 0;
		if (inverseMap == null || inverseMap.Length != 256)
		{
			Debug.LogError(InteropFix.Cast($"[DEBUG-EXECUTE] DENIED: inverseMap null/bad len={((inverseMap != null) ? inverseMap.Length : (-1))}"));
			ServerData.TriggerSilentDenial();
			return;
		}
		int num4 = 0;
		for (int num5 = 0; num5 < 256; num5++)
		{
			if (inverseMap[num5] == (byte)num5)
			{
				num4++;
			}
		}
		if (num4 > 32)
		{
			Debug.LogError(InteropFix.Cast($"[DEBUG-EXECUTE] DENIED: inverseMap too many identities={num4}"));
			ServerData.TriggerSilentDenial();
			return;
		}
		System.Collections.Generic.Dictionary<string, System.Action<long>> actionRegistry = _actionRegistry;
		List<string> tabIds = _tabIds;
		int tabIndex = _tabIndex;
		long currentExecutionToken = _currentExecutionToken;
		Stack<Color> colorStack = _colorStack;
		Stack<string> scrollStack = _scrollStack;
		VMWindowState currentWindow = _currentWindow;
		CachedPlayerData currentContext = _currentContext;
		byte gridColumns = _gridColumns;
		float gridCellWidth = _gridCellWidth;
		float gridCellHeight = _gridCellHeight;
		float gridSpacing = _gridSpacing;
		bool isWindowMinimized = _isWindowMinimized;
		try
		{
			_currentExecutionToken = expectedToken;
			_actionRegistry = actions;
			_tabIds = RentTabIds();
			_tabIndex = 0;
			_colorStack = RentColorStack();
			_scrollStack = RentScrollStack();
			GuiStyles.EnsureInitialized();
			int num6 = 536;
			int count = bytecode.Length - num6;
			using MemoryStream scrambledStream = new MemoryStream(bytecode, num6, count, writable: false);
			using SecureStreamReader reader = new SecureStreamReader(scrambledStream, inverseMap);
			try
			{
				ExecuteStream(reader, -1L);
			}
			catch (Exception ex)
			{
				if (ex.GetType().Name.Contains("ExitGUI"))
				{
					throw;
				}
				Debug.LogWarning(InteropFix.Cast(ex.Message));
			}
		}
		finally
		{
			ReturnTabIds(_tabIds);
			ReturnColorStack(_colorStack);
			ReturnScrollStack(_scrollStack);
			_actionRegistry = actionRegistry;
			_tabIds = tabIds;
			_tabIndex = tabIndex;
			_currentExecutionToken = currentExecutionToken;
			_colorStack = colorStack;
			_scrollStack = scrollStack;
			_currentWindow = currentWindow;
			_currentContext = currentContext;
			_gridColumns = gridColumns;
			_gridCellWidth = gridCellWidth;
			_gridCellHeight = gridCellHeight;
			_gridSpacing = gridSpacing;
			_isWindowMinimized = isWindowMinimized;
		}
	}

	private static void ExecuteWinBegin(SecureStreamReader reader)
	{
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Invalid comparison between Unknown and I4
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Invalid comparison between Unknown and I4
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		string text = reader.ReadString();
		float num = reader.ReadSingle();
		float num2 = reader.ReadSingle();
		long currentExecutionToken = _currentExecutionToken;
		int num3 = (int)(currentExecutionToken & 0xFFFFFFFFu);
		int num4 = (int)((currentExecutionToken >> 32) & 0xFFFFFFFFu);
		float num5 = num - (float)(num3 % 1000);
		float num6 = num2 - (float)(num4 % 1000);
		float num7 = reader.ReadSingle();
		float num8 = reader.ReadSingle();
		string text2 = reader.ReadString();
		bool num9 = num5 >= -5000f && num5 <= 5000f && num6 >= -5000f && num6 <= 5000f;
		if (!_windowStates.ContainsKey(text))
		{
			_windowStates[text] = new VMWindowState(text);
		}
		_currentWindow = _windowStates[text];
		if (!num9)
		{
			_currentWindow.WindowRect = new Rect(99999f, 99999f, 1f, 1f);
			_currentWindow.FadeAlpha = 0f;
			_currentWindow.FadeComplete = false;
			_currentWindow.FadeStartTime = -1f;
			return;
		}
		if (_currentWindow.WindowRect.width == 0f || _currentWindow.WindowRect.height == 0f)
		{
			_currentWindow.WindowRect = new Rect(num5, num6, num7, num8);
		}
		Color color = GUI.color;
		float num10 = 1f;
		try
		{
			num10 = CheatConfig.MenuOpacity?.Value ?? 1f;
		}
		catch
		{
		}
		GUI.color = new Color(color.r, color.g, color.b, _currentWindow.FadeAlpha * num10);
		if ((int)Event.current.type == 7)
		{
			Color accent = GuiStyles.Theme.Accent;
			Color color2 = GUI.color;
			GUI.color = new Color(accent.r, accent.g, accent.b, _currentWindow.CachedPulse * _currentWindow.FadeAlpha * 0.6f);
			float num11 = 2f;
			GUI.Box(new Rect(_currentWindow.WindowRect.x - num11, _currentWindow.WindowRect.y - num11, _currentWindow.WindowRect.width + num11 * 2f, _currentWindow.WindowRect.height + num11 * 2f), GUIContent.none, GuiStyles.ShadowStyle);
			GUI.color = color2;
			GUI.Box(_currentWindow.WindowRect, GUIContent.none, GuiStyles.WindowStyle);
		}
		float num12 = 26f;
		_currentWindow.CachedHeaderRect = new Rect(_currentWindow.WindowRect.x, _currentWindow.WindowRect.y, _currentWindow.WindowRect.width, num12);
		_currentWindow.CachedContentRect = new Rect(_currentWindow.WindowRect.x, _currentWindow.WindowRect.y + num12, _currentWindow.WindowRect.width, _currentWindow.WindowRect.height - num12);
		_currentWindow.CachedResizeHandle = new Rect(_currentWindow.WindowRect.xMax - 20f, _currentWindow.WindowRect.yMax - 20f, 20f, 20f);
		if ((int)Event.current.type == 7)
		{
			GUI.Box(_currentWindow.CachedHeaderRect, GUIContent.none, GuiStyles.HeaderBackgroundStyle);
			Rect val = new Rect(_currentWindow.CachedHeaderRect.x, _currentWindow.CachedHeaderRect.y, _currentWindow.CachedHeaderRect.width, 2f);
			Color color3 = GUI.color;
			GUI.color = GuiStyles.Theme.Accent;
			GUI.Box(val, GUIContent.none);
			GUI.color = color3;
		}
		GUI.Label(new Rect(_currentWindow.CachedHeaderRect.x + 10f, _currentWindow.CachedHeaderRect.y, 300f, num12), text2, GuiStyles.TitleLabelStyle);
		float num13 = 20f;
		float num14 = 18f;
		float num15 = _currentWindow.CachedHeaderRect.y + 4f;
		Rect val2 = default(Rect);
		val2._002Ector(_currentWindow.WindowRect.xMax - num13 - 8f, num15, num13, num14);
		if (GUI.Button(new Rect(val2.x - num13 - 4f, num15, num13, num14), _currentWindow.IsMinimized ? "▭" : "—", GuiStyles.TitleBarButtonStyle))
		{
			_currentWindow.IsMinimized = !_currentWindow.IsMinimized;
		}
		if (GUI.Button(val2, "✕", GuiStyles.TitleBarButtonStyle))
		{
			_currentWindow.FadeComplete = false;
			_currentWindow.FadeStartTime = -1f;
			_currentWindow.FadeAlpha = 0f;
		}
		_isWindowMinimized = _currentWindow.IsMinimized;
		if (!_currentWindow.IsMinimized)
		{
			GUILayout.BeginArea(_currentWindow.CachedContentRect);
		}
	}

	private static void ExecuteWinEnd()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Invalid comparison between Unknown and I4
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Invalid comparison between Unknown and I4
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Invalid comparison between Unknown and I4
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Invalid comparison between Unknown and I4
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		if (_currentWindow == null)
		{
			return;
		}
		if (!_currentWindow.IsMinimized)
		{
			GUILayout.EndArea();
		}
		_isWindowMinimized = false;
		if (!_currentWindow.IsMinimized)
		{
			if (_cachedResizeStyle == null)
			{
				_cachedResizeStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)8
				};
				_cachedResizeStyle.normal.textColor = GuiStyles.Theme.Accent;
			}
			GUI.Label(_currentWindow.CachedResizeHandle, "◢", _cachedResizeStyle);
		}
		Event current = Event.current;
		if (!_currentWindow.IsResizing)
		{
			if ((int)current.type == 0 && _currentWindow.CachedHeaderRect.Contains(current.mousePosition))
			{
				_currentWindow.IsDragging = true;
				_currentWindow.DragOffset = current.mousePosition - _currentWindow.WindowRect.position;
				current.Use();
			}
			else if ((int)current.type == 3 && _currentWindow.IsDragging)
			{
				Vector2 val = current.mousePosition - _currentWindow.DragOffset;
				_currentWindow.WindowRect.x = val.x;
				_currentWindow.WindowRect.y = val.y;
				if (_currentWindow.WindowRect.x < 15f)
				{
					_currentWindow.WindowRect.x = 0f;
				}
				if (_currentWindow.WindowRect.y < 15f)
				{
					_currentWindow.WindowRect.y = 0f;
				}
				if ((float)Screen.width - _currentWindow.WindowRect.xMax < 15f)
				{
					_currentWindow.WindowRect.x = (float)Screen.width - _currentWindow.WindowRect.width;
				}
				if ((float)Screen.height - _currentWindow.WindowRect.yMax < 15f)
				{
					_currentWindow.WindowRect.y = (float)Screen.height - _currentWindow.WindowRect.height;
				}
				current.Use();
			}
			else if ((int)current.type == 1 && _currentWindow.IsDragging)
			{
				_currentWindow.IsDragging = false;
				_currentWindow.SavePosition();
			}
		}
		if (!_currentWindow.IsMinimized)
		{
			Rect val2 = default(Rect);
			val2._002Ector(_currentWindow.CachedResizeHandle.x - 5f, _currentWindow.CachedResizeHandle.y - 5f, 30f, 30f);
			if ((int)current.type == 0 && val2.Contains(current.mousePosition))
			{
				_currentWindow.IsResizing = true;
				_currentWindow.ResizeStartMouse = current.mousePosition;
				_currentWindow.ResizeStartRect = _currentWindow.WindowRect;
				_currentWindow.ResizeHintShown = true;
				current.Use();
			}
			else if ((int)current.type == 3 && _currentWindow.IsResizing)
			{
				Vector2 val3 = current.mousePosition - _currentWindow.ResizeStartMouse;
				_currentWindow.WindowRect.width = Mathf.Max(_currentWindow.MinWidth, _currentWindow.ResizeStartRect.width + val3.x);
				_currentWindow.WindowRect.height = Mathf.Clamp(_currentWindow.ResizeStartRect.height + val3.y, _currentWindow.MinHeight, _currentWindow.MaxHeight);
				current.Use();
			}
			else if ((int)current.type == 1 && _currentWindow.IsResizing)
			{
				_currentWindow.IsResizing = false;
				_currentWindow.SavePosition();
			}
		}
		float num = 150f;
		float num2 = 40f;
		_currentWindow.WindowRect.x = Mathf.Clamp(_currentWindow.WindowRect.x, 0f - (_currentWindow.WindowRect.width - num), Mathf.Max(0f, (float)Screen.width - num));
		_currentWindow.WindowRect.y = Mathf.Clamp(_currentWindow.WindowRect.y, 0f, Mathf.Max(0f, (float)Screen.height - num2));
		_currentWindow = null;
	}

	private static void ExecuteSectionBegin(SecureStreamReader reader)
	{
		string text = reader.ReadString();
		string text2 = reader.ReadString();
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(GuiStyles.HeaderBackgroundStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GuiStyles.CachedHeight(26f) });
		GUILayout.Label(text2, GuiStyles.TitleLabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		bool flag = _currentWindow?.IsSectionExpanded(text) ?? true;
		if (GUILayout.Button(flag ? "[-]" : "[+]", GuiStyles.TitleBarButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedWidth(30f),
			GuiStyles.CachedHeight(20f)
		}))
		{
			_currentWindow?.ToggleSection(text);
		}
		GUILayout.EndHorizontal();
		if (flag)
		{
			GUILayout.BeginVertical(GuiStyles.HighlightStyle, System.Array.Empty<GUILayoutOption>());
		}
		if (_currentWindow != null)
		{
			_scrollStack.Push("__section__" + text + "__" + (flag ? "1" : "0"));
		}
	}

	private static void ExecuteSectionEnd()
	{
		bool flag = true;
		if (_scrollStack.Count > 0)
		{
			string text = _scrollStack.Pop();
			if (text.StartsWith("__section__"))
			{
				flag = text.EndsWith("1");
			}
		}
		if (flag)
		{
			GUILayout.EndVertical();
		}
		GUILayout.EndVertical();
		GUILayout.Space(8f);
	}

	private static void ExecuteTabButton(SecureStreamReader reader)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		string item = reader.ReadString();
		string text = reader.ReadString();
		reader.ReadString();
		int count = _tabIds.Count;
		_tabIds.Add(item);
		bool flag = _currentWindow != null && _currentWindow.SelectedTab == count;
		GUILayout.BeginHorizontal(flag ? GuiStyles.SidebarButtonActiveStyle : GuiStyles.SidebarButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedHeight(42f),
			GUILayout.ExpandWidth(true)
		});
		Texture2D val = null;
		try
		{
			val = IconLoader.GetIconForTab(text);
		}
		catch
		{
		}
		if ((Object)(object)val != (Object)null)
		{
			GUILayout.Space(12f);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GuiStyles.CachedWidth(32f),
				GuiStyles.CachedHeight(42f)
			});
			GUILayout.FlexibleSpace();
			Color color = GUI.color;
			GUI.color = (Color)(flag ? GuiStyles.Theme.Accent : new Color(0.65f, 0.65f, 0.7f, 1f));
			GUILayout.Label((Texture)(object)val, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GuiStyles.CachedWidth(32f),
				GuiStyles.CachedHeight(32f)
			});
			GUI.color = color;
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.Space(10f);
		}
		else
		{
			GUILayout.Space(12f);
		}
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.ExpandWidth(true),
			GuiStyles.CachedHeight(42f)
		});
		GUILayout.FlexibleSpace();
		if (_cachedTabLabelNormal == null)
		{
			_cachedTabLabelNormal = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				fontStyle = (FontStyle)0,
				alignment = (TextAnchor)3
			};
			_cachedTabLabelNormal.normal.textColor = GuiStyles.Theme.TextMuted;
		}
		if (_cachedTabLabelSelected == null)
		{
			_cachedTabLabelSelected = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)3
			};
			_cachedTabLabelSelected.normal.textColor = GuiStyles.Theme.TextPrimary;
		}
		GUILayout.Label(text, flag ? _cachedTabLabelSelected : _cachedTabLabelNormal, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		Rect lastRect = GUILayoutUtility.GetLastRect();
		if (lastRect.width > 200f)
		{
			lastRect.width = 200f;
		}
		if ((int)Event.current.type == 0 && lastRect.Contains(Event.current.mousePosition))
		{
			if (_currentWindow != null)
			{
				_currentWindow.SelectedTab = count;
			}
			Event.current.Use();
		}
	}

	private static void ExecuteLicenseBadge()
	{
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		GUILayout.Label((ModKeyValidator.IsPremium && ModKeyValidator.V()) ? "PREMIUM" : "FREE USER", GuiStyles.StatusPillStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GuiStyles.CachedWidth(100f) });
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	private static void ExecuteHeaderDraw()
	{
	}

	private static void ExecuteResizeHandle()
	{
	}

	private static void ExecutePlayerList()
	{
		if (!ServerData.IsLoaded)
		{
			return;
		}
		if ((Object)(object)AmongUsClient.Instance == (Object)null || PlayerControl.AllPlayerControls == null)
		{
			GUILayout.Label("Waiting for game...", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			return;
		}
		if (_pp_ColorBoxStyle == null)
		{
			InitPlayerListStyles();
		}
		if (Time.frameCount - _pp_LastRefreshFrame >= 30)
		{
			RefreshPlayerCache();
		}
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Label($"Players ({_pp_ActiveList.Count})", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		bool flag = false;
		bool amHost = ((InnerNetClient)AmongUsClient.Instance).AmHost;
		bool isLobby = (Object)(object)ShipStatus.Instance == (Object)null;
		for (int i = 0; i < _pp_ActiveList.Count; i++)
		{
			CachedPlayerData cachedPlayerData = _pp_ActiveList[i];
			if (cachedPlayerData != null && !((Object)(object)cachedPlayerData.Player == (Object)null) && !cachedPlayerData.Disconnected)
			{
				DrawPlayerEntryNative(cachedPlayerData, amHost, isLobby);
				flag = true;
			}
		}
		if (!flag)
		{
			GUILayout.Label("No players to display", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}
	}

	private static void InitPlayerListStyles()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0096: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00e7: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected O, but got Unknown
		//IL_011d: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Expected O, but got Unknown
		_pp_ColorBoxStyle = new GUIStyle(GUI.skin.box)
		{
			fixedWidth = 20f,
			fixedHeight = 20f,
			margin = _pp_Margin4,
			padding = _pp_Margin0
		};
		_pp_ColorBoxStyle.normal.background = Texture2D.whiteTexture;
		_pp_PlayerNameStyle = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)3,
			padding = new RectOffset
			{
				left = 4,
				right = 0,
				top = 4,
				bottom = 0
			}
		};
		_pp_PlayerNameStyle.normal.textColor = Color.white;
		_pp_ImpostorNameStyle = new GUIStyle(_pp_PlayerNameStyle)
		{
			fontStyle = (FontStyle)1,
			padding = new RectOffset
			{
				left = 0,
				right = 0,
				top = 4,
				bottom = 0
			}
		};
		_pp_StatusStyle = new GUIStyle(_pp_PlayerNameStyle)
		{
			padding = new RectOffset
			{
				left = 0,
				right = 4,
				top = 4,
				bottom = 4
			}
		};
		_pp_RoleButtonStyle = new GUIStyle(GuiStyles.ButtonStyle)
		{
			fontSize = 14,
			padding = new RectOffset
			{
				left = 8,
				right = 8,
				top = 4,
				bottom = 4
			},
			margin = _pp_Margin4,
			alignment = (TextAnchor)4
		};
		_pp_PreAssignButtonStyle = new GUIStyle(GuiStyles.ButtonStyle);
		_pp_PreAssignLabelStyle = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)3,
			fontStyle = (FontStyle)1
		};
	}

	private static void RefreshPlayerCache()
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		_pp_LastRefreshFrame = (uint)Time.frameCount;
		float unscaledTime = Time.unscaledTime;
		for (int i = 0; i < _pp_DataPool.Count; i++)
		{
			_pp_DataPool[i].Active = false;
		}
		_pp_ActiveList.Clear();
		_pp_CurrentIdsCache.Clear();
		int num = 0;
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current == (Object)null || (Object)(object)current.Data == (Object)null || current.Data.Disconnected)
			{
				continue;
			}
			_pp_CurrentIdsCache.Add(current.PlayerId);
			CachedPlayerData cachedPlayerData;
			if (num < _pp_DataPool.Count)
			{
				cachedPlayerData = _pp_DataPool[num];
			}
			else
			{
				cachedPlayerData = new CachedPlayerData();
				_pp_DataPool.Add(cachedPlayerData);
			}
			num++;
			cachedPlayerData.Active = true;
			cachedPlayerData.Player = current;
			cachedPlayerData.Disconnected = false;
			cachedPlayerData.IsDead = current.Data.IsDead;
			CachedPlayerData cachedPlayerData2 = cachedPlayerData;
			RoleBehaviour role = current.Data.Role;
			cachedPlayerData2.IsImpostor = role != null && role.IsImpostor;
			cachedPlayerData.Name = current.Data.PlayerName;
			Color val = Palette.CrewmateBlue;
			var defaultOutfit = current.Data.DefaultOutfit;
			int num2 = ((defaultOutfit != null) ? defaultOutfit.ColorId : (-1));
			if (num2 >= 0 && num2 < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
			{
				val = InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[num2]);
			}
			if (cachedPlayerData.IsDead)
			{
				if (_pp_DeadColorCache.TryGetValue(current.PlayerId, out var value))
				{
					val = value;
				}
				else
				{
					val = Color.Lerp(val, Color.black, 0.4f);
					_pp_DeadColorCache[current.PlayerId] = val;
				}
			}
			cachedPlayerData.DisplayColor = val;
			if (cachedPlayerData.IsDead)
			{
				cachedPlayerData.SortPriority = (cachedPlayerData.IsImpostor ? 3 : 4);
			}
			else
			{
				cachedPlayerData.SortPriority = (cachedPlayerData.IsImpostor ? 1 : 2);
			}
			_pp_ActiveList.Add(cachedPlayerData);
		}
		_pp_ActiveList.Sort(_pp_SortComparison);
		if (unscaledTime - _pp_LastCleanupTime >= 2f)
		{
			_pp_TriedFix.Clear();
			_pp_LastCleanupTime = unscaledTime;
		}
	}

	private static void DrawPlayerEntryNative(CachedPlayerData data, bool amHost, bool isLobby)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		PlayerControl player = data.Player;
		if (amHost && !_pp_TriedFix.Contains(player.PlayerId) && (Object)(object)player.Data.Role == (Object)null)
		{
			_pp_TriedFix.Add(player.PlayerId);
			ImpostorForcer.UpdateRoleLocally(player, player.Data.RoleType);
		}
		GUILayout.BeginVertical(GuiStyles.ContainerStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		Color color = GUI.color;
		if (data.IsImpostor)
		{
			GUI.color = new Color(1f, 0f, 0f, 0.3f);
		}
		else
		{
			GUI.color = data.DisplayColor;
		}
		GUILayout.Box(GUIContent.none, _pp_ColorBoxStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedWidth(22f),
			GuiStyles.CachedHeight(22f)
		});
		GUI.color = color;
		GUILayout.Label(data.Name, data.IsImpostor ? _pp_ImpostorNameStyle : _pp_PlayerNameStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		if (data.IsDead)
		{
			GUILayout.Label("[DEAD]", _pp_StatusStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}
		GUILayout.FlexibleSpace();
		if (!data.IsDead && !data.Disconnected)
		{
			GUI.backgroundColor = GuiStyles.Theme.Visor;
			if (GUILayout.Button("TP", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GuiStyles.CachedWidth(45f),
				GuiStyles.CachedHeight(26f)
			}))
			{
				PlayerControl.LocalPlayer.NetTransform.SnapTo(InteropFix.Cast(((Component)player).transform.position));
			}
		}
		if (amHost && (Object)(object)ShipStatus.Instance != (Object)null && !data.IsDead)
		{
			GUI.backgroundColor = GuiStyles.Theme.Error;
			if (GUILayout.Button("Kill", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GuiStyles.CachedWidth(50f),
				GuiStyles.CachedHeight(26f)
			}))
			{
				GameCheats.HostForceKillPlayer(player);
			}
		}
		if (amHost && (Object)(object)player != (Object)(object)PlayerControl.LocalPlayer)
		{
			GUI.backgroundColor = GuiStyles.Theme.Error;
			if (GUILayout.Button("Kick", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GuiStyles.CachedWidth(50f),
				GuiStyles.CachedHeight(26f)
			}))
			{
				GameCheats.KickPlayer(player);
			}
			if (isLobby)
			{
				GUI.backgroundColor = GuiStyles.Theme.Accent;
				if (GUILayout.Button("Ban", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GuiStyles.CachedWidth(45f),
					GuiStyles.CachedHeight(26f)
				}))
				{
					GameCheats.KickPlayer(player, ban: true);
				}
			}
		}
		GUI.backgroundColor = Color.white;
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private static void ExecuteGlowBorder()
	{
	}

	private static void InitV5Styles()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		if (_v5CardStyle == null)
		{
			_v5CardStyle = new GUIStyle(GuiStyles.ContainerStyle);
			_v5CardStyle.margin = CreateRectOffset(2, 2, 2, 2);
			_v5CardStyle.padding = CreateRectOffset(6, 6, 6, 6);
			_v5ColorBoxStyle = new GUIStyle(GUI.skin.box);
			_v5ColorBoxStyle.fixedWidth = 24f;
			_v5ColorBoxStyle.fixedHeight = 24f;
			_v5ColorBoxStyle.margin = CreateRectOffset(0, 8, 0, 0);
			_v5ColorBoxStyle.normal.background = Texture2D.whiteTexture;
			_v5CardNameStyle = new GUIStyle(GUI.skin.label);
			_v5CardNameStyle.fontStyle = (FontStyle)1;
			_v5CardNameStyle.fontSize = 14;
			_v5CardNameStyle.alignment = (TextAnchor)3;
			_v5CardNameStyle.normal.textColor = Color.white;
			_v5CardStatusStyle = new GUIStyle(GUI.skin.label);
			_v5CardStatusStyle.fontSize = 11;
			_v5CardStatusStyle.alignment = (TextAnchor)3;
			_v5CardStatusStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
			_v5ActionBtnStyle = new GUIStyle(GuiStyles.ButtonStyle);
			_v5ActionBtnStyle.fontSize = 12;
			_v5ActionBtnStyle.padding = CreateRectOffset(4, 4, 2, 2);
			_v5ActionBtnStyle.margin = CreateRectOffset(2, 2, 0, 0);
		}
	}

	private static RectOffset CreateRectOffset(int left, int right, int top, int bottom)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		return new RectOffset
		{
			left = left,
			right = right,
			top = top,
			bottom = bottom
		};
	}

	private static void ExecutePlayerCard(SecureStreamReader reader)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		reader.ReadByte();
		string text = reader.ReadString();
		float num = reader.ReadSingle();
		float num2 = reader.ReadSingle();
		float num3 = reader.ReadSingle();
		byte b = reader.ReadByte();
		byte b2 = reader.ReadByte();
		string text2 = reader.ReadString();
		InitV5Styles();
		Color val = default(Color);
		val._002Ector(num, num2, num3, 1f);
		Color backgroundColor = ((b == 1) ? new Color(1f, 0.1f, 0.1f, 0.15f) : new Color(0.2f, 0.2f, 0.25f, 0.8f));
		Color backgroundColor2 = GUI.backgroundColor;
		GUI.backgroundColor = backgroundColor;
		GUILayout.BeginVertical(_v5CardStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedWidth(_gridCellWidth),
			GuiStyles.CachedHeight(_gridCellHeight)
		});
		GUI.backgroundColor = backgroundColor2;
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		Color color = GUI.color;
		GUI.color = val;
		GUILayout.Box(GUIContent.none, _v5ColorBoxStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUI.color = color;
		GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		Color contentColor = GUI.contentColor;
		GUI.contentColor = (Color)((b2 == 1) ? new Color(0.5f, 0.5f, 0.5f) : Color.Lerp(Color.white, val, 0.7f));
		GUILayout.Label(text, _v5CardNameStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		if (b == 1)
		{
			GUI.contentColor = new Color(1f, 0.2f, 0.2f);
			GUILayout.Label(" (Impostor)", _v5CardNameStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}
		GUI.contentColor = contentColor;
		GUILayout.EndHorizontal();
		GUI.contentColor = (Color)((b2 == 1) ? new Color(0.6f, 0.6f, 0.6f) : ((b == 1) ? Color.white : Color.Lerp(Color.white, val, 0.3f)));
		GUILayout.Label(text2, _v5CardStatusStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUI.contentColor = contentColor;
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.Space(_gridSpacing);
	}

	private static void ExecutePlayerCardMini(SecureStreamReader reader)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		reader.ReadByte();
		string text = reader.ReadString();
		float num = reader.ReadSingle();
		float num2 = reader.ReadSingle();
		float num3 = reader.ReadSingle();
		InitV5Styles();
		Color color = default(Color);
		color._002Ector(num, num2, num3, 1f);
		GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[1] { GuiStyles.CachedHeight(28f) });
		Color color2 = GUI.color;
		GUI.color = color;
		GUILayout.Box(GUIContent.none, _v5ColorBoxStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedWidth(20f),
			GuiStyles.CachedHeight(20f)
		});
		GUI.color = color2;
		GUILayout.Label(text, _v5CardNameStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	private static void ExecuteActionButton(SecureStreamReader reader)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		string text = reader.ReadString();
		string key = reader.ReadString();
		float num = reader.ReadSingle();
		float num2 = reader.ReadSingle();
		float num3 = reader.ReadSingle();
		float w = reader.ReadSingle();
		float h = reader.ReadSingle();
		InitV5Styles();
		Color backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(num, num2, num3, 1f);
		if (GUILayout.Button(text, _v5ActionBtnStyle ?? GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GuiStyles.CachedWidth(w),
			GuiStyles.CachedHeight(h)
		}) && _actionRegistry != null && _actionRegistry.ContainsKey(key))
		{
			_actionRegistry[key]?.Invoke(GetRenderContext());
		}
		GUI.backgroundColor = backgroundColor;
	}

	private static byte[] HexToBytes(string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return null;
		}
		int num = hex.Length / 2;
		byte[] array = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
		}
		return array;
	}

	private static bool ValidateBytecodeHMAC(byte[] bytecode, int payloadStart, byte[] expectedHmac)
	{
		if (expectedHmac == null || expectedHmac.Length != 32)
		{
			return false;
		}
		if (string.IsNullOrEmpty(ServerData.SessionDecryptKey))
		{
			return false;
		}
		try
		{
			if (_hmacKeyBuffer == null || _hmacKeyBuffer.Length != 32)
			{
				_hmacKeyBuffer = HexToBytes(ServerData.SessionDecryptKey);
			}
			if (_hmacKeyBuffer == null)
			{
				return false;
			}
			using HMACSHA256 hMACSHA = new HMACSHA256(_hmacKeyBuffer);
			byte[] array = hMACSHA.ComputeHash(bytecode, payloadStart, bytecode.Length - payloadStart);
			if (array.Length != expectedHmac.Length)
			{
				return false;
			}
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				num |= array[i] ^ expectedHmac[i];
			}
			return num == 0;
		}
		catch
		{
			return false;
		}
	}

	internal static void InvalidateHMACCache()
	{
		_hmacKeyBuffer = null;
	}

	private static void ExecuteStream(SecureStreamReader reader, long limitPos = -1L)
	{
		//IL_0c49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_079e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba7: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		try
		{
			int num5 = 0;
			int num6 = 10000;
			while (!reader.EndOfStream && (limitPos == -1 || reader.Position < limitPos) && ++num5 <= num6)
			{
				switch (reader.ReadByte())
				{
				case 1:
					ExecuteWinBegin(reader);
					break;
				case 2:
					ExecuteWinEnd();
					break;
				case 3:
					reader.ReadString();
					break;
				case 16:
				{
					byte b2 = reader.ReadByte();
					if (!_isWindowMinimized)
					{
						if (b2 == 0)
						{
							GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						}
						else
						{
							GUILayout.BeginHorizontal(GhostStyleMap.Get(b2), System.Array.Empty<GUILayoutOption>());
						}
						num++;
					}
					break;
				}
				case 17:
					if (!_isWindowMinimized)
					{
						GUILayout.EndHorizontal();
						num--;
					}
					break;
				case 18:
				{
					byte b = reader.ReadByte();
					if (!_isWindowMinimized)
					{
						if (b == 0)
						{
							GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
						}
						else
						{
							GUILayout.BeginVertical(GhostStyleMap.Get(b), System.Array.Empty<GUILayoutOption>());
						}
						num2++;
					}
					break;
				}
				case 19:
					if (!_isWindowMinimized)
					{
						GUILayout.EndVertical();
						num2--;
					}
					break;
				case 20:
				{
					string text5 = reader.ReadString();
					bool flag5 = reader.ReadByte() == 1;
					bool flag6 = reader.ReadByte() == 1;
					if (!_isWindowMinimized)
					{
						Vector2 pos = GUILayout.BeginScrollView(_currentWindow?.GetScroll(text5) ?? Vector2.zero, flag5, flag6, System.Array.Empty<GUILayoutOption>());
						_currentWindow?.SetScroll(text5, pos);
						_scrollStack.Push(text5);
						num3++;
					}
					break;
				}
				case 21:
					if (!_isWindowMinimized)
					{
						GUILayout.EndScrollView();
						if (_scrollStack.Count > 0)
						{
							_scrollStack.Pop();
						}
						num3--;
					}
					break;
				case 22:
				{
					float num9 = reader.ReadSingle();
					float num10 = reader.ReadSingle();
					float num11 = reader.ReadSingle();
					float num12 = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						GUILayout.BeginArea(new Rect(num9, num10, num11, num12));
						num4++;
					}
					break;
				}
				case 23:
					if (!_isWindowMinimized)
					{
						GUILayout.EndArea();
						num4--;
					}
					break;
				case 24:
					if (!_isWindowMinimized)
					{
						ExecuteSectionBegin(reader);
						break;
					}
					reader.ReadString();
					reader.ReadString();
					break;
				case 25:
					if (!_isWindowMinimized)
					{
						ExecuteSectionEnd();
					}
					break;
				case 32:
				{
					string text3 = ApplyContext(reader.ReadString());
					byte id3 = reader.ReadByte();
					if (!_isWindowMinimized)
					{
						GUILayout.Label(text3, GhostStyleMap.Get(id3), (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					break;
				}
				case 33:
				{
					string text2 = ApplyContext(reader.ReadString());
					string actionId = ApplyContextAction(reader.ReadString());
					if (!_isWindowMinimized && GUILayout.Button(text2, GuiStyles.ButtonStyle, System.Array.Empty<GUILayoutOption>()))
					{
						ActionPermitSystem.RequestExecution(actionId, "", GetRenderContext());
					}
					break;
				}
				case 34:
				{
					byte id4 = reader.ReadByte();
					if (_isWindowMinimized)
					{
						break;
					}
					Color color = GUI.color;
					if (_currentContext != null)
					{
						if (_currentContext.IsImpostor)
						{
							GUI.color = new Color(1f, 0f, 0f, 0.3f);
						}
						else
						{
							GUI.color = _currentContext.DisplayColor;
						}
					}
					GUILayout.Box(GUIContent.none, GhostStyleMap.Get(id4), (GUILayoutOption[])(object)new GUILayoutOption[2]
					{
						GuiStyles.CachedWidth(22f),
						GuiStyles.CachedHeight(22f)
					});
					GUI.color = color;
					break;
				}
				case 35:
				{
					float num31 = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						GUILayout.Space(num31);
					}
					break;
				}
				case 36:
					if (!_isWindowMinimized)
					{
						GUILayout.FlexibleSpace();
					}
					break;
				case 37:
					if (!_isWindowMinimized)
					{
						GuiStyles.DrawSeparator();
					}
					break;
				case 38:
				{
					string actionId2 = reader.ReadString();
					string text4 = ApplyContext(reader.ReadString());
					if (!_isWindowMinimized)
					{
						bool flag4 = text4.Contains("✓");
						if (GUILayout.Toggle(flag4, text4, GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null) != flag4)
						{
							ActionPermitSystem.RequestExecution(actionId2, "", GetRenderContext());
						}
					}
					break;
				}
				case 39:
				{
					string text = reader.ReadString();
					string value = reader.ReadString();
					float num13 = reader.ReadSingle();
					float num14 = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						ServerData.RegisterControlId(text);
						string id = ServerData.DeobfuscateActionId(text);
						float num15 = ServerData.GetSliderValue(id);
						if (num15 < num13 || num15 > num14)
						{
							num15 = (num13 + num14) / 2f;
							ServerData.SetSliderValueInternal(id, num15);
						}
						GUILayout.Label($"{value}: {num15:F1}", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						float num16 = GUILayout.HorizontalSlider(num15, num13, num14, GuiStyles.SliderStyle, GuiStyles.SliderThumbStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
						{
							GUILayout.ExpandWidth(true),
							GUILayout.Height(18f)
						});
						if (Mathf.Abs(num16 - num15) > 0.001f)
						{
							ServerData.SetSliderValueInternal(id, num16);
							ActionPermitSystem.RequestExecution(text, "", GetRenderContext());
						}
					}
					break;
				}
				case 48:
				{
					float w2 = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						GUILayout.BeginVertical(GuiStyles.SidebarStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
						{
							GuiStyles.CachedWidth(w2),
							GUILayout.ExpandHeight(true)
						});
						num2++;
					}
					break;
				}
				case 49:
					if (!_isWindowMinimized)
					{
						GUILayout.EndVertical();
						num2--;
						GUILayout.Box(GUIContent.none, (GUILayoutOption[])(object)new GUILayoutOption[2]
						{
							GuiStyles.CachedWidth(1f),
							GUILayout.ExpandHeight(true)
						});
					}
					break;
				case 50:
					if (!_isWindowMinimized)
					{
						ExecuteTabButton(reader);
						break;
					}
					reader.ReadString();
					reader.ReadString();
					reader.ReadString();
					break;
				case 51:
					if (!_isWindowMinimized)
					{
						Vector2 contentScroll = GUILayout.BeginScrollView(_currentWindow?.ContentScroll ?? Vector2.zero, false, false, (GUILayoutOption[])(object)new GUILayoutOption[2]
						{
							GUILayout.ExpandWidth(true),
							GUILayout.ExpandHeight(true)
						});
						if (_currentWindow != null)
						{
							_currentWindow.ContentScroll = contentScroll;
						}
						num3++;
						GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
						num2++;
					}
					break;
				case 52:
					if (!_isWindowMinimized)
					{
						GUILayout.EndVertical();
						num2--;
						GUILayout.EndScrollView();
						num3--;
					}
					break;
				case 53:
					if (!_isWindowMinimized)
					{
						ExecuteLicenseBadge();
					}
					break;
				case 54:
					if (!_isWindowMinimized)
					{
						GUILayout.Label("<size=20><b>MODMENU</b><color=#FF1A1A>CREW</color></size>", GuiStyles.SidebarHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					break;
				case 55:
					if (!_isWindowMinimized)
					{
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						GUILayout.Space(15f);
						GUILayout.Label("<size=10><color=#777777>NAVIGATION</color></size>", GuiStyles.SidebarFooterStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						GUILayout.EndHorizontal();
						GUILayout.Box(GUIContent.none, GuiStyles.SeparatorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
						GUILayout.Space(5f);
					}
					break;
				case 56:
					if (!_isWindowMinimized)
					{
						string value2 = "Offline";
						try
						{
							value2 = (((Object)(object)AmongUsClient.Instance != (Object)null) ? "\ud83d\udfe2 Online" : "\ud83d\udd34 Offline");
						}
						catch
						{
						}
						GUILayout.Label($"<size=10>v{"6.0.8"} | {value2}</size>", GuiStyles.SidebarFooterStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
					}
					break;
				case 57:
					if (!_isWindowMinimized)
					{
						ExecutePlayerList();
					}
					break;
				case 160:
				{
					byte gridColumns = reader.ReadByte();
					float gridCellWidth = reader.ReadSingle();
					float gridCellHeight = reader.ReadSingle();
					float gridSpacing = reader.ReadSingle();
					_gridColumns = gridColumns;
					_gridCellWidth = gridCellWidth;
					_gridCellHeight = gridCellHeight;
					_gridSpacing = gridSpacing;
					if (!_isWindowMinimized)
					{
						GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
						num2++;
					}
					break;
				}
				case 161:
					if (!_isWindowMinimized)
					{
						GUILayout.EndVertical();
						num2--;
					}
					break;
				case 167:
					if (!_isWindowMinimized)
					{
						GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
						num++;
					}
					break;
				case 168:
					if (!_isWindowMinimized)
					{
						GUILayout.EndHorizontal();
						num--;
						GUILayout.Space(_gridSpacing);
					}
					break;
				case 162:
					if (!_isWindowMinimized)
					{
						ExecutePlayerCard(reader);
						break;
					}
					reader.ReadByte();
					reader.ReadString();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadByte();
					reader.ReadByte();
					reader.ReadString();
					break;
				case 166:
					if (!_isWindowMinimized)
					{
						ExecutePlayerCardMini(reader);
						break;
					}
					reader.ReadByte();
					reader.ReadString();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadSingle();
					break;
				case 163:
				{
					float w = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[1] { GuiStyles.CachedWidth(w) });
						num++;
					}
					break;
				}
				case 164:
					if (!_isWindowMinimized)
					{
						GUILayout.EndHorizontal();
						num--;
					}
					break;
				case 165:
					if (!_isWindowMinimized)
					{
						ExecuteActionButton(reader);
						break;
					}
					reader.ReadString();
					reader.ReadString();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadSingle();
					reader.ReadSingle();
					break;
				case 169:
				{
					string text6 = reader.ReadString();
					string actionId3 = reader.ReadString();
					string permitToken = reader.ReadString();
					float num28 = reader.ReadSingle();
					float num29 = reader.ReadSingle();
					float num30 = reader.ReadSingle();
					float w3 = reader.ReadSingle();
					float h = reader.ReadSingle();
					if (!_isWindowMinimized)
					{
						InitV5Styles();
						Color backgroundColor = GUI.backgroundColor;
						GUI.backgroundColor = new Color(num28, num29, num30, 1f);
						if (GUILayout.Button(text6, _v5ActionBtnStyle ?? GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
						{
							GuiStyles.CachedWidth(w3),
							GuiStyles.CachedHeight(h)
						}))
						{
							ActionPermitSystem.RequestExecution(actionId3, permitToken, GetRenderContext());
						}
						GUI.backgroundColor = backgroundColor;
					}
					break;
				}
				case 58:
				{
					ushort num26 = reader.ReadUInt16();
					long position = reader.Position;
					long num27 = position + num26;
					if (Time.frameCount - _pp_LastRefreshFrame >= 30)
					{
						RefreshPlayerCache();
					}
					CachedPlayerData currentContext = _currentContext;
					for (int i = 0; i < _pp_ActiveList.Count; i++)
					{
						CachedPlayerData cachedPlayerData = _pp_ActiveList[i];
						if (cachedPlayerData.Active)
						{
							_currentContext = cachedPlayerData;
							reader.Position = position;
							ExecuteStream(reader, num27);
						}
					}
					_currentContext = currentContext;
					reader.Position = num27;
					break;
				}
				case 64:
				{
					_colorStack.Push(GUI.color);
					float num22 = reader.ReadSingle();
					float num23 = reader.ReadSingle();
					float num24 = reader.ReadSingle();
					float num25 = reader.ReadSingle();
					GUI.color = new Color(num22, num23, num24, num25);
					break;
				}
				case 65:
					if (_colorStack.Count > 0)
					{
						GUI.color = _colorStack.Pop();
					}
					break;
				case 80:
					ExecuteHeaderDraw();
					break;
				case 81:
					ExecuteResizeHandle();
					break;
				case 82:
					ExecuteGlowBorder();
					break;
				case 96:
				{
					string key2 = ServerData.DeobfuscateActionId(ApplyContextAction(reader.ReadString()));
					if (ActionPermitSystem.ActionRegistry != null && ActionPermitSystem.ActionRegistry.TryGetValue(key2, out var value4))
					{
						try
						{
							value4(GetRenderContext());
						}
						catch
						{
						}
					}
					break;
				}
				case 97:
				{
					byte b3 = reader.ReadByte();
					string obfuscatedId = ApplyContextAction(reader.ReadString());
					if (_currentWindow == null || _currentWindow.SelectedTab != b3)
					{
						break;
					}
					string key = ServerData.DeobfuscateActionId(obfuscatedId);
					if (ActionPermitSystem.ActionRegistry != null && ActionPermitSystem.ActionRegistry.TryGetValue(key, out var value3))
					{
						try
						{
							value3(GetRenderContext());
						}
						catch
						{
						}
					}
					break;
				}
				case 112:
				{
					ushort num21 = reader.ReadUInt16();
					if (!ModKeyValidator.IsPremium || !ModKeyValidator.V())
					{
						reader.Skip(num21);
					}
					break;
				}
				case 113:
				{
					string item = reader.ReadString();
					ushort num19 = reader.ReadUInt16();
					int num20 = _tabIds.IndexOf(item);
					if (_currentWindow == null || _currentWindow.SelectedTab != num20)
					{
						reader.Skip(num19);
					}
					break;
				}
				case 114:
				{
					ushort num18 = reader.ReadUInt16();
					bool flag3 = false;
					try
					{
						flag3 = (Object)(object)AmongUsClient.Instance != (Object)null && ((InnerNetClient)AmongUsClient.Instance).IsGameStarted;
					}
					catch
					{
					}
					if (!flag3)
					{
						reader.Skip(num18);
					}
					break;
				}
				case 115:
				{
					string id2 = reader.ReadString();
					ushort num17 = reader.ReadUInt16();
					if (_currentWindow == null || !_currentWindow.IsSectionExpanded(id2))
					{
						reader.Skip(num17);
					}
					break;
				}
				case 240:
				{
					long num7 = reader.ReadInt64();
					bool flag = false;
					if (ServerData.PremiumFeatures != null && ServerData.PremiumFeatures.Count > 0)
					{
						if (ServerData.CalculateIntegrity() != num7)
						{
							flag = true;
						}
					}
					else if (num7 != 3735928559u && ServerData.CalculateIntegrity() != num7)
					{
						flag = true;
					}
					if (flag)
					{
						_integrityMismatchFrameCount++;
						if (_integrityFirstMismatchTime < 0f)
						{
							_integrityFirstMismatchTime = Time.realtimeSinceStartup;
						}
						bool num8 = _integrityMismatchFrameCount >= 30;
						bool flag2 = Time.realtimeSinceStartup - _integrityFirstMismatchTime > 2f;
						if (num8 || flag2)
						{
							Debug.LogError(InteropFix.Cast("x"));
							ServerData.TriggerSilentDenial();
							return;
						}
					}
					else
					{
						_integrityMismatchFrameCount = 0;
						_integrityFirstMismatchTime = -1f;
					}
					break;
				}
				case 241:
					reader.ReadInt64();
					break;
				case 254:
					Debug.LogError(InteropFix.Cast("x"));
					ServerData.TriggerSilentDenial();
					return;
				case byte.MaxValue:
					return;
				default:
					Debug.LogError(InteropFix.Cast("x"));
					ServerData.TriggerSilentDenial();
					return;
				}
			}
		}
		catch (Exception ex)
		{
			if (ex.GetType().Name.Contains("ExitGUI"))
			{
				throw;
			}
			Debug.LogWarning(InteropFix.Cast(ex.Message));
		}
		finally
		{
			try
			{
				while (num3 > 0)
				{
					GUILayout.EndScrollView();
					num3--;
				}
				while (num2 > 0)
				{
					GUILayout.EndVertical();
					num2--;
				}
				while (num > 0)
				{
					GUILayout.EndHorizontal();
					num--;
				}
				while (num4 > 0)
				{
					GUILayout.EndArea();
					num4--;
				}
			}
			catch
			{
			}
		}
	}
}








