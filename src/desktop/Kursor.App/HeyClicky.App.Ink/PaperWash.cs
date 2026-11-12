using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed record PaperWash(PaperFill Fill, Func<Size, Rect>? Frame = null, bool Clipped = true, Func<Size, PaperPath>? Path = null);
