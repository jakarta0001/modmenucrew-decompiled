using System;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

public class ReplayPuppet : MonoBehaviour
{
	private SpriteRenderer rend;

	public ReplayPlayerInfo Info;

	private Vector3 originalScale = Vector3.one;

	private Vector3 targetPos;

	private bool isDeadCurrent;

	public ReplayPuppet(System.IntPtr ptr)
		: base(ptr)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0006: Unknown result type (might be due to invalid IL or missing references)


	[HideFromIl2Cpp]
	public void Initialize(ReplayPlayerInfo info, SpriteRenderer dummySprite)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		Info = info;
		rend = dummySprite;
		originalScale = ((Component)this).transform.localScale;
	}

	public void UpdateState(Vector2 pos, bool faceRight, bool isDead, AnimState anim)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			((Component)this).transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f);
			float num = (faceRight ? 1f : (-1f));
			Vector3 localScale = ((Component)this).transform.localScale;
			float num2 = Mathf.Abs(localScale.x);
			((Component)this).transform.localScale = new Vector3(num * num2, localScale.y, localScale.z);
			int childCount = ((Component)this).transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = ((Component)this).transform.GetChild(i);
				if (!((Object)(object)child == (Object)null) && (((Object)child).name == "NameTag" || ((Object)child).name == "RoleIndicator"))
				{
					Vector3 localScale2 = child.localScale;
					child.localScale = new Vector3(num * Mathf.Abs(localScale2.x), localScale2.y, localScale2.z);
				}
			}
			HandleAnimState(anim, isDead);
			if (isDead || anim == AnimState.Ghost)
			{
				SetAlpha(0.5f);
			}
			else
			{
				SetAlpha(1f);
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(34, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayPuppet] UpdateState error: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val);
		}
	}

	private void HandleAnimState(AnimState anim, bool isDead)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sign(((Component)this).transform.localScale.x);
		if (num == 0f)
		{
			num = 1f;
		}
		Vector3 val = default(Vector3);
		float num2;
		switch (anim)
		{
		case AnimState.VentEnter:
			val._002Ector(0.1f, 0f, 1f);
			num2 = 10f;
			break;
		case AnimState.VentExit:
			val = originalScale;
			num2 = 10f;
			break;
		case AnimState.Ghost:
			val = originalScale;
			num2 = 100f;
			break;
		default:
			val = originalScale;
			num2 = 15f;
			break;
		}
		Vector3 localScale = ((Component)this).transform.localScale;
		Vector3 val2 = Vector3.Lerp(new Vector3(Mathf.Abs(localScale.x), localScale.y, localScale.z), val, Mathf.Min(Time.deltaTime, 0.1f) * num2);
		((Component)this).transform.localScale = new Vector3(num * Mathf.Abs(val2.x), val2.y, val2.z);
	}

	private void SetAlpha(float a)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (InteropFix.Cast((Object)(object)rend))
		{
			Color color = rend.color;
			rend.color = new Color(color.r, color.g, color.b, a);
		}
		int childCount = ((Component)this).transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = ((Component)this).transform.GetChild(i);
			if (!((Object)(object)child == (Object)null))
			{
				SpriteRenderer component = ((Component)child).GetComponent<SpriteRenderer>();
				if (InteropFix.Cast((Object)(object)component))
				{
					Color color2 = component.color;
					component.color = new Color(color2.r, color2.g, color2.b, a);
				}
			}
		}
	}
}






