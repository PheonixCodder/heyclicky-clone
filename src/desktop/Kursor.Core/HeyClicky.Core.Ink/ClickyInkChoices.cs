using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkChoices
{
	public static List<string> Of(string? text)
	{
		return (from p in (text ?? "").Split('|')
			select p.TrimWs() into p
			where p.Length > 0
			select p).ToList();
	}
}
