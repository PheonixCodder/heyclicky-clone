using System;
using System.IO;
using HeyClicky.App.Home;
using HeyClicky.App.Notch.Peek;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

internal readonly record struct ClickyInkFileEntry(string RawPath, string ResolvedPath, bool IsWebLink, string Name, NotchDocumentStyle Style)
{
	public static ClickyInkFileEntry Of(ClickyInkLiveNote.Block block, ClickyInkActions actions)
	{
		string text = block.Source["path"] ?? "";
		bool flag = ArtifactPaths.IsWebLink(text);
		string text2 = (flag ? text : actions.ResolvePath(text));
		Uri result;
		string name = ((!flag) ? Path.GetFileName(text2.TrimEnd('/', '\\')) : ((Uri.TryCreate(text, UriKind.Absolute, out result) && result.Host.Length > 0) ? result.Host : text));
		return new ClickyInkFileEntry(text, text2, flag, name, NotchDocumentStyle.For(text2, flag));
	}
}
