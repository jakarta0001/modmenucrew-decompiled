using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ModMenuCrew;

public static class TinyPngDecoder
{
	private static readonly byte[] PNG_SIGNATURE = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };

	public static void Decode(byte[] pngData, Texture2D texture)
	{
		if (pngData == null || pngData.Length < 8)
		{
			throw new ArgumentException("Invalid PNG data");
		}
		using MemoryStream memoryStream = new MemoryStream(pngData);
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		byte[] array = binaryReader.ReadBytes(8);
		for (int i = 0; i < 8; i++)
		{
			if (array[i] != PNG_SIGNATURE[i])
			{
				throw new Exception("Invalid PNG signature");
			}
		}
		int num = 0;
		int num2 = 0;
		byte b = 0;
		List<byte> list = new List<byte>();
		while (memoryStream.Position + 8 <= memoryStream.Length)
		{
			uint num3 = ReadBE32(binaryReader);
			string text = new string(binaryReader.ReadChars(4));
			if (num3 > pngData.Length)
			{
				throw new Exception("Invalid chunk length");
			}
			byte[] array2 = binaryReader.ReadBytes((int)num3);
			binaryReader.ReadUInt32();
			switch (text)
			{
			case "IHDR":
				num = (int)GetBE32(array2, 0);
				num2 = (int)GetBE32(array2, 4);
				b = array2[9];
				continue;
			case "IDAT":
				list.AddRange(array2);
				continue;
			default:
				continue;
			case "IEND":
				break;
			}
			break;
		}
		if (list.Count == 0)
		{
			throw new Exception("No IDAT chunks found");
		}
		byte[] data = DecompressZlib(list.ToArray());
		int num4 = b switch
		{
			2 => 3, 
			6 => 4, 
			_ => 0, 
		};
		if (num4 == 0)
		{
			throw new Exception($"Unsupported color type: {b}");
		}
		Color32[] array3 = Unfilter(data, num, num2, num4);
		if (((Texture)texture).width != num || ((Texture)texture).height != num2)
		{
			texture.Reinitialize(num, num2);
		}
		texture.SetPixels32(InteropFix.Cast(array3));
		texture.Apply();
	}

	private static byte[] DecompressZlib(byte[] data)
	{
		if (data.Length < 6)
		{
			throw new Exception("Invalid Zlib data");
		}
		try
		{
			using MemoryStream stream = new MemoryStream(data, 2, data.Length - 6);
			using MemoryStream memoryStream = new MemoryStream();
			using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
			deflateStream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch
		{
			using MemoryStream stream2 = new MemoryStream(data, 2, data.Length - 2);
			using MemoryStream memoryStream2 = new MemoryStream();
			using DeflateStream deflateStream2 = new DeflateStream(stream2, CompressionMode.Decompress);
			deflateStream2.CopyTo(memoryStream2);
			return memoryStream2.ToArray();
		}
	}

	private static Color32[] Unfilter(byte[] data, int width, int height, int bpp)
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		int num = width * bpp;
		Color32[] array = (Color32[])(object)new Color32[width * height];
		byte[] array2 = new byte[num];
		byte[] array3 = new byte[num];
		int num2 = 0;
		for (int i = 0; i < height; i++)
		{
			byte b = data[num2++];
			System.Array.Copy(data, num2, array3, 0, num);
			num2 += num;
			for (int j = 0; j < num; j++)
			{
				byte b2 = (byte)((j >= bpp) ? array3[j - bpp] : 0);
				byte b3 = array2[j];
				byte c = (byte)((j >= bpp) ? array2[j - bpp] : 0);
				byte[] array4 = array3;
				int num3 = j;
				array4[num3] = b switch
				{
					1 => (byte)(array3[j] + b2), 
					2 => (byte)(array3[j] + b3), 
					3 => (byte)(array3[j] + (b2 + b3) / 2), 
					4 => (byte)(array3[j] + Paeth(b2, b3, c)), 
					_ => array3[j], 
				};
			}
			int num4 = height - 1 - i;
			for (int k = 0; k < width; k++)
			{
				int num5 = k * bpp;
				array[num4 * width + k] = new Color32(array3[num5], array3[num5 + 1], array3[num5 + 2], (bpp == 4) ? array3[num5 + 3] : byte.MaxValue);
			}
			byte[] array5 = array3;
			array3 = array2;
			array2 = array5;
		}
		return array;
	}

	private static byte Paeth(byte a, byte b, byte c)
	{
		int num = a + b - c;
		int num2 = Math.Abs(num - a);
		int num3 = Math.Abs(num - b);
		int num4 = Math.Abs(num - c);
		if (num2 > num3 || num2 > num4)
		{
			if (num3 > num4)
			{
				return c;
			}
			return b;
		}
		return a;
	}

	private static uint ReadBE32(BinaryReader r)
	{
		byte[] array = r.ReadBytes(4);
		return (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
	}

	private static uint GetBE32(byte[] d, int o)
	{
		return (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
	}
}


