using System;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Documents;

internal static class MarkdownViewerLinks
{
	internal static void Opened(XamlRoot? root, Uri uri)
	{
		Markdown.RaiseLinkOpened(root, uri);
		if (uri.IsFile)
		{
			ChatBlockViews.OpenPath(uri.LocalPath);
		}
	}
}
