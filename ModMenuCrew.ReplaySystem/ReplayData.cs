using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModMenuCrew.ReplaySystem;

public class ReplayData
{
	public const string MagicHeader = "AMR";

	public const int CurrentVersion = 3;

	public string GameVersion;

	public int MapId;

	public string MapName;

	public DateTime RecordedAt;

	public float TotalDuration;

	public List<ReplayPlayerInfo> Players = new List<ReplayPlayerInfo>();

	public List<ReplayFrame> Frames = new List<ReplayFrame>();

	public List<ReplayEvent> Events = new List<ReplayEvent>();

	public void Save(string path)
	{
		using FileStream output = new FileStream(path, FileMode.Create);
		using BinaryWriter binaryWriter = new BinaryWriter(output);
		binaryWriter.Write("AMR".ToCharArray());
		binaryWriter.Write(3);
		binaryWriter.Write(GameVersion ?? "Unknown");
		binaryWriter.Write(MapId);
		binaryWriter.Write(MapName ?? "Unknown");
		binaryWriter.Write(RecordedAt.ToBinary());
		binaryWriter.Write(TotalDuration);
		binaryWriter.Write(Players.Count);
		foreach (ReplayPlayerInfo player in Players)
		{
			binaryWriter.Write(player.PlayerId);
			binaryWriter.Write(player.Name ?? "Unknown");
			binaryWriter.Write(player.ColorId);
			binaryWriter.Write(player.HatId ?? "");
			binaryWriter.Write(player.SkinId ?? "");
			binaryWriter.Write(player.PetId ?? "");
			binaryWriter.Write(player.IsImpostor);
			binaryWriter.Write(player.RealColor.r);
			binaryWriter.Write(player.RealColor.g);
			binaryWriter.Write(player.RealColor.b);
			binaryWriter.Write(player.RealColor.a);
		}
		binaryWriter.Write(Frames.Count);
		foreach (ReplayFrame frame in Frames)
		{
			binaryWriter.Write(frame.Time);
			binaryWriter.Write((byte)frame.States.Count);
			foreach (PlayerState state in frame.States)
			{
				binaryWriter.Write(state.PlayerId);
				binaryWriter.Write(state.Position.x);
				binaryWriter.Write(state.Position.y);
				binaryWriter.Write(state.FaceRight);
				binaryWriter.Write(state.IsDead);
				binaryWriter.Write(state.IsInVent);
				binaryWriter.Write((byte)state.AnimState);
			}
		}
		binaryWriter.Write(Events.Count);
		foreach (ReplayEvent @event in Events)
		{
			binaryWriter.Write(@event.Time);
			binaryWriter.Write((byte)@event.Type);
			binaryWriter.Write(@event.PlayerId);
			binaryWriter.Write(@event.TargetId);
			binaryWriter.Write(@event.Position.x);
			binaryWriter.Write(@event.Position.y);
			binaryWriter.Write(@event.Description ?? "");
		}
	}

	public static ReplayData Load(string path)
	{
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		ReplayData replayData = new ReplayData();
		if (!File.Exists(path))
		{
			return null;
		}
		using (FileStream input = new FileStream(path, FileMode.Open))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			if (new string(binaryReader.ReadChars(3)) != "AMR")
			{
				throw new Exception("Invalid replay file format");
			}
			int num = binaryReader.ReadInt32();
			if (num > 3)
			{
				throw new Exception("Replay file version is too new");
			}
			replayData.GameVersion = binaryReader.ReadString();
			replayData.MapId = binaryReader.ReadInt32();
			if (num >= 2)
			{
				replayData.MapName = binaryReader.ReadString();
				replayData.RecordedAt = DateTime.FromBinary(binaryReader.ReadInt64());
				replayData.TotalDuration = binaryReader.ReadSingle();
			}
			int num2 = binaryReader.ReadInt32();
			for (int i = 0; i < num2; i++)
			{
				ReplayPlayerInfo replayPlayerInfo = new ReplayPlayerInfo
				{
					PlayerId = binaryReader.ReadByte(),
					Name = binaryReader.ReadString(),
					ColorId = binaryReader.ReadInt32(),
					HatId = binaryReader.ReadString(),
					SkinId = binaryReader.ReadString(),
					PetId = binaryReader.ReadString(),
					IsImpostor = binaryReader.ReadBoolean()
				};
				if (num >= 3)
				{
					byte b = binaryReader.ReadByte();
					byte b2 = binaryReader.ReadByte();
					byte b3 = binaryReader.ReadByte();
					byte b4 = binaryReader.ReadByte();
					replayPlayerInfo.RealColor = new Color32(b, b2, b3, b4);
				}
				else
				{
					replayPlayerInfo.RealColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
				}
				replayData.Players.Add(replayPlayerInfo);
			}
			int num3 = binaryReader.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				ReplayFrame replayFrame = new ReplayFrame();
				replayFrame.Time = binaryReader.ReadSingle();
				int num4 = binaryReader.ReadByte();
				for (int k = 0; k < num4; k++)
				{
					PlayerState playerState = new PlayerState
					{
						PlayerId = binaryReader.ReadByte(),
						Position = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle())
					};
					if (num < 3)
					{
						binaryReader.ReadSingle();
						binaryReader.ReadSingle();
					}
					playerState.FaceRight = binaryReader.ReadBoolean();
					playerState.IsDead = binaryReader.ReadBoolean();
					if (num >= 2)
					{
						playerState.IsInVent = binaryReader.ReadBoolean();
					}
					if (num >= 3)
					{
						playerState.AnimState = (AnimState)binaryReader.ReadByte();
					}
					replayFrame.States.Add(playerState);
				}
				replayData.Frames.Add(replayFrame);
			}
			if (num >= 2)
			{
				int num5 = binaryReader.ReadInt32();
				for (int l = 0; l < num5; l++)
				{
					replayData.Events.Add(new ReplayEvent
					{
						Time = binaryReader.ReadSingle(),
						Type = (ReplayEventType)binaryReader.ReadByte(),
						PlayerId = binaryReader.ReadByte(),
						TargetId = binaryReader.ReadByte(),
						Position = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle()),
						Description = binaryReader.ReadString()
					});
				}
			}
		}
		return replayData;
	}
}
