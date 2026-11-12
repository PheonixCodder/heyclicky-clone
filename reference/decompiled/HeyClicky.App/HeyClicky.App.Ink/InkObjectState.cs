using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public class InkObjectState
{
	private static readonly Stopwatch clock = Stopwatch.StartNew();

	public const double LongAgo = -1000.0;

	private static readonly ConditionalWeakTable<object, Dictionary<string, InkObjectState>> states = new ConditionalWeakTable<object, Dictionary<string, InkObjectState>>();

	private static int currentGeneration;

	private int generation;

	private WeakReference<InkObjectHost>? host;

	private double animateUntil;

	private bool ticking;

	private readonly Dictionary<string, FrameworkElement> fields = new Dictionary<string, FrameworkElement>();

	public static double Now => clock.Elapsed.TotalSeconds;

	public static T For<T>(ClickyInkLiveNote.Block block, string kind) where T : InkObjectState, new()
	{
		object key = ((block.Stamps.Count > 0) ? ((object)block.Stamps[0].Value) : ((object)block));
		ClickyInkEnvironment current = ClickyInkEnvironment.Current;
		string key2 = $"{kind}|{block.Id}|{(current.IsWindowPreview ? "preview" : "")}{(current.IsGlancePreview ? "glance" : "")}";
		Dictionary<string, InkObjectState> orCreateValue = states.GetOrCreateValue(key);
		if (orCreateValue.TryGetValue(key2, out var value) && value is T result && value.generation == currentGeneration)
		{
			return result;
		}
		return (T)(orCreateValue[key2] = new T
		{
			generation = currentGeneration
		});
	}

	public static void ResetAll()
	{
		currentGeneration++;
	}

	internal void Attach(InkObjectHost h)
	{
		host = new WeakReference<InkObjectHost>(h);
	}

	public void Changed()
	{
		WeakReference<InkObjectHost>? weakReference = host;
		if (weakReference != null && weakReference.TryGetTarget(out InkObjectHost target))
		{
			target.Refresh();
		}
	}

	public void Animate(double seconds)
	{
		animateUntil = Math.Max(animateUntil, Now + seconds);
		if (!ticking)
		{
			ticking = true;
			CompositionTarget.Rendering += Tick;
		}
	}

	private void Tick(object? sender, object e)
	{
		Changed();
		if (Now < animateUntil)
		{
			WeakReference<InkObjectHost>? weakReference = host;
			if (weakReference != null && weakReference.TryGetTarget(out InkObjectHost _))
			{
				return;
			}
		}
		CompositionTarget.Rendering -= Tick;
		ticking = false;
		Changed();
	}

	public TField Field<TField>(string key, Func<TField> make) where TField : FrameworkElement
	{
		TField val;
		if (fields.TryGetValue(key, out FrameworkElement value))
		{
			val = value as TField;
			if ((object)val != null)
			{
				goto IL_003d;
			}
		}
		val = make();
		fields[key] = val;
		goto IL_003d;
		IL_003d:
		InkViews.Detach(val);
		return val;
	}
}
