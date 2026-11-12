using System.Numerics;
using HeyClicky.Drawing.Painter;
using Microsoft.ClearScript.V8.FastProxy;

namespace HeyClicky.Drawing.Engine;

internal sealed class HostBridge : V8FastHostObject<HostBridge>
{
	public double R0;

	public double R1;

	public double R2;

	public double R3;

	private SvgPath.Measure? measuredPath;

	private string? measuredPathData;

	public required NativeEngine Engine { get; init; }

	static HostBridge()
	{
		V8FastHostObject<HostBridge>.Configure(delegate(V8FastHostObjectConfiguration<HostBridge> configuration)
		{
			configuration.AddMethodGetter("now", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(DrawingThread.Now);
			});
			configuration.AddMethodGetter("schedule", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.Engine.ScheduleTimer(args.GetInt32(0), args.GetDouble(1));
			});
			configuration.AddMethodGetter("cancel", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.Engine.CancelTimer(args.GetInt32(0));
			});
			configuration.AddMethodGetter("log", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				Log.Info("Drawing", "engine: " + (args.TryGet(0, out string value) ? value : "?"));
			});
			configuration.AddMethodGetter("textWidth", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.Engine.TextWidth(args.GetString(0), args.GetDouble(1), args.GetInt32(2), args.TryGet(3, out string value2) ? (value2 ?? "") : ""));
			});
			configuration.AddMethodGetter("textBox", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				(double, double, double, double) tuple = self.Engine.TextBox(args.GetString(0));
				(self.R0, self.R1, self.R2, self.R3) = tuple;
			});
			configuration.AddMethodGetter("setPath", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.measuredPathData = (args.TryGet(0, out string value3) ? (value3 ?? "") : "");
				self.measuredPath = null;
			});
			configuration.AddMethodGetter("pathLength", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.Measured().TotalLength);
			});
			configuration.AddMethodGetter("pathPoint", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				Vector2 vector = self.Measured().PointAt(args.GetDouble(0));
				self.R0 = vector.X;
				self.R1 = vector.Y;
			});
			configuration.AddMethodGetter("pathBox", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				string value4;
				(double, double, double, double) tuple3 = new SvgPath(args.TryGet(0, out value4) ? (value4 ?? "") : "").BoundingBox();
				(self.R0, self.R1, self.R2, self.R3) = tuple3;
			});
			configuration.AddMethodGetter("inkFor", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.Engine.Screenshot.InkFor(args.GetDouble(0), args.GetDouble(1), args.GetDouble(2), args.GetDouble(3)));
			});
			configuration.AddMethodGetter("busyFor", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.Engine.Screenshot.BusyFor(args.GetDouble(0), args.GetDouble(1), args.GetDouble(2), args.GetDouble(3)));
			});
			configuration.AddMethodGetter("lumFor", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.Engine.Screenshot.BackgroundLuminance(args.GetDouble(0), args.GetDouble(1), args.GetDouble(2), args.GetDouble(3)) ?? (-1.0));
			});
			configuration.AddMethodGetter("figuresFloat", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(NativeEngine.FiguresFloat);
			});
			configuration.AddMethodGetter("caption", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.Engine.RaiseCaption(args.TryGet(0, out string value5) ? (value5 ?? "") : "");
			});
			configuration.AddMethodGetter("markSound", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.Engine.NoteMarkSound(args.GetInt32(0), args.TryGet(1, out string value6) ? (value6 ?? "") : "");
			});
			configuration.AddMethodGetter("buttonReady", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				self.Engine.RaiseButtonReady(args.GetInt32(0), args.GetDouble(1));
			});
			configuration.AddMethodGetter("shape3d", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				Drawing3DAdapter.Prefetch(args.TryGet(0, out string value7) ? (value7 ?? "") : "", args.TryGet(1, out string value8) ? (value8 ?? "") : "");
			});
			configuration.AddMethodGetter("r0", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.R0);
			});
			configuration.AddMethodGetter("r1", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.R1);
			});
			configuration.AddMethodGetter("r2", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.R2);
			});
			configuration.AddMethodGetter("r3", delegate(HostBridge self, in V8FastArgs args, in V8FastResult result)
			{
				result.Set(self.R3);
			});
		});
	}

	private SvgPath.Measure Measured()
	{
		return measuredPath ?? (measuredPath = new SvgPath.Measure(new SvgPath(measuredPathData ?? "")));
	}
}
