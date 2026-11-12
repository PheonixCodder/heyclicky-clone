using System;
using System.Text.Json.Nodes;
using HeyClicky.Core.Notch;

namespace HeyClicky.App.Notch.Peek;

public interface INotchUpdateContentSource
{
	event Action? Changed;

	NotchUpdateContent? Content(string railKey);

	(string Subject, JsonObject Properties)? ShownAnalytics(string railKey, NotchUpdateKind kind)
	{
		return null;
	}
}
