using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using ModMenuCrew.Features;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModMenuCrew.ReplaySystem;

public class ReplayViewer : MonoBehaviour
{
	public static ReplayViewer Instance;

	private ReplayData data;

	private bool isActive;

	private bool isPaused = true;

	private float currentTime;

	private float playbackSpeed = 1f;

	private Dictionary<byte, ReplayPuppet> puppets = new Dictionary<byte, ReplayPuppet>();

	private GameObject puppetRoot;

	private Camera replayCamera;

	private Vector3 cameraPosition;

	private float cameraZoom = 5f;

	private const float MIN_ZOOM = 2f;

	private const float MAX_ZOOM = 15f;

	private byte? followingPlayerId;

	private Dictionary<byte, LineRenderer> routeLines = new Dictionary<byte, LineRenderer>();

	private Dictionary<byte, List<Vector3>> routePoints = new Dictionary<byte, List<Vector3>>();

	private const int MAX_ROUTE_POINTS = 500;

	private Dictionary<Vector2Int, float> heatmapData = new Dictionary<Vector2Int, float>();

	private GameObject heatmapOverlay;

	private Dictionary<byte, GameObject> deadBodies = new Dictionary<byte, GameObject>();

	private ReplayUI ui;

	private int lastFrameIndex = -1;

	private Dictionary<byte, Vector3[]> _routeArrayCache = new Dictionary<byte, Vector3[]>();

	private Dictionary<byte, PlayerState> _nextFrameLookup = new Dictionary<byte, PlayerState>();

	private Sprite cachedTemplateSprite;

	private Material cachedTemplateMaterial;

	private Vector3 _defaultPlayerScale = new Vector3(0.5f, 0.5f, 1f);

	private MonoBehaviour vanillaCameraScript;

	[HideFromIl2Cpp]
	public ReplayData Data => data;

	public bool IsActive => isActive;

	public bool IsPaused => isPaused;

	public float CurrentTime => currentTime;

	public float TotalDuration
	{
		get
		{
			ReplayData replayData = data;
			if (replayData == null)
			{
				ReplayData replayData2 = data;
				if (replayData2 == null || !(replayData2.Frames?.Count > 0))
				{
					return 0f;
				}
				return data.Frames[data.Frames.Count - 1].Time;
			}
			return replayData.TotalDuration;
		}
	}

	public byte? FollowingPlayerId => followingPlayerId;

	public ReplayViewer(System.IntPtr ptr)
		: base(ptr)
	{
	}//IL_0081: Unknown result type (might be due to invalid IL or missing references)
	//IL_0086: Unknown result type (might be due to invalid IL or missing references)


	private void Awake()
	{
		Instance = this;
	}

