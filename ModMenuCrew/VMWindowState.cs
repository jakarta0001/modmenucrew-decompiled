using System.Collections.Generic;
using UnityEngine;

namespace ModMenuCrew;

public class VMWindowState
{
	public Rect WindowRect;

	public float MinWidth = 450f;

	public float MinHeight = 350f;

	public float MaxHeight = 800f;

	public bool IsDragging;

	public bool IsResizing;

	public bool IsMinimized;

	public Vector2 DragOffset;

	public Vector2 ResizeStartMouse;

	public Rect ResizeStartRect;

	public float FadeAlpha;

	public float FadeStartTime = -1f;

	public bool FadeComplete;

	public float CachedPulse;

	public float ResizeHintStart = -1f;

	public bool ResizeHintShown;

	public int SelectedTab;

	public Vector2 SidebarScroll = Vector2.zero;

	public Vector2 ContentScroll = Vector2.zero;

	public Vector2 MainScroll = Vector2.zero;

	public Dictionary<string, bool> SectionStates = new Dictionary<string, bool>();

	public Dictionary<string, Vector2> ScrollStates = new Dictionary<string, Vector2>();

	public Rect CachedHeaderRect;

	public Rect CachedContentRect;

	public Rect CachedResizeHandle;

	public string WindowId;

	public VMWindowState(string id)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		WindowId = id;
		TryLoadPosition();
	}

	public void TryLoadPosition()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = "GhostUI_" + WindowId;
			if (PlayerPrefs.HasKey(text + "_x"))
			{
				WindowRect = new Rect(PlayerPrefs.GetFloat(text + "_x", 100f), PlayerPrefs.GetFloat(text + "_y", 100f), PlayerPrefs.GetFloat(text + "_w", MinWidth), PlayerPrefs.GetFloat(text + "_h", MinHeight));
				if (Screen.width > 0 && Screen.height > 0)
				{
						WindowRect.x = Mathf.Clamp(WindowRect.x, 0f, Mathf.Max(0f, (float)Screen.width - MinWidth));
						WindowRect.y = Mathf.Clamp(WindowRect.y, 0f, (float)Mathf.Max(0, Screen.height - 80));
				}
			}
		}
		catch
		{
		}
	}

	public void SavePosition()
	{
		try
		{
			string text = "GhostUI_" + WindowId;
			PlayerPrefs.SetFloat(text + "_x", WindowRect.x);
			PlayerPrefs.SetFloat(text + "_y", WindowRect.y);
			PlayerPrefs.SetFloat(text + "_w", WindowRect.width);
			PlayerPrefs.SetFloat(text + "_h", WindowRect.height);
			PlayerPrefs.Save();
		}
		catch
		{
		}
	}

	public bool IsSectionExpanded(string id)
	{
		if (!SectionStates.ContainsKey(id))
		{
			SectionStates[id] = true;
		}
		return SectionStates[id];
	}

	public void ToggleSection(string id)
	{
		SectionStates[id] = !IsSectionExpanded(id);
	}

	public Vector2 GetScroll(string id)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!ScrollStates.ContainsKey(id))
		{
			ScrollStates[id] = Vector2.zero;
		}
		return ScrollStates[id];
	}

	public void SetScroll(string id, Vector2 pos)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		ScrollStates[id] = pos;
	}
}


