using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.Data;
using AmongUs.Data.Player;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using ModMenuCrew.Features;
using UnityEngine;

namespace ModMenuCrew.Patches;

[HarmonyPatch(typeof(HatManager), "Initialize")]
public static class CosmeticsUnlockPatch
{
	private static readonly Dictionary<(Type, string), MemberInfo> memberCache = new Dictionary<(Type, string), MemberInfo>();

	private static readonly BindingFlags CaseInsensitiveInstanceFlags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static bool _hasUnlocked = false;

	private static readonly System.Collections.Generic.HashSet<string> _unlockedProductIds = new System.Collections.Generic.HashSet<string>();

	private static object ConvertIfNeeded(object value, Type targetType)
	{
		if (value == null)
		{
			return null;
		}
		Type type = value.GetType();
		if (targetType.IsAssignableFrom(type))
		{
			return value;
		}
		try
		{
			return System.Convert.ChangeType(value, targetType);
		}
		catch
		{
			return value;
		}
	}

	private static MemberInfo GetCachedMember(Type type, string name)
	{
		(Type, string) key = (type, name.ToLowerInvariant());
		if (memberCache.TryGetValue(key, out var value))
		{
			return value;
		}
		PropertyInfo property = type.GetProperty(name, CaseInsensitiveInstanceFlags);
		if (property != null)
		{
			memberCache[key] = property;
			return property;
		}
		FieldInfo field = type.GetField(name, CaseInsensitiveInstanceFlags);
		if (field != null)
		{
			memberCache[key] = field;
			return field;
		}
		memberCache[key] = null;
		return null;
	}

	private static bool TrySetPropertyOrField(object target, object value, params string[] candidateNames)
	{
		if (target == null || candidateNames == null || candidateNames.Length == 0)
		{
			return false;
		}
		Type type = target.GetType();
		foreach (string name in candidateNames)
		{
			MemberInfo cachedMember = GetCachedMember(type, name);
			if (cachedMember is PropertyInfo { CanWrite: not false } propertyInfo)
			{
				try
				{
					object value2 = ConvertIfNeeded(value, propertyInfo.PropertyType);
					propertyInfo.SetValue(target, value2, null);
					return true;
				}
				catch
				{
				}
			}
			if (cachedMember is FieldInfo { IsInitOnly: false } fieldInfo)
			{
				try
				{
					object value3 = ConvertIfNeeded(value, fieldInfo.FieldType);
					fieldInfo.SetValue(target, value3);
					return true;
				}
				catch
				{
				}
			}
		}
		return false;
	}

	private static string GetStringPropertyOrFieldOrDefault(object target, string defaultValue, params string[] candidateNames)
	{
		if (target == null || candidateNames == null || candidateNames.Length == 0)
		{
			return defaultValue;
		}
		Type type = target.GetType();
		foreach (string name in candidateNames)
		{
			MemberInfo cachedMember = GetCachedMember(type, name);
			if (cachedMember is PropertyInfo { CanRead: not false } propertyInfo)
			{
				try
				{
					if (propertyInfo.GetValue(target, null) is string text && !string.IsNullOrEmpty(text))
					{
						return text;
					}
				}
				catch
				{
				}
			}
			if (!(cachedMember is FieldInfo fieldInfo))
			{
				continue;
			}
			try
			{
				if (fieldInfo.GetValue(target) is string text2 && !string.IsNullOrEmpty(text2))
				{
					return text2;
				}
			}
			catch
			{
			}
		}
		return defaultValue;
	}

	public static void Postfix(HatManager __instance)
	{
		if (CheatConfig.OfflineLocalMode?.Value ?? false)
		{
			return;
		}
		if (!_hasUnlocked)
		{
			UnlockAllItems(__instance);
			_hasUnlocked = true;
		}
	}

	public static void ResetUnlockFlag()
	{
		_hasUnlocked = false;
		_unlockedProductIds.Clear();
	}

	private static void UnlockAndPurchaseAll<T>(IEnumerable<T> items, PlayerPurchasesData purchasesData) where T : class
	{
		if (items == null)
		{
			return;
		}
		foreach (T item in items)
		{
			if (item == null)
			{
				continue;
			}
			TrySetPropertyOrField(item, true, "Free");
			TrySetPropertyOrField(item, false, "NotInStore");
			string stringPropertyOrFieldOrDefault = GetStringPropertyOrFieldOrDefault(item, null, "ProductId", "productId");
			if (string.IsNullOrEmpty(stringPropertyOrFieldOrDefault) || _unlockedProductIds.Contains(stringPropertyOrFieldOrDefault))
			{
				continue;
			}
			try
			{
				if (purchasesData != null)
				{
					purchasesData.SetPurchased(stringPropertyOrFieldOrDefault);
				}
				_unlockedProductIds.Add(stringPropertyOrFieldOrDefault);
			}
			catch
			{
			}
		}
	}

