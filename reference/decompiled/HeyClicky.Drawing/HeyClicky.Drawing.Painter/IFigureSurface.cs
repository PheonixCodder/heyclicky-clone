using System;
using HeyClicky.Drawing3D;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal interface IFigureSurface : IStageSurface, IDisposable
{
	ICompositionSurface Surface { get; }
}
