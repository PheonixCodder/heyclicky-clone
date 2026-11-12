using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.App.Notch;

public sealed class NotchCornerGlow
{
	private enum Style
	{
		Wave,
		Orbit,
		Pulse
	}

	private sealed record Appearance(Color Primary, Color Secondary, Color Tertiary, double Opacity, Style Style, double Cycle, double Amplitude, double PulseCycle, double PulseCenter, double PulseAmplitude);

	private readonly Compositor c;

	private readonly CompositionPropertySet props;

	private readonly ContainerVisual pulseLayer;

	private readonly (SpriteVisual sprite, CompositionRadialGradientBrush blob)[] blobs = new(SpriteVisual, CompositionRadialGradientBrush)[3];

	private readonly CompositionColorGradientStop[][] blobStops = new CompositionColorGradientStop[3][];

	private Appearance? current;

	private bool motionInstalled;

	private static readonly (float ax, float ay, float k, float intensity)[] Specs = new(float, float, float, float)[3]
	{
		(0.05f, 1f, 1.05f, 1f),
		(0.22f, 0.95f, 0.85f, 1f),
		(0.42f, 0.92f, 0.65f, 0.9f)
	};

	private static readonly float[] BlobStops = new float[5] { 0f, 0.35f, 0.65f, 0.85f, 1f };

	private static readonly float[] BlobFactors = new float[5] { 1f, 0.95f, 0.7f, 0.3f, 0f };

	private static readonly float[] MaskStops = new float[8] { 0f, 0.42f, 0.55f, 0.68f, 0.8f, 0.9f, 0.97f, 1f };

	private static readonly float[] MaskAlphas = new float[8] { 1f, 1f, 0.88f, 0.65f, 0.38f, 0.18f, 0.05f, 0f };

	public ContainerVisual Root { get; }

	public NotchCornerGlow(Compositor compositor, CompositionPropertySet silhouetteProps, CompositionGeometry clip)
	{
		c = compositor;
		props = silhouetteProps;
		Root = c.CreateContainerVisual();
		Root.RelativeSizeAdjustment = Vector2.One;
		Root.Opacity = 0f;
		pulseLayer = c.CreateContainerVisual();
		pulseLayer.RelativeSizeAdjustment = Vector2.One;
		Root.Children.InsertAtTop(pulseLayer);
		ContainerVisual containerVisual = Part(c.CreateGeometricClip(clip));
		ContainerVisual containerVisual2 = Part(TrailingShoulderClip());
		for (int i = 0; i < 3; i++)
		{
			(float ax, float ay, float k, float intensity) tuple = Specs[i];
			float item = tuple.ax;
			float item2 = tuple.ay;
			float item3 = tuple.k;
			CompositionRadialGradientBrush compositionRadialGradientBrush = c.CreateRadialGradientBrush();
			compositionRadialGradientBrush.MappingMode = CompositionMappingMode.Absolute;
			blobStops[i] = new CompositionColorGradientStop[BlobStops.Length];
			for (int j = 0; j < BlobStops.Length; j++)
			{
				blobStops[i][j] = c.CreateColorGradientStop(BlobStops[j], Colors.Transparent);
				compositionRadialGradientBrush.ColorStops.Add(blobStops[i][j]);
			}
			Bind(compositionRadialGradientBrush, "EllipseCenter", FormattableString.Invariant($"Vector2(p.Width * {1f - item:0.###}, p.Height * {item2:0.###})"));
			Bind(compositionRadialGradientBrush, "EllipseRadius", FormattableString.Invariant($"Vector2(Max(p.Height, 40) * {item3:0.###}, Max(p.Height, 40) * {item3:0.###})"));
			CompositionRadialGradientBrush compositionRadialGradientBrush2 = c.CreateRadialGradientBrush();
			compositionRadialGradientBrush2.MappingMode = CompositionMappingMode.Absolute;
			for (int k = 0; k < MaskStops.Length; k++)
			{
				compositionRadialGradientBrush2.ColorStops.Add(c.CreateColorGradientStop(MaskStops[k], Color.FromArgb((byte)(MaskAlphas[k] * 255f), byte.MaxValue, byte.MaxValue, byte.MaxValue)));
			}
			Bind(compositionRadialGradientBrush2, "EllipseCenter", "Vector2(p.Width, p.Height)");
			Bind(compositionRadialGradientBrush2, "EllipseRadius", "Vector2(p.Height * 1.6, p.Height * 1.6)");
			CompositionMaskBrush compositionMaskBrush = c.CreateMaskBrush();
			compositionMaskBrush.Source = compositionRadialGradientBrush;
			compositionMaskBrush.Mask = compositionRadialGradientBrush2;
			SpriteVisual spriteVisual = Sprite(compositionMaskBrush);
			containerVisual.Children.InsertAtTop(spriteVisual);
			containerVisual2.Children.InsertAtTop(Sprite(compositionMaskBrush));
			blobs[i] = (sprite: spriteVisual, blob: compositionRadialGradientBrush);
		}
	}

