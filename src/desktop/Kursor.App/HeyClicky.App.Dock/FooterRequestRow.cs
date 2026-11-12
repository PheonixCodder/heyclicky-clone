using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HeyClicky.App.Browser;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Dock;

internal sealed class FooterRequestRow : StackPanel
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass6_0
	{
		public double? buttonHeight;

		public double? labelSize;

		public GelButton.Sizing sizing;

		public FooterRequestRow _003C_003E4__this;

		internal void _003CButtons_003Eb__4()
		{
			_003C_003E4__this.dock.Retry(_003C_003E4__this.content);
		}

		internal void _003CButtons_003Eb__5()
		{
			_003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.DoItForMe, _003C_003E4__this.content);
		}

		internal void _003CButtons_003Eb__6()
		{
			_003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.ShowMeHow, _003C_003E4__this.content);
		}

		internal void _003CButtons_003Eb__7()
		{
			_003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.NotNow, _003C_003E4__this.content);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass6_1
	{
		public string answer;

		public Action click;

		public _003C_003Ec__DisplayClass6_0 CS_0024_003C_003E8__locals4;

		internal void _003CButtons_003Eb__1()
		{
			CS_0024_003C_003E8__locals4._003C_003E4__this.TrackAnswer(answer);
			click();
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass6_2
	{
		public string _003CtaskId_003E5__2;

		public string _003CjobId_003E5__3;

		public string _003CtaskId_003E5__4;

		public _003C_003Ec__DisplayClass6_0 CS_0024_003C_003E8__locals5;

		internal void _003CButtons_003Eb__2()
		{
			CS_0024_003C_003E8__locals5._003C_003E4__this.dock.LetClickyDrive(_003CtaskId_003E5__2);
		}

		internal void _003CButtons_003Eb__3()
		{
			CS_0024_003C_003E8__locals5._003C_003E4__this.dock.DeclineComputerUse(_003CtaskId_003E5__2);
		}

		internal void _003CButtons_003Eb__8()
		{
			CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ContinueAfterUserStep(_003CjobId_003E5__3);
		}

		internal void _003CButtons_003Eb__9()
		{
			CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ApproveExtraUsage(_003CtaskId_003E5__4, always: true);
		}

		internal void _003CButtons_003Eb__10()
		{
			CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ApproveExtraUsage(_003CtaskId_003E5__4, always: false);
		}
	}

	[CompilerGenerated]
	private sealed class _003CButtons_003Ed__6 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private double? buttonHeight;

		public double? _003C_003E3__buttonHeight;

		public FooterRequestRow _003C_003E4__this;

		private bool isLarge;

		public bool _003C_003E3__isLarge;

		private CardFooterRequest request;

		public CardFooterRequest _003C_003E3__request;

		private _003C_003Ec__DisplayClass6_2 _003C_003E8__1;

		private ChromeSetupFlow _003Cflow_003E5__2;

		private bool _003CcanDrive_003E5__3;

		FrameworkElement IEnumerator<FrameworkElement>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CButtons_003Ed__6(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E8__1 = null;
			_003Cflow_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			FooterRequestRow footerRequestRow = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				_003C_003Ec__DisplayClass6_0 _003C_003Ec__DisplayClass6_ = new _003C_003Ec__DisplayClass6_0();
				_003C_003Ec__DisplayClass6_.buttonHeight = buttonHeight;
				_003C_003Ec__DisplayClass6_._003C_003E4__this = _003C_003E4__this;
				_003C_003Ec__DisplayClass6_.labelSize = (isLarge ? new double?(15.5) : ((!_003C_003Ec__DisplayClass6_.buttonHeight.HasValue) ? ((double?)null) : new double?(13.0)));
				_003C_003Ec__DisplayClass6_.sizing = (isLarge ? GelButton.Sizing.Large : GelButton.Sizing.Normal);
				_003C_003E8__1 = new _003C_003Ec__DisplayClass6_2();
				_003C_003E8__1.CS_0024_003C_003E8__locals5 = _003C_003Ec__DisplayClass6_;
				switch (request.Kind)
				{
				case CardFooterRequest.Kinds.ComputerUse:
					_003C_003E8__1._003CtaskId_003E5__2 = footerRequestRow.content.TaskId;
					if (_003C_003E8__1._003CtaskId_003E5__2 != null)
					{
						_003C_003E2__current = Button("Let HeyClicky drive", null, GelAppearance.PrimaryBlue, "drive_approved", delegate
						{
							_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.LetClickyDrive(_003C_003E8__1._003CtaskId_003E5__2);
						});
						_003C_003E1__state = 1;
						return true;
					}
					goto end_IL_000f;
				case CardFooterRequest.Kinds.Failed:
				case CardFooterRequest.Kinds.Stopped:
					_003C_003E2__current = Button("Retry", "arrow.clockwise", GelAppearance.PrimaryBlue, "retry", delegate
					{
						_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.Retry(_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.content);
					});
					_003C_003E1__state = 3;
					return true;
				case CardFooterRequest.Kinds.ConnectChrome:
					break;
				case CardFooterRequest.Kinds.UserStep:
					_003C_003E8__1._003CjobId_003E5__3 = footerRequestRow.content.JobId;
					if (_003C_003E8__1._003CjobId_003E5__3 != null)
					{
						_003C_003E2__current = Button("I'm done", "checkmark", GelAppearance.PrimaryBlue, "user_step_done", delegate
						{
							_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ContinueAfterUserStep(_003C_003E8__1._003CjobId_003E5__3);
						});
						_003C_003E1__state = 7;
						return true;
					}
					goto end_IL_000f;
				case CardFooterRequest.Kinds.ExtraUsage:
					_003C_003E8__1._003CtaskId_003E5__4 = footerRequestRow.content.TaskId;
					if (_003C_003E8__1._003CtaskId_003E5__4 != null)
					{
						_003C_003E2__current = Button("Always approve", "infinity", GelAppearance.PrimaryBlue, "extra_usage_always_approve", delegate
						{
							_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ApproveExtraUsage(_003C_003E8__1._003CtaskId_003E5__4, always: true);
						});
						_003C_003E1__state = 8;
						return true;
					}
					goto end_IL_000f;
				default:
					goto end_IL_000f;
				}
				_003Cflow_003E5__2 = ChromeSetupFlow.Shared;
				_003CcanDrive_003E5__3 = _003Cflow_003E5__2.CanDoItForThem;
				if (_003CcanDrive_003E5__3)
				{
					_003C_003E2__current = Button("Get new extension", null, GelAppearance.PrimaryBlue, "chrome_do_it_for_me", delegate
					{
						_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.DoItForMe, _003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.content);
					});
					_003C_003E1__state = 4;
					return true;
				}
				goto IL_0269;
			}
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = Button("Not now", null, GelAppearance.SecondaryWhite, "drive_declined", delegate
				{
					_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.DeclineComputerUse(_003C_003E8__1._003CtaskId_003E5__2);
				});
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				break;
			case 3:
				_003C_003E1__state = -1;
				break;
			case 4:
				_003C_003E1__state = -1;
				goto IL_0269;
			case 5:
				_003C_003E1__state = -1;
				_003C_003E2__current = Button("Not now", null, GelAppearance.SecondaryWhite, "chrome_not_now", delegate
				{
					_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.NotNow, _003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.content);
				});
				_003C_003E1__state = 6;
				return true;
			case 6:
				_003C_003E1__state = -1;
				break;
			case 7:
				_003C_003E1__state = -1;
				break;
			case 8:
				_003C_003E1__state = -1;
				_003C_003E2__current = Button("Approve once", null, GelAppearance.SecondaryWhite, "extra_usage_approve_once", delegate
				{
					_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ApproveExtraUsage(_003C_003E8__1._003CtaskId_003E5__4, always: false);
				});
				_003C_003E1__state = 9;
				return true;
			case 9:
				{
					_003C_003E1__state = -1;
					break;
				}
				IL_0269:
				_003C_003E2__current = Button(_003CcanDrive_003E5__3 ? "I'll do it" : (_003Cflow_003E5__2.AddsAgain ? "Show me how" : ("Add to " + _003Cflow_003E5__2.BrowserName)), null, _003CcanDrive_003E5__3 ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, "chrome_show_me_how", delegate
				{
					_003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.dock.ChooseChrome(ChromeSetupFlow.Choice.ShowMeHow, _003C_003E8__1.CS_0024_003C_003E8__locals5._003C_003E4__this.content);
				});
				_003C_003E1__state = 5;
				return true;
				end_IL_000f:
				break;
			}
			_003C_003E8__1 = null;
			_003Cflow_003E5__2 = null;
			return false;
			GelButton Button(string title, string? symbol, GelAppearance appearance, string answer, Action click)
			{
				_003C_003Ec__DisplayClass6_1 CS_0024_003C_003E8__locals3 = new _003C_003Ec__DisplayClass6_1
				{
					CS_0024_003C_003E8__locals4 = _003C_003E8__1.CS_0024_003C_003E8__locals5,
					answer = answer,
					click = click
				};
				return ArcButtons.Gel(title, symbol, appearance, _003C_003E8__1.CS_0024_003C_003E8__locals5.buttonHeight, _003C_003E8__1.CS_0024_003C_003E8__locals5.labelSize, _003C_003E8__1.CS_0024_003C_003E8__locals5.sizing, delegate
				{
					CS_0024_003C_003E8__locals3.CS_0024_003C_003E8__locals4._003C_003E4__this.TrackAnswer(CS_0024_003C_003E8__locals3.answer);
					CS_0024_003C_003E8__locals3.click();
				});
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<FrameworkElement> IEnumerable<FrameworkElement>.GetEnumerator()
		{
			_003CButtons_003Ed__6 _003CButtons_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CButtons_003Ed__ = this;
			}
			else
			{
				_003CButtons_003Ed__ = new _003CButtons_003Ed__6(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CButtons_003Ed__.request = _003C_003E3__request;
			_003CButtons_003Ed__.isLarge = _003C_003E3__isLarge;
			_003CButtons_003Ed__.buttonHeight = _003C_003E3__buttonHeight;
			return _003CButtons_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
		}
	}

	private const double HorizontalInset = 22.0;

	private readonly ArcDock dock;

	private readonly ArcCardContent content;

	private readonly string analyticsSurface;

	public FooterRequestRow(ArcDock dock, ArcCardContent content, CardFooterRequest request, double? buttonHeight, string analyticsSurface = "dock_window", bool buttonsOnly = false)
	{
		this.dock = dock;
		this.content = content;
		this.analyticsSurface = analyticsSurface;
		base.Padding = new Thickness(22.0, 12.0, 22.0, 12.0);
		base.Spacing = 9.0;
		if (request.Kind == CardFooterRequest.Kinds.Ready)
		{
			CardReadyOutput ready = request.Ready;
			if ((object)ready != null)
			{
				base.Children.Add(ReadyButtons(ready, buttonHeight));
				return;
			}
		}
		if (buttonsOnly)
		{
			base.Padding = new Thickness(0.0);
			base.HorizontalAlignment = HorizontalAlignment.Left;
			FirstThatFitsHorizontally firstThatFitsHorizontally = new FirstThatFitsHorizontally
			{
				Children = { (UIElement)Row(request, isLarge: false, buttonHeight) }
			};
			DockFlow dockFlow = new DockFlow
			{
				HorizontalSpacing = 8.0,
				VerticalSpacing = 8.0
			};
			foreach (FrameworkElement item in Buttons(request, isLarge: false, buttonHeight))
			{
				dockFlow.Children.Add(item);
			}
			firstThatFitsHorizontally.Children.Add(dockFlow);
			base.Children.Add(firstThatFitsHorizontally);
			AutomationProperties.SetName(this, request.Title + ". " + request.Detail);
			return;
		}
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		FrameworkElement frameworkElement2;
		if (request.Kind == CardFooterRequest.Kinds.ConnectChrome)
		{
			FrameworkElement frameworkElement = ChromeIcon.Make(32.0);
			if ((object)frameworkElement != null)
			{
				frameworkElement2 = frameworkElement;
				goto IL_0283;
			}
		}
		HandoffGlyph? glyph = request.Glyph;
		if (glyph.HasValue)
		{
			HandoffGlyph valueOrDefault = glyph.GetValueOrDefault();
			frameworkElement2 = new HandoffChipView(valueOrDefault, content.RailKey, 32.0, null);
		}
		else
		{
			frameworkElement2 = new Grid
			{
				Width = 32.0,
				Height = 32.0,
				Children = 
				{
					(UIElement)new Ellipse
					{
						Fill = Ui.Brush(ArcColors.Of(request.IconBackground))
					},
					(UIElement)ArcSymbols.Make(request.Symbol, 14.0, Colors.White)
				}
			};
		}
		goto IL_0283;
		IL_0283:
		frameworkElement2.VerticalAlignment = VerticalAlignment.Top;
		grid.Children.Add(frameworkElement2);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		stackPanel2.Children.Add(Ui.Text(request.Title, 16.5, FontWeights.SemiBold, ArcColors.Ink));
		if (request.Glyph.HasValue)
		{
			stackPanel2.Children.Add(new PaperStampView("you", DS.Hex(12081670u), 8.5, NotePaperColors.Seed(content.RailKey) % 5 - 7));
		}
		stackPanel.Children.Add(stackPanel2);
		stackPanel.Children.Add(Ui.Text(request.Detail, 14.0, FontWeights.Normal, ArcColors.Ink.WithAlpha(0.6), TextAlignment.Left, 3));
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		base.Children.Add(grid);
		FirstThatFitsHorizontally firstThatFitsHorizontally2 = new FirstThatFitsHorizontally
		{
			Margin = new Thickness(42.0, 0.0, 0.0, 0.0)
		};
		if (!buttonHeight.HasValue)
		{
			firstThatFitsHorizontally2.Children.Add(Row(request, isLarge: true, buttonHeight));
		}
		firstThatFitsHorizontally2.Children.Add(Row(request, isLarge: false, buttonHeight));
		DockFlow dockFlow2 = new DockFlow
		{
			HorizontalSpacing = 8.0,
			VerticalSpacing = 8.0
		};
		foreach (FrameworkElement item2 in Buttons(request, isLarge: false, buttonHeight))
		{
			dockFlow2.Children.Add(item2);
		}
		firstThatFitsHorizontally2.Children.Add(dockFlow2);
		base.Children.Add(firstThatFitsHorizontally2);
		AutomationProperties.SetName(this, request.Title + ". " + request.Detail);
	}

	private FrameworkElement Row(CardFooterRequest request, bool isLarge, double? buttonHeight)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		foreach (FrameworkElement item in Buttons(request, isLarge, buttonHeight))
		{
			stackPanel.Children.Add(item);
		}
		return stackPanel;
	}

	[IteratorStateMachine(typeof(_003CButtons_003Ed__6))]
	private IEnumerable<FrameworkElement> Buttons(CardFooterRequest request, bool isLarge, double? buttonHeight)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CButtons_003Ed__6(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__request = request,
			_003C_003E3__isLarge = isLarge,
			_003C_003E3__buttonHeight = buttonHeight
		};
	}

	private FrameworkElement ReadyButtons(CardReadyOutput output, double? buttonHeight)
	{
		string text = output.ButtonSymbol;
		if (text == null)
		{
			CardReadyOutput.Kinds kind = output.Kind;
			bool flag = ((kind == CardReadyOutput.Kinds.File || kind == CardReadyOutput.Kinds.Several) ? true : false);
			text = (flag ? output.Symbol : null);
		}
		string symbol = text;
		bool flag2 = !buttonHeight.HasValue;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		stackPanel.Children.Add(ArcButtons.Gel(output.ButtonTitle, symbol, GelAppearance.PrimaryBlue, buttonHeight, flag2 ? 15.5 : 13.0, flag2 ? GelButton.Sizing.Large : GelButton.Sizing.Normal, delegate
		{
			TrackAnswer("open_output");
			dock.OpenReadyOutput(content.RailKey, output);
		}));
		string jobId = content.JobId;
		if (jobId != null)
		{
			stackPanel.Children.Add(ArcButtons.Gel("Show me", null, GelAppearance.SecondaryWhite, buttonHeight, flag2 ? 15.5 : 13.0, flag2 ? GelButton.Sizing.Large : GelButton.Sizing.Normal, delegate
			{
				TrackAnswer("show_me");
				dock.ShowJobThroughBrain("job:" + jobId);
			}));
		}
		AutomationProperties.SetName(stackPanel, output.Title + ". " + output.ButtonTitle);
		return stackPanel;
	}

	private void TrackAnswer(string answer)
	{
		dock.TrackRequestAnswered(answer, analyticsSurface, content.RailKey, content.TaskId);
	}
}
