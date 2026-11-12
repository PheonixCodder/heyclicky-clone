using System;
using System.Collections.Generic;
using HeyClicky.App.Notch.Surfaces;

namespace HeyClicky.App.Notch.Peek;

public static class NotchExtensions
{
	private static readonly List<INotchDocumentRenderer> renderers = new List<INotchDocumentRenderer>();

	public static INotchHomeDock? HomeDock { get; set; }

	public static IChatLiveWork? LiveWork { get; set; }

	public static INotchUpdateContentSource? UpdateContent { get; set; }

	public static Action<NotchUpdateContent>? AnnounceFinishedWorkOnRequest { get; set; }

	public static Action<NotchUpdateContent>? MarkFinishedWorkSeen { get; set; }

	public static Func<NotchRestingMarkState>? RestingMarkState { get; set; }

	public static IReadOnlyList<string> ShowcaseSampleIntegrationIds { get; set; } = Array.Empty<string>();

	public static IReadOnlyList<INotchDocumentRenderer> DocumentRenderers => renderers;

	public static event Action? RestingMarkChanged;

	public static event Action? ShowcaseChanged;

	public static void RaiseRestingMarkChanged()
	{
		NotchExtensions.RestingMarkChanged?.Invoke();
	}

	public static void RaiseShowcaseChanged()
	{
		NotchExtensions.ShowcaseChanged?.Invoke();
	}

	public static void RegisterDocumentRenderer(INotchDocumentRenderer renderer)
	{
		renderers.Insert(0, renderer);
	}
}
