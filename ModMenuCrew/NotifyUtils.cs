using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using UnityEngine;
using UnityEngine.Audio;

namespace ModMenuCrew;

public static class NotifyUtils
{
	private struct ActiveEntry
	{
		public LobbyNotificationMessage Msg;

		public string BaseText;

		public float FirstTime;

		public int Count;
	}

	private const string PRE_SUCCESS = "<b><color=#00FF88>[OK]</color></b> ";

	private const string PRE_ERROR = "<b><color=#FF4444>[X]</color></b> ";

	private const string PRE_WARNING = "<b><color=#FFAA00>[!]</color></b> ";

	private const string PRE_INFO = "<b><color=#44BBFF>[i]</color></b> ";

	private const string PRE_SABOTAGE = "<b><color=#FF5555>[SAB]</color></b> ";

	private const string PRE_DOOR = "<b><color=#FF8800>[DOOR]</color></b> ";

	private const string PRE_REPAIR = "<b><color=#44FF88>[FIX]</color></b> ";

	private static readonly Color MOD_NOTIFY_COLOR = new Color(0.45f, 0.85f, 1f, 1f);

	private const float GROUP_WINDOW_SEC = 5f;

	private const int RATE_LIMIT_PER_SEC = 4;

	private const int MAX_PENDING = 12;

	private const int FLUSH_PER_FRAME = 3;

	private const int MAX_TRACKED = 10;

	private static readonly ActiveEntry[] _active = new ActiveEntry[10];

	private static int _activeCount;

	private static float _rateWindowStart;

	private static int _rateCount;

	private static readonly Queue<string> _pending = new Queue<string>();

	private const int MAX_MOD_MESSAGES = 8;

	private static readonly List<LobbyNotificationMessage> _modMessages = new List<LobbyNotificationMessage>();

	public static void Success(string msg)
	{
		Show("<b><color=#00FF88>[OK]</color></b> " + msg);
	}

	public static void Error(string msg)
	{
		Show("<b><color=#FF4444>[X]</color></b> " + msg);
	}

	public static void Warning(string msg)
	{
		Show("<b><color=#FFAA00>[!]</color></b> " + msg);
	}

	public static void Info(string msg)
	{
		Show("<b><color=#44BBFF>[i]</color></b> " + msg);
	}

	public static void Sabotage(string msg)
	{
		Show("<b><color=#FF5555>[SAB]</color></b> " + msg);
	}

	public static void Door(string msg)
	{
		Show("<b><color=#FF8800>[DOOR]</color></b> " + msg);
	}

	public static void Repair(string msg)
	{
		Show("<b><color=#44FF88>[FIX]</color></b> " + msg);
	}

	public static void Show(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		PruneActive(unscaledTime);
		for (int i = 0; i < _activeCount; i++)
		{
			if (!(_active[i].BaseText == message) || !(unscaledTime - _active[i].FirstTime < 5f))
			{
				continue;
			}
			_active[i].Count++;
			try
			{
				if ((Object)(object)_active[i].Msg != (Object)null)
				{
					string text = message + $" <size=80%><color=#AAAAAA>(x{_active[i].Count})</color></size>";
					_active[i].Msg.UpdateMessage(text);
				}
				return;
			}
			catch
			{
				return;
			}
		}
		if (unscaledTime - _rateWindowStart > 1f)
		{
			_rateWindowStart = unscaledTime;
			_rateCount = 0;
		}
		if (_rateCount >= 4)
		{
			Enqueue(message);
			return;
		}
		_rateCount++;
		if (!TryDeliver(message, unscaledTime))
		{
			Enqueue(message);
		}
	}