	public void StartViewer(string replayPath)
	{
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			if (isActive)
			{
				Stop();
			}
			ReplayRecorder.ReplayInProgress = true;
			if (!((Object)(object)ShipStatus.Instance != (Object)null))
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayViewer] Starting in Menu Mode - visuals may be limited");
			}
			data = ReplayData.Load(replayPath);
			if (data == null || data.Frames.Count == 0)
			{
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogError((object)"[ReplayViewer] Failed to load replay or empty");
				ReplayRecorder.ReplayInProgress = false;
				return;
			}
			routeLines.Clear();
			routePoints.Clear();
			puppets.Clear();
			InitializePuppetRoot();
			InitializeCamera();
			SpawnAllPuppets();
			InitializeRoutes();
			InitializeUI();
			currentTime = 0f;
			isPaused = false;
			isActive = true;
			ReplayRecorder.ReplayInProgress = true;
			HideRealPlayers();
			if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null)
			{
				((Component)DestroyableSingleton<HudManager>.Instance).gameObject.SetActive(false);
			}
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(50, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayViewer] Started: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(data.Players.Count);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" players, ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(data.Frames.Count);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" frames, ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(data.Events?.Count ?? 0);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" events");
			}
			log.LogInfo(val);
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(35, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayViewer] StartViewer failed: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Exception>(ex);
			}
			log2.LogError(val2);
			Stop();
		}
	}

	public void Stop()
	{
		ReplayRecorder.ReplayInProgress = false;
		if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null)
		{
			((Component)DestroyableSingleton<HudManager>.Instance).gameObject.SetActive(true);
			try
			{
				DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
			}
			catch
			{
			}
		}
		ShowRealPlayers();
		if (!isActive)
		{
			return;
		}
		isActive = false;
		if (InteropFix.Cast((Object)(object)puppetRoot))
		{
			Object.Destroy((Object)(object)puppetRoot);
		}
		if (InteropFix.Cast((Object)(object)ui))
		{
			Object.Destroy((Object)(object)((Component)ui).gameObject);
		}
		if (InteropFix.Cast((Object)(object)heatmapOverlay))
		{
			Object.Destroy((Object)(object)heatmapOverlay);
		}
		foreach (LineRenderer value in routeLines.Values)
		{
			if (InteropFix.Cast((Object)(object)value))
			{
				Object.Destroy((Object)(object)((Component)value).gameObject);
			}
		}
		routeLines.Clear();
		routePoints.Clear();
		_routeArrayCache.Clear();
		_nextFrameLookup.Clear();
		puppets.Clear();
		foreach (GameObject value2 in deadBodies.Values)
		{
			if (InteropFix.Cast((Object)(object)value2))
			{
				Object.Destroy((Object)(object)value2);
			}
		}
		deadBodies.Clear();
		RestoreCamera();
		data = null;
		((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayViewer] Stopped and Cleaned Up");
	}

	public void TogglePause()
	{
		isPaused = !isPaused;
	}

	public void SetSpeed(float speed)
	{
		playbackSpeed = Mathf.Clamp(speed, 0.1f, 10f);
	}

	public void Seek(float time)
	{
		currentTime = Mathf.Clamp(time, 0f, TotalDuration);
		foreach (List<Vector3> value in routePoints.Values)
		{
			value.Clear();
		}
		_routeArrayCache.Clear();
		foreach (GameObject value2 in deadBodies.Values)
		{
			if (InteropFix.Cast((Object)(object)value2))
			{
				Object.Destroy((Object)(object)value2);
			}
		}
		deadBodies.Clear();
		lastFrameIndex = -1;
		ApplyFrameState(currentTime);
		UpdateRoutes();
	}

	public void StepFrame(int direction)
	{
		if (data != null && data.Frames.Count != 0)
		{
			int index = Mathf.Clamp(FindFrameIndex(currentTime) + direction, 0, data.Frames.Count - 1);
			currentTime = data.Frames[index].Time;
			ApplyFrameState(currentTime);
		}
	}

	public void AdjustZoom(float multiplier)
	{
		cameraZoom = Mathf.Clamp(cameraZoom * multiplier, 2f, 15f);
		if (InteropFix.Cast((Object)(object)replayCamera))
		{
			replayCamera.orthographicSize = cameraZoom;
		}
	}

	public void FollowPlayer(byte playerId)
	{
		if (puppets.ContainsKey(playerId))
		{
			followingPlayerId = playerId;
		}
	}

	public void StopFollowing()
	{
		followingPlayerId = null;
	}

	public void MoveCamera(Vector3 delta)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)replayCamera != (Object)null)
		{
			Transform transform = ((Component)replayCamera).transform;
			transform.position += delta;
		}
	}

	[HideFromIl2Cpp]
	public System.Collections.Generic.IEnumerable<ReplayPuppet> GetPuppets()
	{
		var result = new System.Collections.Generic.List<ReplayPuppet>();
		var enumerator = puppets.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result.Add(enumerator.Current.Value);
		}
		return result;
	}

	private void InitializePuppetRoot()
	{
		puppetRoot = new GameObject("ReplayViewer_Puppets");
		if ((Object)(object)puppetRoot != (Object)null)
		{
			Object.DontDestroyOnLoad((Object)(object)puppetRoot);
		}
	}

	private void InitializeCamera()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if (!((Object)(object)main != (Object)null))
		{
			return;
		}
		cameraPosition = ((Component)main).transform.position;
		replayCamera = main;
		cameraZoom = main.orthographicSize;
		foreach (MonoBehaviour component in ((Component)main).GetComponents<MonoBehaviour>())
		{
			Il2CppSystem.Type il2CppType = ((Object)component).GetIl2CppType();
			if (il2CppType != null && il2CppType.Name == "FollowerCamera")
			{
				vanillaCameraScript = component;
				((Behaviour)component).enabled = false;
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayViewer] Disabled FollowerCamera logic");
				break;
			}
		}
	}

	private void RestoreCamera()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)replayCamera != (Object)null)
		{
			((Component)replayCamera).transform.position = cameraPosition;
			if ((Object)(object)vanillaCameraScript != (Object)null)
			{
				((Behaviour)vanillaCameraScript).enabled = true;
				vanillaCameraScript = null;
				((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogInfo((object)"[ReplayViewer] Restored FollowerCamera logic");
			}
		}
	}

	private void SpawnAllPuppets()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		if (data == null)
		{
			return;
		}
		cachedTemplateSprite = null;
		cachedTemplateMaterial = null;
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		bool flag = default(bool);
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if ((Object)(object)current == (Object)null)
			{
				continue;
			}
			try
			{
				CosmeticsLayer cosmetics = current.cosmetics;
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
				SpriteRenderer val = (SpriteRenderer)obj;
				if ((Object)(object)val != (Object)null && (Object)(object)val.sprite != (Object)null)
				{
					cachedTemplateSprite = val.sprite;
					cachedTemplateMaterial = ((Renderer)val).material;
					_defaultPlayerScale = ((Component)current).transform.localScale;
					ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(72, 2, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayViewer] Cached body template from ");
						BepInExInfoLogInterpolatedStringHandler obj2 = val2;
						NetworkedPlayerInfo obj3 = current.Data;
						((BepInExLogInterpolatedStringHandler)obj2).AppendFormatted<string>(((obj3 != null) ? obj3.PlayerName : null) ?? "?");
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" (cosmetics.BodySprite, scale=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Vector3>(((Component)current).transform.localScale);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(")");
					}
					log.LogInfo(val2);
					break;
				}
				SpriteRenderer component = ((Component)current).GetComponent<SpriteRenderer>();
				if ((Object)(object)component != (Object)null && (Object)(object)component.sprite != (Object)null)
				{
					cachedTemplateSprite = component.sprite;
					cachedTemplateMaterial = ((Renderer)component).material;
					_defaultPlayerScale = ((Component)current).transform.localScale;
					ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(73, 2, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ReplayViewer] Cached body template from ");
						BepInExInfoLogInterpolatedStringHandler obj4 = val2;
						NetworkedPlayerInfo obj5 = current.Data;
						((BepInExLogInterpolatedStringHandler)obj4).AppendFormatted<string>(((obj5 != null) ? obj5.PlayerName : null) ?? "?");
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" (fallback GetComponent, scale=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<Vector3>(((Component)current).transform.localScale);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(")");
					}
					log2.LogInfo(val2);
					break;
				}
			}
			catch
			{
			}
		}
		if ((Object)(object)cachedTemplateSprite == (Object)null)
		{
			((BasePlugin)ModMenuCrewPlugin.Instance).Log.LogWarning((object)"[ReplayViewer] No template sprite available - procedural fallback will be used");
		}
		foreach (ReplayPlayerInfo player in data.Players)
		{
			SpawnPuppet(player);
		}
	}

	[HideFromIl2Cpp]
	private PlayerControl FindSourcePlayer(ReplayPlayerInfo info)
	{
		PlayerControl val = null;
		PlayerControl val2 = null;
		var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PlayerControl current = enumerator.Current;
			if (!((Object)(object)current == (Object)null) && !((Object)(object)current.Data == (Object)null))
			{
				if (current.PlayerId == info.PlayerId)
				{
					val = current;
					break;
				}
				if ((Object)(object)val2 == (Object)null && current.Data.PlayerName == info.Name)
				{
					val2 = current;
				}
			}
		}
		return val ?? val2;
	}

	[HideFromIl2Cpp]
	private SpriteRenderer ClonePlayerVisuals(PlayerControl source, GameObject target, int targetColorId, bool isExactMatch)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Expected O, but got Unknown
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Expected O, but got Unknown
		SpriteRenderer val = target.GetComponent<SpriteRenderer>() ?? target.AddComponent<SpriteRenderer>();
		SpriteRenderer val2 = null;
		try
		{
			CosmeticsLayer cosmetics = source.cosmetics;
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
			val2 = (SpriteRenderer)obj;
		}
		catch
		{
		}
		if ((Object)(object)val2 == (Object)null)
		{
			val2 = ((Component)source).GetComponent<SpriteRenderer>();
		}
		Vector3 one = Vector3.one;
		one = ((!((Object)(object)val2 != (Object)null)) ? _defaultPlayerScale : ((Component)val2).transform.lossyScale);
		target.transform.localScale = one;
		if ((Object)(object)val2 != (Object)null && (Object)(object)val2.sprite != (Object)null)
		{
			val.sprite = val2.sprite;
			((Renderer)val).material = new Material(((Renderer)val2).material);
			try
			{
				PlayerMaterial.SetColors(targetColorId, (Renderer)val);
			}
			catch
			{
			}
		}
		Vector3 localPosition = default(Vector3);
		Vector3 localScale = default(Vector3);
		foreach (SpriteRenderer componentsInChild in ((Component)source).GetComponentsInChildren<SpriteRenderer>(false))
		{
			if ((Object)(object)componentsInChild == (Object)null || (Object)(object)componentsInChild == (Object)(object)val2 || (Object)(object)componentsInChild.sprite == (Object)null)
			{
				continue;
			}
			try
			{
				Vector3 val3 = ((Component)componentsInChild).transform.position - ((Component)source).transform.position;
				localPosition._002Ector((one.x != 0f) ? (val3.x / one.x) : 0f, (one.y != 0f) ? (val3.y / one.y) : 0f, (one.z != 0f) ? (val3.z / one.z) : 0f);
				Vector3 lossyScale = ((Component)componentsInChild).transform.lossyScale;
				localScale._002Ector((one.x != 0f) ? (lossyScale.x / one.x) : 1f, (one.y != 0f) ? (lossyScale.y / one.y) : 1f, (one.z != 0f) ? (lossyScale.z / one.z) : 1f);
				GameObject val4 = new GameObject(((Object)((Component)componentsInChild).gameObject).name);
				val4.transform.SetParent(target.transform);
				val4.transform.localPosition = localPosition;
				val4.transform.localScale = localScale;
				SpriteRenderer val5 = val4.AddComponent<SpriteRenderer>();
				val5.sprite = componentsInChild.sprite;
				((Renderer)val5).material = new Material(((Renderer)componentsInChild).material);
				((Renderer)val5).sortingOrder = ((Renderer)componentsInChild).sortingOrder;
				val5.color = componentsInChild.color;
				val5.flipX = false;
				if (!isExactMatch && (Object)(object)((Renderer)componentsInChild).material != (Object)null && ((Renderer)componentsInChild).material.HasProperty("_BodyColor"))
				{
					try
					{
						PlayerMaterial.SetColors(targetColorId, (Renderer)val5);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		return val;
	}

	[HideFromIl2Cpp]
	public void SpawnPuppet(ReplayPlayerInfo info)
	{
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Expected O, but got Unknown
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0495: Unknown result type (might be due to invalid IL or missing references)
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Expected O, but got Unknown
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Expected O, but got Unknown
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Expected O, but got Unknown
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		bool flag2 = default(bool);
		try
		{
			if ((Object)(object)puppetRoot == (Object)null || ((Il2CppObjectBase)puppetRoot).WasCollected)
			{
				return;
			}
			GameObject val = new GameObject("ReplayPuppet_" + info.Name);
			val.transform.SetParent(puppetRoot.transform);
			SpriteRenderer val2 = null;
			bool flag = false;
			PlayerControl val3 = FindSourcePlayer(info);
			if ((Object)(object)val3 != (Object)null)
			{
				val2 = ClonePlayerVisuals(val3, val, info.ColorId, isExactMatch: true);
				flag = (Object)(object)val2 != (Object)null && (Object)(object)val2.sprite != (Object)null;
				if (flag)
				{
					ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
					BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(39, 2, out flag2);
					if (flag2)
					{
						((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayViewer] Exact clone for ");
						((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(info.Name);
						((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(" (from ");
						BepInExInfoLogInterpolatedStringHandler obj = val4;
						NetworkedPlayerInfo obj2 = val3.Data;
						((BepInExLogInterpolatedStringHandler)obj).AppendFormatted<string>((obj2 != null) ? obj2.PlayerName : null);
						((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(")");
					}
					log.LogInfo(val4);
				}
			}
			if (!flag)
			{
				for (int num = val.transform.childCount - 1; num >= 0; num--)
				{
					Object.Destroy((Object)(object)((Component)val.transform.GetChild(num)).gameObject);
				}
				PlayerControl val5 = null;
				var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
				while (enumerator.MoveNext())
				{
					PlayerControl current = enumerator.Current;
					if ((Object)(object)current != (Object)null)
					{
						val5 = current;
						break;
					}
				}
				if ((Object)(object)val5 != (Object)null)
				{
					val2 = ClonePlayerVisuals(val5, val, info.ColorId, isExactMatch: false);
					flag = (Object)(object)val2 != (Object)null && (Object)(object)val2.sprite != (Object)null;
					if (flag)
					{
						ManualLogSource log2 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
						BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(56, 3, out flag2);
						if (flag2)
						{
							((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayViewer] Template clone for ");
							((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(info.Name);
							((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(" (body from ");
							BepInExInfoLogInterpolatedStringHandler obj3 = val4;
							NetworkedPlayerInfo obj4 = val5.Data;
							((BepInExLogInterpolatedStringHandler)obj3).AppendFormatted<string>((obj4 != null) ? obj4.PlayerName : null);
							((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(", color #");
							((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<int>(info.ColorId);
							((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(")");
						}
						log2.LogInfo(val4);
					}
				}
			}
			if (!flag && (Object)(object)cachedTemplateSprite != (Object)null)
			{
				val.transform.localScale = _defaultPlayerScale;
				if ((Object)(object)val2 == (Object)null)
				{
					val2 = val.AddComponent<SpriteRenderer>();
				}
				val2.sprite = cachedTemplateSprite;
				((Renderer)val2).material = new Material(cachedTemplateMaterial);
				try
				{
					PlayerMaterial.SetColors(info.ColorId, (Renderer)val2);
				}
				catch
				{
				}
				flag = true;
				ManualLogSource log3 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(33, 1, out flag2);
				if (flag2)
				{
					((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayViewer] Cached sprite for ");
					((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(info.Name);
				}
				log3.LogInfo(val4);
			}
			if (!flag)
			{
				val.transform.localScale = _defaultPlayerScale;
				if ((Object)(object)val2 == (Object)null)
				{
					val2 = val.AddComponent<SpriteRenderer>();
				}
				val2.sprite = CreateCrewmateBodySprite();
				if (info.ColorId >= 0 && info.ColorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
				{
					val2.color = InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[info.ColorId]);
				}
				else
				{
					val2.color = InteropFix.Cast(info.RealColor);
				}
				GameObject val6 = new GameObject("Visor");
				val6.transform.SetParent(val.transform);
				val6.transform.localPosition = new Vector3(0f, 0f, -0.001f);
				val6.transform.localScale = Vector3.one;
				SpriteRenderer obj6 = val6.AddComponent<SpriteRenderer>();
				obj6.sprite = CreateVisorSprite();
				((Renderer)obj6).sortingOrder = 6;
				ManualLogSource log4 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				BepInExWarningLogInterpolatedStringHandler val7 = new BepInExWarningLogInterpolatedStringHandler(62, 1, out flag2);
				if (flag2)
				{
					((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("[ReplayViewer] Procedural fallback for ");
					((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<string>(info.Name);
					((BepInExLogInterpolatedStringHandler)val7).AppendLiteral(" (no players available)");
				}
				log4.LogWarning(val7);
			}
			((Renderer)val2).sortingOrder = 5;
			GameObject val8 = new GameObject("NameTag");
			val8.transform.SetParent(val.transform);
			val8.transform.localPosition = new Vector3(0f, 0.8f, 0f);
			TextMeshPro obj7 = val8.AddComponent<TextMeshPro>();
			((TMP_Text)obj7).text = info.Name;
			((TMP_Text)obj7).fontSize = 1.5f;
			((TMP_Text)obj7).alignment = (TextAlignmentOptions)514;
			((Graphic)obj7).color = Color.white;
			((TMP_Text)obj7).outlineColor = InteropFix.Cast(Color.black);
			((TMP_Text)obj7).outlineWidth = 0.2f;
			obj7.sortingOrder = 20;
			if (info.IsImpostor)
			{
				GameObject val9 = new GameObject("RoleIndicator");
				val9.transform.SetParent(val.transform);
				val9.transform.localPosition = new Vector3(0.3f, 0.3f, 0f);
				TextMeshPro obj8 = val9.AddComponent<TextMeshPro>();
				((TMP_Text)obj8).text = "<color=#FF4444>★</color>";
				((TMP_Text)obj8).fontSize = 1f;
				obj8.sortingOrder = 21;
			}
			ReplayPuppet replayPuppet = val.AddComponent<ReplayPuppet>();
			replayPuppet.Initialize(info, val2);
			puppets[info.PlayerId] = replayPuppet;
		}
		catch (Exception ex)
		{
			ManualLogSource log5 = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			BepInExErrorLogInterpolatedStringHandler val10 = new BepInExErrorLogInterpolatedStringHandler(35, 1, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val10).AppendLiteral("[ReplayViewer] SpawnPuppet failed: ");
				((BepInExLogInterpolatedStringHandler)val10).AppendFormatted<string>(ex.Message);
			}
			log5.LogError(val10);
		}
	}

	private void InitializeRoutes()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		foreach (ReplayPlayerInfo player in data.Players)
		{
			GameObject val = new GameObject("Route_" + player.Name);
			val.transform.SetParent(puppetRoot.transform);
			LineRenderer val2 = val.AddComponent<LineRenderer>();
			val2.startWidth = 0.1f;
			val2.endWidth = 0.05f;
			((Renderer)val2).material = new Material(Shader.Find("Sprites/Default"));
			Color val3 = InteropFix.Cast(player.RealColor);
			val3.a = 0.6f;
			val2.startColor = val3;
			val2.endColor = new Color(val3.r, val3.g, val3.b, 0.1f);
			val2.positionCount = 0;
			((Renderer)val2).sortingOrder = 3;
			routeLines[player.PlayerId] = val2;
			routePoints[player.PlayerId] = new List<Vector3>();
		}
	}

	private void InitializeUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		GameObject val = new GameObject("ReplayViewer_UI");
		Object.DontDestroyOnLoad((Object)(object)val);
		ui = val.AddComponent<ReplayUI>();
		ui.Initialize(this);
	}

	private Sprite CreateCrewmateBodySprite()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		int w = 64;
		int h = 80;
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)0;
		Color val2 = default(Color);
		val2._002Ector(0f, 0f, 0f, 0f);
		Color white = Color.white;
		Color arg = default(Color);
		arg._002Ector(0.7f, 0.7f, 0.7f, 1f);
		Color[] colors = (Color[])(object)new Color[w * h];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = val2;
		}
		System.Action<int, int, Color> action = delegate(int x, int y, Color c)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			if (x >= 0 && x < w && y >= 0 && y < h)
			{
				colors[y * w + x] = c;
			}
		};
		for (int num = 22; num < 52; num++)
		{
			float num2 = 37f;
			float num3 = 15f;
			float num4 = ((float)num - num2) / num3;
			if (!(num4 * num4 > 1f))
			{
				float num5 = 10f * Mathf.Sqrt(1f - num4 * num4);
				for (int num6 = (int)(13f - num5); num6 < 13; num6++)
				{
					action(num6, num, arg);
				}
			}
		}
		int num7 = 34;
		int num8 = 42;
		for (int num9 = 18; num9 < h; num9++)
		{
			float num10 = (float)(num9 - num8) / 30f;
			float num11 = ((num10 < 0f) ? (22f + num10 * 4f) : (22f - num10 * 6f));
			if (num11 < 4f)
			{
				continue;
			}
			for (int num12 = 0; num12 < w; num12++)
			{
				float num13 = (float)(num12 - num7) / num11;
				if (num13 * num13 <= 1f)
				{
					action(num12, num9, white);
				}
			}
		}
		for (int num14 = 5; num14 < 20; num14++)
		{
			for (int num15 = 22; num15 < 31; num15++)
			{
				action(num15, num14, white);
			}
			for (int num16 = 37; num16 < 46; num16++)
			{
				action(num16, num14, white);
			}
		}
		for (int num17 = 3; num17 < 5; num17++)
		{
			for (int num18 = 23; num18 < 30; num18++)
			{
				action(num18, num17, white);
			}
			for (int num19 = 38; num19 < 45; num19++)
			{
				action(num19, num17, white);
			}
		}
		val.SetPixels(InteropFix.Cast(colors));
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)w, (float)h), new Vector2(0.5f, 0.25f), 48f);
	}

	private Sprite CreateVisorSprite()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		int num = 64;
		int num2 = 80;
		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)0;
		Color val2 = default(Color);
		val2._002Ector(0f, 0f, 0f, 0f);
		Color val3 = default(Color);
		val3._002Ector(0.2f, 0.8f, 0.95f, 1f);
		Color val4 = default(Color);
		val4._002Ector(0.5f, 0.92f, 1f, 1f);
		Color[] array = (Color[])(object)new Color[num * num2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = val2;
		}
		float num3 = 43f;
		float num4 = 53f;
		float num5 = 12f;
		float num6 = 8f;
		for (int j = 0; j < num2; j++)
		{
			for (int k = 0; k < num; k++)
			{
				float num7 = ((float)k - num3) / num5;
				float num8 = ((float)j - num4) / num6;
				if (num7 * num7 + num8 * num8 <= 1f)
				{
					bool flag = num7 > 0.2f && num8 > 0.1f && num7 * num7 + num8 * num8 > 0.4f;
					array[j * num + k] = (flag ? val4 : val3);
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num2), new Vector2(0.5f, 0.25f), 48f);
	}

	private void ApplyPlayerColor(SpriteRenderer rend, Color realColor, int colorId)
	{
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)rend == (Object)null || (Object)(object)((Renderer)rend).material == (Object)null)
		{
			return;
		}
		try
		{
			bool flag = ((Renderer)rend).material.HasProperty("_BodyColor");
			bool flag2 = ((Renderer)rend).material.HasProperty("_BackColor");
			if (colorId >= 0 && colorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length && (flag || flag2))
			{
				if (flag)
				{
					((Renderer)rend).material.SetColor("_BodyColor", InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[colorId]));
				}
				if (flag2)
				{
					((Renderer)rend).material.SetColor("_BackColor", InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.ShadowColors)[colorId]));
				}
				if (((Renderer)rend).material.HasProperty("_VisorColor"))
				{
					((Renderer)rend).material.SetColor("_VisorColor", InteropFix.Cast(Palette.VisorColor));
				}
			}
			else if (colorId >= 0 && colorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
			{
				rend.color = InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[colorId]);
			}
			else
			{
				rend.color = realColor;
			}
		}
		catch
		{
			rend.color = realColor;
		}
	}

	private void Update()
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		if (!isActive || data == null)
		{
			return;
		}
		try
		{
			float num = Mathf.Min(Time.deltaTime, 0.1f);
			if (!isPaused)
			{
				currentTime += num * playbackSpeed;
				if (currentTime >= TotalDuration)
				{
					currentTime = TotalDuration;
					isPaused = true;
				}
			}
			ApplyFrameState(currentTime);
			UpdateCamera();
			if ((Object)(object)ui != (Object)null && ui.ShowRoutes)
			{
				UpdateRoutes();
			}
			else
			{
				HideRoutes();
			}
			if ((Object)(object)ui != (Object)null && ui.ShowHeatmap)
			{
				UpdateHeatmap();
			}
		}
		catch (Exception ex)
		{
			if (Time.frameCount % 120 == 0)
			{
				ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
				bool flag = default(bool);
				BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(29, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ReplayViewer] Update error: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
				}
				log.LogError(val);
			}
		}
	}

	private void ApplyFrameState(float time)
	{
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		if (data == null || data.Frames.Count == 0)
		{
			return;
		}
		int num = FindFrameIndex(time);
		if (num < 0 || num >= data.Frames.Count)
		{
			return;
		}
		ReplayFrame replayFrame = data.Frames[num];
		ReplayFrame replayFrame2 = ((num < data.Frames.Count - 1) ? data.Frames[num + 1] : null);
		float num2 = 0f;
		if (replayFrame2 != null)
		{
			float num3 = replayFrame2.Time - replayFrame.Time;
			if (num3 > 0.001f)
			{
				num2 = Mathf.Clamp01((time - replayFrame.Time) / num3);
			}
		}
		_nextFrameLookup.Clear();
		if (replayFrame2 != null)
		{
			foreach (PlayerState state in replayFrame2.States)
			{
				_nextFrameLookup[state.PlayerId] = state;
			}
		}
		foreach (PlayerState state2 in replayFrame.States)
		{
			if (!puppets.TryGetValue(state2.PlayerId, out var value) || !((Object)(object)value != (Object)null))
			{
				continue;
			}
			Vector2 pos = state2.Position;
			bool faceRight = state2.FaceRight;
			AnimState animState = state2.AnimState;
			if (replayFrame2 != null && _nextFrameLookup.TryGetValue(state2.PlayerId, out var value2))
			{
				pos = Vector2.Lerp(state2.Position, value2.Position, num2);
				faceRight = ((num2 > 0.5f) ? value2.FaceRight : state2.FaceRight);
				animState = state2.AnimState;
			}
			value.UpdateState(pos, faceRight, state2.IsDead, animState);
			if (!state2.IsDead || deadBodies.ContainsKey(state2.PlayerId))
			{
				continue;
			}
			Vector2 position = state2.Position;
			if (data.Events != null)
			{
				foreach (ReplayEvent @event in data.Events)
				{
					if (@event.Type == ReplayEventType.Kill && @event.TargetId == state2.PlayerId)
					{
						position = @event.Position;
						break;
					}
				}
			}
			SpawnDeadBody(state2.PlayerId, position);
		}
		UpdateDeadBodyVisibility(time);
	}

	private void SpawnDeadBody(byte playerId, Vector2 position)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		if (deadBodies.ContainsKey(playerId))
		{
			return;
		}
		ReplayPlayerInfo replayPlayerInfo = data.Players.Find((ReplayPlayerInfo p) => p.PlayerId == playerId);
		if (replayPlayerInfo != null)
		{
			GameObject val = new GameObject("DeadBody_" + replayPlayerInfo.Name);
			val.transform.SetParent(puppetRoot.transform);
			val.transform.position = new Vector3(position.x, position.y, 0f);
			SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
			val2.sprite = CreateDeadBodySprite();
			if (replayPlayerInfo.ColorId >= 0 && replayPlayerInfo.ColorId < ((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors).Length)
			{
				val2.color = InteropFix.Cast(((Il2CppArrayBase<Color32>)(object)Palette.PlayerColors)[replayPlayerInfo.ColorId]);
			}
			else
			{
				val2.color = InteropFix.Cast(replayPlayerInfo.RealColor);
			}
			((Renderer)val2).sortingOrder = 4;
			GameObject val3 = new GameObject("Bone");
			val3.transform.SetParent(val.transform);
			val3.transform.localPosition = new Vector3(0.3f, 0f, -0.01f);
			SpriteRenderer obj = val3.AddComponent<SpriteRenderer>();
			obj.sprite = CreateBoneSprite();
			((Renderer)obj).sortingOrder = 5;
			deadBodies[playerId] = val;
			ManualLogSource log = ((BasePlugin)ModMenuCrewPlugin.Instance).Log;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(41, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[ReplayViewer] Spawned dead body for ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(replayPlayerInfo.Name);
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(" at ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<Vector2>(position);
			}
			log.LogInfo(val4);
		}
	}

	private Sprite CreateDeadBodySprite()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		int num = 80;
		int num2 = 40;
		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)0;
		Color val2 = default(Color);
		val2._002Ector(0f, 0f, 0f, 0f);
		Color white = Color.white;
		Color[] array = (Color[])(object)new Color[num * num2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = val2;
		}
		int num3 = 40;
		int num4 = 20;
		int num5 = 35;
		int num6 = 15;
		for (int j = 0; j < num2; j++)
		{
			for (int k = 0; k < num; k++)
			{
				float num7 = (float)(k - num3) / (float)num5;
				float num8 = (float)(j - num4) / (float)num6;
				if (num7 * num7 + num8 * num8 <= 1f)
				{
					array[j * num + k] = white;
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num2), new Vector2(0.5f, 0.5f), 48f);
	}

	private Sprite CreateBoneSprite()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		int num = 16;
		int num2 = 16;
		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)0;
		Color val2 = default(Color);
		val2._002Ector(0f, 0f, 0f, 0f);
		Color val3 = default(Color);
		val3._002Ector(0.9f, 0.85f, 0.8f, 1f);
		Color[] array = (Color[])(object)new Color[num * num2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = val2;
		}
		for (int j = 0; j < num; j++)
		{
			array[j * num + j] = val3;
			array[j * num + (num - 1 - j)] = val3;
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num2), new Vector2(0.5f, 0.5f), 32f);
	}

	private void UpdateDeadBodyVisibility(float time)
	{
		if (data == null || data.Events == null)
		{
			return;
		}
		foreach (KeyValuePair<byte, GameObject> deadBody in deadBodies)
		{
			byte key = deadBody.Key;
			GameObject value = deadBody.Value;
			if ((Object)(object)value == (Object)null)
			{
				continue;
			}
			float num = 0f;
			foreach (ReplayEvent @event in data.Events)
			{
				if (@event.Type == ReplayEventType.Kill && @event.TargetId == key)
				{
					num = @event.Time;
					break;
				}
			}
			value.SetActive(time >= num);
		}
	}

	private int FindFrameIndex(float time)
	{
		var frames = data.Frames;
		if (frames.Count == 0)
		{
			return -1;
		}
		if (lastFrameIndex >= 0 && lastFrameIndex < frames.Count - 1 && frames[lastFrameIndex].Time <= time && frames[lastFrameIndex + 1].Time > time)
		{
			return lastFrameIndex;
		}
		int num = 0;
		int num2 = frames.Count - 1;
		while (num < num2)
		{
			int num3 = (num + num2 + 1) / 2;
			if (frames[num3].Time <= time)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		lastFrameIndex = num;
		return num;
	}

	private void UpdateCamera()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)replayCamera == (Object)null))
		{
			if (followingPlayerId.HasValue && puppets.TryGetValue(followingPlayerId.Value, out var value) && (Object)(object)value != (Object)null && (Object)(object)((Component)value).transform != (Object)null)
			{
				Vector3 position = ((Component)value).transform.position;
				position.z = -10f;
				((Component)replayCamera).transform.position = Vector3.Lerp(((Component)replayCamera).transform.position, position, Mathf.Min(Time.deltaTime, 0.1f) * 5f);
			}
			replayCamera.orthographicSize = cameraZoom;
		}
	}

	private void UpdateRoutes()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (data == null)
		{
			return;
		}
		int num = FindFrameIndex(currentTime);
		if (num < 0)
		{
			return;
		}
		Vector3 val = default(Vector3);
		foreach (PlayerState state in data.Frames[num].States)
		{
			if (!routePoints.ContainsKey(state.PlayerId) || !routeLines.ContainsKey(state.PlayerId))
			{
				continue;
			}
			List<Vector3> list = routePoints[state.PlayerId];
			LineRenderer obj = routeLines[state.PlayerId];
			val._002Ector(state.Position.x, state.Position.y, 0f);
			if (list.Count == 0 || Vector3.Distance(list[list.Count - 1], val) > 0.2f)
			{
				list.Add(val);
				if (list.Count > 500)
				{
					list.RemoveAt(0);
				}
			}
			if (!_routeArrayCache.TryGetValue(state.PlayerId, out var value) || value.Length < list.Count)
			{
				value = (Vector3[])(object)new Vector3[Mathf.Max(list.Count, 500)];
				_routeArrayCache[state.PlayerId] = value;
			}
			list.CopyTo(InteropFix.Cast(value));
			obj.positionCount = list.Count;
			obj.SetPositions(InteropFix.Cast(value));
		}
	}

	private void HideRoutes()
	{
		foreach (LineRenderer value in routeLines.Values)
		{
			if (InteropFix.Cast((Object)(object)value))
			{
				value.positionCount = 0;
			}
		}
	}

	private void UpdateHeatmap()
	{
	}

	private void SetLocalPlayerVisibility(bool visible)
	{
		if (!InteropFix.Cast((Object)(object)PlayerControl.LocalPlayer))
		{
			return;
		}
		try
		{
			foreach (Renderer componentsInChild in ((Component)PlayerControl.LocalPlayer).GetComponentsInChildren<Renderer>(true))
			{
				componentsInChild.enabled = visible;
			}
		}
		catch
		{
		}
		try
		{
			foreach (TextMeshPro componentsInChild2 in ((Component)PlayerControl.LocalPlayer).GetComponentsInChildren<TextMeshPro>(true))
			{
				((Behaviour)componentsInChild2).enabled = visible;
			}
		}
		catch
		{
		}
		try
		{
			foreach (Canvas componentsInChild3 in ((Component)PlayerControl.LocalPlayer).GetComponentsInChildren<Canvas>(true))
			{
				((Behaviour)componentsInChild3).enabled = visible;
			}
		}
		catch
		{
		}
	}

	private void HideRealPlayers()
	{
		try
		{
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (InteropFix.Cast((Object)(object)current) && InteropFix.Cast((Object)(object)((Component)current).gameObject))
				{
					((Component)current).gameObject.SetActive(false);
				}
			}
		}
		catch
		{
		}
	}

	private void ShowRealPlayers()
	{
		try
		{
			var enumerator = PlayerControl.AllPlayerControls.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PlayerControl current = enumerator.Current;
				if (InteropFix.Cast((Object)(object)current) && InteropFix.Cast((Object)(object)((Component)current).gameObject))
				{
					((Component)current).gameObject.SetActive(true);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (InteropFix.Cast((Object)(object)PlayerControl.LocalPlayer) && InteropFix.Cast((Object)(object)DestroyableSingleton<HudManager>.Instance))
			{
				DestroyableSingleton<HudManager>.Instance.SetHudActive(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer.Data.Role, !PlayerControl.LocalPlayer.Data.IsDead);
			}
		}
		catch
		{
		}
	}
}













