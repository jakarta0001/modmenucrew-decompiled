using System;
using System.Security.Cryptography;
using System.Text;

namespace ModMenuCrew;

internal static class SystemConfig
{
	private static readonly string _configHash = "2e5d8a1c4f7e0d3a6b9c2e5f8a1d4c7e0b3f6a9d2c5e8b1a4d7c0f3e6a9b2c5";

	internal static bool _isEnabled { get; private set; } = false;

	internal static void Initialize(string config)
	{
		if (string.IsNullOrEmpty(config))
		{
			return;
		}
		using SHA256 sHA = SHA256.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(config);
		if (BitConverter.ToString(sHA.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() == _configHash)
		{
			_isEnabled = true;
		}
	}
}
