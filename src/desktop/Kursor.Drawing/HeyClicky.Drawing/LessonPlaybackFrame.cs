namespace HeyClicky.Drawing;

public sealed record LessonPlaybackFrame(byte[] Pixels, int Width, int Height, double Position);
