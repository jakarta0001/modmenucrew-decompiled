using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModMenuCrew;

internal static class DataProcessor
{
	private static readonly string _hash = "4c1a43b2d5f6e7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9";

	internal static bool ProcessData(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return false;
		}
		if (data.Length != 19)
		{
			return false;
		}
		int num = 0;
		foreach (char c in data)
		{
			num += c;
		}
		if (num % 7 != 0)
		{
			return false;
		}
		return VerifyData(data);
	}

	private static bool VerifyData(string data)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(data + "random_salt");
		return BitConverter.ToString(sHA.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() == _hash;
	}

	internal static async Task<bool> PingServer()
	{
		await Task.Delay(1500);
		return false;
	}
}