	public static void FlushPending()
	{
		if (_pending.Count == 0)
		{
			return;
		}
		HudManager instance = DestroyableSingleton<HudManager>.Instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)instance.Notifier == (Object)null)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		int num = Math.Min(_pending.Count, 3);
		for (int i = 0; i < num; i++)
		{
			try
			{
				DeliverCustom(instance.Notifier, _pending.Dequeue(), unscaledTime);
			}
			catch
			{
			}
		}
	}

	private static bool TryDeliver(string message, float now)
	{
		try
		{
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			if ((Object)(object)instance == (Object)null || (Object)(object)instance.Notifier == (Object)null)
			{
				return false;
			}
			DeliverCustom(instance.Notifier, message, now);
			FlushPending();
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[NotifyUtils] Delivery failed: " + ex.Message));
			return false;
		}
	}

	private static void DeliverCustom(NotificationPopper popper, string message, float now)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		LobbyNotificationMessage newMsg = Object.Instantiate<LobbyNotificationMessage>(popper.notificationMessageOrigin, Vector3.zero, Quaternion.identity, ((Component)popper).transform);
		((Component)newMsg).transform.localPosition = new Vector3(0f, 0f, -2f);
		newMsg.SetUp(message, popper.settingsChangeSprite, MOD_NOTIFY_COLOR, InteropFix.Cast((Action)delegate
		{
			RemoveActive(newMsg);
			_modMessages.Remove(newMsg);
		}));
		try
		{
			SoundManager.Instance.PlaySoundImmediate(popper.settingsChangeSound, false, 1f, 1f, (AudioMixerGroup)null);
		}
		catch
		{
		}
		ShiftModMessages(popper.spacingY);
		EnforceModMessageLimit();
		_modMessages.Add(newMsg);
		TrackActive(newMsg, message, now);
	}

	private static void ShiftModMessages(float spacingY)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		for (int num = _modMessages.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)_modMessages[num] == (Object)null)
			{
				_modMessages.RemoveAt(num);
			}
			else
			{
				Transform transform = ((Component)_modMessages[num]).transform;
				transform.localPosition += new Vector3(0f, spacingY, 0f);
			}
		}
	}

	private static void EnforceModMessageLimit()
	{
		while (_modMessages.Count >= 8)
		{
			if ((Object)(object)_modMessages[0] != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)_modMessages[0]).gameObject);
			}
			_modMessages.RemoveAt(0);
		}
	}

	private static void TrackActive(LobbyNotificationMessage msg, string baseText, float now)
	{
		if (_activeCount >= 10)
		{
			for (int i = 0; i < _activeCount - 1; i++)
			{
				_active[i] = _active[i + 1];
			}
			_activeCount--;
		}
		_active[_activeCount] = new ActiveEntry
		{
			Msg = msg,
			BaseText = baseText,
			FirstTime = now,
			Count = 1
		};
		_activeCount++;
	}

	private static void RemoveActive(LobbyNotificationMessage msg)
	{
		for (int i = 0; i < _activeCount; i++)
		{
			if ((Object)(object)_active[i].Msg == (Object)(object)msg)
			{
				for (int j = i; j < _activeCount - 1; j++)
				{
					_active[j] = _active[j + 1];
				}
				_active[_activeCount - 1] = default(ActiveEntry);
				_activeCount--;
				break;
			}
		}
	}

	private static void PruneActive(float now)
	{
		for (int num = _activeCount - 1; num >= 0; num--)
		{
			bool flag = (Object)(object)_active[num].Msg == (Object)null;
			if (!flag)
			{
				try
				{
					flag = (Object)(object)((Component)_active[num].Msg).gameObject == (Object)null;
				}
				catch
				{
					flag = true;
				}
			}
			bool flag2 = now - _active[num].FirstTime > 15f;
			if (flag || flag2)
			{
				for (int i = num; i < _activeCount - 1; i++)
				{
					_active[i] = _active[i + 1];
				}
				_active[_activeCount - 1] = default(ActiveEntry);
				_activeCount--;
			}
		}
	}

	private static void Enqueue(string message)
	{
		if (_pending.Count < 12)
		{
			_pending.Enqueue(message);
		}
	}

	public static Texture2D MakeTex(int width, int height, Color color)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		Texture2D val = new Texture2D(width, height);
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return val;
	}
}