	private static void UnlockAllItems(HatManager manager)
	{
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)manager == (Object)null)
		{
			Debug.LogError(InteropFix.Cast("[ModMenuCrew] HatManager is null."));
			return;
		}
		try
		{
			PlayerData player = DataManager.Player;
			if (player == null)
			{
				return;
			}
			PlayerPurchasesData purchases = player.Purchases;
			PetData[] obj = ((IEnumerable<PetData>)manager.allPets)?.ToArray() ?? System.Array.Empty<PetData>();
			HatData[] items = ((IEnumerable<HatData>)manager.allHats)?.ToArray() ?? System.Array.Empty<HatData>();
			SkinData[] items2 = ((IEnumerable<SkinData>)manager.allSkins)?.ToArray() ?? System.Array.Empty<SkinData>();
			VisorData[] items3 = ((IEnumerable<VisorData>)manager.allVisors)?.ToArray() ?? System.Array.Empty<VisorData>();
			NamePlateData[] items4 = ((IEnumerable<NamePlateData>)manager.allNamePlates)?.ToArray() ?? System.Array.Empty<NamePlateData>();
			Il2CppArrayBase<BundleData> val = manager.allBundles?.ToArray();
			BundleData[] array = ((val != null) ? InteropFix.Cast(val) : System.Array.Empty<BundleData>());
			UnlockAndPurchaseAll(obj, purchases);
			UnlockAndPurchaseAll(items, purchases);
			UnlockAndPurchaseAll(items2, purchases);
			UnlockAndPurchaseAll(items3, purchases);
			UnlockAndPurchaseAll(items4, purchases);
			System.Collections.Generic.HashSet<string> hashSet = new System.Collections.Generic.HashSet<string>();
			PetData[] array2 = obj;
			foreach (PetData val2 in array2)
			{
				if (val2 != null && ((CosmeticData)val2).ProductId != null)
				{
					hashSet.Add(((CosmeticData)val2).ProductId);
				}
			}
			List<BundleData> list = new List<BundleData>(array.Length);
			BundleData[] array3 = array;
			foreach (BundleData val3 in array3)
			{
				if ((Object)(object)val3 == (Object)null)
				{
					continue;
				}
				bool flag = true;
				if (val3.cosmetics != null)
				{
					foreach (CosmeticData item in (Il2CppArrayBase<CosmeticData>)(object)val3.cosmetics)
					{
						if ((Object)(object)item == (Object)null)
						{
							flag = false;
							break;
						}
						PetData val4 = (PetData)(object)((item is PetData) ? item : null);
						if (val4 != null && !hashSet.Contains(((CosmeticData)val4).ProductId))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					list.Add(val3);
				}
			}
			UnlockAndPurchaseAll(list, purchases);
			try
			{
				UnlockAndPurchaseAll((IEnumerable<BundleData>)(manager.allFeaturedBundles?.ToArray()), purchases);
				UnlockAndPurchaseAll((IEnumerable<CosmicubeData>)(manager.allFeaturedCubes?.ToArray()), purchases);
				UnlockAndPurchaseAll((IEnumerable<CosmeticData>)(manager.allFeaturedItems?.ToArray()), purchases);
			}
			catch (Exception ex)
			{
				Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] Featured unlock error: " + ex.Message));
			}
			Il2CppArrayBase<StarBundle> val5 = manager.allStarBundles?.ToArray();
			if (val5 != null)
			{
				foreach (StarBundle item2 in val5)
				{
					if (!((Object)(object)item2 == (Object)null))
					{
						TrySetPropertyOrField(item2, 0, "price", "Price");
						TrySetPropertyOrField(item2, true, "Free");
					}
				}
			}
			if (player.store != null)
			{
				DateTime now = DateTime.Now;
				player.store.LastBundlesViewDate = now;
				player.store.LastHatsViewDate = now;
				player.store.LastOutfitsViewDate = now;
				player.store.LastVisorsViewDate = now;
				player.store.LastPetsViewDate = now;
				player.store.LastNameplatesViewDate = now;
				player.store.LastCosmicubeViewDate = now;
			}
			try
			{
				InventoryManager instance = DestroyableSingleton<InventoryManager>.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					instance.UnusedBeans = 999999;
					PropertyInfo property = typeof(InventoryManager).GetProperty("UnusedStars", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (property != null)
					{
						MethodInfo setMethod = property.GetSetMethod(nonPublic: true);
						if (setMethod != null)
						{
							setMethod.Invoke(instance, new object[1] { 999999 });
						}
					}
					Debug.Log(InteropFix.Cast("[ModMenuCrew] Stars & Beans set to 999999."));
				}
			}
			catch (Exception ex2)
			{
				Debug.LogWarning(InteropFix.Cast("[ModMenuCrew] Stars/Beans set error (non-fatal): " + ex2.Message));
			}
			((AbstractSaveData)player).Save();
			Debug.Log(InteropFix.Cast($"[ModMenuCrew] All cosmetics unlocked! Total: {_unlockedProductIds.Count} items."));
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ModMenuCrew] UnlockAllItems critical error: {value}"));
		}
	}
}





