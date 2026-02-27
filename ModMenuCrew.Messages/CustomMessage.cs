using System.Collections.Generic;
using System.Linq;
using Hazel;
using Il2CppSystem;
using InnerNet;
using UnityEngine;

namespace ModMenuCrew.Messages;

public class CustomMessage
{
	public byte Tag { get; set; }

	public int SenderId { get; set; }

	public string SenderName { get; set; }

	public string Content { get; set; }

	public System.DateTime Timestamp { get; set; }

	public MessageType Type { get; set; }

	public CustomMessage(byte tag, int senderId, string senderName, string content, MessageType type)
	{
		Tag = tag;
		SenderId = senderId;
		SenderName = senderName;
		Content = content;
		Timestamp = System.DateTime.UtcNow;
		Type = type;
	}

	public void Serialize(MessageWriter writer)
	{
		writer.Write(Tag);
		writer.WritePacked(SenderId);
		writer.Write(SenderName ?? "");
		writer.Write(Content ?? "");
		writer.Write((float)Timestamp.ToBinary());
		writer.Write((byte)Type);
	}

	public static CustomMessage Deserialize(MessageReader reader)
	{
		return new CustomMessage(reader.ReadByte(), reader.ReadPackedInt32(), reader.ReadString(), reader.ReadString(), (MessageType)reader.ReadByte())
		{
			Timestamp = System.DateTime.FromBinary((long)reader.ReadUInt64())
		};
	}

	public void SendBypass()
	{
		if (!((Object)(object)AmongUsClient.Instance == (Object)null) && ((InnerNetClient)AmongUsClient.Instance).AmConnected && !((Object)(object)PlayerControl.LocalPlayer == (Object)null))
		{
			MessageWriter val = ((InnerNetClient)AmongUsClient.Instance).StartRpcImmediately(((InnerNetObject)PlayerControl.LocalPlayer).NetId, (byte)201, (SendOption)1, -1);
			Serialize(val);
			((InnerNetClient)AmongUsClient.Instance).FinishRpcImmediately(val);
		}
	}

	public static void HandleBypass(MessageReader reader)
	{
		try
		{
			CustomMessage message = Deserialize(reader);
			if ((Object)(object)DestroyableSingleton<HudManager>.Instance != (Object)null && (Object)(object)DestroyableSingleton<HudManager>.Instance.Chat != (Object)null)
			{
				PlayerControl val = ((IEnumerable<PlayerControl>)PlayerControl.AllPlayerControls.ToArray()).FirstOrDefault((PlayerControl p) => p.PlayerId == message.SenderId);
				if ((Object)(object)val != (Object)null)
				{
					DestroyableSingleton<HudManager>.Instance.Chat.AddChat(val, message.Content, true);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("Error processing message: " + ex.Message));
		}
	}

	public static void SendMessageToAll(string messageContent)
	{
		if (!((Object)(object)PlayerControl.LocalPlayer == (Object)null))
		{
			new CustomMessage(0, PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.Data.PlayerName, messageContent, MessageType.Broadcast).SendBypass();
		}
	}

	public static void SendPrivateMessage(string messageContent, PlayerControl targetPlayer)
	{
		if (!((Object)(object)PlayerControl.LocalPlayer == (Object)null) && !((Object)(object)targetPlayer == (Object)null))
		{
			new CustomMessage(0, PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.Data.PlayerName, messageContent, MessageType.Private).SendBypass();
		}
	}
}


