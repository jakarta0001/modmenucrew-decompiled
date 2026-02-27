using System.Collections.Generic;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew.Features;

public static class EventLogger
{
	private static readonly List<GameEvent> _filteredCache = new List<GameEvent>();

	public static List<GameEvent> Events { get; private set; } = new List<GameEvent>();

	public static int MaxEvents { get; set; } = 100;

	public static bool IsEnabled => CheatConfig.EventLoggerEnabled?.Value ?? false;

	public static bool ShowKills { get; set; } = true;

	public static bool ShowTasks { get; set; } = true;

	public static bool ShowVents { get; set; } = true;

	public static bool ShowSabotages { get; set; } = true;

	public static bool ShowReports { get; set; } = true;

	public static bool ShowVotes { get; set; } = true;

	public static bool ShowUI { get; set; } = false;

	public static Vector2 ScrollPosition { get; set; } = Vector2.zero;

	public static void Log(GameEventType type, string message, string playerName = "", string roleName = "", string location = "")
	{
		if (IsEnabled)
		{
			GameEvent gameEvent = new GameEvent(type, message, playerName, roleName, location);
			Events.Add(gameEvent);
			if (Events.Count > MaxEvents)
			{
				int count = Events.Count - MaxEvents;
				Events.RemoveRange(0, count);
			}
			Debug.Log(InteropFix.Cast($"[EventLogger] {gameEvent.Type}: {gameEvent.Message}"));
		}
	}

