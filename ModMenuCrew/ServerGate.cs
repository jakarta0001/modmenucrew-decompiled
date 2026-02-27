using System;
using System.Runtime.CompilerServices;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ModMenuCrew;

internal static class ServerGate
{
	private static string _renderKey;

	private static long _renderExpiresXor;

	private const long EXPIRES_SENTINEL = 7800105011364196425L;

	private static long _renderNonce;

	private static int _consecutiveFailures;

	private static int _logThrottle;

	private static long RenderExpires => _renderExpiresXor ^ 0x6C3F8A2E5D1B7049L;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool CanRender()
	{
		return true;
	}

	internal static void UpdateRenderPermission(string renderKey, long expires, long nonce)
	{
		if (!string.IsNullOrEmpty(renderKey) && renderKey.Length == 64 && expires > 0 && nonce > 0)
		{
			_renderKey = renderKey;
			_renderExpiresXor = expires ^ 0x6C3F8A2E5D1B7049L;
			_renderNonce = nonce;
			_consecutiveFailures = 0;
		}
	}

	internal static void Revoke()
	{
		_renderKey = null;
		_renderExpiresXor = 0L;
		_renderNonce = 0L;
		ServerData.SetLoaded(loaded: false);
	}

	internal static bool IsNearExpiration()
	{
		if (string.IsNullOrEmpty(_renderKey))
		{
			return true;
		}
		return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ModKeyValidator.ServerTimeOffsetMs > RenderExpires - 10000;
	}

	internal static void RecordFailure()
	{
		_consecutiveFailures++;
		if (_consecutiveFailures >= 3)
		{
			Revoke();
		}
	}

	internal static int GetRemainingSeconds()
	{
		if (string.IsNullOrEmpty(_renderKey))
		{
			return 0;
		}
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ModKeyValidator.ServerTimeOffsetMs;
		long num2 = RenderExpires - num;
		if (num2 <= 0)
		{
			return 0;
		}
		return (int)(num2 / 1000);
	}
}

