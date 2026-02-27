using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModMenuCrew.Patches;

[HarmonyPatch(typeof(VersionShower))]
public static class VersionShowerPatch
{
	private static bool _typeRegistered;

	[HarmonyPostfix]
	[HarmonyPatch("Start")]
	public static void Postfix(VersionShower __instance)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.text == (Object)null)
			{
				return;
			}
			TextMeshPro text = __instance.text;
			((TMP_Text)text).richText = true;
			((Graphic)text).color = new Color(0f, 1f, 0.55f, 1f);
			((TMP_Text)text).outlineColor = InteropFix.Cast(new Color(0f, 1f, 0.55f, 1f));
			((TMP_Text)text).outlineWidth = 0.2f;
			if (!_typeRegistered)
			{
				try
				{
					ClassInjector.RegisterTypeInIl2Cpp<VersionShowerFx>();
				}
				catch
				{
				}
				_typeRegistered = true;
			}
			((TMP_Text)text).text = StripBuildNum(((TMP_Text)text).text);
			VersionShowerFx versionShowerFx = ((Component)__instance).gameObject.GetComponent<VersionShowerFx>();
			if ((Object)(object)versionShowerFx == (Object)null)
			{
				versionShowerFx = ((Component)__instance).gameObject.AddComponent<VersionShowerFx>();
			}
			versionShowerFx.Initialize(text);
		}
		catch (Exception value)
		{
			Debug.LogError(InteropFix.Cast($"[ModMenuCrew] VersionShowerPatch error: {value}"));
		}
	}

	private static string StripBuildNum(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		int num = text.IndexOf("(build num:");
		if (num >= 0)
		{
			int num2 = num;
			if (num2 > 0 && text[num2 - 1] == ' ')
			{
				num2--;
			}
			int num3 = text.IndexOf(')', num);
			if (num3 >= 0)
			{
				return text.Remove(num2, num3 - num2 + 1);
			}
			return text.Substring(0, num2);
		}
		return text;
	}
}


