using System;
using UnityEngine;

namespace ModMenuCrew.Features;

public class GameEvent
{
	public DateTime Time { get; set; }

	public GameEventType Type { get; set; }

	public string Message { get; set; }

	public string PlayerName { get; set; }

	public string RoleName { get; set; }

	public string Location { get; set; }

	public Color Color { get; set; }

	public GameEvent(GameEventType type, string message, string playerName = "", string roleName = "", string location = "")
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		Time = DateTime.Now;
		Type = type;
		Message = message;
		PlayerName = playerName;
		RoleName = roleName;
		Location = location;
		Color = GetColorForType(type);
	}

	private Color GetColorForType(GameEventType type)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		return (Color)(type switch
		{
			GameEventType.Kill => new Color(1f, 0.2f, 0.2f), 
			GameEventType.Task => new Color(0.2f, 1f, 0.2f), 
			GameEventType.Vent => new Color(0.5f, 0.5f, 1f), 
			GameEventType.Sabotage => new Color(1f, 0.5f, 0f), 
			GameEventType.Report => new Color(1f, 1f, 0.2f), 
			GameEventType.Meeting => new Color(0.8f, 0.8f, 0.8f), 
			GameEventType.Vote => new Color(0.6f, 0.4f, 1f), 
			GameEventType.Shapeshift => new Color(1f, 0.2f, 1f), 
			_ => Color.white, 
		});
	}
}
