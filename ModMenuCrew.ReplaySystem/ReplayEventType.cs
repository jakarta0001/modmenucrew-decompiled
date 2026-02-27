namespace ModMenuCrew.ReplaySystem;

public enum ReplayEventType : byte
{
	None,
	Kill,
	Meeting,
	Vote,
	Exiled,
	Sabotage,
	SabotageFixed,
	TaskComplete,
	Vent,
	Shapeshift,
	Revive,
	Disconnect,
	GameStart,
	GameEnd,
	Report
}
