using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing.Overlay;

public static class GrabCursors
{
	private readonly record struct Capsule(Vector2 A, Vector2 B, float Radius);

	private struct ICONINFO
	{
		public bool fIcon;

		public int xHotspot;

		public int yHotspot;

		public nint hbmMask;

		public nint hbmColor;
	}

	private struct BITMAPV5HEADER
	{
		public uint bV5Size;

		public int bV5Width;

		public int bV5Height;

		public ushort bV5Planes;

		public ushort bV5BitCount;

		public uint bV5Compression;

		public uint bV5SizeImage;

		public int bV5XPelsPerMeter;

		public int bV5YPelsPerMeter;

		public uint bV5ClrUsed;

		public uint bV5ClrImportant;

		public uint bV5RedMask;

		public uint bV5GreenMask;

		public uint bV5BlueMask;

		public uint bV5AlphaMask;

		public uint bV5CSType;

		public long ep1;

		public long ep2;

		public long ep3;

		public long ep4;

		public int ep5;

		public uint bV5GammaRed;

		public uint bV5GammaGreen;

		public uint bV5GammaBlue;

		public uint bV5Intent;

		public uint bV5ProfileData;

		public uint bV5ProfileSize;

		public uint bV5Reserved;
	}

	private static readonly Dictionary<(bool Closed, int Size), nint> cursors = new Dictionary<(bool, int), nint>();

	private const int SM_CXCURSOR = 13;

	private const uint BI_BITFIELDS = 3u;

	public static nint Get(bool closed, uint dpi)
	{
		int num = Math.Max(32, GetSystemMetricsForDpi(13, dpi));
		lock (cursors)
		{
			if (cursors.TryGetValue((closed, num), out var value))
			{
				return value;
			}
			value = Create(closed, num);
			cursors[(closed, num)] = value;
			return value;
		}
	}

	private static Capsule[] Hand(bool closed)
	{
		if (closed)
		{
			return new Capsule[5]
			{
				new Capsule(new Vector2(11.3f, 12.6f), new Vector2(11.3f, 15f), 1.9f),
				new Capsule(new Vector2(14.9f, 11.6f), new Vector2(14.9f, 15f), 1.9f),
				new Capsule(new Vector2(18.5f, 11.9f), new Vector2(18.5f, 15f), 1.9f),
				new Capsule(new Vector2(21.8f, 13.2f), new Vector2(21.8f, 16f), 1.8f),
				new Capsule(new Vector2(9.6f, 19.6f), new Vector2(8f, 16.4f), 1.9f)
			};
		}
		return new Capsule[5]
		{
			new Capsule(new Vector2(11.2f, 6.2f), new Vector2(11.2f, 15.5f), 1.8f),
			new Capsule(new Vector2(14.8f, 4.6f), new Vector2(14.8f, 15.5f), 1.8f),
			new Capsule(new Vector2(18.4f, 5.6f), new Vector2(18.4f, 15.5f), 1.8f),
			new Capsule(new Vector2(21.8f, 8.6f), new Vector2(21.8f, 16.5f), 1.7f),
			new Capsule(new Vector2(9.8f, 20.2f), new Vector2(5.2f, 13.6f), 1.8f)
		};
	}

	private static float CapsuleDistance(Vector2 p, Capsule c)
	{
		Vector2 vector = c.B - c.A;
		float num = Math.Clamp(Vector2.Dot(p - c.A, vector) / Math.Max(0.0001f, vector.LengthSquared()), 0f, 1f);
		return Vector2.Distance(p, c.A + vector * num) - c.Radius;
	}

	private static float PalmDistance(Vector2 p)
	{
		Vector2 vector = new Vector2(16.7f, 20f);
		Vector2 vector2 = new Vector2(7.3f, 7f) - new Vector2(4.6f);
		Vector2 value = Vector2.Abs(p - vector) - vector2;
		return Vector2.Max(value, Vector2.Zero).Length() + Math.Min(Math.Max(value.X, value.Y), 0f) - 4.6f;
	}

	private static nint Create(bool closed, int size)
	{
		Capsule[] array = Hand(closed);
		float num = 32f / (float)size;
		float num2 = 1.15f;
		uint[] array2 = new uint[size * size];
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				Vector2 p = new Vector2(((float)j + 0.5f) * num, ((float)i + 0.5f) * num);
				float num3 = PalmDistance(p);
				Capsule[] array3 = array;
				foreach (Capsule c in array3)
				{
					num3 = Math.Min(num3, CapsuleDistance(p, c));
				}
				float num4 = num3 / num;
				float num5 = num2 / num;
				float num6 = Math.Clamp(0.5f - num4, 0f, 1f);
				float num7 = Math.Clamp(0.5f - (num4 + num5), 0f, 1f);
				if (!(num6 <= 0f))
				{
					byte b = (byte)Math.Round(24f + 231f * (num7 / num6));
					byte b2 = (byte)Math.Round(255f * num6);
					array2[i * size + j] = (uint)((b2 << 24) | (b << 16) | (b << 8) | b);
				}
			}
		}
		BITMAPV5HEADER bITMAPV5HEADER = default(BITMAPV5HEADER);
		bITMAPV5HEADER.bV5Size = (uint)Marshal.SizeOf<BITMAPV5HEADER>();
		bITMAPV5HEADER.bV5Width = size;
		bITMAPV5HEADER.bV5Height = -size;
		bITMAPV5HEADER.bV5Planes = 1;
		bITMAPV5HEADER.bV5BitCount = 32;
		bITMAPV5HEADER.bV5Compression = 3u;
		bITMAPV5HEADER.bV5RedMask = 16711680u;
		bITMAPV5HEADER.bV5GreenMask = 65280u;
		bITMAPV5HEADER.bV5BlueMask = 255u;
		bITMAPV5HEADER.bV5AlphaMask = 4278190080u;
		BITMAPV5HEADER header = bITMAPV5HEADER;
		nint dC = GetDC(IntPtr.Zero);
		nint bits;
		nint num8 = CreateDIBSection(dC, ref header, 0u, out bits, IntPtr.Zero, 0u);
		ReleaseDC(IntPtr.Zero, dC);
		Marshal.Copy(Array.ConvertAll(array2, (uint v) => (int)v), 0, bits, array2.Length);
		GCHandle gCHandle = GCHandle.Alloc(new byte[(size + 15) / 16 * 2 * size], GCHandleType.Pinned);
		nint num9;
		try
		{
			num9 = CreateBitmap(size, size, 1u, 1u, gCHandle.AddrOfPinnedObject());
		}
		finally
		{
			gCHandle.Free();
		}
		ICONINFO iCONINFO = default(ICONINFO);
		iCONINFO.fIcon = false;
		iCONINFO.xHotspot = size / 2;
		iCONINFO.yHotspot = size / 2;
		iCONINFO.hbmMask = num9;
		iCONINFO.hbmColor = num8;
		ICONINFO info = iCONINFO;
		nint result = CreateIconIndirect(ref info);
		DeleteObject(num8);
		DeleteObject(num9);
		return result;
	}

	[DllImport("user32.dll")]
	private static extern int GetSystemMetricsForDpi(int index, uint dpi);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint window, nint dc);

	[DllImport("user32.dll")]
	private static extern nint CreateIconIndirect(ref ICONINFO info);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint dc, ref BITMAPV5HEADER header, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern nint CreateBitmap(int width, int height, uint planes, uint bitCount, nint bits);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint gdiObject);
}
