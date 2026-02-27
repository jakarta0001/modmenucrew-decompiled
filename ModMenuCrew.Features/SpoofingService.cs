using System.Runtime.CompilerServices;
using AmongUs.Data;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class SpoofingService
{
	private static uint _originalLevel = 0u;

	private static bool _levelCached = false;

	private static string _originalFriendCode = "";

	private static bool _friendCodeCached = false;

	private static string _cachedPlatformName = null;

	private static Platforms _cachedPlatformType = (Platforms)0;

	private static readonly System.Random _rng = new System.Random();

	private static bool ShouldPersistProfileChanges()
	{
		return !(CheatConfig.OfflineLocalMode?.Value ?? false);
	}

	internal static bool EnableLevelSpoof { get; set; } = false;

	internal static bool EnablePlatformSpoof { get; set; } = false;

	internal static bool EnableFriendCodeSpoof { get; set; } = false;

	internal static uint SpoofedLevel { get; set; } = 100u;

	internal static uint CustomLevel { get; set; } = 0u;

	internal static Platforms SpoofedPlatform { get; set; } = (Platforms)2;

	internal static string SpoofedPlatformName { get; set; } = "";

	internal static string SpoofedFriendCode { get; set; } = "";

	public static uint GetEffectiveLevel()
	{
		if (CustomLevel == 0)
		{
			return SpoofedLevel;
		}
		return CustomLevel;
	}

	internal static void SetCustomLevel(uint level)
	{
		if (level != 0 && level <= 999)
		{
			CustomLevel = level;
			ApplyLevelSpoof();
		}
	}

	internal static void SetLevel(uint level)
	{
		SpoofedLevel = level;
		CustomLevel = 0u;
		ApplyLevelSpoof();
	}

	internal static void ApplyLevelSpoof()
	{
		if (!EnableLevelSpoof)
		{
			return;
		}
		try
		{
			if (DataManager.Player == null || DataManager.Player.Stats == null)
			{
				Debug.LogWarning(InteropFix.Cast("[SpoofingService] DataManager not available"));
				return;
			}
			if (!_levelCached)
			{
				_originalLevel = DataManager.Player.Stats.Level;
				_levelCached = true;
				Debug.Log(InteropFix.Cast($"[SpoofingService] Level original: {_originalLevel}"));
			}
			uint effectiveLevel = GetEffectiveLevel();
			uint level = DataManager.Player.Stats.Level;
			if (effectiveLevel != 0 && effectiveLevel != level)
			{
				DataManager.Player.Stats.Level = effectiveLevel;
				if (ShouldPersistProfileChanges())
				{
					((AbstractSaveData)DataManager.Player).Save();
				}
				Debug.Log(InteropFix.Cast($"[SpoofingService] Level local: {level} -> {effectiveLevel}"));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[SpoofingService] Level error: " + ex.Message));
		}
	}

	internal static void RestoreLevel()
	{
		if (!_levelCached)
		{
			return;
		}
		try
		{
			if (DataManager.Player != null && DataManager.Player.Stats != null)
			{
				DataManager.Player.Stats.Level = _originalLevel;
				if (ShouldPersistProfileChanges())
				{
					((AbstractSaveData)DataManager.Player).Save();
				}
				Debug.Log(InteropFix.Cast($"[SpoofingService] Level restaurado: {_originalLevel}"));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[SpoofingService] Error restoring: " + ex.Message));
		}
	}

	internal static void ApplyPlatformSpoof(PlatformSpecificData data)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		if (!EnablePlatformSpoof || data == null)
		{
			return;
		}
		try
		{
			data.Platform = SpoofedPlatform;
			if (!string.IsNullOrEmpty(SpoofedPlatformName))
			{
				data.PlatformName = SpoofedPlatformName;
			}
			else
			{
				if (_cachedPlatformName == null || _cachedPlatformType != SpoofedPlatform)
				{
					_cachedPlatformName = GetDefaultPlatformName(SpoofedPlatform);
					_cachedPlatformType = SpoofedPlatform;
				}
				data.PlatformName = _cachedPlatformName;
			}
			Platforms spoofedPlatform = SpoofedPlatform;
			if ((int)spoofedPlatform != 4 && (int)spoofedPlatform != 9)
			{
				if ((int)spoofedPlatform == 10)
				{
					data.PsnPlatformId = GenerateFakePsnId();
				}
			}
			else
			{
				data.XboxPlatformId = GenerateFakeXboxId();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[SpoofingService] Platform error: " + ex.Message));
		}
	}

	private static string GetDefaultPlatformName(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		return ((int)platform - 1) switch
		{
			1 => $"Player{_rng.Next(1000, 99999)}", 
			0 => $"Player{_rng.Next(1000, 99999)}", 
			3 => $"Player{_rng.Next(1000, 99999)}", 
			2 => $"Player{_rng.Next(1000, 99999)}", 
			4 => $"Player{_rng.Next(1000, 99999)}", 
			6 => "", 
			5 => "", 
			7 => "", 
			8 => $"Player{_rng.Next(1000, 99999)}", 
			9 => "", 
			_ => "", 
		};
	}

	private static ulong GenerateFakeXboxId()
	{
		byte[] array = new byte[8];
		_rng.NextBytes(array);
		return BitConverter.ToUInt64(array, 0) & 0xFFFFFFFFFFFFL;
	}

	private static ulong GenerateFakePsnId()
	{
		byte[] array = new byte[8];
		_rng.NextBytes(array);
		return BitConverter.ToUInt64(array, 0) & 0xFFFFFFFFFFFFL;
	}

	internal static void SetPlatform(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		SpoofedPlatform = platform;
		_cachedPlatformName = null;
	}

	internal static void SetPlatformName(string name)
	{
		SpoofedPlatformName = name ?? "";
	}

	internal static void ApplyFriendCodeSpoof()
	{
		if (!EnableFriendCodeSpoof)
		{
			return;
		}
		try
		{
			if (!_friendCodeCached)
			{
				try
				{
					_originalFriendCode = DestroyableSingleton<EOSManager>.Instance.FriendCode ?? "";
				}
				catch
				{
				}
				_friendCodeCached = true;
			}
			string text = ((!string.IsNullOrEmpty(SpoofedFriendCode)) ? SpoofedFriendCode : GenerateRandomFriendCode());
			try
			{
				DestroyableSingleton<EOSManager>.Instance.FriendCode = text;
			}
			catch
			{
			}
			if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				PlayerControl.LocalPlayer.FriendCode = text;
			}
			Debug.Log(InteropFix.Cast("[SpoofingService] FriendCode spoofed to: " + text));
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[SpoofingService] FriendCode error: " + ex.Message));
		}
	}

	public static string GenerateRandomFriendCode()
	{
		string[] array = new string[10] { "cosmic", "stellar", "nebula", "astral", "lunar", "solar", "vortex", "plasma", "photon", "quasar" };
		string[] array2 = new string[10] { "flux", "wave", "beam", "core", "node", "link", "zone", "pulse", "spark", "glow" };
		return $"{array[_rng.Next(array.Length)]}{array2[_rng.Next(array2.Length)]}#{_rng.Next(1000, 9999)}";
	}

	internal static void RestoreFriendCode()
	{
		if (!_friendCodeCached)
		{
			return;
		}
		try
		{
			try
			{
				DestroyableSingleton<EOSManager>.Instance.FriendCode = _originalFriendCode;
			}
			catch
			{
			}
			if ((Object)(object)PlayerControl.LocalPlayer != (Object)null)
			{
				PlayerControl.LocalPlayer.FriendCode = _originalFriendCode;
			}
		}
		catch
		{
		}
	}

	public unsafe static string PlatformToString(Platforms platform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected I4, but got Unknown
		return (int)platform switch
		{
			0 => "Unknown", 
			2 => "Steam", 
			1 => "Epic", 
			4 => "Microsoft Store", 
			3 => "Mac", 
			5 => "Itch.io", 
			7 => "Android", 
			6 => "iPhone", 
			8 => "Switch", 
			9 => "Xbox", 
			10 => "PlayStation", 
			_ => ((object)(*(Platforms*)(&platform))/*cast due to .constrained prefix*/).ToString(), 
		};
	}

	public static Platforms[] GetAllPlatforms()
	{
		return new Platforms[10]
		{
			(Platforms)1,
			(Platforms)2,
			(Platforms)3,
			(Platforms)4,
			(Platforms)5,
			(Platforms)6,
			(Platforms)7,
			(Platforms)8,
			(Platforms)9,
			(Platforms)10
		};
	}

	public static string GetStatus()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		if (EnableLevelSpoof)
		{
			text += $"Lv{GetEffectiveLevel()} ";
		}
		if (EnablePlatformSpoof)
		{
			text = text + PlatformToString(SpoofedPlatform) + " ";
		}
		if (EnableFriendCodeSpoof)
		{
			text += "FC ";
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text.Trim();
		}
		return "OFF";
	}

	internal static void DisableAll()
	{
		EnableLevelSpoof = false;
		EnablePlatformSpoof = false;
		EnableFriendCodeSpoof = false;
		RestoreLevel();
		RestoreFriendCode();
	}

	internal static void MarkForReapplication()
	{
		ApplyLevelSpoof();
		if (EnableFriendCodeSpoof)
		{
			ApplyFriendCodeSpoof();
		}
	}

	public static int GetActiveCount()
	{
		int num = 0;
		if (EnableLevelSpoof)
		{
			num++;
		}
		if (EnablePlatformSpoof)
		{
			num++;
		}
		if (EnableFriendCodeSpoof)
		{
			num++;
		}
		return num;
	}
}




