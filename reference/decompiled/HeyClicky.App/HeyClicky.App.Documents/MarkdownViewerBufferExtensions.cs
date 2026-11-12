using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace HeyClicky.App.Documents;

internal static class MarkdownViewerBufferExtensions
{
	public static IBuffer AsBuffer(this byte[] bytes)
	{
		return WindowsRuntimeBufferExtensions.AsBuffer(bytes);
	}
}
