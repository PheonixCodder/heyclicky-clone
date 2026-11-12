using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public static class ClickyInkEvent
{
	private sealed class State : InkObjectState
	{
		public bool WasAdded;

		public double AddedAt = -1000.0;
	}

	private static readonly Color Ink = PaperColor.Hex("#3A2C1A");

	private static readonly Color Gold = PaperColor.Hex("#B89150");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions? actions = null)
	{
		State state = InkObjectState.For<State>(block, "event");
		return new InkObjectHost(state, () => Build(block, clock, style, state, actions));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, State state, ClickyInkActions? actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkDocument.Block source = block.Source;
		string? text = source["when"] ?? source["start"] ?? source["date"];
		DateTimeOffset? start = ClickyInkEventDates.Date(text);
		DateTimeOffset? end = ((!start.HasValue) ? ((DateTimeOffset?)null) : ClickyInkEventDates.End(start: start.GetValueOrDefault(), text: source["end"]));
		bool isAllDay = ClickyInkEventDates.IsDateOnly(text);
		StackPanel stackPanel = InkViews.VStack(5.0, ClickyInkRubberStamp.TypedLabel("save the date", Gold), InkViews.Words(block.Words, clock, style, InkObjectFonts.Serif(16.0, FontWeights.SemiBold), Ink));
		if (start.HasValue)
		{
			DateTimeOffset valueOrDefault2 = start.GetValueOrDefault();
			TextBlock item = InkObjectFonts.Serif(12.5, null).Apply(new TextBlock
			{
				Text = ClickyInkEventText.When(valueOrDefault2, end, isAllDay),
				FontStyle = FontStyle.Italic,
				Foreground = Ui.Brush(Ink.Opacity(0.7)),
				TextWrapping = TextWrapping.Wrap,
				LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
				LineHeight = 14.912500000000001
			});
			stackPanel.Children.Add(item);
		}
		string text2 = source["where"] ?? source["place"];
		if (text2 != null && text2.Length > 0)
		{
			FrameworkElement frameworkElement = InkViews.Symbol("mappin.and.ellipse", 12.0, Ink.Opacity(0.7)) ?? new Grid();
			ClickyInkHStack item2 = new ClickyInkHStack(5.0, InkVerticalAlignment.Center, frameworkElement, ClickyInkHStack.Flexible(InkViews.Plain(ClickyInkNoteActions.Plain(text2), InkObjectFonts.Serif(12.0, null), Ink.Opacity(0.7), delegate(InkTextView t)
			{
				t.LineLimit = 1;
			})));
			stackPanel.Children.Add(item2);
		}
		if (start.HasValue)
		{
			ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(state.WasAdded ? "Opened in Calendar" : "Add to Calendar", delegate
			{
				AddToCalendar(block, state, actions);
			}, state.WasAdded ? ClickyPaperGel.Mint : ClickyPaperGel.Blue, 26.0, 13.0, 12.0, state.WasAdded ? "checkmark" : "calendar.badge.plus")
			{
				Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
			};
			clickyPaperGelButton.Opacity = Math.Clamp(InkSpring.Value(0.3, 0.8, InkObjectState.Now - state.AddedAt), 0.0, 1.0);
			stackPanel.Children.Add(clickyPaperGelButton);
		}
		ClickyInkHStack content = new ClickyInkHStack(16.0, InkVerticalAlignment.Top, CalendarPage(start), ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer())
		{
			Margin = new Thickness(20.0, 20.0, 20.0, 18.0)
		};
		InkPaperSheet background = new InkPaperSheet("event-card", ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#FFFCF4")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.07), 2.0, 0.0, 1.0)
		});
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(ClickyShape.Rectangle, new PaperStroke(Gold.Opacity(0.45)));
		clickyShapeView.Margin = new Thickness(6.0);
		ClickyShapeView clickyShapeView2 = ClickyShapeView.Stroke(ClickyShape.Rectangle, new PaperStroke(Gold.Opacity(0.25), 0.6));
		clickyShapeView2.Margin = new Thickness(9.0);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(content, clickyShapeView, clickyShapeView2).Behind(background);
		clickyInkOverlay.Width = contentWidth - 12.0;
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay.Transformed(0.0, 0.0, -0.25, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
			Children = { (UIElement)clickyInkOverlay }
		};
	}

	private static FrameworkElement CalendarPage(DateTimeOffset? start)
	{
		string text;
		string text2;
		string text3;
		if (start.HasValue)
		{
			DateTimeOffset valueOrDefault = start.GetValueOrDefault();
			(text, text2, text3) = ClickyInkEventText.Page(valueOrDefault);
		}
		else
		{
			text = "—";
			text2 = "?";
			text3 = "";
		}
		Color color = PaperColor.Hex("#2B2620");
		InkTextView inkTextView = InkViews.Plain(text, ClickyPaperFonts.Typed(10.5, FontWeights.ExtraBold), PaperColor.White, delegate(InkTextView t)
		{
			t.Tracking = 1.0;
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView.VerticalAlignment = VerticalAlignment.Center;
		Grid grid = new Grid
		{
			Height = 21.0,
			Background = Ui.Brush(PaperColor.Hex("#DE5A52")),
			Children = { (UIElement)inkTextView }
		};
		InkTextView inkTextView2 = InkViews.Plain(text2, InkObjectFonts.Rounded(28.0, FontWeights.ExtraBold), color);
		inkTextView2.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView2.VerticalAlignment = VerticalAlignment.Center;
		Grid grid2 = new Grid
		{
			Height = 38.0,
			Children = { (UIElement)inkTextView2 }
		};
		InkTextView inkTextView3 = InkViews.Plain(text3, ClickyPaperFonts.Typed(9.0, FontWeights.SemiBold), color.Opacity(0.55), delegate(InkTextView t)
		{
			t.Tracking = 0.8;
		});
		inkTextView3.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView3.VerticalAlignment = VerticalAlignment.Center;
		Grid grid3 = new Grid
		{
			Height = 15.0,
			Children = { (UIElement)inkTextView3 }
		};
		StackPanel item = InkViews.VStack(0.0, grid, grid2, grid3);
		Grid content = new Grid
		{
			Width = 62.0,
			CornerRadius = new CornerRadius(6.0),
			Background = Ui.Brush(PaperColor.White),
			Children = { (UIElement)item }
		};
		IClickyShape shape = ClickyShape.RoundedRectangle(6.0);
		PaperFill fill = PaperFill.Of(PaperColor.White);
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.08), 1.5, 0.0, 1.0)
		};
		InkPaperSheet background = new InkPaperSheet("event-page", shape, fill, null, shadows);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 22.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top
		};
		for (int i = 0; i < 2; i++)
		{
			Rectangle item2 = new Rectangle
			{
				Width = 7.0,
				Height = 14.0,
				RadiusX = 3.5,
				RadiusY = 3.5,
				StrokeThickness = 2.4,
				Stroke = new LinearGradientBrush
				{
					StartPoint = new Point(0.5, 0.0),
					EndPoint = new Point(0.5, 1.0),
					GradientStops = 
					{
						new GradientStop
						{
							Color = PaperColor.Hex("#8E939C"),
							Offset = 0.0
						},
						new GradientStop
						{
							Color = PaperColor.Hex("#4E535C"),
							Offset = 1.0
						}
					}
				}
			};
			stackPanel.Children.Add(item2);
		}
		stackPanel.Transformed(0.0, -8.0, 0.0, 1.0, null);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(content, stackPanel).Behind(background);
		clickyInkOverlay.Width = 62.0;
		clickyInkOverlay.VerticalAlignment = VerticalAlignment.Top;
		clickyInkOverlay.Transformed(0.0, 0.0, -1.5, 1.0, null);
		clickyInkOverlay.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		return clickyInkOverlay;
	}

	private static void AddToCalendar(ClickyInkLiveNote.Block block, State state, ClickyInkActions? actions)
	{
		ClickyInkDocument.Block source = block.Source;
		string text = source["when"] ?? source["start"] ?? source["date"];
		DateTimeOffset? dateTimeOffset = ClickyInkEventDates.Date(text);
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			DateTimeOffset? end = ClickyInkEventDates.End(source["end"], valueOrDefault);
			string text2 = ClickyInkNoteActions.Plain(source.Text);
			string contents = ClickyInkEventDates.Ics((text2.Length == 0) ? "Event" : text2, valueOrDefault, end, ClickyInkEventDates.IsDateOnly(text), source["where"] ?? source["place"], source["note"] ?? source["url"]);
			try
			{
				string text3 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clicky-events");
				Directory.CreateDirectory(text3);
				string text4 = System.IO.Path.Combine(text3, Guid.NewGuid().ToString("N").Substring(0, 8)
					.ToUpperInvariant() + ".ics");
				File.WriteAllText(text4, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				ShellOpen.Start(text4);
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "add to calendar: " + ex.Message);
				return;
			}
			actions?.Track("event_added_to_calendar", ClickyInkDocument.Kind.Event, new JsonObject((JsonNodeOptions?)null)
			{
				["is_all_day"] = ClickyInkEventDates.IsDateOnly(text),
				["has_end"] = end.HasValue,
				["was_added_before"] = state.WasAdded
			});
			state.WasAdded = true;
			state.AddedAt = InkObjectState.Now;
			state.Changed();
			state.Animate(InkSpring.SettleSeconds(0.3, 0.8));
		}
	}
}
