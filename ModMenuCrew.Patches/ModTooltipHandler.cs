using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModMenuCrew.Patches;

public class ModTooltipHandler : MonoBehaviour
{
	private TextMeshPro _textComponent;

	private Camera _mainCamera;

	private GameObject _tooltipObject;

	private TextMeshProUGUI _tooltipText;

	private RectTransform _tooltipRect;

	private float _lastUpdateTime;

	private const float UPDATE_INTERVAL = 0.05f;

	private int _lastLinkIndex = -1;

	private static readonly Vector3 TOOLTIP_OFFSET = new Vector3(15f, 15f, 0f);

	public ModTooltipHandler(System.IntPtr ptr)
		: base(ptr)
	{
	}

	public void Setup(TextMeshPro textComponent)
	{
		_textComponent = textComponent;
		_mainCamera = null;
	}

	private void Update()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_textComponent == (UnityEngine.Object)null || !((Component)this).gameObject.activeInHierarchy || Time.time - _lastUpdateTime < 0.05f)
		{
			return;
		}
		_lastUpdateTime = Time.time;
		if ((UnityEngine.Object)(object)_mainCamera == (UnityEngine.Object)null)
		{
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			_mainCamera = ((instance != null) ? instance.UICamera : null) ?? Camera.main;
		}
		if ((UnityEngine.Object)(object)_mainCamera == (UnityEngine.Object)null)
		{
			return;
		}
		int num = TMP_TextUtilities.FindIntersectingLink((TMP_Text)(object)_textComponent, Input.mousePosition, _mainCamera);
		if (num == _lastLinkIndex && num == -1)
		{
			return;
		}
		_lastLinkIndex = num;
		if (num != -1)
		{
			TMP_TextInfo textInfo = ((TMP_Text)_textComponent).textInfo;
			if (((textInfo != null) ? textInfo.linkInfo : null) != null && num < ((TMP_Text)_textComponent).textInfo.linkCount)
			{
				TMP_LinkInfo val = ((Il2CppArrayBase<TMP_LinkInfo>)(object)((TMP_Text)_textComponent).textInfo.linkInfo)[num];
				ShowTooltip(val.GetLinkID());
				return;
			}
		}
		HideTooltip();
	}

	private void ShowTooltip(string text)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(text))
		{
			if ((UnityEngine.Object)(object)_tooltipObject == (UnityEngine.Object)null)
			{
				CreateTooltipObject();
			}
			if (!((UnityEngine.Object)(object)_tooltipObject == (UnityEngine.Object)null))
			{
				_tooltipObject.SetActive(true);
				((TMP_Text)_tooltipText).text = text;
				((Transform)_tooltipRect).position = Input.mousePosition + TOOLTIP_OFFSET;
			}
		}
	}

	private void HideTooltip()
	{
		if ((UnityEngine.Object)(object)_tooltipObject != (UnityEngine.Object)null && _tooltipObject.activeSelf)
		{
			_tooltipObject.SetActive(false);
		}
	}

	private void CreateTooltipObject()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		Canvas val = null;
		int num = int.MinValue;
		foreach (Canvas item in UnityEngine.Object.FindObjectsOfType<Canvas>())
		{
			if (item.sortingOrder > num)
			{
				num = item.sortingOrder;
				val = item;
			}
		}
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			Debug.LogError(InteropFix.Cast("[ModTooltip] No Canvas found."));
			return;
		}
		_tooltipObject = new GameObject("ModTooltip");
		_tooltipObject.transform.SetParent(((Component)val).transform, false);
		_tooltipRect = _tooltipObject.AddComponent<RectTransform>();
		_tooltipRect.pivot = new Vector2(0f, 1f);
		_tooltipObject.AddComponent<LayoutElement>().minWidth = 100f;
		ContentSizeFitter obj = _tooltipObject.AddComponent<ContentSizeFitter>();
		obj.horizontalFit = (ContentSizeFitter.FitMode)2;
		obj.verticalFit = (ContentSizeFitter.FitMode)2;
		((Graphic)_tooltipObject.AddComponent<Image>()).color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
		GameObject val2 = new GameObject("TooltipText");
		val2.transform.SetParent((Transform)(object)_tooltipRect, false);
		_tooltipText = val2.AddComponent<TextMeshProUGUI>();
		((TMP_Text)_tooltipText).fontSize = 14f;
		((Graphic)_tooltipText).color = Color.white;
		((TMP_Text)_tooltipText).alignment = (TextAlignmentOptions)513;
		((TMP_Text)_tooltipText).margin = new Vector4(10f, 6f, 10f, 6f);
		RectTransform component = ((Component)_tooltipText).GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.sizeDelta = Vector2.zero;
		_tooltipObject.SetActive(false);
	}

	private void OnDestroy()
	{
		if ((UnityEngine.Object)(object)_tooltipObject != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_tooltipObject);
			_tooltipObject = null;
		}
		_textComponent = null;
		_mainCamera = null;
	}
}



