using HarmonyLib;

namespace ModMenuCrew.Features;

[HarmonyPatch(typeof(MeetingHud), "CastVote")]
public static class VoteLogPatch
{
	public static void Postfix(byte srcPlayerId, byte suspectPlayerId)
	{
		try
		{
			GameData instance = GameData.Instance;
			NetworkedPlayerInfo obj = ((instance != null) ? instance.GetPlayerById(srcPlayerId) : null);
			string text = ((obj != null) ? obj.PlayerName : null) ?? $"Player {srcPlayerId}";
			string text2;
			switch (suspectPlayerId)
			{
			case 253:
				text2 = "Skip";
				break;
			case byte.MaxValue:
				text2 = "No Vote";
				break;
			default:
			{
				GameData instance2 = GameData.Instance;
				object obj2;
				if (instance2 == null)
				{
					obj2 = null;
				}
				else
				{
					NetworkedPlayerInfo playerById = instance2.GetPlayerById(suspectPlayerId);
					obj2 = ((playerById != null) ? playerById.PlayerName : null);
				}
				if (obj2 == null)
				{
					obj2 = $"Player {suspectPlayerId}";
				}
				text2 = (string)obj2;
				break;
			}
			}
			EventLogger.Log(GameEventType.Vote, text + " voted for " + text2, text);
		}
		catch
		{
		}
	}
}
