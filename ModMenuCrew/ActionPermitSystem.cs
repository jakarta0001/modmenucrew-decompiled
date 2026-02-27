using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Il2CppSystem;
using ModMenuCrew.Monitoring;
using ModMenuCrew.Networking;
using ModMenuCrew.UI.Menus;
using UnityEngine;

namespace ModMenuCrew;

public static class ActionPermitSystem
{
	private class PendingAction
	{
		public string ActionId;

		public string PermitToken;

		public long FrameToken;

		public float RequestTime;

		public string ClientChallenge;
	}

	private static readonly ConcurrentDictionary<string, PendingAction> _pending = new ConcurrentDictionary<string, PendingAction>();

	private static readonly Dictionary<string, System.Action<long>> _unifiedRegistry = new Dictionary<string, System.Action<long>>();

	private static readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

	private static long _sequenceNumber = 0L;

	private const int MAX_PENDING = 50;

	private const float PERMIT_TIMEOUT = 8f;

	private static readonly System.Collections.Generic.HashSet<string> _localOnlyActions = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
	{
		"cheat_section_quick", "cheat_section_movement", "cheat_section_esp", "cheat_section_roles", "cheat_protect_expand", "toggle_filters", "filter_impostors", "filter_crewmates", "role_close", "preassign_close",
		"preassign_clear_all", "ro_dd", "bt_dd", "ro_toggle", "bp_clear", "h_col_p", "h_col_n", "h_col_apply", "h_start", "h_rnd_col",
		"h_map_rm", "h_map_lobby", "cheat_reveal_sus"
	};

	private const int EXPECTED_LOCAL_ACTIONS_COUNT = 23;

	private const int RSA_SIGNATURE_HEX_LENGTH = 512;

	private static readonly (Type type, string method)[] ATTESTATION_METHODS = new(Type, string)[7]
	{
		(typeof(ActionPermitSystem), "OnServerApproval"),
		(typeof(ActionPermitSystem), "RequestExecution"),
		(typeof(CertificatePinner), "ValidateServerCertificate"),
		(typeof(GhostUI), "Execute"),
		(typeof(GhostUI), "VerifyRsaSignature"),
		(typeof(GhostUI), "VerifyBytecodeSignatureV5"),
		(null, "RSA_MODULUS_HASH")
	};

	private static string _attestationSeed = null;

	private static int[] _attestationIndices = null;

	private static string _cachedAttestationProof = null;

	private static string _lastProofSeed = null;

	private const BindingFlags ALL_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	internal static Dictionary<string, System.Action<long>> ActionRegistry => _unifiedRegistry;

	internal static void UpdateAttestation(string seed, int[] methodIndices)
	{
		_attestationSeed = seed;
		_attestationIndices = methodIndices;
		_cachedAttestationProof = null;
		_lastProofSeed = null;
	}

	internal static string ComputeAttestationProof()
	{
		if (string.IsNullOrEmpty(_attestationSeed) || _attestationIndices == null || _attestationIndices.Length == 0)
		{
			return "";
		}
		if (_lastProofSeed == _attestationSeed && _cachedAttestationProof != null)
		{
			return _cachedAttestationProof;
		}
		try
		{
			byte[] array = HexToBytes(_attestationSeed);
			if (array == null)
			{
				return "";
			}
			using MemoryStream memoryStream = new MemoryStream();
			using SHA256 sHA = SHA256.Create();
			int[] obj = (int[])_attestationIndices.Clone();
			System.Array.Sort(obj);
			int[] array2 = obj;
			foreach (int num in array2)
			{
				if (num < 0 || num >= ATTESTATION_METHODS.Length)
				{
					continue;
				}
				var (type, text) = ATTESTATION_METHODS[num];
				if (type == null && text == "RSA_MODULUS_HASH")
				{
					byte[] rsaModulusHash = GhostUI.GetRsaModulusHash();
					if (rsaModulusHash != null && rsaModulusHash.Length != 0)
					{
						memoryStream.Write(rsaModulusHash, 0, rsaModulusHash.Length);
					}
					continue;
				}
				MethodInfo method = type.GetMethod(text, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					continue;
				}
				MethodBody methodBody = method.GetMethodBody();
				if (methodBody != null)
				{
					byte[] iLAsByteArray = methodBody.GetILAsByteArray();
					if (iLAsByteArray != null && iLAsByteArray.Length != 0)
					{
						byte[] array3 = sHA.ComputeHash(iLAsByteArray);
						memoryStream.Write(array3, 0, array3.Length);
					}
				}
			}
			using HMACSHA256 hMACSHA = new HMACSHA256(array);
			_cachedAttestationProof = BitConverter.ToString(hMACSHA.ComputeHash(memoryStream.ToArray())).Replace("-", "").ToLowerInvariant();
			_lastProofSeed = _attestationSeed;
			return _cachedAttestationProof;
		}
		catch
		{
			return "";
		}
	}

