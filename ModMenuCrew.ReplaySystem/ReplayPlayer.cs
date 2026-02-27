using System;
using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModMenuCrew.ReplaySystem;

public class ReplayPlayer : MonoBehaviour
{
	public static ReplayPlayer Instance;

	private ReplayData currentData;

	private bool isPlaying;

	private float currentTime;

	private float playbackSpeed = 1f;

	private Dictionary<byte, ReplayPuppet> puppets = new Dictionary<byte, ReplayPuppet>();

	private GameObject puppetRoot;

	public bool IsPlaying => isPlaying;

	public float CurrentTime => currentTime;

	public float TotalDuration
	{
		get
		{
			if (currentData == null || currentData.Frames.Count <= 0)
			{
				return 0f;
			}
			return currentData.Frames[currentData.Frames.Count - 1].Time;
		}
	}

	public ReplayPlayer(System.IntPtr ptr)
		: base(ptr)
	{
	}

	private void Awake()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		Instance = this;
		puppetRoot = new GameObject("ReplayPuppets");
		Object.DontDestroyOnLoad((Object)(object)puppetRoot);
	}

	public void LoadAndPlay(string path)
	{
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		if ((Object)(object)ReplayViewer.Instance != (Object)null)
		{
			ReplayViewer.Instance.StartViewer(path);
			return;
		}
		bool flag = default(bool);
		try
		{
			if ((Object)(object)ShipStatus.Instance == (Object)null)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayPlayer] Cannot load replay: ShipStatus not available (not in game)");
				return;
			}
			Cleanup();
			currentData = ReplayData.Load(path);
			if (currentData == null)
			{
				return;
			}
			foreach (ReplayPlayerInfo player in currentData.Players)
			{
				SpawnPuppet(player);
			}
			currentTime = 0f;
			isPlaying = false;
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(32, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Replay Loaded: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentData.Players.Count);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" players, ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentData.Frames.Count);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" frames");
			}
			log.LogInfo(val);
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(23, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Failed to load replay: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex);
			}
			log2.LogError(val2);
		}
	}

	[HideFromIl2Cpp]
	public void SpawnPuppet(ReplayPlayerInfo info)
	{
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Expected O, but got Unknown
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			if ((Object)(object)puppetRoot == (Object)null || ((Il2CppObjectBase)puppetRoot).WasCollected)
			{
				puppetRoot = new GameObject("ReplayPuppets");
				if ((Object)(object)puppetRoot == (Object)null)
				{
					((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[ReplayPlayer] Failed to create puppetRoot GameObject");
					return;
				}
				Object.DontDestroyOnLoad((Object)(object)puppetRoot);
			}
			if ((Object)(object)puppetRoot.transform == (Object)null)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[ReplayPlayer] puppetRoot.transform is null - game may not be fully loaded");
				return;
			}
			GameObject val = new GameObject("Puppet_" + info.Name);
			if ((Object)(object)val == (Object)null || ((Il2CppObjectBase)val).WasCollected)
			{
				ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(54, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayPlayer] Failed to create puppet GameObject for ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(info.Name);
				}
				log.LogError(val2);
				return;
			}
			if ((Object)(object)val.transform == (Object)null)
			{
				ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(40, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayPlayer] go.transform is null for ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(info.Name);
				}
				log2.LogError(val2);
				Object.Destroy((Object)(object)val);
				return;
			}
			val.transform.SetParent(puppetRoot.transform);
			SpriteRenderer val3 = val.AddComponent<SpriteRenderer>();
			bool flag2 = false;
			PlayerControl val4 = null;
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null))
				{
					if (current.PlayerId == info.PlayerId)
					{
						val4 = current;
						break;
					}
					if ((Object)(object)val4 == (Object)null && current.Data.PlayerName == info.Name)
					{
						val4 = current;
					}
				}
			}
			if ((Object)(object)val4 == (Object)null)
			{
				enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current2 = enumerator.Current;
					if ((Object)(object)current2 != (Object)null)
					{
						val4 = current2;
						break;
					}
				}
			}
			if ((Object)(object)val4 != (Object)null)
			{
				try
				{
					CosmeticsLayer cosmetics = val4.cosmetics;
					object obj;
					if (cosmetics == null)
					{
						obj = null;
					}
					else
					{
						PlayerBodySprite currentBodySprite = cosmetics.currentBodySprite;
						obj = ((currentBodySprite != null) ? currentBodySprite.BodySprite : null);
					}
					SpriteRenderer val5 = (SpriteRenderer)obj;
					if ((Object)(object)val5 == (Object)null)
					{
						val5 = ((Component)val4).GetComponent<SpriteRenderer>();
					}
					if ((Object)(object)val5 != (Object)null)
					{
						val.transform.localScale = ((Component)val5).transform.lossyScale;
					}
					if ((Object)(object)val5 != (Object)null && (Object)(object)val5.sprite != (Object)null)
					{
						val3.sprite = val5.sprite;
						((Renderer)val3).material = new Material(((Renderer)val5).material);
						try
						{
							PlayerMaterial.SetColors(info.ColorId, (Renderer)val3);
						}
						catch
						{
						}
						flag2 = true;
					}
				}
				catch
				{
				}
			}
			if (!flag2)
			{
				val3.sprite = GetDummySprite();
				if (info.ColorId >= 0 && info.ColorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
				{
					val3.color = InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[info.ColorId]);
				}
				else
				{
					val3.color = InteropFix.Cast(info.RealColor);
				}
			}
			((Renderer)val3).sortingOrder = 5;
			GameObject val6 = new GameObject("NameTag");
			val6.transform.SetParent(val.transform);
			val6.transform.localPosition = new Vector3(0f, 0.8f, 0f);
			TextMeshPro obj4 = val6.AddComponent<TextMeshPro>();
			((TMP_Text)obj4).text = info.Name;
			((TMP_Text)obj4).fontSize = 2f;
			((TMP_Text)obj4).alignment = (TextAlignmentOptions)514;
			((Graphic)obj4).color = Color.white;
			((TMP_Text)obj4).outlineColor = InteropFix.Cast(Color.black);
			((TMP_Text)obj4).outlineWidth = 0.2f;
			ReplayPuppet replayPuppet = val.AddComponent<ReplayPuppet>();
			replayPuppet.Initialize(info, val3);
			puppets[info.PlayerId] = replayPuppet;
		}
		catch (Exception ex)
		{
			ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(46, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayPlayer] Exception in SpawnPuppet for ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(info?.Name);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(": ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log3.LogError(val2);
		}
	}

	private Sprite GetDummySprite()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		int num = 64;
		int num2 = 64;
		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false);
		Color[] array = (Color[])(object)new Color[num * num2];
		Vector2 val2 = default(Vector2);
		val2._002Ector((float)(num / 2), (float)(num2 / 2));
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num3 = Vector2.Distance(new Vector2((float)j, (float)i * 0.8f), new Vector2(val2.x, val2.y * 0.8f));
				Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(val2.x + 8f, val2.y + 10f));
				if (((float)j - (val2.x + 8f)) * ((float)j - (val2.x + 8f)) / 144f + ((float)i - (val2.y + 10f)) * ((float)i - (val2.y + 10f)) / 64f <= 1f)
				{
					array[i * num + j] = new Color(0.5f, 0.8f, 1f, 1f);
				}
				else if (num3 < 26f)
				{
					array[i * num + j] = Color.white;
				}
				else
				{
					array[i * num + j] = Color.clear;
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num2), new Vector2(0.5f, 0.5f), 32f);
	}

	private void Update()
	{
		if (isPlaying && currentData != null)
		{
			currentTime += Mathf.Min(Time.deltaTime, 0.1f) * playbackSpeed;
			if (currentTime >= TotalDuration)
			{
				currentTime = TotalDuration;
				isPlaying = false;
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayPlayer] Playback completed");
			}
			ApplyFrameState(currentTime);
		}
	}

	private void ApplyFrameState(float time)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		var frames = currentData.Frames;
		int num = FindFrameIndex(time);
		if (num < 0 || num >= frames.Count)
		{
			return;
		}
		ReplayFrame replayFrame = frames[num];
		ReplayFrame replayFrame2 = ((num < frames.Count - 1) ? frames[num + 1] : null);
		float num2 = 0f;
		if (replayFrame2 != null)
		{
			float num3 = replayFrame2.Time - replayFrame.Time;
			if (num3 > 0.001f)
			{
				num2 = (time - replayFrame.Time) / num3;
			}
		}
		foreach (PlayerState state in replayFrame.States)
		{
			if (!puppets.TryGetValue(state.PlayerId, out var value))
			{
				continue;
			}
			Vector2 pos = state.Position;
			if (replayFrame2 != null)
			{
				PlayerState playerState = replayFrame2.States.Find((PlayerState s) => s.PlayerId == state.PlayerId);
				if (playerState != null)
				{
					pos = Vector2.Lerp(state.Position, playerState.Position, num2);
				}
			}
			value.UpdateState(pos, state.FaceRight, state.IsDead, state.AnimState);
		}
	}

	private int FindFrameIndex(float time)
	{
		var frames = currentData.Frames;
		if (frames.Count == 0)
		{
			return -1;
		}
		int num = 0;
		int num2 = frames.Count - 1;
		while (num < num2)
		{
			int num3 = num + (num2 - num + 1) / 2;
			if (frames[num3].Time <= time)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return num;
	}

	public void SetSpeed(float speed)
	{
		playbackSpeed = speed;
	}

	public void Seek(float time)
	{
		currentTime = Mathf.Clamp(time, 0f, TotalDuration);
	}

	public void Cleanup()
	{
		isPlaying = false;
		currentData = null;
		if (puppets == null)
		{
			return;
		}
		foreach (ReplayPuppet value in puppets.Values)
		{
			if (InteropFix.Cast((Object)(object)value) && InteropFix.Cast((Object)(object)((Component)value).gameObject))
			{
				Object.Destroy((Object)(object)((Component)value).gameObject);
			}
		}
		puppets.Clear();
	}

	private void OnDestroy()
	{
		Cleanup();
		if (InteropFix.Cast((Object)(object)puppetRoot))
		{
			Object.Destroy((Object)(object)puppetRoot);
		}
	}
}









