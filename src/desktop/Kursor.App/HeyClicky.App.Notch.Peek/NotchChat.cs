using HeyClicky.App.Home;
using HeyClicky.Core.Notch;

namespace HeyClicky.App.Notch.Peek;

public static class NotchChat
{
	public static ConversationHistory? History { get; set; }

	public static ChatHandOffs? HandOffs { get; set; }

	public static LessonService? Lessons { get; set; }
}