	private ContainerVisual Part(CompositionClip clip)
	{
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.RelativeSizeAdjustment = Vector2.One;
		containerVisual.Clip = clip;
		pulseLayer.Children.InsertAtTop(containerVisual);
		return containerVisual;
	}

	private SpriteVisual Sprite(CompositionBrush brush)
	{
		SpriteVisual spriteVisual = c.CreateSpriteVisual();
		spriteVisual.Brush = brush;
		Bind(spriteVisual, "Offset", "Vector3((p.EnvW - p.Width) / 2, p.Gap, 0)");
		Bind(spriteVisual, "Size", "Vector2(p.Width, p.Height)");
		return spriteVisual;
	}

	private CompositionGeometricClip TrailingShoulderClip()
	{
		float num = 10f;
		CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
		canvasPathBuilder.BeginFigure(0f, 0f);
		canvasPathBuilder.AddLine(num, 0f);
		canvasPathBuilder.AddArc(new Vector2(0f, num), num, num, 0f, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
		CompositionGeometricClip compositionGeometricClip = c.CreateGeometricClip(c.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(canvasPathBuilder))));
		Bind(compositionGeometricClip, "Offset", FormattableString.Invariant($"Vector2((p.EnvW + p.Width) / 2 - {num}, p.Gap)"));
		Bind(compositionGeometricClip, "Scale", "Vector2(p.Concavity, p.Concavity)");
		return compositionGeometricClip;
	}

	private void Bind(CompositionObject target, string property, string expression)
	{
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(expression);
		expressionAnimation.SetReferenceParameter("p", props);
		target.StartAnimation(property, expressionAnimation);
	}

	private static Appearance? Resolve(NotchPresentation p, Color? agentTint)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			NotchLiveEvent @event = liveEvent.Event;
			if (!NotchGeometry.IsWorkingBar(p))
			{
				if (!(@event is NotchLiveEvent.ActivityListening))
				{
					if (!(@event is NotchLiveEvent.ActivityThinking) && !(@event is NotchLiveEvent.ActivityCustomStatus) && !(@event is NotchLiveEvent.AgentMultiActivity))
					{
						if (!(@event is NotchLiveEvent.Dictation))
						{
							if (!(@event is NotchLiveEvent.ActivitySpeaking))
							{
								if (!(@event is NotchLiveEvent.RestingMark))
								{
									if (!(@event is NotchLiveEvent.AgentTask))
									{
										if (!(@event is NotchLiveEvent.IntegrationConnected) && !(@event is NotchLiveEvent.HandoffQueued) && !(@event is NotchLiveEvent.HandoffSent))
										{
											if (@event is NotchLiveEvent.IntegrationSuggestion || @event is NotchLiveEvent.SidecarLearningSuggestion || @event is NotchLiveEvent.SpotifyVoiceTip)
											{
												return new Appearance(DS.Colors.CursorBlue, DS.Rgb(0.25, 0.75, 1.0), DS.Rgb(0.7, 0.9, 1.0), 0.85, Style.Orbit, 12.0, 6.0, 6.0, 0.9, 0.12);
											}
											return null;
										}
										return new Appearance(DS.Rgb(0.35, 0.95, 0.35), DS.Rgb(0.65, 1.0, 0.4), DS.Rgb(0.3, 0.9, 0.55), 1.0, Style.Pulse, 5.0, 7.0, 2.5, 0.9, 0.18);
									}
									if (agentTint.HasValue)
									{
										Color valueOrDefault = agentTint.GetValueOrDefault();
										return new Appearance(valueOrDefault, valueOrDefault.WithAlpha(0.85), valueOrDefault.WithAlpha(0.95), 1.0, Style.Orbit, 9.0, 10.0, 4.5, 0.93, 0.16);
									}
									return new Appearance(DS.Rgb(0.2, 0.95, 0.55), DS.Rgb(0.3, 0.85, 1.0), DS.Rgb(0.65, 1.0, 0.4), 1.0, Style.Orbit, 9.0, 10.0, 4.5, 0.93, 0.16);
								}
								Color primary = DS.Hex(1755979u);
								return new Appearance(primary, primary.WithAlpha(0.85), primary.WithAlpha(0.95), 1.0, Style.Orbit, 11.0, 10.0, 5.5, 0.92, 0.18);
							}
							return new Appearance(DS.Rgb(1.0, 0.5, 0.2), DS.Rgb(1.0, 0.25, 0.45), DS.Rgb(1.0, 0.75, 0.35), 1.0, Style.Pulse, 6.0, 10.0, 3.0, 0.92, 0.14);
						}
						return new Appearance(DS.Rgb(0.129, 0.773, 0.369), DS.Rgb(0.05, 0.6, 0.4), DS.Rgb(0.55, 0.95, 0.65), 1.0, Style.Wave, 5.0, 8.0, 2.6, 0.92, 0.18);
					}
					if (agentTint.HasValue)
					{
						Color valueOrDefault2 = agentTint.GetValueOrDefault();
						return new Appearance(valueOrDefault2, valueOrDefault2.WithAlpha(0.85), valueOrDefault2.WithAlpha(0.95), 1.0, Style.Orbit, 11.0, 10.0, 5.5, 0.92, 0.18);
					}
					return new Appearance(DS.Rgb(0.85, 0.15, 1.0), DS.Rgb(1.0, 0.2, 0.7), DS.Rgb(0.55, 0.35, 1.0), 1.0, Style.Orbit, 11.0, 10.0, 5.5, 0.92, 0.18);
				}
				return new Appearance(DS.Rgb(0.1, 0.92, 1.0), DS.Rgb(0.2, 0.5, 1.0), DS.Rgb(0.55, 1.0, 1.0), 1.0, Style.Wave, 6.0, 8.0, 3.0, 0.92, 0.18);
			}
		}
		return null;
	}

	public void Update(NotchPresentation p, Color? agentTint = null)
	{
		Appearance appearance = Resolve(p, agentTint);
		if (appearance == current)
		{
			return;
		}
		Appearance appearance2 = current;
		current = appearance;
		if ((object)appearance == null)
		{
			FadeTo(0f, 0.34);
			return;
		}
		bool crossFade = (object)appearance2 != null && !Anim.Instant;
		Color[] array = new Color[3] { appearance.Primary, appearance.Secondary, appearance.Tertiary };
		for (int i = 0; i < 3; i++)
		{
			Color color = array[i];
			for (int j = 0; j < BlobStops.Length; j++)
			{
				SetStopColor(blobStops[i][j], Color.FromArgb((byte)Math.Round((float)(int)color.A * Specs[i].intensity * BlobFactors[j]), color.R, color.G, color.B), crossFade);
			}
		}
		if (!motionInstalled || (object)appearance2 == null || !SameMotion(appearance2, appearance))
		{
			for (int k = 0; k < 3; k++)
			{
				InstallMotion(blobs[k].blob, appearance, k);
			}
			InstallPulse(appearance);
			motionInstalled = true;
		}
		FadeTo((float)appearance.Opacity, 0.34);
	}

	private static bool SameMotion(Appearance x, Appearance y)
	{
		if (x.Style == y.Style && x.Cycle == y.Cycle && x.Amplitude == y.Amplitude && x.PulseCycle == y.PulseCycle && x.PulseCenter == y.PulseCenter)
		{
			return x.PulseAmplitude == y.PulseAmplitude;
		}
		return false;
	}

	private void SetStopColor(CompositionColorGradientStop stop, Color color, bool crossFade)
	{
		if (!crossFade)
		{
			stop.StopAnimation("Color");
			stop.Color = color;
			return;
		}
		ColorKeyFrameAnimation colorKeyFrameAnimation = c.CreateColorKeyFrameAnimation();
		colorKeyFrameAnimation.InsertKeyFrame(1f, color, SystemEasing.Expressive(c));
		colorKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.34);
		stop.StartAnimation("Color", colorKeyFrameAnimation);
	}

	private void FadeTo(float opacity, double seconds)
	{
		if (Anim.Instant)
		{
			Root.StopAnimation("Opacity");
			Root.Opacity = opacity;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, opacity, SystemEasing.Expressive(c));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		Root.StartAnimation("Opacity", scalarKeyFrameAnimation);
	}

	private void InstallMotion(CompositionRadialGradientBrush brush, Appearance a, int blobIndex)
	{
		if (a.Amplitude == 0.0 || a.Cycle <= 0.0)
		{
			brush.StopAnimation("Offset");
			brush.Offset = Vector2.Zero;
			return;
		}
		double num = (double)blobIndex * Math.PI / 2.0;
		double amplitude = a.Amplitude;
		(double, double, double, double) tuple = a.Style switch
		{
			Style.Wave => (amplitude * -1.0, (0.0 - amplitude) * 0.18, num - Math.PI / 2.0, a.Cycle), 
			Style.Orbit => (amplitude * -1.0, amplitude, num, a.Cycle), 
			_ => (amplitude * 0.45 * -1.0, amplitude * 0.45, num, a.Cycle), 
		};
		Vector2KeyFrameAnimation vector2KeyFrameAnimation = c.CreateVector2KeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		for (int i = 0; i <= 64; i++)
		{
			double num2 = (double)i / 64.0 * tuple.Item4 * 2.0 * Math.PI / tuple.Item4 + tuple.Item3;
			vector2KeyFrameAnimation.InsertKeyFrame((float)i / 64f, new Vector2((float)(tuple.Item1 * Math.Cos(num2)), (float)(tuple.Item2 * Math.Sin(num2))), easingFunction);
		}
		vector2KeyFrameAnimation.Duration = TimeSpan.FromSeconds(tuple.Item4);
		vector2KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
		brush.StartAnimation("Offset", vector2KeyFrameAnimation);
	}

	private void InstallPulse(Appearance a)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		for (int i = 0; i <= 64; i++)
		{
			double num = (double)i / 64.0 * a.PulseCycle;
			double value = a.PulseCenter + Math.Sin(num * 2.0 * Math.PI / a.PulseCycle) * a.PulseAmplitude;
			scalarKeyFrameAnimation.InsertKeyFrame((float)i / 64f, (float)Math.Clamp(value, 0.0, 1.0), easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(a.PulseCycle);
		scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
		pulseLayer.StartAnimation("Opacity", scalarKeyFrameAnimation);
	}

	public void Quiesce()
	{
		pulseLayer.StopAnimation("Opacity");
		(SpriteVisual, CompositionRadialGradientBrush)[] array = blobs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Item2.StopAnimation("Offset");
		}
		motionInstalled = false;
	}
}
