using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkConnect
{
	private sealed class State : InkObjectState
	{
		public bool Subscribed;

		public bool Built;

		public ClickyInkConnections.Phase? ShownPhase;

		public double PhaseChangedAt = -1000.0;

		public bool StampShown;

		public double StampChangedAt = -1000.0;
	}

	private static readonly Color Ink = PaperColor.Hex("#3A2A12");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "connect");
		if (!state.Subscribed)
		{
			state.Subscribed = true;
			ClickyInkConnections.Shared.Changed += delegate
			{
				state.Animate(InkSpring.SettleSeconds(0.32, 0.85));
			};
		}
		return new InkObjectHost(state, () => Build(block, style, actions, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, ClickyInkStyle style, ClickyInkActions actions, State state)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkConnections connections = ClickyInkConnections.Shared;
		ClickyInkDocument.Block source = block.Source;
		string text = ClickyInkNoteActions.Plain(source.Text);
		string text2 = ClickyInkNoteActions.Plain(source["app"] ?? text);
		ClickyInkConnections.App catalogApp = connections.AppNamed(text2) ?? connections.AppNamed(text);
		string appName = catalogApp?.DisplayName ?? ((text.Length == 0) ? text2 : text);
		string slug = catalogApp?.Slug ?? text2;
		ClickyInkConnections.Phase phase = connections.PhaseFor(slug);
		bool isAlreadyConnected = (object)catalogApp != null && catalogApp.IsConnected && !catalogApp.NeedsReconnect;
		string text3 = ClickyInkConnectRules.Title(phase, appName, isAlreadyConnected, catalogApp?.NeedsReconnect ?? false);
		bool flag = ClickyInkConnectRules.ShowsConnectedStamp(phase, isAlreadyConnected);
		if (!state.Built)
		{
			state.Built = true;
			state.ShownPhase = phase;
			state.StampShown = flag;
		}
		if (!object.Equals(state.ShownPhase, phase))
		{
			state.ShownPhase = phase;
			state.PhaseChangedAt = InkObjectState.Now;
		}
		if (flag != state.StampShown)
		{
			state.StampShown = flag;
			state.StampChangedAt = InkObjectState.Now;
		}
		StackPanel stackPanel = InkViews.VStack(7.0, ClickyInkRubberStamp.TypedLabel(ClickyInkConnectRules.Label(phase, isAlreadyConnected), Ink.Opacity(0.5)), InkViews.Plain(text3, ClickyPaperFonts.Typed(15.0, FontWeights.Bold), Ink));
		string text4 = source["why"] ?? source["note"];
		if (text4 != null && text4.Length > 0 && !(phase is ClickyInkConnections.Phase.Connected))
		{
			stackPanel.Children.Add(InkViews.Plain(ClickyInkNoteActions.Plain(text4), ClickyPaperFonts.Hand(15.0), Ink.Opacity(0.7)));
		}
		stackPanel.Children.Add(Controls(phase, text3, appName, isAlreadyConnected, style, delegate
		{
			actions.Perform(ClickyInkNoteActions.Connected(source).ForAnalytics("connect_confirmed", "already_connected"));
		}, StartConnecting));
		UIElementCollection children = stackPanel.Children;
		children[children.Count - 1].Opacity = Math.Clamp(InkSpring.Value(0.32, 0.85, InkObjectState.Now - state.PhaseChangedAt), 0.0, 1.0);
		ClickyInkHStack content = new ClickyInkHStack(13.0, InkVerticalAlignment.Top, Logo(catalogApp), ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer())
		{
			Margin = new Thickness(46.0, 15.0, 16.0, 15.0)
		};
		ClickyInkLuggageTagShape shape = new ClickyInkLuggageTagShape();
		InkPaperSheet background = new InkPaperSheet("connect-tag", shape, new PaperFill.Linear(new(double, Color)[2]
		{
			(0.0, PaperColor.Hex("#FDF9F0")),
			(1.0, PaperColor.Hex("#F7EFDE"))
		}, new Point(0f, 0f), new Point(1f, 1f)), 0.3, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.1), 3.0, 0.0, 1.5)
		});
		ClickyShapeView item = ClickyShapeView.Stroke(shape, new PaperStroke(PaperColor.Hex("#A9824B").Opacity(0.22)));
		FrameworkElement item2 = Twine();
		FrameworkElement item3 = Grommet();
		List<UIElement> list = new List<UIElement> { item, item2, item3 };
		if (flag || InkObjectState.Now - state.StampChangedAt < 1.5)
		{
			double num = Math.Clamp(InkSpring.Value(0.32, 0.85, InkObjectState.Now - state.StampChangedAt), 0.0, 1.2);
			double num2 = (flag ? num : (1.0 - Math.Min(1.0, num)));
			if (num2 > 0.001)
			{
				FrameworkElement frameworkElement = ClickyInkRubberStamp.Make("connected", ClickyPaperInk.DoneGreen, 6.0);
				frameworkElement.HorizontalAlignment = HorizontalAlignment.Right;
				frameworkElement.VerticalAlignment = VerticalAlignment.Top;
				frameworkElement.Margin = new Thickness(0.0, 10.0, 12.0, 0.0);
				Grid grid = new Grid
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					Children = { (UIElement)frameworkElement }
				};
				grid.Transformed(0.0, 0.0, 0.0, 1.6 - 0.6 * num2, null);
				grid.Opacity = Math.Clamp(num2, 0.0, 1.0);
				list.Add(grid);
			}
		}
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(content, list.ToArray()).Behind(background);
		clickyInkOverlay.Width = contentWidth - 24.0;
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay.Margin = new Thickness(18.0, 0.0, 0.0, 0.0);
		clickyInkOverlay.Transformed(0.0, 0.0, -0.3, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkOverlay }
		};
		void StartConnecting()
		{
			Action<ClickyInkNoteAction> perform = actions.Perform;
			actions.Track("connect_started", ClickyInkDocument.Kind.Connect, new JsonObject((JsonNodeOptions?)null)
			{
				["is_retry"] = (object)phase != null,
				["is_reconnect"] = catalogApp?.NeedsReconnect ?? false
			});
			connections.StartConnecting(slug, delegate
			{
				perform(ClickyInkNoteActions.Connected(source).ForAnalytics("connect_confirmed", "after_sign_in"));
			});
		}
	}

	private static FrameworkElement Grommet()
	{
		Grid grid = new Grid();
		grid.Width = 18.0;
		grid.Height = 18.0;
		grid.Margin = new Thickness(13.0, 0.0, 0.0, 0.0);
		grid.HorizontalAlignment = HorizontalAlignment.Left;
		grid.VerticalAlignment = VerticalAlignment.Center;
		grid.IsHitTestVisible = false;
		UIElementCollection children = grid.Children;
		IClickyShape circle = ClickyShape.Circle;
		PaperFill fill = PaperFill.Of(PaperColor.Hex("#C9A15E"));
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.2), 0.8, 0.0, 0.6)
		};
		PaperStroke outline = new PaperStroke(PaperColor.Hex("#8A6A33"), 0.8);
		children.Add(new InkPaperSheet("grommet", circle, fill, null, shadows, outline));
		grid.Children.Add(new Ellipse
		{
			Width = 9.0,
			Height = 9.0,
			Fill = Ui.Brush(PaperColor.Hex("#5E4A2A").Opacity(0.55)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
		return grid;
	}

	private static FrameworkElement Twine()
	{
		PaperPath p = new PaperPath().MoveTo(52.0, 34.0).CubicTo(new Point(2f, 2f), new Point(28f, 40f), new Point(6f, 26f));
		PaperDrawing paperDrawing = new PaperDrawing().Stroke(p, PaperStroke.Round(PaperColor.Hex("#8E6F45").Opacity(0.7), 1.5)).Sized(54.0, 40.0);
		paperDrawing.HorizontalAlignment = HorizontalAlignment.Left;
		paperDrawing.VerticalAlignment = VerticalAlignment.Center;
		paperDrawing.Transformed(-30.0, -17.0, 0.0, 1.0, null);
		return paperDrawing;
	}

	private static FrameworkElement Logo(ClickyInkConnections.App? app)
	{
		FrameworkElement frameworkElement = (((object)app != null) ? IntegrationLogos.Artwork(app.Slug, app.DisplayName, 28.0, 16.0, app.LogoUrl?.AbsoluteUri) : (InkViews.Symbol("powerplug.fill", 16.0, PaperColor.Hex("#8A5300"), bold: true) ?? new Grid()));
		frameworkElement.Width = 28.0;
		frameworkElement.Height = 28.0;
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		IClickyShape shape = ClickyShape.RoundedRectangle(10.0);
		PaperFill fill = PaperFill.Of(PaperColor.White);
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.06), 1.0, 0.0, 0.5)
		};
		InkPaperSheet item = new InkPaperSheet("connect-logo", shape, fill, null, shadows);
		ClickyShapeView item2 = ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(10.0), new PaperStroke(PaperColor.Hex("#A9824B").Opacity(0.4)));
		Grid obj = new Grid
		{
			Width = 44.0,
			Height = 44.0,
			VerticalAlignment = VerticalAlignment.Top,
			Children = 
			{
				(UIElement)item,
				(UIElement)frameworkElement,
				(UIElement)item2
			}
		};
		obj.Transformed(0.0, 0.0, -1.5, 1.0, null);
		return obj;
	}

	private static FrameworkElement Controls(ClickyInkConnections.Phase? phase, string title, string appName, bool isAlreadyConnected, ClickyInkStyle style, Action carryOn, Action startConnecting)
	{
		if (!(phase is ClickyInkConnections.Phase.Opening))
		{
			if (!(phase is ClickyInkConnections.Phase.WaitingForSignIn))
			{
				if (!(phase is ClickyInkConnections.Phase.Checking))
				{
					if (!(phase is ClickyInkConnections.Phase.Connected))
					{
						if (phase is ClickyInkConnections.Phase.Failed failed)
						{
							return InkViews.VStack(7.0, InkViews.Plain(failed.Message, ClickyPaperFonts.Typed(12.0, null), PaperColor.Hex("#C2410C")), new ClickyPaperGelButton("Try again", startConnecting, ClickyPaperGel.Blue, 28.0, 13.0, 12.5, "arrow.clockwise"));
						}
						if (!isAlreadyConnected)
						{
							return new ClickyPaperGelButton(title, startConnecting, ClickyPaperGel.Blue, 28.0, 13.0, 12.5, "link");
						}
						return new ClickyPaperGelButton("Carry on", carryOn, ClickyPaperGel.Blue, 28.0, 13.0, 12.5);
					}
					return InkViews.Plain("Done. The job is carrying on with it.", ClickyPaperFonts.Hand(15.0), ClickyPaperInk.DoneGreen);
				}
				return Progress("Checking the connection…", style);
			}
			Pressable pressable = InkViews.PlainButton(InkViews.Plain("Open the sign-in page again", ClickyPaperFonts.Typed(11.5, FontWeights.Medium), style.Fresh), startConnecting);
			return InkViews.VStack(6.0, Progress("Sign in to " + appName + " in your browser, then come back here.", style), pressable);
		}
		return Progress("Opening the sign-in page…", style);
	}

	private static FrameworkElement Progress(string words, ClickyInkStyle style)
	{
		return new ClickyInkHStack(7.0, InkVerticalAlignment.Center, InkViews.Spinner(16.0), ClickyInkHStack.Flexible(InkViews.Plain(words, ClickyPaperFonts.Typed(12.0, null), style.Quiet)));
	}
}
