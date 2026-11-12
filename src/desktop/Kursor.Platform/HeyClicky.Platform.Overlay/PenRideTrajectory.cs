using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Overlay;

public sealed class PenRideTrajectory
{
	private sealed record Stroke(PointD[] Points, double[] Lengths, double BeginTime, double Duration, double X1, double Y1, double X2, double Y2)
	{
		public double EndTime => BeginTime + Duration;
	}

	private sealed record Flight(OverlayCursorFlightPath Path, double StartTime, double EndTime)
	{
		public double? Progress(double time)
		{
			if (!(time >= StartTime) || !(time < EndTime))
			{
				return null;
			}
			return (time - StartTime) / Math.Max(0.001, EndTime - StartTime);
		}
	}

	private const double StepSeconds = 1.0 / 120.0;

	private const double MinimumFlightDistance = 40.0;

	private const double MinimumFlightSeconds = 0.12;

	private const double RestingRotationDegrees = -35.0;

	private readonly List<Stroke> strokes = new List<Stroke>();

	private readonly List<Flight> flights = new List<Flight>();

	private double trajectoryStart;

	private PointD[] positions = Array.Empty<PointD>();

	private PointD[] velocities = Array.Empty<PointD>();

	private (double Degrees, double Scale)[] turnsAndScales = Array.Empty<(double, double)>();

	private double displayWidth;

	private double displayHeight;

	private const uint SpiGetClientAreaAnimation = 4162u;

	public bool EndsSettled { get; private set; }

	public double EndTime => trajectoryStart + (double)Math.Max(0, positions.Length - 1) * (1.0 / 120.0);

	public bool IsEmpty => positions.Length == 0;

	public static bool AnimationsAreReduced
	{
		get
		{
			try
			{
				int value;
				return SystemParametersInfo(4162u, 0u, out value, 0u) && value == 0;
			}
			catch
			{
				return false;
			}
		}
	}

	public void Begin(PointD start, double now, double width, double height)
	{
		strokes.Clear();
		flights.Clear();
		displayWidth = width;
		displayHeight = height;
		trajectoryStart = now;
		positions = new PointD[1] { start };
		velocities = new PointD[1];
		turnsAndScales = new(double, double)[1] { (0.0, 1.0) };
		EndsSettled = false;
	}

	public void Add(IEnumerable<(PointD[] InkPoints, double BeginTime, double Duration, double X1, double Y1, double X2, double Y2)> newStrokes, double now)
	{
		foreach (var newStroke in newStrokes)
		{
			if (newStroke.InkPoints.Length != 0)
			{
				PointD[] array = newStroke.InkPoints.Select(CursorCenterForInk).ToArray();
				double[] array2 = new double[array.Length];
				for (int i = 1; i < array.Length; i++)
				{
					array2[i] = array2[i - 1] + Distance(array[i - 1], array[i]);
				}
				strokes.Add(new Stroke(array, array2, newStroke.BeginTime, Math.Max(0.05, newStroke.Duration), newStroke.X1, newStroke.Y1, newStroke.X2, newStroke.Y2));
			}
		}
		strokes.Sort((Stroke first, Stroke second) => first.BeginTime.CompareTo(second.BeginTime));
		strokes.RemoveAll((Stroke stroke) => stroke.EndTime < now - 2.0);
		flights.RemoveAll((Flight flight) => flight.EndTime < now - 2.0);
		PlanFlights(now);
		Replan(now);
	}

