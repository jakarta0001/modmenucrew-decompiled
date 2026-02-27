using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ModMenuCrew.Monitoring;
using ModMenuCrew.Networking;

namespace ModMenuCrew;

public static class IntegrityGuard
{
	private static byte[] _derivedKey;

	private static int _isIntactXor = 0;

	private const int INTACT_SENTINEL = 1513922175;

	private static long _verifyTagXor = 0L;

	private const long VERIFY_TAG_SENTINEL = 7876562145509142585L;

	private static bool _initialized = false;

	private static readonly byte[] _expectedHash = new byte[32]
	{
		121, 25, 100, 91, 250, 5, 169, 70, 74, 203,
		146, 44, 86, 17, 167, 22, 238, 137, 110, 248,
		212, 182, 160, 76, 231, 246, 104, 38, 194, 163,
		11, 107
	};

	public static bool IsIntact
	{
		get
		{
			return true;
		}
	}

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		try
		{
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			MethodBase[] array = new MethodBase[7]
			{
				typeof(ActionPermitSystem).GetMethod("OnServerApproval", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(ActionPermitSystem).GetMethod("RequestExecution", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(CertificatePinner).GetMethod("ValidateServerCertificate", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(GhostUI).GetMethod("Execute", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(GhostUI).GetMethod("ValidateTimestampAntiReplay", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(ModMenuCrewPlugin).GetMethod("CheckForSelfPatches", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				typeof(ModKeyValidator).GetProperty("IsPremium", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true)
			};
			foreach (MethodBase methodBase in array)
			{
				if (methodBase == null)
				{
					continue;
				}
				MethodBody methodBody = methodBase.GetMethodBody();
				if (methodBody != null)
				{
					byte[] iLAsByteArray = methodBody.GetILAsByteArray();
					if (iLAsByteArray != null)
					{
						incrementalHash.AppendData(iLAsByteArray);
					}
				}
			}
			byte[] hashAndReset = incrementalHash.GetHashAndReset();
			_derivedKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, hashAndReset, 32, Encoding.UTF8.GetBytes("MMC_Integrity_2026"), Encoding.UTF8.GetBytes("ActionKey_v2"));
			byte[] array2 = SHA256.HashData(_derivedKey);
			bool flag = true;
			for (int j = 0; j < _expectedHash.Length; j++)
			{
				if (_expectedHash[j] != 0)
				{
					flag = false;
					break;
				}
			}
			bool num = !flag && CryptographicOperations.FixedTimeEquals(array2, _expectedHash);
			_isIntactXor = (num ? 1 : 0) ^ 0x5A3C9E7F;
			if (num && _derivedKey != null && _derivedKey.Length >= 8)
			{
				_verifyTagXor = BitConverter.ToInt64(_derivedKey, 0) ^ BitConverter.ToInt64(_derivedKey, _derivedKey.Length - 8) ^ 0x6D4F2B8A1E5C7039L;
			}
		}
		catch
		{
			_isIntactXor = 0;
			_verifyTagXor = 0L;
		}
	}
}

