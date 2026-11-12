using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace HeyClicky.App.Home;

internal static class DemoFiles
{
	public static void WriteChartPng(string path)
	{
		if (File.Exists(path) && new FileInfo(path).Length > 100)
		{
			return;
		}
		byte[] array = new byte[115360];
		int[] array2 = new int[5] { 70, 95, 82, 120, 134 };
		for (int i = 0; i < 160; i++)
		{
			int num = i * 721;
			for (int j = 0; j < 240; j++)
			{
				int num2 = (j - 20) / 44;
				int num3 = (j - 20) % 44;
				bool num4 = j >= 20 && num2 < array2.Length && num3 < 30 && 144 - i < array2[num2] && i < 144;
				bool flag = i == 144 && j >= 14 && j < 230;
				byte b;
				byte b2;
				byte b3;
				if (!num4)
				{
					if (!flag)
					{
						b = 250;
						b2 = 250;
						b3 = 247;
					}
					else
					{
						b = 96;
						b2 = 96;
						b3 = 96;
					}
				}
				else
				{
					b = 79;
					b2 = 140;
					b3 = byte.MaxValue;
				}
				array[num + 1 + j * 3] = b;
				array[num + 2 + j * 3] = b2;
				array[num + 3 + j * 3] = b3;
			}
		}
		using MemoryStream memoryStream = new MemoryStream();
		using (ZLibStream zLibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			zLibStream.Write(array);
		}
		using MemoryStream memoryStream2 = new MemoryStream();
		memoryStream2.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
		byte[] array3 = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(array3, 240);
		BinaryPrimitives.WriteInt32BigEndian(array3.AsSpan(4), 160);
		array3[8] = 8;
		array3[9] = 2;
		Chunk(memoryStream2, "IHDR", array3);
		Chunk(memoryStream2, "IDAT", memoryStream.ToArray());
		Chunk(memoryStream2, "IEND", Array.Empty<byte>());
		File.WriteAllBytes(path, memoryStream2.ToArray());
	}

	private static void Chunk(Stream png, string type, byte[] data)
	{
		byte[] array = new byte[4];
		BinaryPrimitives.WriteInt32BigEndian(array, data.Length);
		png.Write(array);
		byte[] array2 = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
		png.Write(array2);
		uint num = uint.MaxValue;
		byte[] array3 = array2;
		foreach (byte b in array3)
		{
			num ^= b;
			for (int j = 0; j < 8; j++)
			{
				num = (((num & 1) != 0) ? (0xEDB88320u ^ (num >> 1)) : (num >> 1));
			}
		}
		byte[] array4 = new byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(array4, ~num);
		png.Write(array4);
	}

	public static void WritePdf(string path, string title, string line)
	{
		if (File.Exists(path) && new FileInfo(path).Length > 100)
		{
			return;
		}
		string text = $"BT /F1 22 Tf 48 520 Td ({title}) Tj ET BT /F1 12 Tf 48 490 Td ({line}) Tj ET 0.31 0.55 1 rg 48 300 80 150 re f 148 300 80 110 re f 248 300 80 180 re f";
		string[] array = new string[5]
		{
			"<< /Type /Catalog /Pages 2 0 R >>",
			"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
			"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 420 595] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
			$"<< /Length {text.Length} >>\nstream\n{text}\nendstream",
			"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
		};
		StringBuilder stringBuilder = new StringBuilder("%PDF-1.4\n");
		List<int> list = new List<int>();
		StringBuilder stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler;
		for (int i = 0; i < array.Length; i++)
		{
			list.Add(stringBuilder.Length);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
			handler.AppendFormatted(i + 1);
			handler.AppendLiteral(" 0 obj\n");
			handler.AppendFormatted(array[i]);
			handler.AppendLiteral("\nendobj\n");
			stringBuilder3.Append(ref handler);
		}
		int length = stringBuilder.Length;
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(28, 1, stringBuilder2);
		handler.AppendLiteral("xref\n0 ");
		handler.AppendFormatted(array.Length + 1);
		handler.AppendLiteral("\n0000000000 65535 f \n");
		stringBuilder4.Append(ref handler);
		foreach (int item in list)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendFormatted(item, "D10");
			handler.AppendLiteral(" 00000 n \n");
			stringBuilder5.Append(ref handler);
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(50, 2, stringBuilder2);
		handler.AppendLiteral("trailer\n<< /Size ");
		handler.AppendFormatted(array.Length + 1);
		handler.AppendLiteral(" /Root 1 0 R >>\nstartxref\n");
		handler.AppendFormatted(length);
		handler.AppendLiteral("\n%%EOF\n");
		stringBuilder6.Append(ref handler);
		File.WriteAllText(path, stringBuilder.ToString(), Encoding.ASCII);
	}
}
