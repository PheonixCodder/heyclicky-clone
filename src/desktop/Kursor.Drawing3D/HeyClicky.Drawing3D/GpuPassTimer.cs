using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Vortice.Direct3D11;

namespace HeyClicky.Drawing3D;

internal sealed class GpuPassTimer
{
	public static readonly GpuPassTimer? Current = ((Environment.GetEnvironmentVariable("CANVAS_3D_PROFILE") == "1") ? new GpuPassTimer() : null);

	private const int Frames = 6;

	private const int MarksPerFrame = 16;

	private readonly ID3D11Query[] disjoint = new ID3D11Query[6];

	private readonly ID3D11Query[,] stamps = new ID3D11Query[6, 16];

	private readonly string[,] names = new string[6, 16];

	private readonly int[] counts = new int[6];

	private readonly bool[] pending = new bool[6];

	private readonly Dictionary<string, (double total, int frames)> totals = new Dictionary<string, (double, int)>();

	private int frame;

	private GpuPassTimer()
	{
		ID3D11Device1 device = GpuDevice.Shared.Device;
		for (int i = 0; i < 6; i++)
		{
			disjoint[i] = device.CreateQuery(new QueryDescription(QueryType.TimestampDisjoint));
			for (int j = 0; j < 16; j++)
			{
				stamps[i, j] = device.CreateQuery(new QueryDescription(QueryType.Timestamp));
			}
		}
	}

	public void BeginFrame(ID3D11DeviceContext context)
	{
		frame = (frame + 1) % 6;
		if (pending[frame])
		{
			Collect(context, frame);
		}
		counts[frame] = 0;
		context.Begin(disjoint[frame]);
		Mark(context, "start");
	}

	public void Mark(ID3D11DeviceContext context, string name)
	{
		int num = counts[frame];
		if (num < 16)
		{
			names[frame, num] = name;
			context.End(stamps[frame, num]);
			counts[frame] = num + 1;
		}
	}

	public void EndFrame(ID3D11DeviceContext context)
	{
		context.End(disjoint[frame]);
		pending[frame] = true;
	}

	private void Collect(ID3D11DeviceContext context, int f)
	{
		pending[f] = false;
		QueryDataTimestampDisjoint result;
		while (!context.GetData<QueryDataTimestampDisjoint>(disjoint[f], AsyncGetDataFlags.None, out result))
		{
			Thread.Yield();
		}
		if ((bool)result.Disjoint)
		{
			return;
		}
		ulong num = 0uL;
		for (int i = 0; i < counts[f]; i++)
		{
			ulong result2;
			while (!context.GetData<ulong>(stamps[f, i], AsyncGetDataFlags.None, out result2))
			{
				Thread.Yield();
			}
			if (i > 0)
			{
				var (num2, num3) = totals.GetValueOrDefault(names[f, i]);
				totals[names[f, i]] = (num2 + (double)(result2 - num) * 1000.0 / (double)result.Frequency, num3 + 1);
			}
			num = result2;
		}
	}

	public string Report()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, (double, int)> item3 in totals.OrderByDescending<KeyValuePair<string, (double, int)>, double>((KeyValuePair<string, (double total, int frames)> kv) => kv.Value.total))
		{
			item3.Deconstruct(out var key, out var value);
			(double, int) tuple = value;
			string value2 = key;
			double item = tuple.Item1;
			int item2 = tuple.Item2;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder2);
			handler.AppendFormatted(value2);
			handler.AppendLiteral(" ");
			handler.AppendFormatted(item / (double)Math.Max(1, item2), "F3");
			handler.AppendLiteral(" ms, ");
			stringBuilder2.Append(ref handler);
		}
		totals.Clear();
		return stringBuilder.ToString().TrimEnd(' ', ',');
	}
}
