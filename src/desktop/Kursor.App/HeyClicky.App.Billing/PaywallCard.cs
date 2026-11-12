using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Billing;

public sealed class PaywallCard : StackPanel
{
	[CompilerGenerated]
	private sealed class _003CContentSection_003Ed__27 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public PaywallCard _003C_003E4__this;

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
		public _003CContentSection_003Ed__27(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			PaywallCard paywallCard = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				if (paywallCard.payload.IsPaymentProblem)
				{
					_003C_003E2__current = paywallCard.OfferStack();
					_003C_003E1__state = 1;
					return true;
				}
				IReadOnlyList<PaywallPlanOption> plans = paywallCard.payload.Plans;
				if (plans != null)
				{
					if (plans.Count == 0)
					{
						_003C_003E2__current = paywallCard.MaxedOutSection();
						_003C_003E1__state = 3;
						return true;
					}
					StackPanel stackPanel = new StackPanel
					{
						Spacing = 14.0
					};
					if (paywallCard.payload.SupportsYearly)
					{
						stackPanel.Children.Add(paywallCard.IntervalToggle());
					}
					stackPanel.Children.Add(paywallCard.PlansRow(plans));
					_003C_003E2__current = stackPanel;
					_003C_003E1__state = 4;
					return true;
				}
				_003C_003E2__current = paywallCard.OfferStack();
				_003C_003E1__state = 5;
				return true;
			}
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = paywallCard.CtaButton();
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
				break;
			case 5:
				_003C_003E1__state = -1;
				_003C_003E2__current = paywallCard.CtaButton();
				_003C_003E1__state = 6;
				return true;
			case 6:
				_003C_003E1__state = -1;
				break;
			}
			return false;
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
			_003CContentSection_003Ed__27 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CContentSection_003Ed__27(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
		}
	}

	private readonly PaywallPayload payload;

	private readonly PaywallSource source;

	private readonly PaywallPalette pal;

	private readonly string currentTier;

	private readonly IPaywallCardHost host;

	private ReferralPaywallDiscount? referral;

	private string selectedInterval;

	private string? launchErrorText;

	private string? launchingPlanKey;

	private bool isLaunchingCheckout;

	private readonly List<GelButton> planButtons = new List<GelButton>();

	private const string GiftPath = "M3,8 h18 v4 h-18 z M4,13 h7 v8 h-7 z M13,13 h7 v8 h-7 z M11,8 h2 v13 h-2 z M12,7.5 C10.5,4 7.5,3 6.6,4.6 C5.7,6.2 8,7.6 12,7.5 Z M12,7.5 C13.5,4 16.5,3 17.4,4.6 C18.3,6.2 16,7.6 12,7.5 Z";

	private bool OffersFreeOption => source.OffersFreeOption();

	private ReferralPaywallDiscount? ReferralDiscount
	{
		get
		{
			if (!payload.IsPaymentProblem)
			{
				IReadOnlyList<PaywallPlanOption> plans = payload.Plans;
				if (plans != null && plans.Count > 0 && currentTier == "free")
				{
					return referral;
				}
			}
			return null;
		}
	}

	public string? LaunchErrorText => launchErrorText;

	private bool ShouldShowEyebrow
	{
		get
		{
			if (!payload.IsPaymentProblem)
			{
				IReadOnlyList<PaywallPlanOption> plans = payload.Plans;
				if (plans != null)
				{
					return plans.Count == 0;
				}
				return false;
			}
			return true;
		}
	}

	private string EyebrowText
	{
		get
		{
			if (payload.IsPaymentProblem)
			{
				return "Update your payment method to keep HeyClicky running.";
			}
			IReadOnlyList<PaywallPlanOption> plans = payload.Plans;
			if (plans != null)
			{
				if (plans.Count == 0)
				{
					return "You're on our biggest plan, HeyClicky Max.";
				}
				if (plans.Count == 1 && plans[0].Key == "max")
				{
					return "Upgrade to HeyClicky Max to keep it going!";
				}
				return "Upgrade HeyClicky to keep it going!";
			}
			return "Upgrade to HeyClicky Pro to keep it going!";
		}
	}

	public PaywallCard(PaywallPayload payload, PaywallSource source, PaywallPalette palette, string currentTier, ReferralPaywallDiscount? referral, IPaywallCardHost host)
	{
		this.payload = payload;
		this.source = source;
		pal = palette;
		this.currentTier = currentTier;
		this.host = host;
		this.referral = referral;
		selectedInterval = payload.DefaultInterval ?? "month";
		base.Orientation = Orientation.Vertical;
		base.Spacing = 22.0;
		Build();
	}

	public void SetReferral(ReferralPaywallDiscount? discount)
	{
		if (!(discount == referral))
		{
			referral = discount;
			Build();
			host.ContentResized();
		}
	}

	private void Build()
	{
		base.Children.Clear();
		planButtons.Clear();
		base.Children.Add(Header());
		ReferralPaywallDiscount referralDiscount = ReferralDiscount;
		if ((object)referralDiscount != null)
		{
			base.Children.Add(ReferralBanner(referralDiscount));
		}
		if (ShouldShowEyebrow)
		{
			TextBlock item = Ui.Text(EyebrowText, 14.0, FontWeights.SemiBold, pal.Secondary, TextAlignment.Left, 0, wrap: true);
			base.Children.Add(item);
		}
		foreach (FrameworkElement item2 in ContentSection())
		{
			base.Children.Add(item2);
		}
		string text = launchErrorText;
		if (text != null)
		{
			base.Children.Add(ErrorRow(text));
		}
	}

	private FrameworkElement Header()
	{
		Grid grid = new Grid
		{
			Width = 44.0,
			Height = 34.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid.Children.Add(new BrandMark(44.0, pal.Primary)
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
		TextBlock textBlock = Ui.Text(OffersFreeOption ? "Choose your HeyClicky plan" : payload.Title, 19.0, FontWeights.Bold, pal.Primary, TextAlignment.Left, 2, wrap: true);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Grid obj = new Grid
		{
			ColumnSpacing = 12.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			},
			Children = { (UIElement)grid }
		};
		Grid.SetColumn(textBlock, 1);
		obj.Children.Add(textBlock);
		GelIconButton gelIconButton = new GelIconButton("\ue711", GelAppearance.SecondaryOutline, 9.0, 24.0, "Close")
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
		};
		if (!pal.IsDark)
		{
			TextBlock textBlock2 = gelIconButton.Children.OfType<TextBlock>().FirstOrDefault();
			if ((object)textBlock2 != null)
			{
				textBlock2.Foreground = Ui.Brush(DS.Black(0.55));
			}
		}
		gelIconButton.Click += delegate
		{
			ClickyAnalytics.TrackPaywallDismissed(source.WireName());
			host.Close(PaywallCloseKind.Dismissed);
		};
		Grid.SetColumn(gelIconButton, 2);
		obj.Children.Add(gelIconButton);
		return obj;
	}

	private FrameworkElement ReferralBanner(ReferralPaywallDiscount d)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		stackPanel.Children.Add(Icons.PathIcon("M3,8 h18 v4 h-18 z M4,13 h7 v8 h-7 z M13,13 h7 v8 h-7 z M11,8 h2 v13 h-2 z M12,7.5 C10.5,4 7.5,3 6.6,4.6 C5.7,6.2 8,7.6 12,7.5 Z M12,7.5 C13.5,4 16.5,3 17.4,4.6 C18.3,6.2 16,7.6 12,7.5 Z", 12.0, pal.Accent));
		TextBlock textBlock = Ui.Text("", 12.5, FontWeights.Medium, pal.Primary);
		textBlock.Inlines.Add(new Run
		{
			Text = "You're getting "
		});
		textBlock.Inlines.Add(new Run
		{
			Text = d.PercentText + " off",
			FontWeight = FontWeights.Bold
		});
		textBlock.Inlines.Add(new Run
		{
			Text = " " + d.PeriodText + " from"
		});
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Children.Add(textBlock);
		string referrerFirstName = d.ReferrerFirstName;
		if (referrerFirstName != null)
		{
			Grid grid = new Grid
			{
				Width = 20.0,
				Height = 20.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			grid.Children.Add(new Ellipse
			{
				Fill = Ui.Brush(pal.Accent)
			});
			grid.Children.Add(Ui.Text(referrerFirstName.Substring(0, 1).ToUpperInvariant(), 10.0, FontWeights.Bold, Colors.White).Center());
			string referrerAvatarUrl = d.ReferrerAvatarUrl;
			if (referrerAvatarUrl != null)
			{
				Ellipse item = new Ellipse
				{
					Fill = new ImageBrush
					{
						ImageSource = new BitmapImage(new Uri(referrerAvatarUrl)),
						Stretch = Stretch.UniformToFill
					}
				};
				grid.Children.Add(item);
			}
			grid.Children.Add(new Ellipse
			{
				Stroke = Ui.Brush(DS.White(0.8)),
				StrokeThickness = 1.0
			});
			TextBlock textBlock2 = Ui.Text("", 12.5, FontWeights.Medium, pal.Primary);
			textBlock2.Inlines.Add(new Run
			{
				Text = referrerFirstName,
				FontWeight = FontWeights.Bold
			});
			textBlock2.Inlines.Add(new Run
			{
				Text = "'s invite"
			});
			textBlock2.VerticalAlignment = VerticalAlignment.Center;
			stackPanel.Children.Add(new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5.0,
				Children = 
				{
					(UIElement)grid,
					(UIElement)textBlock2
				}
			});
		}
		else
		{
			TextBlock textBlock3 = Ui.Text("a friend's invite", 12.5, FontWeights.Medium, pal.Primary);
			textBlock3.VerticalAlignment = VerticalAlignment.Center;
			stackPanel.Children.Add(textBlock3);
		}
		return new Border
		{
			Child = stackPanel,
			Padding = new Thickness(12.0, 9.0, 12.0, 9.0),
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(pal.Accent.WithAlpha(0.1)),
			BorderBrush = Ui.Brush(pal.Accent.WithAlpha(0.25)),
			BorderThickness = new Thickness(0.5)
		};
	}

	[IteratorStateMachine(typeof(_003CContentSection_003Ed__27))]
	private IEnumerable<FrameworkElement> ContentSection()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CContentSection_003Ed__27(-2)
		{
			_003C_003E4__this = this
		};
	}

	private FrameworkElement PlansRow(IReadOnlyList<PaywallPlanOption> plans)
	{
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0
		};
		List<FrameworkElement> list = new List<FrameworkElement>();
		if (OffersFreeOption)
		{
			list.Add(FreeOptionCard());
		}
		foreach (PaywallPlanOption plan in plans)
		{
			list.Add(PlanOptionCard(plan));
		}
		for (int i = 0; i < list.Count; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			Grid.SetColumn(list[i], i);
			grid.Children.Add(list[i]);
		}
		return grid;
	}

	private Border CardFrame(FrameworkElement top, FrameworkElement cta)
	{
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star),
			MinHeight = 24.0
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.Children.Add(top);
		Grid.SetRow(cta, 2);
		grid.Children.Add(cta);
		return new Border
		{
			Child = grid,
			Padding = new Thickness(18.0),
			CornerRadius = new CornerRadius(16.0),
			Background = Ui.Brush(pal.BenefitSurface),
			BorderBrush = Ui.Brush(pal.BenefitBorder),
			BorderThickness = new Thickness(0.5),
			VerticalAlignment = VerticalAlignment.Stretch
		};
	}

	private FrameworkElement Rule()
	{
		return new Border
		{
			Height = 1.0,
			Background = Ui.Brush(pal.BenefitBorder.WithAlpha(0.4)),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
	}

	private FrameworkElement Description(string text)
	{
		TextBlock textBlock = Ui.Text(text, 11.5, FontWeights.Normal, pal.Secondary, TextAlignment.Left, 0, wrap: true);
		textBlock.MinHeight = 30.590000000000003;
		return textBlock;
	}

	private FrameworkElement FreeOptionCard()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0
		};
		stackPanel.Children.Add(Ui.Text("Free", 16.0, FontWeights.SemiBold, pal.Primary));
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 2.0
		};
		stackPanel2.Children.Add(Ui.Text("$0", 30.0, FontWeights.Bold, pal.Primary));
		if (payload.SupportsYearly || (object)ReferralDiscount != null)
		{
			stackPanel2.Children.Add(Caption("No credit card needed", pal.Secondary));
		}
		stackPanel.Children.Add(stackPanel2);
		stackPanel.Children.Add(Description("Get to know HeyClicky. Upgrade whenever you are ready."));
		stackPanel.Children.Add(Rule());
		stackPanel.Children.Add(Features(("25 talk messages a month", "Includes your free monthly message allowance."), ("plenty of dictation", "Includes your free monthly dictation allowance."), ("25 agent messages a month", "Includes your free agent message allowance. Upgrade for more.")));
		GelButton gelButton = new GelButton("Use HeyClicky for free", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		gelButton.Click += delegate
		{
			if (launchingPlanKey == null)
			{
				ClickyAnalytics.TrackPaywallFreeSelected(source.WireName());
				host.Close(PaywallCloseKind.FreeChosen);
			}
		};
		planButtons.Add(gelButton);
		return CardFrame(stackPanel, gelButton);
	}

	private FrameworkElement PlanOptionCard(PaywallPlanOption plan)
	{
		bool flag = selectedInterval == "year" && plan.SupportsYearly;
		(string amount, string period) tuple = SplitPriceDisplay(flag ? (plan.YearlyPriceDisplay ?? plan.PriceDisplay) : plan.PriceDisplay);
		string item = tuple.amount;
		string item2 = tuple.period;
		ReferralPaywallDiscount referralDiscount = ReferralDiscount;
		string text = (((object)referralDiscount != null && (!flag || referralDiscount.AppliesToYearlyPlans)) ? referralDiscount.DiscountedAmountText(item) : null);
		string text2 = ((text == null || (object)referralDiscount == null) ? null : ((flag ? "First year" : ((referralDiscount.Months == 1) ? "First month" : $"First {referralDiscount.Months} months")) + ", then " + item + item2));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0
		};
		stackPanel2.Children.Add(Ui.Text((plan.Key == "pro") ? "Pro" : "Max", 16.0, FontWeights.SemiBold, pal.Primary));
		if (plan.Recommended)
		{
			stackPanel2.Children.Add(PopularBadge());
		}
		stackPanel.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 2.0
		};
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 3.0
		};
		if (text != null)
		{
			TextBlock textBlock = Ui.Text(item, 17.0, FontWeights.SemiBold, pal.Secondary);
			textBlock.TextDecorations = TextDecorations.Strikethrough;
			textBlock.Margin = new Thickness(0.0, 0.0, 3.0, 0.0);
			textBlock.VerticalAlignment = VerticalAlignment.Bottom;
			textBlock.Padding = new Thickness(0.0, 0.0, 0.0, 5.0);
			stackPanel4.Children.Add(textBlock);
			stackPanel4.Children.Add(BigPrice(text));
		}
		else
		{
			stackPanel4.Children.Add(BigPrice(item));
		}
		TextBlock textBlock2 = Ui.Text(item2, 12.0, FontWeights.Medium, pal.Secondary);
		textBlock2.VerticalAlignment = VerticalAlignment.Bottom;
		textBlock2.Padding = new Thickness(0.0, 0.0, 0.0, 7.0);
		stackPanel4.Children.Add(textBlock2);
		stackPanel3.Children.Add(stackPanel4);
		if (payload.SupportsYearly || (object)referralDiscount != null)
		{
			stackPanel3.Children.Add(Caption(text2 ?? (flag ? (plan.YearlyBilledSummary ?? "") : ""), (text2 != null) ? pal.Accent : pal.Secondary));
		}
		stackPanel.Children.Add(stackPanel3);
		stackPanel.Children.Add(Description((plan.Key == "pro") ? "Great for everyday HeyClicky use." : "For power users who lean on agents."));
		stackPanel.Children.Add(Rule());
		stackPanel.Children.Add(Features(("Unlimited talk", "Ask, point and talk without watching the meter."), ("Unlimited dictation", "Hold to dictate as much as you want, everywhere."), ($"{plan.AgentsCap} agent messages a month", "Handoff your work, build and automate work.")));
		GelButton cta = new GelButton(plan.CtaLabel, (OffersFreeOption || !plan.Recommended) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		cta.IsLoading = launchingPlanKey == plan.Key;
		cta.Click += delegate
		{
			OnTapPlanAsync(plan, cta);
		};
		planButtons.Add(cta);
		return CardFrame(stackPanel, cta);
	}

	private TextBlock BigPrice(string amount)
	{
		return Ui.Text(amount, 30.0, FontWeights.Bold, pal.Primary);
	}

	private FrameworkElement Caption(string text, Color color)
	{
		TextBlock textBlock = Ui.Text(text, 10.5, FontWeights.Medium, color);
		textBlock.Height = 13.0;
		textBlock.MaxLines = 1;
		return textBlock;
	}

	private FrameworkElement PopularBadge()
	{
		return new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)new GelSurface(GelAppearance.PrimaryBlue, 100.0),
				(UIElement)Ui.Text("Popular", 8.5, FontWeights.Bold, pal.ActionInk).Pad(6.0, 2.0, 6.0, 2.0)
			}
		};
	}

	private FrameworkElement Features(params (string text, string tip)[] rows)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 9.0
		};
		for (int i = 0; i < rows.Length; i++)
		{
			var (text, tip) = rows[i];
			stackPanel.Children.Add(FeatureRow(text, tip));
		}
		return stackPanel;
	}

	private FrameworkElement FeatureRow(string text, string tip)
	{
		Grid grid = new Grid
		{
			Width = 12.0,
			Height = 12.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(pal.Accent)
		});
		TextBlock textBlock = Icons.Glyph("\ue73e", 7.0, pal.CardSurface);
		textBlock.FontWeight = FontWeights.Bold;
		grid.Children.Add(textBlock);
		TextBlock textBlock2 = Ui.Text(text, 11.5, FontWeights.Medium, pal.Primary, TextAlignment.Left, 0, wrap: true);
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 7.0,
			Children = 
			{
				(UIElement)grid,
				(UIElement)textBlock2,
				(UIElement)InfoTip(tip)
			}
		};
	}

	private FrameworkElement InfoTip(string tip)
	{
		Pressable button = new Pressable
		{
			Width = 14.0,
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock glyph = Icons.Glyph("\ue946", 11.0, pal.Secondary);
		glyph.Opacity = 0.6;
		button.Children.Add(glyph);
		button.HoverChanged += delegate(bool h)
		{
			glyph.Opacity = (h ? 1.0 : 0.6);
		};
		button.Tip(tip);
		Flyout flyout = new Flyout
		{
			Placement = FlyoutPlacementMode.Bottom,
			Content = new TextBlock
			{
				Text = tip,
				FontSize = 12.0,
				TextWrapping = TextWrapping.WrapWholeWords,
				Width = 200.0,
				IsTextScaleFactorEnabled = false
			}
		};
		button.Click += delegate
		{
			flyout.ShowAt(button);
		};
		return button;
	}

	private static (string amount, string period) SplitPriceDisplay(string price)
	{
		int num = price.IndexOf('/');
		if (num >= 0)
		{
			string item = price.Substring(0, num).Trim();
			int num2 = num;
			return (amount: item, period: price.Substring(num2, price.Length - num2));
		}
		return (amount: price, period: "");
	}

	private FrameworkElement IntervalToggle()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0
		};
		stackPanel.Children.Add(IntervalSegment("month", "Monthly", null));
		stackPanel.Children.Add(IntervalSegment("year", "Yearly", payload.YearlySavingsLabel));
		return new Border
		{
			Child = stackPanel,
			Padding = new Thickness(3.0),
			CornerRadius = new CornerRadius(100.0),
			Background = Ui.Brush(pal.BenefitSurface),
			BorderBrush = Ui.Brush(pal.BenefitBorder),
			BorderThickness = new Thickness(0.5),
			HorizontalAlignment = HorizontalAlignment.Left
		}.Capsule();
	}

	private FrameworkElement IntervalSegment(string interval, string label, string? badge)
	{
		bool flag = selectedInterval == interval;
		Color color = (flag ? pal.ActionInk : pal.Primary.WithAlpha(0.6));
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			Padding = new Thickness(12.0, 6.0, 12.0, 6.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(Ui.Text(label, 11.5, FontWeights.SemiBold, color).Center());
		if (badge != null)
		{
			Color c = (flag ? pal.ActionInk.WithAlpha(0.12) : pal.Accent.WithAlpha(0.12));
			Grid grid = new Grid
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			grid.Children.Add(new ShadowLayer(100.0, DS.Black(0.18), 1.25, 1.0));
			grid.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(100.0),
				Background = Ui.Brush(c)
			}.Capsule());
			grid.Children.Add(Ui.Text(badge, 9.5, FontWeights.ExtraBold, flag ? pal.ActionInk : pal.Accent).Pad(6.0, 2.5, 6.0, 2.5));
			stackPanel.Children.Add(grid);
		}
		Pressable pressable = new Pressable();
		if (flag)
		{
			pressable.Children.Add(new GelSurface(GelAppearance.PrimaryBlue, 100.0));
		}
		pressable.Children.Add(stackPanel);
		pressable.Click += delegate
		{
			if (!(selectedInterval == interval))
			{
				selectedInterval = interval;
				ClickyAnalytics.TrackPaywallIntervalToggled(payload.Surface, interval, payload.DefaultInterval ?? "month", source.WireName());
				Sounds.Play((interval == "year") ? "skill-up" : "skill-down", 0.18);
				Build();
				host.ContentResized();
			}
		};
		return pressable;
	}

	private async Task OnTapPlanAsync(PaywallPlanOption plan, GelButton button)
	{
		if (launchingPlanKey == null)
		{
			launchingPlanKey = plan.Key;
			button.IsLoading = true;
			SetPlanButtonsEnabled(on: false);
			SetError(null);
			string interval = ((selectedInterval == "year" && plan.SupportsYearly) ? "year" : "month");
			ClickyAnalytics.TrackPaywallCTAClicked(payload.Surface, plan.Key, interval, source.WireName());
			string text;
			try
			{
				text = await host.StartCheckoutAsync(plan.Key, interval, payload.Surface, source);
			}
			catch (Exception ex)
			{
				text = ex.Message;
			}
			launchingPlanKey = null;
			button.IsLoading = false;
			SetPlanButtonsEnabled(on: true);
			if (text == null)
			{
				host.Close(PaywallCloseKind.HandedOff);
			}
			else
			{
				SetError(text);
			}
		}
	}

	private void SetPlanButtonsEnabled(bool on)
	{
		foreach (GelButton planButton in planButtons)
		{
			planButton.IsEnabledGel = on;
		}
	}

	private FrameworkElement CtaButton()
	{
		string iconGlyph = (payload.IsPaymentProblem ? "\ue8c7" : "\uf4a5");
		GelButton cta = new GelButton(payload.CtaLabel, GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, iconGlyph, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		cta.IsLoading = isLaunchingCheckout;
		cta.Click += async delegate
		{
			if (!isLaunchingCheckout)
			{
				isLaunchingCheckout = true;
				cta.IsLoading = true;
				cta.IsEnabledGel = false;
				SetError(null);
				ClickyAnalytics.TrackPaywallCTAClicked(payload.Surface, null, null, source.WireName());
				string text2;
				try
				{
					string text = ((!payload.IsPaymentProblem) ? (await host.StartCheckoutAsync("pro", "month", payload.Surface, source)) : (await host.OpenPortalAsync()));
					text2 = text;
				}
				catch (Exception ex)
				{
					text2 = ex.Message;
				}
				isLaunchingCheckout = false;
				cta.IsLoading = false;
				cta.IsEnabledGel = true;
				if (text2 == null)
				{
					host.Close(PaywallCloseKind.HandedOff);
				}
				else
				{
					SetError(text2);
				}
			}
		};
		return cta;
	}

	private void SetError(string? error)
	{
		if (error == launchErrorText)
		{
			return;
		}
		launchErrorText = error;
		if (base.Children.Count > 0)
		{
			UIElementCollection children = base.Children;
			if (children[children.Count - 1] is FrameworkElement { Tag: string tag } frameworkElement && tag == "error")
			{
				base.Children.Remove(frameworkElement);
			}
		}
		if (error != null)
		{
			FrameworkElement frameworkElement2 = ErrorRow(error);
			base.Children.Add(frameworkElement2);
			if (!Anim.Instant)
			{
				frameworkElement2.Opacity = 0.0;
				frameworkElement2.Translation = new Vector3(0f, -3f, 0f);
				Anim.OpacitySpring(frameworkElement2, 1f, new SwiftSpring(0.35, 0.75));
				Anim.TranslationSpring(frameworkElement2, Vector3.Zero, new SwiftSpring(0.35, 0.75));
			}
		}
		host.ContentResized();
	}

	private FrameworkElement ErrorRow(string text)
	{
		TextBlock textBlock = Icons.Glyph("\ue7ba", 10.0, pal.ErrorText);
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
		TextBlock textBlock2 = Ui.Text(text, 11.0, FontWeights.Normal, pal.ErrorText, TextAlignment.Left, 0, wrap: true);
		Grid obj = new Grid
		{
			ColumnSpacing = 6.0,
			Tag = "error",
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				}
			},
			Children = { (UIElement)textBlock }
		};
		Grid.SetColumn(textBlock2, 1);
		obj.Children.Add(textBlock2);
		return obj;
	}

	private FrameworkElement MaxedOutSection()
	{
		return BenefitRow(payload.Body, "Your agent messages refresh each month");
	}

	private FrameworkElement OfferStack()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0
		};
		if (payload.IsPaymentProblem)
		{
			stackPanel.Children.Add(BenefitRow(payload.Body, "Your subscription is paused until payment succeeds"));
		}
		else
		{
			stackPanel.Children.Add(BenefitRow("Unlimited talk to HeyClicky.", "Ask, point and talk without watching the meter"));
			stackPanel.Children.Add(BenefitRow("Unlimited dictation.", "Hold to dictate as much as you want, everywhere"));
			stackPanel.Children.Add(BenefitRow(payload.Body, "Handoff your work, build and automate work"));
		}
		return stackPanel;
	}

	private FrameworkElement BenefitRow(string title, string subtitle)
	{
		Ellipse item = new Ellipse
		{
			Width = 6.0,
			Height = 6.0,
			Fill = Ui.Brush(pal.Accent),
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(5.0, 7.0, 5.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0
		};
		stackPanel.Children.Add(Ui.Text(title, 14.0, FontWeights.SemiBold, pal.Primary, TextAlignment.Left, 2, wrap: true));
		stackPanel.Children.Add(Ui.Text(subtitle, 11.0, FontWeights.Normal, pal.Secondary, TextAlignment.Left, 2, wrap: true));
		Grid grid = new Grid
		{
			ColumnSpacing = 11.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.Children.Add(item);
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		return new Border
		{
			Child = grid,
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(pal.BenefitSurface),
			BorderBrush = Ui.Brush(pal.BenefitBorder),
			BorderThickness = new Thickness(0.5)
		};
	}
}
