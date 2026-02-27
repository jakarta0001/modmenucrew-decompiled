using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

public class ReplayEvent
{
	public float Time;

	public ReplayEventType Type;

	public byte PlayerId;

	public byte TargetId;

	public Vector2 Position;

	public string Description;

	public string GetIcon()
	{
		return Type switch
		{
			ReplayEventType.Kill => "\ud83d\udc80", 
			ReplayEventType.Meeting => "\ud83d\udce2", 
			ReplayEventType.Vote => "✋", 
			ReplayEventType.Exiled => "\ud83d\ude80", 
			ReplayEventType.Sabotage => "⚠\ufe0f", 
			ReplayEventType.SabotageFixed => "\ud83d\udd27", 
			ReplayEventType.TaskComplete => "✅", 
			ReplayEventType.Vent => "\ud83d\udd73\ufe0f", 
			ReplayEventType.Shapeshift => "\ud83c\udfad", 
			ReplayEventType.Disconnect => "\ud83d\udcf4", 
			ReplayEventType.GameStart => "\ud83c\udfae", 
			ReplayEventType.GameEnd => "\ud83c\udfc1", 
			ReplayEventType.Report => "\ud83d\udea8", 
			_ => "❓", 
		};
	}

	public Color GetColor()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		return (Color)(Type switch
		{
			ReplayEventType.Kill => new Color(1f, 0.2f, 0.2f), 
			ReplayEventType.Meeting => new Color(1f, 0.9f, 0.2f), 
			ReplayEventType.Vote => new Color(0.4f, 0.8f, 1f), 
			ReplayEventType.Exiled => new Color(1f, 0.5f, 0f), 
			ReplayEventType.Sabotage => new Color(1f, 0.3f, 0.3f), 
			ReplayEventType.SabotageFixed => new Color(0.3f, 1f, 0.3f), 
			ReplayEventType.TaskComplete => new Color(0.3f, 1f, 0.5f), 
			ReplayEventType.Vent => new Color(0.5f, 0.5f, 0.5f), 
			ReplayEventType.Shapeshift => new Color(0.8f, 0.4f, 1f), 
			ReplayEventType.Report => new Color(0.2f, 0.4f, 1f), 
			_ => Color.white, 
		});
	}
}
