using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Input;

public static class PrecisionTouchpad
{
	private sealed class Device
	{
		public nint Preparsed;

		public ushort[] Fingers = Array.Empty<ushort>();

		public int RemainingInFrame;
	}

	private struct LinkCollectionNode
	{
		public ushort LinkUsage;

		public ushort LinkUsagePage;

		public ushort Parent;

		public ushort NumberOfChildren;

		public ushort NextSibling;

		public ushort FirstChild;

		public uint Bits;

		public nint UserContext;
	}

	private const ushort Digitizer = 13;

	private const ushort FingerUsage = 34;

	private const ushort TipSwitch = 66;

	private const ushort ContactId = 81;

	private const ushort ContactCount = 84;

	private const int HidpStatusSuccess = 1114112;

	private const int MaxContacts = 10;

	private static readonly Dictionary<nint, Device?> devices = new Dictionary<nint, Device>();

	private static readonly object gate = new object();

	private static readonly int[] contactIds = new int[10];

	private static readonly bool[] contactDown = new bool[10];

	private static readonly ushort[] usages = new ushort[16];

	private static int contactSlots;

	private static TouchpadSnapshot state;

	public static TouchpadSnapshot Snapshot
	{
		get
		{
			lock (gate)
			{
				return state;
			}
		}
	}