	internal static void SetActionRegistry(Dictionary<string, System.Action<long>> registry)
	{
		if (registry == null)
		{
			return;
		}
		foreach (KeyValuePair<string, System.Action<long>> item in registry)
		{
			_unifiedRegistry[item.Key] = item.Value;
		}
	}

	internal static void ClearRegistry()
	{
		_unifiedRegistry.Clear();
		_pending.Clear();
		_attestationSeed = null;
		_attestationIndices = null;
		_cachedAttestationProof = null;
		_lastProofSeed = null;
	}

	private static bool IsLocalOnlyAction(string actionId)
	{
		if (string.IsNullOrEmpty(actionId))
		{
			return false;
		}
		if (_localOnlyActions.Contains(actionId))
		{
			return true;
		}
		if (actionId.StartsWith("role_open_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("preassign_open_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("preassign_clear_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("preassign_set_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("tp_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("kill_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("kick_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("ban_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("role_set_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("bp_add_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("bp_sub_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("ro_sel_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("bt_sel_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("cheat_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("sp_", System.StringComparison.Ordinal))
		{
			return true;
		}
		if (actionId.StartsWith("ev_", System.StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	internal static void RequestExecution(string actionId, string permitToken, long frameToken)
	{
		if (string.IsNullOrEmpty(actionId))
		{
			return;
		}
		if (_localOnlyActions.Count > 23)
		{
			ServerData.TriggerSilentDenial();
			return;
		}
		string text = ServerData.DeobfuscateActionId(actionId);
		if (IsLocalOnlyAction(text))
		{
			if (_unifiedRegistry.TryGetValue(text, out var value))
			{
				try
				{
					value(frameToken);
				}
				catch
				{
				}
			}
		}
		else if (_pending.Count < 50)
		{
			string text2 = $"{actionId}_{++_sequenceNumber}_{Time.frameCount}";
			string clientChallenge;
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				byte[] array = new byte[16];
				randomNumberGenerator.GetBytes(array);
				clientChallenge = BitConverter.ToString(array).Replace("-", "").ToLowerInvariant();
			}
			_pending[text2] = new PendingAction
			{
				ActionId = actionId,
				PermitToken = permitToken,
				FrameToken = frameToken,
				RequestTime = Time.realtimeSinceStartup,
				ClientChallenge = clientChallenge
			};
			RealtimeConnection.SendActionRequest(text2, actionId, permitToken, clientChallenge);
		}
	}

	internal static void OnServerApproval(string requestId, string serverNonce)
	{
		if (string.IsNullOrEmpty(requestId) || !_pending.TryRemove(requestId, out var value))
		{
			return;
		}
		if (string.IsNullOrEmpty(serverNonce) || serverNonce.Length != 512)
		{
			_mainThreadQueue.Enqueue(delegate
			{
				ServerData.TriggerSilentDenial();
			});
			return;
		}
		try
		{
			string s = requestId + "|" + value.ClientChallenge;
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			byte[] array = HexToBytes(serverNonce);
			if (array == null || array.Length != 256)
			{
				_mainThreadQueue.Enqueue(delegate
				{
					ServerData.TriggerSilentDenial();
				});
				return;
			}
			if (!GhostUI.VerifyRsaSignature(bytes, array))
			{
				_mainThreadQueue.Enqueue(delegate
				{
					ServerData.TriggerSilentDenial();
				});
				return;
			}
		}
		catch
		{
			_mainThreadQueue.Enqueue(delegate
			{
				ServerData.TriggerSilentDenial();
			});
			return;
		}
		string key = ServerData.DeobfuscateActionId(value.ActionId);
		if (!_unifiedRegistry.TryGetValue(key, out var value2))
		{
			return;
		}
		System.Action<long> capturedAction = value2;
		float capturedRequestTime = value.RequestTime;
		_mainThreadQueue.Enqueue(delegate
		{
			if (Time.realtimeSinceStartup - capturedRequestTime > 8f || (AntiTamper.IsTamperDetected && UnityEngine.Random.Range(0, 100) < 20))
			{
				return;
			}
			try
			{
				capturedAction(GhostUI.GetRenderContext());
			}
			catch (Exception ex)
			{
				Debug.LogError(InteropFix.Cast(ex.Message));
			}
			try
			{
				PlayerPickMenu.TriggerRealtimeUpdate(force: true);
			}
			catch
			{
			}
		});
	}

	internal static void EnqueueMainThread(Action action)
	{
		if (action != null)
		{
			_mainThreadQueue.Enqueue(action);
		}
	}

	internal static void CleanupExpired()
	{
		for (int i = 0; i < 50; i++)
		{
			if (!_mainThreadQueue.TryDequeue(out var result))
			{
				break;
			}
			try
			{
				result();
			}
			catch
			{
			}
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<string, PendingAction> item in _pending)
		{
			if (realtimeSinceStartup - item.Value.RequestTime > 8f)
			{
				_pending.TryRemove(item.Key, out var _);
			}
		}
	}

	private static byte[] HexToBytes(string hex)
	{
		if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
		{
			return null;
		}
		try
		{
			byte[] array = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2)
			{
				array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return array;
		}
		catch
		{
			return null;
		}
	}
}