	public static void LogKill(PlayerControl killer, PlayerControl victim, string location = "")
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		if (!IsEnabled || !ShowKills)
		{
			return;
		}
		object obj;
		if (killer == null)
		{
			obj = null;
		}
		else
		{
			NetworkedPlayerInfo data = killer.Data;
			obj = ((data != null) ? data.PlayerName : null);
		}
		if (obj == null)
		{
			obj = "Unknown";
		}
		string text = (string)obj;
		object obj2;
		if (killer == null)
		{
			obj2 = null;
		}
		else
		{
			NetworkedPlayerInfo data2 = killer.Data;
			if (data2 == null)
			{
				obj2 = null;
			}
			else
			{
				RoleBehaviour role = data2.Role;
				obj2 = ((role != null) ? ((object)role.Role/*cast due to .constrained prefix*/).ToString() : null);
			}
		}
		if (obj2 == null)
		{
			obj2 = "Unknown";
		}
		string text2 = (string)obj2;
		object obj3;
		if (victim == null)
		{
			obj3 = null;
		}
		else
		{
			NetworkedPlayerInfo data3 = victim.Data;
			obj3 = ((data3 != null) ? data3.PlayerName : null);
		}
		if (obj3 == null)
		{
			obj3 = "Unknown";
		}
		string value = (string)obj3;
		if (string.IsNullOrEmpty(location))
		{
			HudManager instance = DestroyableSingleton<HudManager>.Instance;
			object obj4;
			if (instance == null)
			{
				obj4 = null;
			}
			else
			{
				RoomTracker roomTracker = instance.roomTracker;
				obj4 = ((roomTracker != null) ? roomTracker.LastRoom : null);
			}
			if ((Object)obj4 != (Object)null)
			{
				location = ((object)DestroyableSingleton<HudManager>.Instance.roomTracker.LastRoom.RoomId/*cast due to .constrained prefix*/).ToString();
			}
		}
		Log(GameEventType.Kill, $"{text} ({text2}) killed {value}", text, text2, location);
	}

	public static void LogTask(PlayerControl player, string taskName)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (!IsEnabled || !ShowTasks)
		{
			return;
		}
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			NetworkedPlayerInfo data = player.Data;
			obj = ((data != null) ? data.PlayerName : null);
		}
		if (obj == null)
		{
			obj = "Unknown";
		}
		string text = (string)obj;
		object obj2;
		if (player == null)
		{
			obj2 = null;
		}
		else
		{
			NetworkedPlayerInfo data2 = player.Data;
			if (data2 == null)
			{
				obj2 = null;
			}
			else
			{
				RoleBehaviour role = data2.Role;
				obj2 = ((role != null) ? ((object)role.Role/*cast due to .constrained prefix*/).ToString() : null);
			}
		}
		if (obj2 == null)
		{
			obj2 = "Unknown";
		}
		string roleName = (string)obj2;
		Log(GameEventType.Task, text + " completed " + taskName, text, roleName);
	}

	public static void LogVent(PlayerControl player, int ventId, bool entering)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (!IsEnabled || !ShowVents)
		{
			return;
		}
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			NetworkedPlayerInfo data = player.Data;
			obj = ((data != null) ? data.PlayerName : null);
		}
		if (obj == null)
		{
			obj = "Unknown";
		}
		string text = (string)obj;
		object obj2;
		if (player == null)
		{
			obj2 = null;
		}
		else
		{
			NetworkedPlayerInfo data2 = player.Data;
			if (data2 == null)
			{
				obj2 = null;
			}
			else
			{
				RoleBehaviour role = data2.Role;
				obj2 = ((role != null) ? ((object)role.Role/*cast due to .constrained prefix*/).ToString() : null);
			}
		}
		if (obj2 == null)
		{
			obj2 = "Unknown";
		}
		string text2 = (string)obj2;
		string value = (entering ? "entered" : "exited");
		Log(GameEventType.Vent, $"{text} ({text2}) {value} vent {ventId}", text, text2);
	}

	public static void LogSabotage(SystemTypes system, PlayerControl player = null)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (IsEnabled && ShowSabotages)
		{
			object obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = player.Data;
				obj = ((data != null) ? data.PlayerName : null);
			}
			if (obj == null)
			{
				obj = "Someone";
			}
			string text = (string)obj;
			Log(GameEventType.Sabotage, $"{text} sabotaged {system}", text);
		}
	}

	public static void LogReport(PlayerControl reporter, PlayerControl body)
	{
		if (IsEnabled && ShowReports)
		{
			object obj;
			if (reporter == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = reporter.Data;
				obj = ((data != null) ? data.PlayerName : null);
			}
			if (obj == null)
			{
				obj = "Unknown";
			}
			string text = (string)obj;
			object obj2;
			if (body == null)
			{
				obj2 = null;
			}
			else
			{
				NetworkedPlayerInfo data2 = body.Data;
				obj2 = ((data2 != null) ? data2.PlayerName : null);
			}
			if (obj2 == null)
			{
				obj2 = "Unknown";
			}
			string text2 = (string)obj2;
			Log(GameEventType.Report, text + " reported " + text2 + "'s body", text);
		}
	}

	public static void LogVote(PlayerControl voter, PlayerControl target)
	{
		if (IsEnabled && ShowVotes)
		{
			object obj;
			if (voter == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = voter.Data;
				obj = ((data != null) ? data.PlayerName : null);
			}
			if (obj == null)
			{
				obj = "Unknown";
			}
			string text = (string)obj;
			object obj2;
			if (target == null)
			{
				obj2 = null;
			}
			else
			{
				NetworkedPlayerInfo data2 = target.Data;
				obj2 = ((data2 != null) ? data2.PlayerName : null);
			}
			if (obj2 == null)
			{
				obj2 = "Skip";
			}
			string text2 = (string)obj2;
			Log(GameEventType.Vote, text + " voted for " + text2, text);
		}
	}

	public static void Clear()
	{
		Events.Clear();
	}

	public static List<GameEvent> GetFilteredEvents()
	{
		_filteredCache.Clear();
		foreach (GameEvent @event in Events)
		{
			if (@event.Type switch
			{
				GameEventType.Kill => ShowKills, 
				GameEventType.Task => ShowTasks, 
				GameEventType.Vent => ShowVents, 
				GameEventType.Sabotage => ShowSabotages, 
				GameEventType.Report => ShowReports, 
				GameEventType.Vote => ShowVotes, 
				_ => true, 
			})
			{
				_filteredCache.Add(@event);
			}
		}
		return _filteredCache;
	}
}

