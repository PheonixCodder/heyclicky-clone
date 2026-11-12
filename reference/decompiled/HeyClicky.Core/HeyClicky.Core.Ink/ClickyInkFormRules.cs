namespace HeyClicky.Core.Ink;

public static class ClickyInkFormRules
{
	public enum FieldKind
	{
		Choices,
		File,
		Blank
	}

	public static FieldKind Kind(string hint)
	{
		if (ClickyInkChoices.Of(hint).Count > 1)
		{
			return FieldKind.Choices;
		}
		string text = hint.Lowered();
		if ((!(text == "file") && !(text == "folder")) || 1 == 0)
		{
			return FieldKind.Blank;
		}
		return FieldKind.File;
	}
}
