using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkQuestionAnswerRules
{
	public static List<string> Options(ClickyInkDocument.Block block)
	{
		return ClickyInkChoices.Of(block["options"] ?? block["choices"]).Take(6).ToList();
	}

	public static string? TypedPlaceholder(ClickyInkDocument.Block block)
	{
		string[] array = new string[2] { "input", "other" };
		foreach (string text in array)
		{
			string text2 = block[text];
			if (text2 != null)
			{
				if (text2.Length != 0)
				{
					return text2;
				}
				if (!(text == "other"))
				{
					return "Type your answer";
				}
				return "Something else…";
			}
		}
		return null;
	}

	public static bool HasAnswers(ClickyInkDocument.Block block)
	{
		return new string[5] { "options", "choices", "input", "other", "file" }.Any((string key) => block[key] != null);
	}
}
