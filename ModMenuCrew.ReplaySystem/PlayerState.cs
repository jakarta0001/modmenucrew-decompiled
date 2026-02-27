using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

public class PlayerState
{
	public byte PlayerId;

	public Vector2 Position;

	public bool FaceRight;

	public bool IsDead;

	public bool IsInVent;

	public AnimState AnimState;
}
