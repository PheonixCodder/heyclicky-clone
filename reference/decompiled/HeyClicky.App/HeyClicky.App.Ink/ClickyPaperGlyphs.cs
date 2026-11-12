using System;
using System.Collections.Generic;

namespace HeyClicky.App.Ink;

public static class ClickyPaperGlyphs
{
	public static readonly ClickyPaperGlyph[] AllCases = Enum.GetValues<ClickyPaperGlyph>();

	private static readonly Dictionary<ClickyPaperGlyph, PaperPath> cache = new Dictionary<ClickyPaperGlyph, PaperPath>();

	public static string RawValue(this ClickyPaperGlyph glyph)
	{
		return glyph.ToString().ToLowerInvariant();
	}

	public static string SvgPathData(this ClickyPaperGlyph glyph)
	{
		return glyph switch
		{
			ClickyPaperGlyph.Plane => "M3 11.5 21 4l-6.5 16-3.2-6.3L3 11.5Z M11.3 13.7 21 4", 
			ClickyPaperGlyph.Bubble => "M4 6.5C4 5.1 5.1 4 6.5 4h11C18.9 4 20 5.1 20 6.5v7c0 1.4-1.1 2.5-2.5 2.5H10l-4.5 4v-4C4.6 16 4 15 4 13.5Z", 
			ClickyPaperGlyph.Bars => "M4 20h16 M7.5 20v-5 M11 20v-9 M14.5 20v-7 M18 20V5", 
			ClickyPaperGlyph.Desk => "M3 10.5h18 M6 10.5V20 M18 10.5V20 M9 10.5V5h6v5.5", 
			ClickyPaperGlyph.Pencil => "M4.5 19.5l1-4.4L15.6 5a2 2 0 0 1 2.8 0l.7.7a2 2 0 0 1 0 2.8L9 18.6l-4.5.9Z M13.6 7.1l3.4 3.4", 
			ClickyPaperGlyph.Spark => "M12 3 C 12.7 8.6, 15.4 11.3, 21 12 C 15.4 12.7, 12.7 15.4, 12 21 C 11.3 15.4, 8.6 12.7, 3 12 C 8.6 11.3, 11.3 8.6, 12 3 Z", 
			ClickyPaperGlyph.Tray => "M4 13.5 6.4 5.8A1.6 1.6 0 0 1 7.9 4.7h8.2a1.6 1.6 0 0 1 1.5 1.1L20 13.5V18a1.6 1.6 0 0 1-1.6 1.6H5.6A1.6 1.6 0 0 1 4 18Z M4 13.5h4.6l1.3 2.4h4.2l1.3-2.4H20", 
			ClickyPaperGlyph.Calendar => "M5 6.5h14a1 1 0 0 1 1 1V19a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7.5a1 1 0 0 1 1-1Z M4 10.5h16 M8.5 4v4.5 M15.5 4v4.5", 
			ClickyPaperGlyph.Doc => "M6 3.5h8.5L19 8v12.5H6Z M14.5 3.5V8H19 M9 12.5h7 M9 16h5", 
			ClickyPaperGlyph.Globe => "M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18Z M3 12h18 M12 3c2.6 2.6 3.8 5.6 3.8 9s-1.2 6.4-3.8 9c-2.6-2.6-3.8-5.6-3.8-9S9.4 5.6 12 3Z", 
			ClickyPaperGlyph.Cart => "M3 4.5h2.6l2.2 10.5h10.4l2-7.5H7 M9.5 19.5a.8.8 0 1 0 0-.1Z M17 19.5a.8.8 0 1 0 0-.1Z", 
			ClickyPaperGlyph.Music => "M9 18V6l10-2v12 M6.5 15.5a2.5 2.5 0 1 0 0 5a2.5 2.5 0 1 0 0-5Z M16.5 13.5a2.5 2.5 0 1 0 0 5a2.5 2.5 0 1 0 0-5Z", 
			ClickyPaperGlyph.Code => "M8.5 7 3.5 12l5 5 M15.5 7l5 5-5 5 M13.5 4.5l-3 15", 
			ClickyPaperGlyph.Mail => "M3.5 6.5h17v11h-17Z M3.5 7l8.5 6.5L20.5 7", 
			ClickyPaperGlyph.Pin => "M12 21s-6.5-6.2-6.5-11a6.5 6.5 0 0 1 13 0c0 4.8-6.5 11-6.5 11Z M12 7.5a2.5 2.5 0 1 0 0 5a2.5 2.5 0 1 0 0-5Z", 
			_ => "M4 4v16h16 M7.5 15l3.5-4 3 2.5 5-6", 
		};
	}

	public static PaperPath Path(this ClickyPaperGlyph glyph)
	{
		lock (cache)
		{
			if (cache.TryGetValue(glyph, out PaperPath value))
			{
				return value;
			}
			PaperPath paperPath = ClickySVGPath.Path(glyph.SvgPathData());
			cache[glyph] = paperPath;
			return paperPath;
		}
	}
}