	public (PointD Position, PointD Velocity, double TurnDegrees, double Scale) Sample(double time)
	{
		if (positions.Length == 0)
		{
			return (Position: default(PointD), Velocity: default(PointD), TurnDegrees: 0.0, Scale: 1.0);
		}
		double num = (time - trajectoryStart) / (1.0 / 120.0);
		if (num <= 0.0)
		{
			return (Position: positions[0], Velocity: velocities[0], TurnDegrees: turnsAndScales[0].Degrees, Scale: turnsAndScales[0].Scale);
		}
		if (num >= (double)(positions.Length - 1))
		{
			return (Position: positions[^1], Velocity: default(PointD), TurnDegrees: turnsAndScales[^1].Degrees, Scale: turnsAndScales[^1].Scale);
		}
		int num2 = (int)num;
		double fraction = num - (double)num2;
		(double, double) tuple = turnsAndScales[num2];
		(double, double) tuple2 = turnsAndScales[num2 + 1];
		return (Position: Lerp(positions[num2], positions[num2 + 1]), Velocity: Lerp(velocities[num2], velocities[num2 + 1]), TurnDegrees: tuple.Item1 + (tuple2.Item1 - tuple.Item1) * fraction, Scale: tuple.Item2 + (tuple2.Item2 - tuple.Item2) * fraction);
		PointD Lerp(PointD a, PointD b)
		{
			return new PointD(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
		}
	}

	private PointD CursorCenterForInk(PointD point)
	{
		return new PointD(Math.Max(20.0, Math.Min(point.X + 8.0, displayWidth - 20.0)), Math.Max(20.0, Math.Min(point.Y + 12.0, displayHeight - 20.0)));
	}

	private PointD Target(double time)
	{
		for (int num = flights.Count - 1; num >= 0; num--)
		{
			double? num2 = flights[num].Progress(time);
			if (num2.HasValue)
			{
				double valueOrDefault = num2.GetValueOrDefault();
				return FlightPosition(flights[num].Path, valueOrDefault);
			}
		}
		if (strokes.Count == 0)
		{
			if (positions.Length == 0)
			{
				return default(PointD);
			}
			return positions[^1];
		}
		Stroke stroke = null;
		foreach (Stroke stroke2 in strokes)
		{
			if (stroke2.BeginTime <= time)
			{
				stroke = stroke2;
			}
		}
		if ((object)stroke == null)
		{
			return strokes[0].Points[0];
		}
		double num3 = stroke.Lengths[^1];
		if (stroke.Points.Length < 2 || num3 <= 0.0)
		{
			return stroke.Points[0];
		}
		double x = Math.Clamp((time - stroke.BeginTime) / stroke.Duration, 0.0, 1.0);
		double num4 = Easing.CubicBezier(stroke.X1, stroke.Y1, stroke.X2, stroke.Y2, x) * num3;
		int i;
		for (i = 1; i < stroke.Lengths.Length - 1 && stroke.Lengths[i] < num4; i++)
		{
		}
		PointD pointD = stroke.Points[i - 1];
		PointD pointD2 = stroke.Points[i];
		double num5 = Math.Max(0.001, stroke.Lengths[i] - stroke.Lengths[i - 1]);
		double num6 = Math.Clamp((num4 - stroke.Lengths[i - 1]) / num5, 0.0, 1.0);
		return new PointD(pointD.X + (pointD2.X - pointD.X) * num6, pointD.Y + (pointD2.Y - pointD.Y) * num6);
	}

	private static PointD FlightPosition(OverlayCursorFlightPath path, double progress)
	{
		(double, double, double, double) tuple = path.Pose(progress);
		return new PointD(tuple.Item1, tuple.Item2);
	}

	private void PlanFlights(double now)
	{
		for (int i = 0; i < strokes.Count; i++)
		{
			Stroke stroke = strokes[i];
			if (stroke.BeginTime <= now)
			{
				continue;
			}
			PointD destination = stroke.Points[0];
			if (flights.Any((Flight flight) => Math.Abs(flight.EndTime - stroke.BeginTime) < 0.0005 && flight.Path.EndX == destination.X && flight.Path.EndY == destination.Y))
			{
				continue;
			}
			List<Stroke> list = (from other in strokes.Take(i)
				where other.BeginTime < stroke.BeginTime
				select other).ToList();
			double num = ((list.Count > 0) ? list.Max((Stroke other) => other.EndTime) : now);
			double num2 = Math.Max(now, num);
			if (!(stroke.BeginTime - num2 < 0.12) && (flights.Count <= 0 || !(stroke.BeginTime < flights.Max((Flight flight) => flight.EndTime) - 0.0005)))
			{
				PointD a = ((num > now && list.Count > 0) ? list.MaxBy((Stroke other) => other.EndTime).Points[^1] : Sample(now).Position);
				if (!(Distance(a, destination) < 40.0))
				{
					flights.Add(new Flight(new OverlayCursorFlightPath(a.X, a.Y, destination.X, destination.Y), num2, stroke.BeginTime));
				}
			}
		}
	}

	private (double Degrees, double Scale) FlightTurnAndScale(double time)
	{
		for (int num = flights.Count - 1; num >= 0; num--)
		{
			double? num2 = flights[num].Progress(time);
			if (num2.HasValue)
			{
				double valueOrDefault = num2.GetValueOrDefault();
				(double, double, double, double) tuple = flights[num].Path.Pose(valueOrDefault);
				double num3 = (tuple.Item3 - -35.0) % 360.0;
				if (num3 > 180.0)
				{
					num3 -= 360.0;
				}
				if (num3 < -180.0)
				{
					num3 += 360.0;
				}
				double num4 = Math.Min(1.0, Math.Min(valueOrDefault, 1.0 - valueOrDefault) / 0.2);
				double num5 = num4 * num4 * (3.0 - 2.0 * num4);
				return (Degrees: num3 * num5, Scale: tuple.Item4);
			}
		}
		return (Degrees: 0.0, Scale: 1.0);
	}

	private void Replan(double now)
	{
		(PointD Position, PointD Velocity, double TurnDegrees, double Scale) tuple = Sample(now);
		PointD item = tuple.Position;
		PointD item2 = tuple.Velocity;
		double num = ((strokes.Count > 0) ? strokes.Max((Stroke stroke) => stroke.EndTime) : now);
		List<PointD> list = new List<PointD> { item };
		List<PointD> list2 = new List<PointD> { item2 };
		List<(double, double)> list3 = new List<(double, double)> { FlightTurnAndScale(now) };
		double time = now;
		bool endsSettled = false;
		double num2 = Math.Max(num, now) + 3.0;
		while (time < num2)
		{
			time += 1.0 / 120.0;
			PointD pointD = Target(time);
			if (flights.Any((Flight flight) => flight.Progress(time).HasValue))
			{
				item2 = new PointD((pointD.X - item.X) / (1.0 / 120.0), (pointD.Y - item.Y) / (1.0 / 120.0));
				item = pointD;
				list.Add(item);
				list2.Add(item2);
				list3.Add(FlightTurnAndScale(time));
				continue;
			}
			double num3 = item.X - pointD.X;
			double num4 = item.Y - pointD.Y;
			double num5 = 0.1 + 0.14 * Math.Min(1.0, Math.Sqrt(num3 * num3 + num4 * num4) / 500.0);
			double num6 = Math.PI * 2.0 / num5;
			double num7 = Math.Exp((0.0 - num6) * (1.0 / 120.0));
			double num8 = (num3 + (item2.X + num6 * num3) * (1.0 / 120.0)) * num7;
			double num9 = (num4 + (item2.Y + num6 * num4) * (1.0 / 120.0)) * num7;
			item2 = new PointD((item2.X - num6 * (1.0 / 120.0) * (item2.X + num6 * num3)) * num7, (item2.Y - num6 * (1.0 / 120.0) * (item2.Y + num6 * num4)) * num7);
			item = new PointD(pointD.X + num8, pointD.Y + num9);
			list.Add(item);
			list2.Add(item2);
			list3.Add((0.0, 1.0));
			if (!(time >= num) || !(Math.Sqrt(num8 * num8 + num9 * num9) < 0.3) || !(Math.Sqrt(item2.X * item2.X + item2.Y * item2.Y) < 4.0))
			{
				continue;
			}
			list[list.Count - 1] = pointD;
			list2[list2.Count - 1] = default(PointD);
			endsSettled = true;
			break;
		}
		trajectoryStart = now;
		positions = list.ToArray();
		velocities = list2.ToArray();
		turnsAndScales = Enumerable.ToArray(list3);
		EndsSettled = endsSettled;
	}

	private static double Distance(PointD a, PointD b)
	{
		return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint winIni);
}