	private static double Now => (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

	internal unsafe static void Read(nint deviceHandle, byte* hid, uint available)
	{
		if (available < 8)
		{
			return;
		}
		uint num = *(uint*)hid;
		uint num2 = *(uint*)(hid + 4);
		if (num == 0 || (ulong)(8 + (long)num * (long)num2) > (ulong)available)
		{
			return;
		}
		Device device = DeviceFor(deviceHandle);
		if (device != null)
		{
			for (uint num3 = 0u; num3 < num2; num3++)
			{
				ReadReport(device, hid + 8 + num3 * num, num);
			}
		}
	}

	private unsafe static void ReadReport(Device device, byte* report, uint size)
	{
		uint num = 0u;
		if (HidP_GetUsageValue(0, 13, 0, 84, &num, device.Preparsed, report, size) == 1114112 && num != 0)
		{
			device.RemainingInFrame = (int)num;
		}
		int num2 = Math.Min(device.RemainingInFrame, device.Fingers.Length);
		if (num2 <= 0)
		{
			return;
		}
		device.RemainingInFrame -= num2;
		for (int i = 0; i < num2; i++)
		{
			ushort linkCollection = device.Fingers[i];
			uint id = 0u;
			if (HidP_GetUsageValue(0, 13, linkCollection, 81, &id, device.Preparsed, report, size) != 1114112)
			{
				continue;
			}
			uint num3 = (uint)usages.Length;
			bool down = false;
			fixed (ushort* ptr = usages)
			{
				if (HidP_GetUsages(0, 13, linkCollection, ptr, &num3, device.Preparsed, report, size) == 1114112)
				{
					for (int j = 0; j < num3; j++)
					{
						if (ptr[j] == 66)
						{
							down = true;
							break;
						}
					}
				}
			}
			Note((int)id, down);
		}
		Publish();
	}

	private static void Note(int id, bool down)
	{
		for (int i = 0; i < contactSlots; i++)
		{
			if (contactIds[i] == id)
			{
				if (down)
				{
					contactDown[i] = true;
					return;
				}
				contactSlots--;
				contactIds[i] = contactIds[contactSlots];
				contactDown[i] = contactDown[contactSlots];
				return;
			}
		}
		if (down && contactSlots < 10)
		{
			contactIds[contactSlots] = id;
			contactDown[contactSlots] = true;
			contactSlots++;
		}
	}

	private static void Publish()
	{
		int num = contactSlots;
		lock (gate)
		{
			TouchpadSnapshot touchpadSnapshot = state;
			double now = Now;
			if (num > 0 && touchpadSnapshot.Fingers == 0)
			{
				state = new TouchpadSnapshot(Present: true, num, touchpadSnapshot.Touch + 1, now, touchpadSnapshot.LiftedAt);
			}
			else if (num == 0 && touchpadSnapshot.Fingers > 0)
			{
				state = touchpadSnapshot with
				{
					Present = true,
					Fingers = 0,
					LiftedAt = now
				};
			}
			else
			{
				state = touchpadSnapshot with
				{
					Present = true,
					Fingers = num
				};
			}
		}
	}

	internal unsafe static void ReadDevicesNow()
	{
		try
		{
			uint count = 0u;
			NativeHid.GetRawInputDeviceList(null, ref count, 16u);
			if (count == 0 || count > 256)
			{
				return;
			}
			byte* ptr = stackalloc byte[(int)(count * 16)];
			count = NativeHid.GetRawInputDeviceList(ptr, ref count, 16u);
			if (count == uint.MaxValue)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				nint num2 = *(nint*)(ptr + i * 16);
				if (*(uint*)(ptr + i * 16 + 8) == 2)
				{
					byte* ptr2 = stackalloc byte[32];
					*(int*)ptr2 = 32;
					uint size = 32u;
					if (NativeHid.GetRawInputDeviceInfoW(num2, 536870923u, ptr2, ref size) != uint.MaxValue && *(ushort*)(ptr2 + 20) == 13 && *(ushort*)(ptr2 + 22) == 5)
					{
						num++;
						DeviceFor(num2);
					}
				}
			}
			if (num == 0)
			{
				Log.Info("Touchpad", "no precision touchpad on this PC (a touchpad's swipes are told from its wheel events)");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Touchpad", "listing touchpads: " + ex.Message);
		}
	}

	private unsafe static Device? DeviceFor(nint handle)
	{
		if (devices.TryGetValue(handle, out Device value))
		{
			return value;
		}
		Device device = null;
		try
		{
			uint size = 0u;
			NativeHid.GetRawInputDeviceInfoW(handle, 536870917u, null, ref size);
			if (size != 0)
			{
				nint num = Marshal.AllocHGlobal((int)size);
				if (NativeHid.GetRawInputDeviceInfoW(handle, 536870917u, (void*)num, ref size) == size)
				{
					ushort[] array = FingerCollections(num);
					if (array.Length != 0)
					{
						device = new Device
						{
							Preparsed = num,
							Fingers = array
						};
					}
				}
				if (device == null)
				{
					Marshal.FreeHGlobal(num);
				}
			}
			Log.Info("Touchpad", (device == null) ? "a touchpad HID device without finger collections (not read)" : $"precision touchpad: {device.Fingers.Length} fingers a report");
		}
		catch (Exception ex)
		{
			Log.Warn("Touchpad", "reading the touchpad's layout: " + ex.Message);
		}
		devices[handle] = device;
		return device;
	}

	private unsafe static ushort[] FingerCollections(nint preparsed)
	{
		uint num = 0u;
		HidP_GetLinkCollectionNodes(null, &num, preparsed);
		if (num == 0 || num > 256)
		{
			return Array.Empty<ushort>();
		}
		LinkCollectionNode* ptr = stackalloc LinkCollectionNode[(int)num];
		if (HidP_GetLinkCollectionNodes(ptr, &num, preparsed) != 1114112)
		{
			return Array.Empty<ushort>();
		}
		List<ushort> list = new List<ushort>();
		for (ushort num2 = 0; num2 < num; num2++)
		{
			if (ptr[(int)num2].LinkUsagePage == 13 && ptr[(int)num2].LinkUsage == 34)
			{
				list.Add(num2);
			}
		}
		return list.ToArray();
	}

	[DllImport("hid.dll")]
	private unsafe static extern int HidP_GetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage, uint* value, nint preparsed, byte* report, uint reportLength);

	[DllImport("hid.dll")]
	private unsafe static extern int HidP_GetUsages(int reportType, ushort usagePage, ushort linkCollection, ushort* usageList, uint* usageLength, nint preparsed, byte* report, uint reportLength);

	[DllImport("hid.dll")]
	private unsafe static extern int HidP_GetLinkCollectionNodes(LinkCollectionNode* nodes, uint* length, nint preparsed);
}
