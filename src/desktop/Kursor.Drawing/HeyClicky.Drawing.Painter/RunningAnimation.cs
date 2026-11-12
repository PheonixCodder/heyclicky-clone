using System.Numerics;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal readonly record struct RunningAnimation(CompositionObject Target, string Property, Vector3 FinalValue, int Components);
