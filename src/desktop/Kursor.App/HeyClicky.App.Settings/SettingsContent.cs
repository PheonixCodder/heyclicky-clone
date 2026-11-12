using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Backend;
using HeyClicky.Agents.Harness;
using HeyClicky.Agents.Routines;
using HeyClicky.App.Billing;
using HeyClicky.App.Browser;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Onboarding;
using HeyClicky.App.OneBrain;
using HeyClicky.App.Portraits;
using HeyClicky.App.Proactive;
using HeyClicky.App.Services;
using HeyClicky.App.Services.Updates;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Browser;
using HeyClicky.Core.Connectors;
using HeyClicky.Core.Dictation;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Audio;
using HeyClicky.Platform.Input;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using WinRT;
using WinRT.HeyClickyVtableClasses;
using WinRT.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace HeyClicky.App.Settings;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(HeyClicky_App_Settings_SettingsContentWinRTTypeDetails))]
public sealed class SettingsContent : Grid
{
	private enum DeletionStep
	{
		Reason,
		TypeToConfirm,
		Farewell
	}

	private sealed class ConnectorDraft
	{
		public string? EditingId;

		public CustomConnector? Existing;

		public string Name = "";

		public bool Remote = true;

		public string Url = "";

		public string Command = "";

		public string Arguments = "";

		public string Auth = "browser";

		public string ApiKey = "";

		public string OAuthClientId = "";

		public string Headers = "";

		public string Environment = "";

		public bool ShowAdvanced;

		public bool AttemptedSave;

		public string? Problem;

		public bool UsesAdvanced
		{
			get
			{
				if (Remote && OAuthClientId.Trim().Length <= 0)
				{
					return Headers.Trim().Length > 0;
				}
				return true;
			}
		}

		public List<(string Key, string Value)> ParsedHeaders => KeyValueLines(Headers, new char[2] { ':', '=' });

		public List<(string Key, string Value)> ParsedEnvironment => KeyValueLines(Environment, new char[1] { '=' });

		public string? ValidationProblem
		{
			get
			{
				if (Name.Trim().Length == 0)
				{
					return "Give the connector a name.";
				}
				string text;
				if (Remote)
				{
					Uri result;
					bool flag = !Uri.TryCreate(Url.Trim(), UriKind.Absolute, out result);
					if (!flag)
					{
						text = result.Scheme;
						bool flag2 = ((text == "https" || text == "http") ? true : false);
						flag = !flag2;
					}
					if (flag || result.Host.Length == 0)
					{
						return "Enter the server's full URL, starting with https://.";
					}
					if (Auth == "apiKey" && ApiKey.Trim().Length == 0)
					{
						return "Paste the API key, or pick a different authentication.";
					}
					if (result.UserInfo.Length > 0)
					{
						return "Remove credentials from the URL and use the authentication fields.";
					}
					foreach (var parsedHeader in ParsedHeaders)
					{
						string item = parsedHeader.Key;
						if (CustomConnector.IsValidHeaderName(item))
						{
							continue;
						}
						text = "\"" + item + "\" is not a valid header name.";
						goto IL_0180;
					}
				}
				else
				{
					if (Command.Trim().Length == 0)
					{
						return "Enter the command that starts the server.";
					}
					foreach (var item3 in ParsedEnvironment)
					{
						string item2 = item3.Key;
						if (CustomConnector.IsValidEnvironmentVariableName(item2))
						{
							continue;
						}
						text = "\"" + item2 + "\" is not a valid environment variable name.";
						goto IL_0180;
					}
				}
				return null;
				IL_0180:
				return text;
			}
		}

		public string? AdvisoryNote
		{
			get
			{
				if (Remote)
				{
					if ((object)Existing != null && !new CustomConnector("", Name, "remote", Url, null, Array.Empty<string>(), Array.Empty<string>(), HasApiKey: false)
					{
						OAuthClientId = OAuthClientId
					}.HasSameAuthenticationTarget(Existing))
					{
						return "Changing the server or client ID clears its saved credentials. Save, then sign in again or add the new key.";
					}
					if (!Url.Trim().StartsWith("http://", StringComparison.OrdinalIgnoreCase))
					{
						return null;
					}
					return "This server uses plain http, so its traffic is not encrypted.";
				}
				string text = Command.Trim();
				if (text.Length <= 0 || CommandResolver.IsResolvable(text))
				{
					return null;
				}
				return "\"" + text + "\" wasn't found on this PC. Use the full path, or install it first.";
			}
		}
	}

	private sealed record Departure(string Id, string Name, Func<FrameworkElement> Logo, int Index, Guid Token);

	private sealed class RoutineSettingsRow : StackPanel
	{
		private static readonly SwiftSpring RowSpring = new SwiftSpring(0.3, 0.86);

		private const double DescriptionLineHeight = 17.96;

		private readonly SettingsContent owner;

		private readonly string id;

		private readonly TextBlock title = Ui.Text("", 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 1);

		private readonly TextBlock status = Ui.Text("", 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 1);

		private readonly StackPanel trailing = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12.0,
			VerticalAlignment = VerticalAlignment.Center
		};

		private ClickyRoutine? routine;

		private bool confirmingDelete;

		private bool justStartedRun;

		private bool descriptionExpanded;

		private bool descriptionHovered;

		private DateTimeOffset? runStartedSeen;

		private (bool Confirming, bool Running, bool Paused, string Title)? trailingBuiltFor;

		private string? descriptionBuiltFor;

		private Pressable? descriptionButton;

		private readonly Grid descriptionHolder = new Grid
		{
			VerticalAlignment = VerticalAlignment.Top
		};

		private readonly TextBlock descriptionText = Ui.Text("", 12.0, FontWeights.Normal, SK.TextSecondary, TextAlignment.Left, 0, wrap: true);

		private TextBlock? moreLabel;

		private TextBlock? moreChevron;

		private SpringValue? descriptionHeight;

		private Action? descriptionSettled;

		private bool descriptionTicking;

		private RoutineScheduler? Scheduler => owner.host.Agents?.Routines;

		private bool IsRunning
		{
			get
			{
				if (!justStartedRun)
				{
					return routine?.IsRunInFlight(DateTimeOffset.Now, RoutineScheduler.RunStaleAfter) ?? false;
				}
				return true;
			}
		}

		private string Description => routine?.TaskPrompt.Trim() ?? "";

		private bool DescriptionOpens
		{
			get
			{
				if (Description.Length <= 140)
				{
					return Description.Contains('\n');
				}
				return true;
			}
		}

		public RoutineSettingsRow(SettingsContent owner, string id)
		{
			this.owner = owner;
			this.id = id;
			base.Spacing = 8.0;
			base.Padding = new Thickness(14.0, 11.0, 14.0, 11.0);
			StackPanel item = new StackPanel
			{
				Spacing = 2.5,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 24.0, 0.0),
				Children = 
				{
					(UIElement)title,
					(UIElement)status
				}
			};
			Grid grid = new Grid
			{
				ColumnSpacing = 12.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			Grid.SetColumn(trailing, 1);
			grid.Children.Add(item);
			grid.Children.Add(trailing);
			base.Children.Add(grid);
			descriptionText.LineHeight = 17.96;
			descriptionText.MaxLines = 2;
			descriptionHolder.Children.Add(descriptionText);
		}

		public void Update(ClickyRoutine r)
		{
			if (r.LastRunStartedAt != runStartedSeen)
			{
				justStartedRun = false;
				runStartedSeen = r.LastRunStartedAt;
			}
			routine = r;
			Refresh(animated: false);
		}

		private void Refresh(bool animated)
		{
			using (MessagesPalette.Use(owner.look))
			{
				ClickyRoutine clickyRoutine = routine;
				if (clickyRoutine == null)
				{
					return;
				}
				title.Text = ((clickyRoutine.Title.Length == 0) ? "Routine" : clickyRoutine.Title);
				title.Foreground = Ui.Brush(clickyRoutine.IsPaused ? SK.TextSecondary : SK.TextPrimary);
				status.Text = (confirmingDelete ? "Delete it? Its past runs stay in the chat." : StatusLine(clickyRoutine));
				bool flag = clickyRoutine.IsPausedAfterRepeatedFailures || (clickyRoutine.LastRunFailed && !clickyRoutine.IsPaused);
				status.Foreground = Ui.Brush(confirmingDelete ? SK.TextSecondary : (flag ? SK.Warning : SK.TextTertiary));
				(bool, bool, bool, string) tuple = (confirmingDelete, IsRunning, clickyRoutine.IsPaused, clickyRoutine.Title);
				(bool, bool, bool, string)? tuple2 = trailingBuiltFor;
				(bool, bool, bool, string) tuple3 = tuple;
				bool hasValue = tuple2.HasValue;
				if (!hasValue)
				{
					goto IL_016c;
				}
				if (hasValue)
				{
					(bool, bool, bool, string) valueOrDefault = tuple2.GetValueOrDefault();
					if (valueOrDefault.Item1 != tuple3.Item1 || valueOrDefault.Item2 != tuple3.Item2 || valueOrDefault.Item3 != tuple3.Item3 || valueOrDefault.Item4 != tuple3.Item4)
					{
						goto IL_016c;
					}
				}
				goto IL_0208;
				IL_0208:
				RefreshDescription();
				return;
				IL_016c:
				tuple2 = trailingBuiltFor;
				bool flag2 = tuple2.HasValue && tuple2.GetValueOrDefault().Item1 != confirmingDelete;
				trailingBuiltFor = tuple;
				BuildTrailing(clickyRoutine);
				if (animated && flag2)
				{
					trailing.Opacity = 0.0;
					status.Opacity = 0.0;
					Anim.OpacitySpring(trailing, 1f, RowSpring);
					Anim.OpacitySpring(status, 1f, RowSpring);
				}
				goto IL_0208;
			}
		}

		private void BuildTrailing(ClickyRoutine r)
		{
			trailing.Children.Clear();
			if (confirmingDelete)
			{
				trailing.Children.Add(SK.PillButton("Cancel", delegate
				{
					confirmingDelete = false;
					Refresh(animated: true);
				}));
				trailing.Children.Add(SK.PillButton("Delete", delegate
				{
					confirmingDelete = false;
					Scheduler?.Delete(id, "settings");
				}, prominent: false, destructive: true));
				return;
			}
			bool isRunning = IsRunning;
			Pressable pressable = SK.PillButton(isRunning ? "Running…" : "Run now", delegate
			{
				if (!IsRunning)
				{
					RoutineScheduler scheduler = Scheduler;
					if (scheduler != null)
					{
						justStartedRun = true;
						Refresh(animated: false);
						scheduler.RunNowAsync(id).ContinueWith(delegate(Task<bool> t)
						{
							if (!t.IsCompletedSuccessfully || !t.Result)
							{
								owner.dq.TryEnqueue(delegate
								{
									justStartedRun = false;
									Refresh(animated: false);
								});
							}
						}, TaskScheduler.Default);
					}
				}
			});
			pressable.Opacity = (isRunning ? 0.55 : 1.0);
			pressable.VerticalAlignment = VerticalAlignment.Center;
			trailing.Children.Add(pressable);
			trailing.Children.Add(DeleteButton(r));
			Pressable pressable2 = SK.Switch(!r.IsPaused, delegate(bool on)
			{
				Scheduler?.SetPaused(id, !on, "settings");
			});
			pressable2.VerticalAlignment = VerticalAlignment.Center;
			pressable2.Tip(r.IsPaused ? "Turn this routine back on" : "Pause this routine");
			AutomationProperties.SetName(pressable2, r.IsPaused ? ("Turn " + r.Title + " back on") : ("Pause " + r.Title));
			trailing.Children.Add(pressable2);
		}

		private Pressable DeleteButton(ClickyRoutine r)
		{
			Pressable button = new Pressable
			{
				Width = 26.0,
				Height = 26.0,
				CornerRadius = new CornerRadius(13.0),
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock glyph = Icons.Glyph("\ue74d", 11.5, SK.TextTertiary);
			glyph.FontWeight = FontWeights.Medium;
			glyph.HorizontalAlignment = HorizontalAlignment.Center;
			glyph.VerticalAlignment = VerticalAlignment.Center;
			button.Children.Add(glyph);
			button.HoverChanged += delegate(bool hovered)
			{
				button.Background = Ui.Brush(owner.look.Ink.WithAlpha(hovered ? 0.1 : 0.0));
				glyph.Foreground = Ui.Brush(hovered ? SK.Destructive : owner.look.ThemeTextTertiary);
			};
			button.Click += delegate
			{
				confirmingDelete = true;
				Refresh(animated: true);
			};
			button.Tip("Delete this routine");
			AutomationProperties.SetName(button, "Delete " + r.Title);
			return button;
		}

		private void RefreshDescription()
		{
			string description = Description;
			if (description == descriptionBuiltFor)
			{
				return;
			}
			descriptionBuiltFor = description;
			if ((object)descriptionButton != null)
			{
				base.Children.Remove(descriptionButton);
			}
			descriptionButton = null;
			moreLabel = (moreChevron = null);
			if (descriptionHolder.Parent is Panel panel)
			{
				panel.Children.Remove(descriptionHolder);
			}
			StopDescriptionAnimation();
			if (description.Length == 0)
			{
				return;
			}
			bool descriptionOpens = DescriptionOpens;
			if (!descriptionOpens)
			{
				descriptionExpanded = false;
			}
			descriptionText.Text = description;
			descriptionText.MaxLines = ((!descriptionExpanded) ? 2 : 0);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 4.0,
				Children = { (UIElement)descriptionHolder }
			};
			if (descriptionOpens)
			{
				moreLabel = Ui.Text(descriptionExpanded ? "Show less" : "Show more", 10.5, FontWeights.SemiBold, SK.TextTertiary);
				moreChevron = Icons.Glyph("\ue70d", 10.5, SK.TextTertiary);
				moreChevron.FontWeight = FontWeights.SemiBold;
				moreChevron.Rotation = (descriptionExpanded ? 180 : 0);
				TextBlock chevron = moreChevron;
				chevron.SizeChanged += delegate
				{
					chevron.CenterPoint = new Vector3((float)chevron.ActualWidth / 2f, (float)chevron.ActualHeight / 2f, 0f);
				};
				TextBlock[] array = new TextBlock[2] { moreLabel, moreChevron };
				for (int i = 0; i < array.Length; i++)
				{
					array[i].VerticalAlignment = VerticalAlignment.Center;
				}
				stackPanel.Children.Add(new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 3.0,
					Children = 
					{
						(UIElement)moreLabel,
						(UIElement)moreChevron
					}
				});
				ColorShowMore();
			}
			Pressable pressable = new Pressable(descriptionOpens)
			{
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			pressable.Children.Add(stackPanel);
			if (descriptionOpens)
			{
				pressable.HoverChanged += delegate(bool hovered)
				{
					descriptionHovered = hovered;
					ColorShowMore();
				};
				pressable.Click += ToggleDescription;
			}
			descriptionButton = pressable;
			base.Children.Add(pressable);
		}

		private void ColorShowMore()
		{
			SolidColorBrush foreground = Ui.Brush(descriptionHovered ? owner.look.ThemeTextSecondary : owner.look.ThemeTextTertiary);
			if ((object)moreLabel != null)
			{
				moreLabel.Foreground = foreground;
			}
			if ((object)moreChevron != null)
			{
				moreChevron.Foreground = foreground;
			}
		}

		private void ToggleDescription()
		{
			if (!DescriptionOpens || (object)moreLabel == null || (object)moreChevron == null)
			{
				return;
			}
			double actualWidth = descriptionText.ActualWidth;
			double actualHeight = descriptionHolder.ActualHeight;
			descriptionExpanded = !descriptionExpanded;
			moreLabel.Text = (descriptionExpanded ? "Show less" : "Show more");
			float from = ((!descriptionExpanded) ? 180 : 0);
			float num = (descriptionExpanded ? 180 : 0);
			moreChevron.Rotation = num;
			if (!Anim.Instant && !Anim.ReduceMotion)
			{
				ScalarKeyFrameAnimation scalarKeyFrameAnimation = SpringCurve.Scalar(Ui.Compositor, RowSpring, from, num);
				scalarKeyFrameAnimation.Target = "Rotation";
				moreChevron.StartAnimation(scalarKeyFrameAnimation);
			}
			double num2 = TextHeight(0, actualWidth);
			double target = (descriptionExpanded ? num2 : TextHeight(2, actualWidth));
			if (Anim.Instant || Anim.ReduceMotion || actualWidth <= 0.0 || actualHeight <= 0.0)
			{
				StopDescriptionAnimation();
				descriptionText.MaxLines = ((!descriptionExpanded) ? 2 : 0);
				return;
			}
			descriptionText.MaxLines = 0;
			descriptionText.Height = num2;
			if (descriptionHeight == null)
			{
				descriptionHeight = new SpringValue(actualHeight, 0.25);
			}
			if (!descriptionTicking)
			{
				descriptionHeight.To(Ui.Compositor, actualHeight, null);
			}
			descriptionHeight.To(Ui.Compositor, target, RowSpring);
			bool expanded = descriptionExpanded;
			descriptionSettled = delegate
			{
				descriptionText.MaxLines = ((!expanded) ? 2 : 0);
				descriptionText.Height = double.NaN;
			};
			if (!descriptionTicking)
			{
				descriptionTicking = true;
				CompositionTarget.Rendering += OnDescriptionFrame;
			}
		}

		private void OnDescriptionFrame(object? sender, object e)
		{
			SpringValue springValue = descriptionHeight;
			if (springValue == null)
			{
				StopDescriptionAnimation();
				return;
			}
			double num = Math.Max(0.0, springValue.ValueAt(SpringCurve.Now));
			descriptionHolder.Height = num;
			descriptionHolder.Clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, Math.Max(0.0, descriptionHolder.ActualWidth), num)
			};
			if (num == springValue.Target)
			{
				StopDescriptionAnimation();
			}
		}

		private void StopDescriptionAnimation()
		{
			if (descriptionTicking)
			{
				CompositionTarget.Rendering -= OnDescriptionFrame;
			}
			descriptionTicking = false;
			descriptionHeight = null;
			descriptionHolder.Height = double.NaN;
			descriptionHolder.Clip = null;
			Action? action = descriptionSettled;
			descriptionSettled = null;
			action?.Invoke();
		}

		private double TextHeight(int maxLines, double width)
		{
			TextBlock textBlock = Ui.Text(descriptionText.Text, 12.0, FontWeights.Normal, SK.TextSecondary, TextAlignment.Left, 0, wrap: true);
			textBlock.LineHeight = 17.96;
			textBlock.MaxLines = maxLines;
			textBlock.Measure(new Size(Math.Max(1.0, width), double.PositiveInfinity));
			return textBlock.DesiredSize.Height;
		}

		private static string CadenceWords(int minutes)
		{
			if (minutes != 10080)
			{
				return ClickyRoutine.CadenceLabelFor(minutes);
			}
			return "Every week";
		}

		private static string NextRunWords(DateTimeOffset at)
		{
			DateTimeOffset dateTimeOffset = at.ToLocalTime();
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			string text = dateTimeOffset.ToString(currentCulture.DateTimeFormat.ShortTimePattern, currentCulture);
			if (dateTimeOffset.Date == DateTime.Today)
			{
				return "today " + text;
			}
			if (dateTimeOffset.Date == DateTime.Today.AddDays(1.0))
			{
				return "tomorrow " + text;
			}
			return currentCulture.DateTimeFormat.GetAbbreviatedDayName(dateTimeOffset.DayOfWeek) + " " + text;
		}

		private string StatusLine(ClickyRoutine r)
		{
			List<string> list = new List<string> { r.HasSetTime ? r.ScheduleLabel : CadenceWords(r.IntervalMinutes) };
			if (r.IsPausedAfterRepeatedFailures)
			{
				list.Add($"Paused after {r.ConsecutiveFailedRunCount} failed runs");
			}
			else if (r.IsPaused)
			{
				list.Add("Paused");
			}
			else if (IsRunning)
			{
				list.Add("Running now");
			}
			else if (r.PendingRetryAt.HasValue)
			{
				list.Add("Retrying " + NextRunWords(r.NextRunAt));
			}
			else if (r.LastRunFailed)
			{
				list.Add("Last run failed");
			}
			else
			{
				list.Add("Next " + NextRunWords(r.NextRunAt));
			}
			if (r.RunCount > 0)
			{
				list.Add($"Ran {r.RunCount}×");
			}
			return string.Join(" · ", list);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass130_0
	{
		public bool v;

		internal void _003CProactiveDeveloperGroups_003Eb__6(AppSettings x)
		{
			x.UpcomingMeetingsEnabled = v;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass130_1
	{
		public bool v;

		internal void _003CProactiveDeveloperGroups_003Eb__7(AppSettings x)
		{
			x.OnboardingLanguageStepEnabled = v;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass130_2
	{
		public bool v;

		internal void _003CProactiveDeveloperGroups_003Eb__8(AppSettings x)
		{
			x.RemoteTasksEnabled = v;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass180_0
	{
		public SettingsContent _003C_003E4__this;

		public Pressable edit;

		public string code;

		public string shareUrl;

		internal void _003CLinkParts_003Eb__8(string t)
		{
			_003C_003E4__this.referralCodeDraft = t;
		}

		internal void _003CLinkParts_003Eb__5(object _, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				_ = _003C_003E4__this.SaveReferralCodeAsync();
			}
		}

		internal void _003CLinkParts_003Eb__6()
		{
			_003C_003E4__this.SaveReferralCodeAsync();
		}

		internal void _003CLinkParts_003Eb__7()
		{
			_003C_003E4__this.editingReferralCode = false;
			_003C_003E4__this.referralCodeProblem = "";
			_003C_003E4__this.Render();
		}

		internal void _003CLinkParts_003Eb__0(bool h)
		{
			edit.Background = Ui.Brush(Colors.Black.WithAlpha(h ? 0.14 : 0.07));
		}

		internal void _003CLinkParts_003Eb__1()
		{
			_003C_003E4__this.editingReferralCode = true;
			_003C_003E4__this.referralCodeDraft = code;
			_003C_003E4__this.referralCodeProblem = "";
			_003C_003E4__this.Render();
		}

		internal void _003CLinkParts_003Eb__2()
		{
			_003C_003E4__this.CopyReferral("link", shareUrl);
		}

		internal void _003CLinkParts_003Eb__3()
		{
			_003C_003E4__this.CopyReferral("code", code.ToUpperInvariant());
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass180_1
	{
		public TextBox input;

		internal void _003CLinkParts_003Eb__4(object _s, RoutedEventArgs _e)
		{
			input.Focus(FocusState.Programmatic);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass24_0
	{
		public IReadOnlyList<string> profiles;

		public ClickyBrowserBridge bridge;

		public bool isEmbedded;

		internal void _003CBrowserConnectionRows_003Eb__1(int i)
		{
			if (!(profiles[i] == bridge.SelectedProfile))
			{
				TrackBrowserSettingsAction("settings_profile_changed", isEmbedded, new JsonObject((JsonNodeOptions?)null)
				{
					["profile_count"] = bridge.Profiles.Count,
					["profile_index"] = bridge.Profiles.ToList().IndexOf(profiles[i])
				});
				bridge.SelectedProfile = profiles[i];
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003CBrowserConnectionRows_003Ed__24 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement? _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private bool isEmbedded;

		public bool _003C_003E3__isEmbedded;

		public SettingsContent _003C_003E4__this;

		private _003C_003Ec__DisplayClass24_0 _003C_003E8__1;

		private string _003CbrowserName_003E5__2;

		FrameworkElement? IEnumerator<FrameworkElement>.Current
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
		public _003CBrowserConnectionRows_003Ed__24(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E8__1 = null;
			_003CbrowserName_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			SettingsContent settingsContent = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E8__1 = new _003C_003Ec__DisplayClass24_0();
				_003C_003E8__1.isEmbedded = isEmbedded;
				_003C_003E8__1.bridge = ClickyBrowserBridge.Shared;
				_003C_003E2__current = settingsContent.BrowserHeader(_003C_003E8__1.isEmbedded);
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				if (!_003C_003E8__1.bridge.IsConnected)
				{
					return false;
				}
				_003CbrowserName_003E5__2 = _003C_003E8__1.bridge.SetupBrowserName;
				_003C_003E8__1.profiles = _003C_003E8__1.bridge.Profiles;
				if (_003C_003E8__1.profiles.Count > 1)
				{
					int selected = Math.Max(0, _003C_003E8__1.profiles.ToList().IndexOf(_003C_003E8__1.bridge.SelectedProfile ?? ""));
					Pressable pressable = SK.MenuPicker(_003C_003E8__1.profiles.Select((string _, int i) => $"Profile {i + 1}").ToList(), selected, delegate(int i)
					{
						if (!(_003C_003E8__1.profiles[i] == _003C_003E8__1.bridge.SelectedProfile))
						{
							TrackBrowserSettingsAction("settings_profile_changed", _003C_003E8__1.isEmbedded, new JsonObject((JsonNodeOptions?)null)
							{
								["profile_count"] = _003C_003E8__1.bridge.Profiles.Count,
								["profile_index"] = _003C_003E8__1.bridge.Profiles.ToList().IndexOf(_003C_003E8__1.profiles[i])
							});
							_003C_003E8__1.bridge.SelectedProfile = _003C_003E8__1.profiles[i];
						}
					});
					pressable.MaxWidth = 140.0;
					pressable.Enabled = _003C_003E8__1.bridge.ActiveCount == 0;
					pressable.Opacity = ((_003C_003E8__1.bridge.ActiveCount == 0) ? 1.0 : 0.5);
					_003C_003E2__current = SK.Row(_003CbrowserName_003E5__2 + " profile", "The profile HeyClicky works in.", pressable, null);
					_003C_003E1__state = 2;
					return true;
				}
				goto IL_01ec;
			case 2:
				_003C_003E1__state = -1;
				goto IL_01ec;
			case 3:
				{
					_003C_003E1__state = -1;
					return false;
				}
				IL_01ec:
				_003C_003E2__current = settingsContent.BrowserDisconnectRow(_003CbrowserName_003E5__2, _003C_003E8__1.isEmbedded);
				_003C_003E1__state = 3;
				return true;
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
			_003CBrowserConnectionRows_003Ed__24 _003CBrowserConnectionRows_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CBrowserConnectionRows_003Ed__ = this;
			}
			else
			{
				_003CBrowserConnectionRows_003Ed__ = new _003CBrowserConnectionRows_003Ed__24(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CBrowserConnectionRows_003Ed__.isEmbedded = _003C_003E3__isEmbedded;
			return _003CBrowserConnectionRows_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CDemoAgentMomentRows_003Ed__65 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SettingsContent _003C_003E4__this;

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
		public _003CDemoAgentMomentRows_003Ed__65(int _003C_003E1__state)
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
			SettingsContent CS_0024_003C_003E8__locals2 = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E2__current = SK.ActionRow("Run welcome-website agent", "Shippy builds the personalized welcome website, as at 1:25 in the intro video.", delegate
				{
					CS_0024_003C_003E8__locals2.host.App?.DebugLaunchOnboardingWebsiteAgent();
				}, null, null);
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = SK.ActionRow("Run SpaceX report agent", "Research Buddy writes the SpaceX competitor report, as at 1:07 in the intro video.", delegate
				{
					CS_0024_003C_003E8__locals2.host.App?.DebugLaunchOnboardingSpaceXReportAgent();
				}, null, null);
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				return false;
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
			_003CDemoAgentMomentRows_003Ed__65 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CDemoAgentMomentRows_003Ed__65(0)
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

	[CompilerGenerated]
	private sealed class _003CLinkParts_003Ed__180 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SettingsContent _003C_003E4__this;

		private JsonObject terms;

		public JsonObject _003C_003E3__terms;

		private _003C_003Ec__DisplayClass180_0 _003C_003E8__1;

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
		public _003CLinkParts_003Ed__180(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E8__1 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			SettingsContent settingsContent = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				_003C_003E8__1 = new _003C_003Ec__DisplayClass180_0();
				_003C_003E8__1._003C_003E4__this = _003C_003E4__this;
				_003C_003E8__1.code = Str(settingsContent.referral["code"]) ?? "";
				_003C_003E8__1.shareUrl = Str(settingsContent.referral["share_url"]) ?? "";
				string text = Str(settingsContent.referral["share_display_text"]) ?? _003C_003E8__1.shareUrl;
				string text2 = Str(settingsContent.referral["share_display_prefix"]) ?? "";
				if (Bool(settingsContent.referral["is_code_disabled"]))
				{
					_003C_003E2__current = new StackPanel
					{
						Spacing = 5.0,
						Children = 
						{
							(UIElement)CardLabel("Your link is paused"),
							(UIElement)Ui.Text("New people can't join through your link right now. Everything you have already earned is unchanged. Questions? Write to " + (Str(terms["payout_contact_email"]) ?? "hi@heyclicky.com") + ".", 12.5, FontWeights.Normal, MessagesPalette.Current.SecondaryText, TextAlignment.Left, 0, wrap: true)
						}
					};
					_003C_003E1__state = 1;
					return true;
				}
				if (settingsContent.editingReferralCode)
				{
					_003C_003Ec__DisplayClass180_1 CS_0024_003C_003E8__locals4 = new _003C_003Ec__DisplayClass180_1();
					Grid grid;
					(grid, CS_0024_003C_003E8__locals4.input) = SK.SearchField("yourname", settingsContent.referralCodeDraft, delegate(string t)
					{
						_003C_003E8__1._003C_003E4__this.referralCodeDraft = t;
					}, magnifier: false, 15.0, null);
					CS_0024_003C_003E8__locals4.input.Loaded += delegate
					{
						CS_0024_003C_003E8__locals4.input.Focus(FocusState.Programmatic);
					};
					CS_0024_003C_003E8__locals4.input.KeyDown += delegate(object _, KeyRoutedEventArgs e)
					{
						if (e.Key == VirtualKey.Enter)
						{
							e.Handled = true;
							_ = _003C_003E8__1._003C_003E4__this.SaveReferralCodeAsync();
						}
					};
					Grid grid2 = new Grid
					{
						ColumnSpacing = 6.0
					};
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = GridLength.Auto
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition());
					TextBlock textBlock = Ui.Text((text2.Length > 0) ? text2 : "heyclicky.com/", 15.0, FontWeights.SemiBold, SK.TextTertiary);
					textBlock.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(grid, 1);
					grid2.Children.Add(textBlock);
					grid2.Children.Add(grid);
					StackPanel stackPanel = new StackPanel
					{
						Spacing = 9.0,
						Children = 
						{
							(UIElement)CardLabel("Pick your link"),
							(UIElement)grid2
						}
					};
					stackPanel.Children.Add(Ui.Text((settingsContent.referralCodeProblem.Length > 0) ? settingsContent.referralCodeProblem : "Letters, numbers and hyphens. Links you've already shared keep working.", 11.0, (settingsContent.referralCodeProblem.Length > 0) ? FontWeights.SemiBold : FontWeights.Medium, (settingsContent.referralCodeProblem.Length > 0) ? SK.Destructive : SK.TextTertiary, TextAlignment.Left, 0, wrap: true));
					GelButton gelButton3 = new GelButton("Save", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, null, null);
					gelButton3.Click += delegate
					{
						_003C_003E8__1._003C_003E4__this.SaveReferralCodeAsync();
					};
					GelButton gelButton4 = new GelButton("Cancel", GelAppearance.SecondaryOutline, GelButton.Sizing.Normal, 100.0, null, null, null);
					gelButton4.Click += delegate
					{
						_003C_003E8__1._003C_003E4__this.editingReferralCode = false;
						_003C_003E8__1._003C_003E4__this.referralCodeProblem = "";
						_003C_003E8__1._003C_003E4__this.Render();
					};
					stackPanel.Children.Add(Ui.H(10.0, gelButton3, gelButton4));
					_003C_003E2__current = stackPanel;
					_003C_003E1__state = 2;
					return true;
				}
				TextBlock textBlock2 = new TextBlock
				{
					FontSize = 21.0,
					FontWeight = FontWeights.Bold,
					TextTrimming = TextTrimming.CharacterEllipsis,
					MaxLines = 1,
					VerticalAlignment = VerticalAlignment.Center,
					IsTextSelectionEnabled = true
				};
				if (text2.Length > 0 && text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					textBlock2.Inlines.Add(new Run
					{
						Text = text.Substring(0, text2.Length),
						Foreground = Ui.Brush(Colors.Black.WithAlpha(0.5))
					});
					InlineCollection inlines = textBlock2.Inlines;
					Run run = new Run();
					string text3 = text;
					int length = text2.Length;
					run.Text = text3.Substring(length, text3.Length - length);
					run.Foreground = Ui.Brush(Colors.Black);
					inlines.Add(run);
				}
				else
				{
					textBlock2.Inlines.Add(new Run
					{
						Text = text,
						Foreground = Ui.Brush(Colors.Black)
					});
				}
				_003C_003E8__1.edit = new Pressable
				{
					CornerRadius = new CornerRadius(100.0),
					Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
					Background = Ui.Brush(Colors.Black.WithAlpha(0.07)),
					VerticalAlignment = VerticalAlignment.Center,
					Tag = "referral-edit-link"
				};
				StackPanel item = Ui.H(4.0, Icons.Glyph("\ue70f", 10.0, Colors.Black.WithAlpha(0.6)), Ui.Text("Edit", 11.0, FontWeights.Bold, Colors.Black.WithAlpha(0.6)));
				_003C_003E8__1.edit.Children.Add(item);
				_003C_003E8__1.edit.HoverChanged += delegate(bool h)
				{
					_003C_003E8__1.edit.Background = Ui.Brush(Colors.Black.WithAlpha(h ? 0.14 : 0.07));
				};
				_003C_003E8__1.edit.Click += delegate
				{
					_003C_003E8__1._003C_003E4__this.editingReferralCode = true;
					_003C_003E8__1._003C_003E4__this.referralCodeDraft = _003C_003E8__1.code;
					_003C_003E8__1._003C_003E4__this.referralCodeProblem = "";
					_003C_003E8__1._003C_003E4__this.Render();
				};
				_003C_003E8__1.edit.Tip("Pick your own link");
				Grid grid3 = new Grid
				{
					ColumnSpacing = 8.0,
					Margin = new Thickness(16.0, 10.0, 10.0, 10.0)
				};
				grid3.ColumnDefinitions.Add(new ColumnDefinition());
				grid3.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				Grid.SetColumn(_003C_003E8__1.edit, 1);
				grid3.Children.Add(textBlock2);
				grid3.Children.Add(_003C_003E8__1.edit);
				Grid grid4 = new Grid
				{
					Children = 
					{
						(UIElement)new GelSurface(GelAppearance.SecondaryWhite, 14.0),
						(UIElement)grid3
					}
				};
				AutomationProperties.SetName(grid4, "Your referral link, " + text);
				_003C_003E2__current = new StackPanel
				{
					Spacing = 5.0,
					Children = 
					{
						(UIElement)CardLabel("Share this to earn"),
						(UIElement)grid4
					}
				};
				_003C_003E1__state = 3;
				return true;
			}
			case 1:
				_003C_003E1__state = -1;
				return false;
			case 2:
				_003C_003E1__state = -1;
				return false;
			case 3:
			{
				_003C_003E1__state = -1;
				GelButton gelButton = new GelButton((settingsContent.referralCopied == "link") ? "Link copied" : "Copy link", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, (settingsContent.referralCopied == "link") ? "\ue73e" : "\ue71b", null, null);
				gelButton.Click += delegate
				{
					_003C_003E8__1._003C_003E4__this.CopyReferral("link", _003C_003E8__1.shareUrl);
				};
				GelButton gelButton2 = new GelButton((settingsContent.referralCopied == "code") ? "Code copied" : "Copy code", GelAppearance.SecondaryWhite, GelButton.Sizing.Large, 100.0, (settingsContent.referralCopied == "code") ? "\ue73e" : "\ue8c8", null, null);
				gelButton2.Click += delegate
				{
					_003C_003E8__1._003C_003E4__this.CopyReferral("code", _003C_003E8__1.code.ToUpperInvariant());
				};
				_003C_003E2__current = new HeyClicky.App.Home.WrapPanel
				{
					HorizontalSpacing = 10.0,
					VerticalSpacing = 10.0,
					Children = 
					{
						(UIElement)gelButton,
						(UIElement)gelButton2
					}
				};
				_003C_003E1__state = 4;
				return true;
			}
			case 4:
				_003C_003E1__state = -1;
				return false;
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
			_003CLinkParts_003Ed__180 _003CLinkParts_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CLinkParts_003Ed__ = this;
			}
			else
			{
				_003CLinkParts_003Ed__ = new _003CLinkParts_003Ed__180(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CLinkParts_003Ed__.terms = _003C_003E3__terms;
			return _003CLinkParts_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CProactiveDeveloperGroups_003Ed__130 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SettingsContent _003C_003E4__this;

		private ProactiveCoordinator _003Cp_003E5__2;

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
		public _003CProactiveDeveloperGroups_003Ed__130(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cp_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			SettingsContent CS_0024_003C_003E8__locals12 = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				_003Cp_003E5__2 = ProactiveCoordinator.Current;
				if (_003Cp_003E5__2 == null)
				{
					List<FrameworkElement> list2 = new List<FrameworkElement>();
					list2.AddRange(CS_0024_003C_003E8__locals12.DemoAgentMomentRows());
					_003C_003E2__current = SK.Group("Moments", null, new global::_003C_003Ez__ReadOnlyList<FrameworkElement>(list2));
					_003C_003E1__state = 1;
					return true;
				}
				SettingsStore coreSettings = _003Cp_003E5__2.CoreSettings;
				_003C_003E2__current = SK.Group("Previews", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
				{
					SK.ToggleRow("Upcoming meetings", "A meeting countdown notch before each Google Calendar event.", coreSettings.Read((AppSettings x) => x.UpcomingMeetingsEnabled), delegate(bool v)
					{
						_003C_003Ec__DisplayClass130_0 CS_0024_003C_003E8__locals6 = new _003C_003Ec__DisplayClass130_0
						{
							v = v
						};
						CS_0024_003C_003E8__locals12.Quiet("upcoming_meetings", CS_0024_003C_003E8__locals6.v, delegate(AppSettings x)
						{
							x.UpcomingMeetingsEnabled = CS_0024_003C_003E8__locals6.v;
						});
					}),
					SK.ToggleRow("Language step in onboarding", "Ask which languages to speak before the color picker.", coreSettings.Read((AppSettings x) => x.OnboardingLanguageStepEnabled), delegate(bool v)
					{
						_003C_003Ec__DisplayClass130_1 CS_0024_003C_003E8__locals10 = new _003C_003Ec__DisplayClass130_1
						{
							v = v
						};
						CS_0024_003C_003E8__locals12.Quiet("onboarding_language_step", CS_0024_003C_003E8__locals10.v, delegate(AppSettings x)
						{
							x.OnboardingLanguageStepEnabled = CS_0024_003C_003E8__locals10.v;
						});
					}),
					SK.ToggleRow("Remote Tasks", "Reports from scheduled tasks in the agent bubbles, with Fix Task.", coreSettings.Read((AppSettings x) => x.RemoteTasksEnabled), delegate(bool v)
					{
						_003C_003Ec__DisplayClass130_2 CS_0024_003C_003E8__locals14 = new _003C_003Ec__DisplayClass130_2
						{
							v = v
						};
						CS_0024_003C_003E8__locals12.Quiet("remote_tasks", CS_0024_003C_003E8__locals14.v, delegate(AppSettings x)
						{
							x.RemoteTasksEnabled = CS_0024_003C_003E8__locals14.v;
						});
					})
				}));
				_003C_003E1__state = 2;
				return true;
			}
			case 1:
				_003C_003E1__state = -1;
				return false;
			case 2:
			{
				_003C_003E1__state = -1;
				List<FrameworkElement> list = new List<FrameworkElement>();
				list.Add(SK.ActionRow("Fetch suggestions now", "Finds real tasks in your connected apps, then invites you through the morning notch. Takes a few minutes.", _003Cp_003E5__2.DebugFetchNow, null, null));
				list.Add(SK.ActionRow("Reset suggestions", "Clears the queued cards, the yes/no history, today's automatic check, and the morning notch.", _003Cp_003E5__2.DebugReset, null, null));
				list.AddRange(CS_0024_003C_003E8__locals12.DemoAgentMomentRows());
				_003C_003E2__current = SK.Group("Moments", null, new global::_003C_003Ez__ReadOnlyList<FrameworkElement>(list));
				_003C_003E1__state = 3;
				return true;
			}
			case 3:
				_003C_003E1__state = -1;
				return false;
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
			_003CProactiveDeveloperGroups_003Ed__130 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CProactiveDeveloperGroups_003Ed__130(0)
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

	private string? billingActionError;

	private static readonly (string Name, string Glyph)[] DeletionReasons = new(string, string)[6]
	{
		("Not useful for me", "\ue9ce"),
		("Too buggy", "\uebe8"),
		("Too expensive", "\ue8c7"),
		("Privacy concerns", "\ue72e"),
		("Found something better", "\ue734"),
		("Other", "\ue712")
	};

	private static readonly (string Capability, string Name, string Glyph, string SettingsUri, bool Pinned)[] PrivacyCapabilities = new(string, string, string, string, bool)[8]
	{
		("broadFileSystemAccess", "Files & Folders", "\ue8b7", "ms-settings:privacy-broadfilesystemaccess", true),
		("documentsLibrary", "Documents", "\ue8a5", "ms-settings:privacy-documents", false),
		("picturesLibrary", "Pictures", "\ue91b", "ms-settings:privacy-pictures", false),
		("contacts", "Contacts", "\ue77b", "ms-settings:privacy-contacts", false),
		("appointments", "Calendar", "\ue787", "ms-settings:privacy-calendar", false),
		("microphone", "Microphone", "\ue720", "ms-settings:privacy-microphone", false),
		("webcam", "Camera", "\ue722", "ms-settings:privacy-webcam", false),
		("location", "Location", "\ue81d", "ms-settings:privacy-location", false)
	};

	private bool isConfirmingBrowserDisconnect;

	private StackPanel? browserCardHost;

	private static readonly Dictionary<string, ImageSource> browserIcons = new Dictionary<string, ImageSource>();

	private static readonly HashSet<string> browserIconsLookedUp = new HashSet<string>();

	private ConnectorDraft? connectorDraft;

	private string? confirmRemoveConnector;

	private static readonly TimeSpan ParkedConnectorDraftRetention = TimeSpan.FromSeconds(120L);

	private static ConnectorDraft? parkedConnectorDraft;

	private static DateTime parkedConnectorDraftAt;

	private bool connectorEventsHooked;

	private static readonly (string Id, string Name)[] BrainModels = new(string, string)[13]
	{
		("gpt-6.1-sol", "GPT-6.1 Sol"),
		("gpt-6.1-sol-medium", "GPT-6.1 Sol (medium)"),
		("gpt-6.1-sol-fast", "GPT-6.1 Sol fast"),
		("gpt-6.1-sol-fast-medium", "GPT-6.1 Sol fast (medium)"),
		("gpt-6.1-sol-ultrafast", "GPT-6.1 Sol Ultrafast (low)"),
		("gpt-6.1-sol-ultrafast-medium", "GPT-6.1 Sol Ultrafast (medium)"),
		("gpt-6.1-sol-ultrafast-high", "GPT-6.1 Sol Ultrafast (high)"),
		("gpt-6.1-terra-fast-medium", "GPT-6.1 Terra fast (medium)"),
		("claude-haiku-5-5", "Claude Haiku 5.5 (experimental)"),
		("claude-sonnet-5-5", "Claude Sonnet 5.5 (experimental)"),
		("claude-opus-5-5", "Claude Opus 5.5 (experimental)"),
		("deepseek-v4.1-flash-medium", "DeepSeek V4.1 Flash 1017 (medium, experimental)"),
		("deepseek-v4.1-flash-high", "DeepSeek V4.1 Flash 1017 (high, experimental)")
	};

	private static DateTime? oneBrainFreshSessionStartedAt;

	private const string DefaultBrainModel = "gpt-6.1-sol-fast-medium";

	private static readonly (string Code, string Name, string? Native)[] Languages = new(string, string, string)[83]
	{
		("af", "Afrikaans", null),
		("am", "Amharic", "አማርኛ"),
		("ar", "Arabic", "العربية"),
		("hy", "Armenian", "Հայերեն"),
		("rup", "Aromanian", "Armãneashti"),
		("as", "Assamese", "অসম\u09c0য\u09bc\u09be"),
		("az", "Azerbaijani", "Azərbaycanca"),
		("be", "Belarusian", "Беларуская"),
		("bn", "Bengali", "ব\u09be\u0982ল\u09be"),
		("bs", "Bosnian", "Bosanski"),
		("bg", "Bulgarian", "Български"),
		("my", "Burmese", "မ\u103cန\u103aမ\u102c"),
		("yue", "Cantonese", "粵語"),
		("kea", "Cape Verdean Creole", "Kabuverdianu"),
		("ca", "Catalan", "Català"),
		("ceb", "Cebuano", "Sinugboanon"),
		("zh", "Chinese", "中文"),
		("hr", "Croatian", "Hrvatski"),
		("cs", "Czech", "Čeština"),
		("da", "Danish", "Dansk"),
		("nl", "Dutch", "Nederlands"),
		("en", "English", null),
		("et", "Estonian", "Eesti"),
		("fi", "Finnish", "Suomi"),
		("fr", "French", "Français"),
		("gl", "Galician", "Galego"),
		("ka", "Georgian", "ქართული"),
		("de", "German", "Deutsch"),
		("el", "Greek", "Ελληνικά"),
		("gu", "Gujarati", "ગ\u0ac1જર\u0abeત\u0ac0"),
		("ha", "Hausa", null),
		("he", "Hebrew", "עברית"),
		("hi", "Hindi", "ह\u093fन\u094dद\u0940"),
		("hu", "Hungarian", "Magyar"),
		("is", "Icelandic", "Íslenska"),
		("id", "Indonesian", "Bahasa Indonesia"),
		("it", "Italian", "Italiano"),
		("ja", "Japanese", "日本語"),
		("jv", "Javanese", "Basa Jawa"),
		("kn", "Kannada", "ಕನ\u0ccdನಡ"),
		("kk", "Kazakh", "Қазақ тілі"),
		("km", "Khmer", "ខ\u17d2ម\u17c2រ"),
		("ko", "Korean", "한국어"),
		("ky", "Kyrgyz", "Кыргызча"),
		("lv", "Latvian", "Latviešu"),
		("ln", "Lingala", "Lingála"),
		("lt", "Lithuanian", "Lietuvių"),
		("mk", "Macedonian", "Македонски"),
		("ms", "Malay", "Bahasa Melayu"),
		("ml", "Malayalam", "മലയ\u0d3eള\u0d02"),
		("mt", "Maltese", "Malti"),
		("mr", "Marathi", "मर\u093eठ\u0940"),
		("mn", "Mongolian", "Монгол"),
		("ne", "Nepali", "न\u0947प\u093eल\u0940"),
		("no", "Norwegian", "Norsk"),
		("or", "Odia", "ଓଡ\u0b3c\u0b3fଆ"),
		("ps", "Pashto", "پښتو"),
		("fa", "Persian", "فارسی"),
		("pl", "Polish", "Polski"),
		("pt", "Portuguese", "Português"),
		("pa", "Punjabi", "ਪ\u0a70ਜ\u0a3eਬ\u0a40"),
		("ro", "Romanian", "Română"),
		("ru", "Russian", "Русский"),
		("sr", "Serbian", "Српски"),
		("sd", "Sindhi", "سنڌي"),
		("si", "Sinhala", "ස\u0dd2\u0d82හල"),
		("sk", "Slovak", "Slovenčina"),
		("sl", "Slovenian", "Slovenščina"),
		("so", "Somali", "Soomaali"),
		("es", "Spanish", "Español"),
		("sw", "Swahili", "Kiswahili"),
		("sv", "Swedish", "Svenska"),
		("tl", "Tagalog", "Tagalog"),
		("tg", "Tajik", "Тоҷикӣ"),
		("ta", "Tamil", "தம\u0bbfழ\u0bcd"),
		("te", "Telugu", "త\u0c46ల\u0c41గ\u0c41"),
		("th", "Thai", "ไทย"),
		("tr", "Turkish", "Türkçe"),
		("uk", "Ukrainian", "Українська"),
		("ur", "Urdu", "اردو"),
		("uz", "Uzbek", "Oʻzbekcha"),
		("vi", "Vietnamese", "Tiếng Việt"),
		("yo", "Yoruba", "Yorùbá")
	};

	private static readonly string[] PopularLanguageCodes = new string[12]
	{
		"en", "es", "fr", "de", "hi", "zh", "ja", "ko", "pt", "ru",
		"ar", "it"
	};

	private static readonly (string Code, string Name, string? Native)[] LanguagesForGrid = PopularLanguageCodes.Select((string c) => Languages.First(((string Code, string Name, string Native) l) => l.Code == c)).Concat<(string, string, string)>(Languages.Where(((string Code, string Name, string Native) l) => !PopularLanguageCodes.Contains(l.Code)).OrderBy<(string, string, string), string>(((string Code, string Name, string Native) l) => l.Name, StringComparer.Ordinal)).ToArray();

	private const int MaxDictionaryWords = 50;

	private const int MaxDictionaryWordLength = 60;

	private string languageSearch = "";

	private string draftWord = "";

	private Grid? languageGridHost;

	private bool focusWordInputAfterRender;

	private AgentIntegrationCatalog? catalog;

	private bool loadingIntegrations;

	private string? integrationError;

	private string integrationSearch = "";

	private readonly HashSet<string> integrationBusy = new HashSet<string>();

	private readonly HashSet<string> accountBusy = new HashSet<string>();

	private string? confirmDisconnectIntegration;

	private string? confirmDisconnectAccount;

	private (string Id, int Baseline, DateTime Started)? awaitingConnection;

	private string? justConnected;

	private StackPanel? integrationListHost;

	private string? editingLabelAccount;

	private readonly Dictionary<string, Departure> departures = new Dictionary<string, Departure>();

	private List<(string Id, string Name, Func<FrameworkElement> Logo)> lastConnected = new List<(string, string, Func<FrameworkElement>)>();

	private readonly Dictionary<string, FrameworkElement> departureRows = new Dictionary<string, FrameworkElement>();

	private static readonly double[] ShimmerNameWidths = new double[6] { 92.0, 120.0, 74.0, 108.0, 86.0, 130.0 };

	private static readonly double[] ShimmerDescriptionFractions = new double[6] { 0.62, 0.48, 0.7, 0.55, 0.66, 0.44 };

	private JsonObject? referral;

	private bool referralLoading;

	private bool referralFailed;

	private string? referralCopied;

	private bool editingReferralCode;

	private string referralCodeDraft = "";

	private string referralCodeProblem = "";

	private string friendCodeDraft = "";

	private string friendCodeMessage = "";

	private DateTimeOffset? referralLoadedAt;

	private string? referralLoadedFor;

	private bool isSurfaceSettled = true;

	private bool referralPageSettled = true;

	private bool referralSkeletonShown;

	private int referralSettleGeneration;

	private bool referralCelebrate;

	private readonly ReferralEstimatorCard.State estimatorState = new ReferralEstimatorCard.State();

	private const int ReferralFriendsPage = 10;

	private int referralVisibleFriends = 10;

	private static readonly (string Name, string Asset, string Plan, string Detail, string? Earned)[] SampleFriends = new(string, string, string, string, string)[3]
	{
		("Farza", "referral-sample-farza", "max", "Subscribed for 2 months", "+$50"),
		("Nilesh", "referral-sample-nilesh", "pro", "Subscribed for 3 months", "+$15"),
		("Kelli", "referral-sample-kelli", "free", "Joined through your link", null)
	};

	private static readonly double[] ReferralSkeletonNameWidths = new double[3] { 110.0, 90.0, 124.0 };

	private readonly Dictionary<string, RoutineSettingsRow> routineRows = new Dictionary<string, RoutineSettingsRow>();

	private bool routinesHooked;

	private ShortcutRecorder? recorder;

	private ShortcutRecorder.Slot? conflictRow;

	private ShortcutRecorder.Slot? conflictWith;

	private bool recordingHooked;

	private static readonly (string Id, string Name, string Subtitle, string? Fill, string? Stroke, Color Color)[] Voices = new(string, string, string, string, string, Color)[10]
	{
		("cedar", "Cedar", "Warm default", "M12,2 C15.3,2 17.8,4.4 18,7.4 C20.3,8.2 21.8,10.3 21.8,12.7 C21.8,15.8 19.3,18.2 16.2,18.2 L13.2,18.2 L13.2,21 C13.2,21.6 12.7,22 12,22 C11.3,22 10.8,21.6 10.8,21 L10.8,18.2 L7.8,18.2 C4.7,18.2 2.2,15.8 2.2,12.7 C2.2,10.3 3.7,8.2 6,7.4 C6.2,4.4 8.7,2 12,2 Z", null, DS.Rgb(0.42, 0.72, 0.52)),
		("marin", "Marin", "Bright & natural", null, "M3,7 C5,4.8 7,4.8 9,7 C11,9.2 13,9.2 15,7 C17,4.8 19,4.8 21,7 M3,12 C5,9.8 7,9.8 9,12 C11,14.2 13,14.2 15,12 C17,9.8 19,9.8 21,12 M3,17 C5,14.8 7,14.8 9,17 C11,19.2 13,19.2 15,17 C17,14.8 19,14.8 21,17", DS.Rgb(0.36, 0.66, 1.0)),
		("alloy", "Alloy", "Balanced", "M9,5 a3,3 0 1 0 6,0 a3,3 0 1 0 -6,0 Z M2,12 a3,3 0 1 0 6,0 a3,3 0 1 0 -6,0 Z M9,12 a3,3 0 1 0 6,0 a3,3 0 1 0 -6,0 Z M16,12 a3,3 0 1 0 6,0 a3,3 0 1 0 -6,0 Z M9,19 a3,3 0 1 0 6,0 a3,3 0 1 0 -6,0 Z", null, DS.Rgb(0.6, 0.62, 0.7)),
		("ash", "Ash", "Calm & even", "M9.5,4 C11.7,4 13.6,5.4 14.2,7.4 C14.8,7.2 15.4,7.1 16,7.1 C18.6,7.1 20.6,9 20.8,11.5 C22.1,12.2 23,13.6 23,15.2 C23,17.6 21.1,19.5 18.7,19.5 L5.6,19.5 C3,19.5 1,17.5 1,15 C1,12.9 2.4,11.1 4.4,10.6 C4.4,10.4 4.4,10.2 4.4,10 C4.4,6.7 6.6,4 9.5,4 Z", null, DS.Rgb(0.55, 0.58, 0.64)),
		("ballad", "Ballad", "Expressive", "M10.2,4.2 L10.2,15.3 C9.5,14.9 8.7,14.7 7.8,14.7 C5.6,14.7 3.8,16.1 3.8,17.9 C3.8,19.6 5.6,21 7.8,21 C10,21 11.9,19.6 11.9,17.9 L11.9,8.3 C14.4,8.9 16.6,10.1 17.5,12.6 C17.9,11.8 18.1,10.9 18.1,10 C18.1,6.6 14.7,5.4 12.2,3.6 C11.4,3 10.2,3.2 10.2,4.2 Z", null, DS.Rgb(0.74, 0.46, 0.92)),
		("coral", "Coral", "Friendly", "M12,21 C12,21 2.5,15.3 2.5,8.8 C2.5,5.9 4.8,3.6 7.6,3.6 C9.4,3.6 11,4.6 12,6.1 C13,4.6 14.6,3.6 16.4,3.6 C19.2,3.6 21.5,5.9 21.5,8.8 C21.5,15.3 12,21 12,21 Z", null, DS.Rgb(1.0, 0.5, 0.55)),
		("echo", "Echo", "Crisp", "M10,12 a2,2 0 1 0 4,0 a2,2 0 1 0 -4,0 Z", "M8.3,8.3 A5.2,5.2 0 0 0 8.3,15.7 M15.7,8.3 A5.2,5.2 0 0 1 15.7,15.7 M5.2,5.2 A9.6,9.6 0 0 0 5.2,18.8 M18.8,5.2 A9.6,9.6 0 0 1 18.8,18.8", DS.Rgb(0.4, 0.7, 0.86)),
		("sage", "Sage", "Measured", "M20.6,3.2 C12.2,3.4 4.2,6.4 4.2,13.9 C4.2,15.8 4.8,17.4 5.8,18.7 L3.6,20.9 L4.9,22.2 L7.1,20 C8.3,20.9 9.8,21.4 11.5,21.4 C18.6,21.4 20.7,13.2 20.6,3.2 Z", null, DS.Rgb(0.46, 0.74, 0.46)),
		("shimmer", "Shimmer", "Light & airy", "M10,3 C10.6,7.6 12.4,9.4 17,10 C12.4,10.6 10.6,12.4 10,17 C9.4,12.4 7.6,10.6 3,10 C7.6,9.4 9.4,7.6 10,3 Z M18,13 C18.3,15.2 18.8,15.7 21,16 C18.8,16.3 18.3,16.8 18,19 C17.7,16.8 17.2,16.3 15,16 C17.2,15.7 17.7,15.2 18,13 Z M17,2 C17.2,3.4 17.6,3.8 19,4 C17.6,4.2 17.2,4.6 17,6 C16.8,4.6 16.4,4.2 15,4 C16.4,3.8 16.8,3.4 17,2 Z", null, DS.Rgb(0.96, 0.78, 0.36)),
		("verse", "Verse", "Lively", "M5,3.5 L19,3.5 A3,3 0 0 1 22,6.5 L22,15 A3,3 0 0 1 19,18 L11.5,18 L6.5,21.8 L6.5,18 L5,18 A3,3 0 0 1 2,15 L2,6.5 A3,3 0 0 1 5,3.5 Z M6.5,8 L6.5,9.8 L17.5,9.8 L17.5,8 Z M6.5,11.7 L6.5,13.5 L14,13.5 L14,11.7 Z", null, DS.Rgb(1.0, 0.62, 0.34))
	};

	private static readonly (string? Fill, string? Stroke, Color Color)[] ExpressiveGlyphs = new(string, string, Color)[30]
	{
		("M2,12 a10,10 0 1 0 20,0 a10,10 0 1 0 -20,0 Z M7.8,9.5 a1.5,1.7 0 1 0 3,0 a1.5,1.7 0 1 0 -3,0 Z M13.2,9.5 a1.5,1.7 0 1 0 3,0 a1.5,1.7 0 1 0 -3,0 Z M7.2,13.4 C8.4,17 15.6,17 16.8,13.4 L15.1,13.4 C14,15.3 10,15.3 8.9,13.4 Z", null, DS.Rgb(1.0, 0.7, 0.3)),
		("M7,12 a5,5 0 1 0 10,0 a5,5 0 1 0 -10,0 Z", "M12,1.8 L12,3.6 M12,20.4 L12,22.2 M1.8,12 L3.6,12 M20.4,12 L22.2,12 M4.8,4.8 L6.1,6.1 M17.9,17.9 L19.2,19.2 M4.8,19.2 L6.1,17.9 M17.9,6.1 L19.2,4.8", DS.Rgb(1.0, 0.84, 0.36)),
		(null, "M3,8 L14,8 C16,8 17,6.8 17,5.5 C17,4.2 16,3.2 14.7,3.2 C13.6,3.2 12.8,3.9 12.5,4.8 M3,12 L19,12 C20.8,12 22,13.2 22,14.8 C22,16.4 20.8,17.5 19.3,17.5 C18,17.5 17.1,16.7 16.8,15.6 M3,16 L11,16 C12.6,16 13.6,17.1 13.6,18.5 C13.6,19.9 12.6,20.8 11.3,20.8", DS.Rgb(0.52, 0.8, 0.92)),
		("M13.5,2 L4.5,13.5 L11,13.5 L10,22 L19.5,10 L13,10 Z", null, DS.Rgb(1.0, 0.48, 0.36)),
		("M12,2 C12.9,8.6 15.4,11.1 22,12 C15.4,12.9 12.9,15.4 12,22 C11.1,15.4 8.6,12.9 2,12 C8.6,11.1 11.1,8.6 12,2 Z", null, DS.Rgb(0.98, 0.56, 0.8)),
		("M12,2 C12.6,5.6 17.8,8.4 17.8,14.2 C17.8,18.6 15.2,22 12,22 C8.8,22 6.2,18.8 6.2,15.2 C6.2,12.2 7.8,10.3 9.2,9 C9.2,11 10,12.4 11.2,13 C10.8,9.4 11,5.4 12,2 Z", null, DS.Rgb(0.96, 0.58, 0.4)),
		("M7,21 C4.8,19.2 4,16.8 4,14 L4,9.5 C4,8.7 4.7,8 5.5,8 C6.3,8 7,8.7 7,9.5 L7,12 L8,12 L8,4.5 C8,3.7 8.7,3 9.5,3 C10.3,3 11,3.7 11,4.5 L11,11 L12,11 L12,3.5 C12,2.7 12.7,2 13.5,2 C14.3,2 15,2.7 15,3.5 L15,11 L16,11 L16,5 C16,4.2 16.7,3.5 17.5,3.5 C18.3,3.5 19,4.2 19,5 L19,15 C19,18.9 16.4,22 12.6,22 L10.5,22 C9.2,22 8,21.7 7,21 Z", "M20.8,2.6 C21.8,3.4 22.3,4.4 22.4,5.6", DS.Rgb(0.5, 0.78, 0.54)),
		("M3,21 L8.2,7.6 L16.4,15.8 Z", "M13,3 L13.6,5.6 M17,7.2 C18.6,6.2 20,6.6 21,7.8 M18.4,11.2 L21,11.8 M10.4,5.4 C11.1,4.5 11.1,3.5 10.5,2.7", DS.Rgb(0.7, 0.52, 0.96)),
		("M12,2 L21,12 L12,22 L3,12 Z", null, DS.Rgb(0.4, 0.62, 0.96)),
		("M3,4.5 C5.8,3.6 8.9,3.8 11.2,5.4 L11.2,20.4 C8.9,18.9 5.8,18.7 3,19.6 Z M12.8,5.4 C15.1,3.8 18.2,3.6 21,4.5 L21,19.6 C18.2,18.7 15.1,18.9 12.8,20.4 Z", null, DS.Rgb(0.58, 0.64, 0.74)),
		("M12,2 L20,5 L20,11 C20,16.2 16.6,20.4 12,22 C7.4,20.4 4,16.2 4,11 L4,5 Z", null, DS.Rgb(0.46, 0.56, 0.7)),
		("M7,13.5 a5,5 0 0 1 10,0 Z", "M12,3.5 L12,5.5 M5.4,6.6 L6.8,8 M18.6,6.6 L17.2,8 M2.5,13.5 L4.5,13.5 M19.5,13.5 L21.5,13.5 M4,17 L20,17 M7,20.5 L17,20.5", DS.Rgb(0.98, 0.74, 0.5)),
		("M12,2.5 L14.9,8.6 L21.5,9.4 L16.6,13.9 L17.9,20.5 L12,17.2 L6.1,20.5 L7.4,13.9 L2.5,9.4 L9.1,8.6 Z", null, DS.Rgb(1.0, 0.8, 0.3)),
		("M7,19.5 C4.2,19.5 2,17.4 2,14.7 C2,12.3 3.8,10.3 6.1,10 C6.8,6.9 9.4,4.5 12.6,4.5 C16.1,4.5 18.9,7.2 19.1,10.6 C21,11.2 22.3,12.9 22.3,15 C22.3,17.5 20.3,19.5 17.8,19.5 Z", null, DS.Rgb(0.66, 0.78, 0.92)),
		("M12,2.5 C12,2.5 5,10.4 5,14.8 C5,18.7 8.1,21.8 12,21.8 C15.9,21.8 19,18.7 19,14.8 C19,10.4 12,2.5 12,2.5 Z", null, DS.Rgb(0.38, 0.72, 0.96)),
		("M15,3 A9.3,9.3 0 1 0 21,15.6 A7.4,7.4 0 0 1 15,3 Z", null, DS.Rgb(0.62, 0.58, 0.86)),
		("M3,10 h2 v4 h-2 z M7,7 h2 v10 h-2 z M11,3 h2 v18 h-2 z M15,6 h2 v12 h-2 z M19,9 h2 v6 h-2 z", null, DS.Rgb(0.52, 0.66, 0.92)),
		("M12,2 L14.2,4.2 L17.3,3.6 L18.1,6.6 L21,7.8 L20.1,10.8 L21.9,13.4 L19.4,15.3 L19.4,18.4 L16.3,18.8 L14.6,21.5 L12,20 L9.4,21.5 L7.7,18.8 L4.6,18.4 L4.6,15.3 L2.1,13.4 L3.9,10.8 L3,7.8 L5.9,6.6 L6.7,3.6 L9.8,4.2 Z", null, DS.Rgb(0.9, 0.56, 0.74)),
		("M8,4.6 L11,6.3 L11,9.8 L8,11.5 L5,9.8 L5,6.3 Z M16,4.6 L19,6.3 L19,9.8 L16,11.5 L13,9.8 L13,6.3 Z M12,12.3 L15,14 L15,17.5 L12,19.2 L9,17.5 L9,14 Z", null, DS.Rgb(0.44, 0.8, 0.76)),
		("M1.5,20 L8.5,8 L12.4,14.6 L15.5,9.5 L22.5,20 Z", null, DS.Rgb(0.64, 0.56, 0.48)),
		("M12,2 C8.1,2 5,5 5,8.8 C5,11.3 6.3,13.1 7.8,14.5 C8.6,15.2 9,16 9,17 L15,17 C15,16 15.4,15.2 16.2,14.5 C17.7,13.1 19,11.3 19,8.8 C19,5 15.9,2 12,2 Z M9,18.5 L15,18.5 L15,19.8 L9,19.8 Z M10,21 L14,21 C14,21.8 13.1,22.4 12,22.4 C10.9,22.4 10,21.8 10,21 Z", null, DS.Rgb(0.98, 0.84, 0.42)),
		(Voices[4].Fill, null, DS.Rgb(0.96, 0.5, 0.62)),
		("M12,5 A8,8 0 1 0 19.5,16.5 A6.4,6.4 0 0 1 12,5 Z M18,2 C18.3,3.7 18.8,4.2 20.5,4.5 C18.8,4.8 18.3,5.3 18,7 C17.7,5.3 17.2,4.8 15.5,4.5 C17.2,4.2 17.7,3.7 18,2 Z", null, DS.Rgb(0.8, 0.74, 0.92)),
		("M2,13.5 L13,4.5 L11.5,10.5 L22,10.5 L11,19.5 L12.5,13.5 Z", null, DS.Rgb(0.4, 0.58, 0.8)),
		("M2,12 a10,10 0 1 0 20,0 a10,10 0 1 0 -20,0 Z M7,8.6 L17,8.6 L17,10.6 L7,10.6 Z M7,13.4 L17,13.4 L17,15.4 L7,15.4 Z", null, DS.Rgb(0.56, 0.7, 0.66)),
		(Voices[7].Fill, null, DS.Rgb(0.54, 0.72, 0.44)),
		("M2,12 a10,10 0 1 0 20,0 a10,10 0 1 0 -20,0 Z M6.5,11 L13.8,11 L11,8.2 L12.4,6.8 L17.6,12 L12.4,17.2 L11,15.8 L13.8,13 L6.5,13 Z", null, DS.Rgb(0.96, 0.56, 0.4)),
		("M4,6 L17,6 L17,12.5 C17,15.5 14.5,18 11.5,18 L9.5,18 C6.5,18 4,15.5 4,12.5 Z M17,7.5 L18.5,7.5 C20.4,7.5 21.8,8.9 21.8,10.6 C21.8,12.4 20.4,13.8 18.5,13.8 L16.8,13.8 L17,12 L18.5,12 C19.3,12 19.9,11.4 19.9,10.6 C19.9,9.9 19.3,9.3 18.5,9.3 L17,9.3 Z M2.5,19.5 L18.5,19.5 C18.5,20.6 17.6,21.5 16.5,21.5 L4.5,21.5 C3.4,21.5 2.5,20.6 2.5,19.5 Z", null, DS.Rgb(0.74, 0.58, 0.44)),
		(Voices[5].Fill, null, DS.Rgb(0.96, 0.6, 0.7)),
		("M12,3 L23,8.5 L12,14 L1,8.5 Z M5.5,11.5 L5.5,16 C7,18 9.4,19 12,19 C14.6,19 17,18 18.5,16 L18.5,11.5 L12,14.8 Z", "M21,9.5 L21,15", DS.Rgb(0.5, 0.62, 0.9))
	};

	private static readonly (double Multiplier, string Label)[] Speeds = new(double, string)[5]
	{
		(0.5, "0.5x"),
		(0.75, "0.75x"),
		(1.0, "1x"),
		(1.25, "1.25x"),
		(1.5, "1.5x")
	};

	private IReadOnlyList<AudioDeviceInfo> captureDevices = Array.Empty<AudioDeviceInfo>();

	private IReadOnlyList<AudioDeviceInfo> renderDevices = Array.Empty<AudioDeviceInfo>();

	private MediaPlayer? voicePreview;

	private int voicePreviewRequest;

	private bool languageListOpen;

	private string voiceLanguageSearch = "";

	private Grid? languageListHost;

	private bool choosingVoiceLanguages;

	private bool voiceLanguageLimitReached;

	private readonly Dictionary<string, FrameworkElement> previewSpinners = new Dictionary<string, FrameworkElement>();

	private DispatcherTimer? previewSpinnerTimer;

	private WasapiMicrophoneCapture? micTest;

	private DispatcherTimer? micTestTimer;

	private double micTestLevel;

	private bool micTestHeard;

	private DateTime micTestStarted;

	private string micTestStatus = "Tap to test selected input";

	private Color micTestStatusColor;

	private StackPanel? micBars;

	private TextBlock? micStatusText;

	private GelButton? micTestButton;

	private bool audioDeviceChangesHooked;

	private bool audioDeviceUnloadHooked;

	private readonly SettingsHost host;

	private readonly AppServices s;

	private readonly IAppActions actions;

	private readonly StackPanel sidebarItems = new StackPanel
	{
		Spacing = 16.0
	};

	private readonly ColumnDefinition sidebarColumn = new ColumnDefinition
	{
		Width = new GridLength(228.0)
	};

	private readonly ContentControl content = new ContentControl
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch,
		IsTabStop = false
	};

	private readonly ScrollViewer scroller;

	private readonly Grid overlayLayer = new Grid();

	private readonly Microsoft.UI.Dispatching.DispatcherQueue dq;

	private readonly MessagesPalette look = MessagesPalette.Current;

	private SettingsWindow.Page page;

	private int quietWrites;

	private bool isShown = true;

	private bool subscribed;

	private bool loadedBefore;

	private readonly Grid sidebar;

	private SettingsWindow.Page? lastBuiltPage;

	private bool renderQueued;

	private readonly Dictionary<SettingsWindow.Page, (Pressable Row, Grid IconBox, TextBlock Text, string Glyph)> sidebarRows = new Dictionary<SettingsWindow.Page, (Pressable, Grid, TextBlock, string)>();

	private readonly Dictionary<SettingsWindow.Page, bool> sidebarRowSelected = new Dictionary<SettingsWindow.Page, bool>();

	private string? sidebarSignature;

	private static readonly Dictionary<string, BitmapImage> AvatarImages = new Dictionary<string, BitmapImage>();

	private const string PersonFillPath = "M12,2.5 A4.75,4.75 0 1 1 12,12 A4.75,4.75 0 1 1 12,2.5 Z M4,20.2 C4,16.4 7.6,13.8 12,13.8 C16.4,13.8 20,16.4 20,20.2 C20,21 19.5,21.5 18.7,21.5 L5.3,21.5 C4.5,21.5 4,21 4,20.2 Z";

	private const string TextCursorPath = "M7.5,3 L16.5,3 L16.5,5 L13,5 L13,19 L16.5,19 L16.5,21 L7.5,21 L7.5,19 L11,19 L11,5 L7.5,5 Z";

	private const string CursorArrowPath = "M6,2.5 L6,19.5 L10.2,15.6 L13,21.8 L15.6,20.6 L12.9,14.6 L18.6,14.6 Z";

	private static readonly string HexGridPath = string.Concat(new(double, double)[7]
	{
		(12.0, 12.0),
		(18.5, 12.0),
		(15.25, 17.63),
		(8.75, 17.63),
		(5.5, 12.0),
		(8.75, 6.37),
		(15.25, 6.37)
	}.Select(((double, double) c) => FormattableString.Invariant($"M{c.Item1 - 2.5},{c.Item2} a2.5,2.5 0 1 0 5,0 a2.5,2.5 0 1 0 -5,0 Z ")));

	private const string CommandGlyph = "⌘";

	private bool billingHooked;

	private string? shownPlanSignature;

	public static AgentIntegrationCatalog? DemoCatalog { get; set; }

	public bool IsSurfaceSettled
	{
		get
		{
			return isSurfaceSettled;
		}
		set
		{
			if (isSurfaceSettled != value)
			{
				isSurfaceSettled = value;
				if (page == SettingsWindow.Page.Referral)
				{
					Render();
				}
			}
		}
	}

	public bool ReferralShowsSkeleton
	{
		get
		{
			if (page == SettingsWindow.Page.Referral && host.IsSignedIn && !referralFailed)
			{
				if (isSurfaceSettled && referralPageSettled)
				{
					return referral == null;
				}
				return true;
			}
			return false;
		}
	}

	public JsonObject? ReferralForTests => referral;

	public int ReferralRequestsForTests { get; private set; }

	private ShortcutRecorder Recorder
	{
		get
		{
			if (recorder != null)
			{
				return recorder;
			}
			recorder = new ShortcutRecorder(dq);
			recorder.Validate = delegate(ShortcutRecorder.Slot slot, HotkeyBinding binding)
			{
				HotkeyBindings b = host.Read((AppSettings x) => x.EffectiveHotkeys);
				ShortcutRecorder.Slot? slot2 = HotkeyRules.Conflict(b, slot, binding);
				if (!slot2.HasValue)
				{
					return (string?)null;
				}
				ShortcutRecorder.Slot valueOrDefault = slot2.GetValueOrDefault();
				return ShortcutCaptureRules.ConflictLine(binding, HotkeyRules.Title(valueOrDefault), HotkeyRules.Get(b, valueOrDefault));
			};
			recorder.Changed += delegate
			{
				SetMonitorRecording(recorder.Mode.HasValue);
				if (page == SettingsWindow.Page.Shortcuts)
				{
					Render();
				}
			};
			return recorder;
		}
	}

	public SettingsWindow.Page CurrentPage => page;

	public SettingsHost Host => host;

	public double SidebarWidth
	{
		set
		{
			sidebarColumn.Width = new GridLength(value);
		}
	}

	public event Action<SettingsWindow.Page>? PageChanged;

	private FrameworkElement AccountPage()
	{
		if (!s.Auth.IsSignedIn)
		{
			GelButton gelButton = new GelButton("Sign in with Google", GelAppearance.PrimaryBlue, GelButton.Sizing.Compact, 100.0, null, null, null)
			{
				HorizontalAlignment = HorizontalAlignment.Left
			};
			gelButton.Click += delegate
			{
				actions.SignIn();
			};
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 12.0,
				Padding = new Thickness(16.0),
				Children = 
				{
					(UIElement)Ui.Text("You're not signed in", 14.0, FontWeights.SemiBold, SK.TextPrimary),
					(UIElement)Ui.Text("Sign in to sync your plan, connect integrations, and let agents work for you.", 11.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true),
					(UIElement)gelButton
				}
			};
			GelButton gelButton2 = DevSignIn.Button(actions, GelButton.Sizing.Compact);
			if ((object)gelButton2 != null)
			{
				gelButton2.HorizontalAlignment = HorizontalAlignment.Left;
				stackPanel.Children.Add(gelButton2);
			}
			return SK.Scaffold("Account", "Your plan, usage, and account.", new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(SK.Group(null, stackPanel)));
		}
		return SK.Scaffold("Account", "Your plan, usage, and account.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
		{
			PlanGroup(),
			AccountActionsGroup()
		}));
	}

	private FrameworkElement PlanGroup()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Padding = new Thickness(16.0)
		};
		BillingUi billing = host.Billing;
		if (billing == null)
		{
			stackPanel.Children.Add(Ui.Text("Your plan shows here when HeyClicky is connected to its servers.", 11.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true));
			return SK.Group("Plan", stackPanel);
		}
		BillingPlanCard planCard = billing.PlanCard;
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 5.0
		};
		stackPanel2.Children.Add(Ui.Text(planCard.Title, 14.0, FontWeights.SemiBold, SK.TextPrimary, TextAlignment.Left, 0, wrap: true));
		string description = planCard.Description;
		if (description != null)
		{
			stackPanel2.Children.Add(Ui.Text(description, 11.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true));
		}
		string teamLabel = planCard.TeamLabel;
		if (teamLabel != null)
		{
			stackPanel2.Children.Add(Ui.H(5.0, Icons.Glyph("\ue716", 10.0, SK.TextSecondary), Ui.Text(teamLabel, 11.0, FontWeights.SemiBold, SK.TextSecondary)));
		}
		string errorText = planCard.ErrorText;
		if (errorText != null)
		{
			stackPanel2.Children.Add(Ui.Text(errorText, 11.0, FontWeights.SemiBold, SK.Destructive, TextAlignment.Left, 0, wrap: true));
		}
		string cancellationLabel = planCard.CancellationLabel;
		if (cancellationLabel != null)
		{
			stackPanel2.Children.Add(SK.Alert(cancellationLabel, SK.Warning));
		}
		stackPanel.Children.Add(stackPanel2);
		if (planCard.Meters.Count > 0)
		{
			Grid grid = new Grid
			{
				ColumnSpacing = 20.0
			};
			for (int i = 0; i < planCard.Meters.Count; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				FrameworkElement frameworkElement = UsageMeter(planCard.Meters[i].Title, planCard.Meters[i].ValueText, planCard.Meters[i].Progress);
				Grid.SetColumn(frameworkElement, i);
				grid.Children.Add(frameworkElement);
			}
			stackPanel.Children.Add(grid);
		}
		WrapPanel wrapPanel = SK.Wrap(10.0);
		foreach (BillingPlanAction action in planCard.Actions)
		{
			BillingActionStyle style = action.Style;
			GelButton gelButton = new GelButton(action.Title, style switch
			{
				BillingActionStyle.Primary => GelAppearance.PrimaryBlue, 
				BillingActionStyle.SecondaryWhite => GelAppearance.SecondaryWhite, 
				_ => GelAppearance.SecondaryOutline, 
			}, GelButton.Sizing.Large, 100.0, null, null, null);
			BillingPlanActionKind kind = action.Kind;
			gelButton.Click += delegate
			{
				RunPlanActionAsync(billing, kind);
			};
			wrapPanel.Children.Add(gelButton);
		}
		if (wrapPanel.Children.Count > 0)
		{
			stackPanel.Children.Add(wrapPanel);
		}
		string text = billingActionError;
		if (text != null)
		{
			stackPanel.Children.Add(SK.Alert(text, SK.Destructive));
		}
		return SK.Group("Plan", stackPanel);
	}

	private async Task RunPlanActionAsync(BillingUi billing, BillingPlanActionKind kind)
	{
		billingActionError = null;
		Log.Info("settings", $"plan action: {kind}");
		string error = await billing.RunPlanActionAsync(kind);
		dq.TryEnqueue(delegate
		{
			billingActionError = error;
			if (page == SettingsWindow.Page.Account)
			{
				Render();
			}
		});
	}

	private static FrameworkElement UsageMeter(string title, string value, double progress)
	{
		Grid grid = new Grid
		{
			Height = 5.0
		};
		grid.Children.Add(new Border
		{
			Background = Ui.Brush(SK.Ink.WithAlpha(0.1))
		}.Capsule());
		Border fill = new Border
		{
			Background = Ui.Brush(SK.Ink.WithAlpha(0.85)),
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 4.0
		}.Capsule();
		grid.Children.Add(fill);
		grid.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			fill.Width = Math.Max(4.0, e.NewSize.Width * Math.Clamp(progress, 0.0, 1.0));
		};
		return new StackPanel
		{
			Spacing = 4.0,
			Children = 
			{
				(UIElement)Ui.Text(title, 10.5, FontWeights.Medium, SK.TextTertiary),
				(UIElement)Ui.Text(value, 12.0, FontWeights.SemiBold, SK.TextPrimary),
				(UIElement)grid
			}
		};
	}

	private FrameworkElement AccountActionsGroup()
	{
		return SK.Group("Account", SK.ActionRow("Log out", s.Auth.User?.Email, delegate
		{
			Log.Info("settings", "log out");
			s.Auth.SignOutAsync();
		}, null, null), SK.ActionRow("Delete account", "Permanently erase your account and all its data.", ShowAccountDeletion, null, null, destructive: true), SK.ActionRow("Quit HeyClicky", null, actions.Quit, null, null, destructive: true));
	}

	private void ShowAccountDeletion()
	{
		Log.Info("settings", "account deletion: opened");
		DeletionStep step = DeletionStep.Reason;
		string picked = null;
		string detail = "";
		string confirmation = "";
		string error = null;
		bool deleting = false;
		GelButton deleteButton = null;
		Grid card = new Grid
		{
			Width = 400.0,
			Padding = new Thickness(24.0),
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(DS.Hex(1710618u)),
			BorderBrush = Ui.Brush(DS.White(0.1)),
			BorderThickness = new Thickness(0.5)
		};
		Build();
		ShowOverlay(card);
		void Build()
		{
			card.Children.Clear();
			StackPanel stackPanel = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			Color color = DS.CursorColor(host.Read((AppSettings x) => x.CursorColorHex));
			Grid grid = new Grid
			{
				Height = 50.0,
				Width = 50.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
			};
			grid.Children.Add(Ui.GlowSpot(color, 0.45, 80.0, 0.3).Pad(-15.0));
			grid.Children.Add(IntroMark.Create(34.0, color).Center());
			stackPanel.Children.Add(grid);
			string text = step switch
			{
				DeletionStep.Reason => "Delete your account forever?", 
				DeletionStep.TypeToConfirm => "One last step.", 
				_ => "Done. Take care.", 
			};
			object text2 = step switch
			{
				DeletionStep.Reason => "This wipes your chats, agents, and everything HeyClicky remembers about you, and cancels any subscription. It can't be undone.\n\nBefore you go: what made you leave?\nYour answer helps us fix it for the next person.", 
				DeletionStep.TypeToConfirm => "Type DELETE in all caps to confirm. Your account is erased the moment you hit the button, and there's no way back.\n\nYour agents' files on this PC go to the Recycle Bin. A few leftover records clear out within 30 days.", 
				_ => "Your account and everything HeyClicky knew about you are gone. Thanks for giving it a try.", 
			};
			stackPanel.Children.Add(Ui.Text(text, 20.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true));
			TextBlock textBlock = Ui.Text((string)text2, 13.0, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
			textBlock.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			textBlock.LineHeight = 19.0;
			stackPanel.Children.Add(textBlock);
			if (step == DeletionStep.Reason)
			{
				WrapPanel wrapPanel = new WrapPanel
				{
					Spacing = 8.0,
					Margin = new Thickness(0.0, 22.0, 0.0, 0.0),
					HorizontalAlignment = HorizontalAlignment.Center
				};
				(string, string)[] deletionReasons = DeletionReasons;
				for (int i = 0; i < deletionReasons.Length; i++)
				{
					(string, string) tuple = deletionReasons[i];
					string name = tuple.Item1;
					string item = tuple.Item2;
					bool isPicked = picked == name;
					Pressable chip = new Pressable
					{
						CornerRadius = new CornerRadius(100.0),
						Background = Ui.Brush(isPicked ? Colors.White : DS.White(0.05)),
						BorderBrush = Ui.Brush(DS.White(isPicked ? 0.0 : 0.1)),
						BorderThickness = new Thickness(0.5)
					};
					chip.Children.Add(Ui.H(7.0, Icons.Glyph(item, 13.0, isPicked ? DS.Black(0.7) : DS.Colors.TextSecondary), Ui.Text(name, 13.0, FontWeights.Medium, isPicked ? DS.Black(0.9) : DS.Colors.TextPrimary)).Pad(13.0, 9.0, 13.0, 9.0));
					chip.HoverChanged += delegate(bool h)
					{
						if (!isPicked)
						{
							chip.Background = Ui.Brush(DS.White(h ? 0.12 : 0.05));
						}
					};
					chip.Click += delegate
					{
						picked = ((picked == name) ? null : name);
						Build();
					};
					wrapPanel.Children.Add(chip);
				}
				stackPanel.Children.Add(wrapPanel);
				(Grid Box, TextBox Input) tuple2 = SK.SearchField("Anything more you want us to know? (optional)", detail ?? "", delegate(string t)
				{
					detail = ((t.Length > 500) ? t.Substring(0, 500) : t);
				}, magnifier: false, 13.0, null);
				Grid item2 = tuple2.Box;
				TextBox item3 = tuple2.Input;
				item3.AcceptsReturn = true;
				item3.TextWrapping = TextWrapping.Wrap;
				item3.MinHeight = 52.0;
				item3.MaxHeight = 90.0;
				item3.CornerRadius = new CornerRadius(12.0);
				item2.MaxWidth = 320.0;
				item2.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
				stackPanel.Children.Add(item2);
				stackPanel.Children.Add(TwoButtons("Never mind", delegate
				{
					HideOverlay();
					Log.Info("settings", "account deletion: cancelled");
				}, "Continue", GelAppearance.PrimaryBlue, delegate
				{
					step = DeletionStep.TypeToConfirm;
					Build();
				}, rightEnabled: true));
			}
			else if (step == DeletionStep.TypeToConfirm)
			{
				var (grid2, input) = SK.SearchField("DELETE", confirmation ?? "", delegate(string t)
				{
					confirmation = t;
					UpdateDeleteButton();
				}, magnifier: false, 15.0, null);
				input.FontFamily = DS.Fonts.Mono;
				input.FontWeight = FontWeights.SemiBold;
				input.TextAlignment = TextAlignment.Center;
				input.CornerRadius = new CornerRadius(12.0);
				grid2.MaxWidth = 200.0;
				grid2.Margin = new Thickness(0.0, 22.0, 0.0, 0.0);
				input.Loaded += delegate
				{
					input.Focus(FocusState.Programmatic);
				};
				stackPanel.Children.Add(grid2);
				FrameworkElement frameworkElement = TwoButtons("Go back", delegate
				{
					error = null;
					confirmation = "";
					step = DeletionStep.Reason;
					Build();
				}, deleting ? "Deleting…" : "Delete forever", GelAppearance.PrimaryBlue, delegate
				{
					PerformDeletion();
				}, confirmation?.Trim() == "DELETE" && !deleting);
				stackPanel.Children.Add(frameworkElement);
				deleteButton = (GelButton)((Grid)frameworkElement).Children[1];
				input.KeyDown += delegate(object _, KeyRoutedEventArgs e)
				{
					if (e.Key == VirtualKey.Enter && confirmation?.Trim() == "DELETE")
					{
						_ = PerformDeletion();
					}
				};
				if (error != null)
				{
					TextBlock textBlock2 = Ui.Text(error, 11.5, FontWeights.SemiBold, DS.Rgb(1.0, 0.62, 0.55), TextAlignment.Center, 0, wrap: true);
					textBlock2.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
					stackPanel.Children.Add(textBlock2);
				}
			}
			card.Children.Add(stackPanel);
			if (step != DeletionStep.Farewell)
			{
				Pressable pressable = new Pressable
				{
					Width = 22.0,
					Height = 22.0,
					CornerRadius = new CornerRadius(11.0),
					Background = Ui.Brush(DS.White(0.08)),
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0.0, -14.0, -14.0, 0.0)
				};
				TextBlock textBlock3 = Icons.Glyph("\ue711", 10.0, DS.White(0.72));
				textBlock3.FontWeight = FontWeights.Bold;
				pressable.Children.Add(textBlock3);
				pressable.Enabled = !deleting;
				pressable.Click += HideOverlay;
				card.Children.Add(pressable);
			}
		}
		async Task PerformDeletion()
		{
			if (deleting || confirmation?.Trim() != "DELETE")
			{
				return;
			}
			deleting = true;
			error = null;
			Build();
			bool flag = string.IsNullOrWhiteSpace(detail);
			string text3 = ((picked == null) ? ((!flag) ? detail.Trim() : null) : ((!flag) ? (picked + ": " + detail.Trim()) : picked));
			string text4 = text3;
			Log.Info("settings", "account deletion: deleting (reason: " + (text4 ?? "none") + ")");
			try
			{
				WorkerClient? obj = host.Worker ?? throw new InvalidOperationException("Account deletion needs the HeyClicky servers.");
				ClickyAnalytics.FlushPendingEvents();
				await obj.DeleteAccountAsync(text4);
				await PurgeLocalAccountDataAsync();
				ClickyAnalytics.TrackAccountDeleted();
				dq.TryEnqueue(delegate
				{
					deleting = false;
					step = DeletionStep.Farewell;
					Build();
				});
				await Task.Delay(2400);
				dq.TryEnqueue(HideOverlay);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				Log.Warn("settings", "account deletion failed: " + ex3.Message);
				dq.TryEnqueue(delegate
				{
					deleting = false;
					string text5;
					if (ex3 is WorkerHttpException { Status: 401 })
					{
						text5 = "Your sign-in expired. Sign in again, then delete your account.";
					}
					else
					{
						bool flag2 = ((ex3 is TaskCanceledException || ex3 is HttpRequestException) ? true : false);
						text5 = (flag2 ? "The connection dropped before we could confirm the deletion finished. Please try again. If this keeps happening, email hi@heyclicky.com." : "Nothing was deleted. Please try again, or email hi@heyclicky.com.");
					}
					error = text5;
					Build();
				});
			}
		}
		void UpdateDeleteButton()
		{
			if ((object)deleteButton != null)
			{
				deleteButton.IsEnabledGel = confirmation?.Trim() == "DELETE" && !deleting;
			}
		}
	}

	private async Task PurgeLocalAccountDataAsync()
	{
		string projectsRoot = host.Agents?.ProjectsRoot;
		string codexHome = host.Agents?.CodexHome;
		try
		{
			AgentService agents = host.Agents;
			if (agents != null)
			{
				agents.ForgetWorkReportsForDeletedAccount(agents.CurrentUserId?.Invoke());
				await agents.SignOutAsync();
				agents.ClickyApps.ForgetAll();
				agents.DeleteAllLocalClickys();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "purge: agents sign-out: " + ex.Message);
		}
		await s.Auth.SignOutAsync();
		NotchChat.History?.Reset();
		NotchChat.HandOffs?.Reset();
		(s.Presenter as NotchPresenter)?.Updates.Reset();
		ProactiveCoordinator.Current?.ForgetForAccountDeletion();
		ClickyRoster.Shared.ForgetAll();
		await Task.Run(delegate
		{
			AgentHarnessRunnerProcess.Stop();
			DictationTranscriptArchive.DeleteAll();
			string root = AppPaths.Root;
			string[] array = new string[3]
			{
				(projectsRoot == null) ? null : System.IO.Path.Combine(projectsRoot, "agents"),
				(projectsRoot == null) ? null : System.IO.Path.Combine(projectsRoot, "jobs"),
				LessonStore.Root
			};
			foreach (string text in array)
			{
				if (text != null && Directory.Exists(text))
				{
					try
					{
						FileSystem.DeleteDirectory(text, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
						Log.Info("settings", "purge: " + text + " → Recycle Bin");
					}
					catch (Exception ex2)
					{
						Log.Warn("settings", "purge: " + text + ": " + ex2.Message);
					}
				}
			}
			array = new string[8]
			{
				System.IO.Path.Combine(root, "agents"),
				codexHome,
				System.IO.Path.Combine(root, "OneBrain"),
				System.IO.Path.Combine(root, "HeyClickyHistory"),
				System.IO.Path.Combine(root, "Ink"),
				System.IO.Path.Combine(root, "helper"),
				System.IO.Path.Combine(root, "ClickyApps"),
				GoodMorningBrief.ResearchFilePath
			};
			foreach (string text2 in array)
			{
				if (text2 != null)
				{
					try
					{
						if (Directory.Exists(text2))
						{
							Directory.Delete(text2, recursive: true);
							goto IL_0163;
						}
						if (File.Exists(text2))
						{
							File.Delete(text2);
							goto IL_0163;
						}
						goto end_IL_013c;
						IL_0163:
						Log.Info("settings", "purge: " + text2 + " erased");
						end_IL_013c:;
					}
					catch (Exception ex3)
					{
						Log.Warn("settings", "purge: " + text2 + ": " + ex3.Message);
					}
				}
			}
		});
		dq.TryEnqueue(delegate
		{
			GoodMorningBrief.Shared?.RefreshStatusFromDisk();
		});
	}

	private static FrameworkElement TwoButtons(string leftTitle, Action left, string rightTitle, GelAppearance rightAppearance, Action right, bool rightEnabled)
	{
		Grid obj = new Grid
		{
			ColumnSpacing = 10.0,
			MaxWidth = 320.0,
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0),
			ColumnDefinitions = 
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			}
		};
		GelButton gelButton = new GelButton(leftTitle, GelAppearance.SecondaryOutline, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		gelButton.Click += left;
		GelButton r = new GelButton(rightTitle, rightAppearance, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		r.IsEnabledGel = rightEnabled;
		r.Click += delegate
		{
			if (r.IsEnabledGel)
			{
				right();
			}
		};
		Grid.SetColumn(r, 1);
		obj.Children.Add(gelButton);
		obj.Children.Add(r);
		return obj;
	}

	private FrameworkElement AgentsPage()
	{
		AppSettings current = host.Current;
		string @default = AgentFolders.Default;
		string agentFolderPath = current.AgentFolderPath;
		string trailingText = ((agentFolderPath != null) ? AgentFolders.Abbreviate(agentFolderPath) : "HeyClicky default");
		List<FrameworkElement> list = new List<FrameworkElement> { SK.ActionRow("Agent folder", "Where agents spawn and put the things they make.", delegate
		{
			PickAgentFolderAsync();
		}, trailingText) };
		if (current.AgentFolderPath != null)
		{
			list.Add(SK.ActionRow("Reset to default folder", AgentFolders.Abbreviate(@default), delegate
			{
				host.Set("agent_folder", "default", delegate(AppSettings x)
				{
					x.AgentFolderPath = null;
				});
			}, null, null));
		}
		return SK.Scaffold("Agents", "Where agents work and how much rope they get.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[4]
		{
			SK.Group("Workspace", "The default is in your local app data, so OneDrive doesn't sync agents' scratch files.", list),
			SK.Group("Autonomy", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
			{
				SK.ToggleRow("Auto-approve extra usage", "Let long tasks keep running without asking each time. Uses more of your plan's agent messages.", current.AgentAutoApproveExtraUsage, delegate(bool v)
				{
					Quiet("auto_approve_extra_usage", v, delegate(AppSettings x)
					{
						x.AgentAutoApproveExtraUsage = v;
					});
				}),
				SK.ToggleRow("Always approve file uploads", "Let jobs add files from your PC to websites without asking each turn. Keys, keychains and browser profiles stay off limits.", current.AgentAlwaysApproveFileUploads, delegate(bool v)
				{
					Quiet("always_approve_file_uploads", v, delegate(AppSettings x)
					{
						x.AgentAlwaysApproveFileUploads = v;
					});
				}),
				null
			})),
			SK.Group("Announcements", "HeyClicky stays quiet while you're on a call or sharing your screen, and in Do Not Disturb your scheduled routines finish quietly. Finished work waits in the chat and the notch shows how many jobs have something new.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				SK.ToggleRow("Speak when a job starts or finishes", "HeyClicky's voice tells you what a job got done.", current.AgentAnnouncementsVoiceEnabled, delegate(bool v)
				{
					Quiet("agent_voice_announcements", v, delegate(AppSettings x)
					{
						x.AgentAnnouncementsVoiceEnabled = v;
					});
				}),
				SK.ToggleRow("Show updates beside the cursor", "A job's reply streams in a bubble next to your cursor when it finishes.", current.AgentCursorTextAnnouncementsEnabled, delegate(bool v)
				{
					Quiet("agent_cursor_text_announcements", v, delegate(AppSettings x)
					{
						x.AgentCursorTextAnnouncementsEnabled = v;
					});
				})
			})),
			SK.Group("What agents can reach", "Windows privacy settings HeyClicky's agents inherit. Tap one to review it in Windows Settings.", PermissionRows())
		}));
	}

	private async Task PickAgentFolderAsync()
	{
		try
		{
			FolderPicker obj = new FolderPicker
			{
				SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
				CommitButtonText = "Use Folder",
				FileTypeFilter = { "*" }
			};
			InitializeWithWindow.Initialize(obj, Native.GetForegroundWindow());
			StorageFolder storageFolder = await obj.PickSingleFolderAsync();
			if ((object)storageFolder != null)
			{
				string path = storageFolder.Path;
				host.Set("agent_folder", path, delegate(AppSettings x)
				{
					x.AgentFolderPath = path;
				});
			}
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "agent folder picker: " + ex.Message);
		}
	}

	private List<FrameworkElement?> PermissionRows()
	{
		List<FrameworkElement> list = new List<FrameworkElement>();
		(string, string, string, string, bool)[] privacyCapabilities = PrivacyCapabilities;
		for (int i = 0; i < privacyCapabilities.Length; i++)
		{
			(string, string, string, string, bool) tuple = privacyCapabilities[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			string uri = tuple.Item4;
			bool item4 = tuple.Item5;
			string text = ConsentStatus(item);
			if (item4 || !(text != "GRANTED"))
			{
				list.Add(PermissionRow(item2, item3, item4 ? "Folders an agent opens or writes to." : null, item4 ? null : text, delegate
				{
					host.OpenUrl(uri);
				}));
			}
		}
		if (list.Count == 0)
		{
			list.Add(SK.Row("Nothing granted yet", "Agents will ask when a task needs access to your data.", null, null));
		}
		return list;
	}

	private static string ConsentStatus(string capability)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\" + capability);
			using RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\" + capability + "\\NonPackaged");
			string text = registryKey?.GetValue("Value") as string;
			string text2 = registryKey2?.GetValue("Value") as string;
			if (text == "Deny" || text2 == "Deny")
			{
				return "DENIED";
			}
			if (text == "Allow" && text2 != "Deny")
			{
				return "GRANTED";
			}
			return "NOT GRANTED";
		}
		catch
		{
			return "UNKNOWN";
		}
	}

	private FrameworkElement PermissionRow(string name, string glyph, string? subtitle, string? status, Action onClick)
	{
		Pressable p = new Pressable();
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(18.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.Children.Add(Icons.Glyph(glyph, 12.0, SK.TextSecondary));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)Ui.Text(name, 13.0, FontWeights.Medium, SK.TextPrimary) }
		};
		if (subtitle != null)
		{
			stackPanel.Children.Add(Ui.Text(subtitle, 11.0, FontWeights.Normal, SK.TextTertiary));
		}
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		if (status != null && status != "UNKNOWN")
		{
			Color color = ((status == "GRANTED") ? SK.Positive : ((status == "DENIED") ? SK.Destructive : SK.TextTertiary));
			TextBlock textBlock = Ui.Text(status, 10.0, FontWeights.Bold, color);
			textBlock.CharacterSpacing = 40;
			Border border = new Border
			{
				CornerRadius = new CornerRadius(100.0),
				Background = Ui.Brush(color.WithAlpha(0.14)),
				Padding = new Thickness(8.0, 3.5, 8.0, 3.5),
				Child = textBlock,
				VerticalAlignment = VerticalAlignment.Center
			}.Capsule();
			Grid.SetColumn(border, 2);
			grid.Children.Add(border);
		}
		p.Children.Add(grid);
		p.HoverChanged += delegate(bool h)
		{
			p.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.05 : 0.0));
		};
		p.Click += onClick;
		return p;
	}

	public void OnBrowserConnectionChanged()
	{
		if (page == SettingsWindow.Page.Integrations && (object)browserCardHost != null)
		{
			FillBrowserCard();
		}
	}

	private FrameworkElement BrowserConnectionSection()
	{
		browserCardHost = new StackPanel();
		FillBrowserCard();
		Task.Run(async delegate
		{
			ClickyBrowserBridge.Shared.RefreshSetupBrowserName();
			await ClickyBrowserBridge.Shared.StartIfEnabledAsync();
		});
		return browserCardHost;
	}

	private void FillBrowserCard()
	{
		using (MessagesPalette.Use(look))
		{
			if ((object)browserCardHost != null)
			{
				browserCardHost.Children.Clear();
				bool flag = integrationSearch.Length == 0 || "chrome browser connection heyclicky".Contains(integrationSearch.Trim(), StringComparison.OrdinalIgnoreCase);
				browserCardHost.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				if (flag)
				{
					browserCardHost.Children.Add(SK.Group("Browser", "Works with Chrome, Edge, Brave, Arc and other Chrome-based browsers. HeyClicky's extension is in the Chrome Web Store.", BrowserConnectionRows(isEmbedded: false)));
				}
			}
		}
	}

	[IteratorStateMachine(typeof(_003CBrowserConnectionRows_003Ed__24))]
	public IEnumerable<FrameworkElement?> BrowserConnectionRows(bool isEmbedded = true)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CBrowserConnectionRows_003Ed__24(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__isEmbedded = isEmbedded
		};
	}

	private Grid BrowserHeader(bool isEmbedded)
	{
		ClickyBrowserBridge shared = ClickyBrowserBridge.Shared;
		ChromeSetupFlow shared2 = ChromeSetupFlow.Shared;
		string browserName = shared2.BrowserName;
		Image image = new Image
		{
			Width = 32.0,
			Height = 32.0,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center
		};
		LoadBrowserIcon(image);
		StackPanel stackPanel = Ui.H(7.0, Ui.Text(browserName, 13.0, FontWeights.SemiBold, SK.TextPrimary));
		FrameworkElement frameworkElement = BrowserStatusBadge(shared, shared2);
		if ((object)frameworkElement != null)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			stackPanel.Children.Add(frameworkElement);
		}
		object obj;
		if (shared.ActiveCount <= 0)
		{
			if (!shared.IsConnected)
			{
				string setupFailureMessage = shared.SetupFailureMessage;
				obj = ((setupFailureMessage != null && !shared2.IsBusy) ? setupFailureMessage : (shared2.IsBusy ? ("Setting it up. It connects by itself once it's added to " + browserName + ".") : (shared.IsAwaitingExtension ? ("Finish in " + browserName + ": add HeyClicky from the Chrome Web Store. It connects by itself.") : (shared2.AddsAgain ? "There's a new version of the extension that updates itself. Swap it in once and you're set." : "Let HeyClicky work in the sites you're already signed into. You can stop it anytime."))));
			}
			else
			{
				obj = "HeyClicky can work in the sites you're signed into. Stop it anytime from the toolbar.";
			}
		}
		else
		{
			obj = "A job is using " + browserName + " right now, in a tab group next to yours.";
		}
		string text = (string)obj;
		StackPanel stackPanel2 = Ui.V(3.0, stackPanel, Ui.Text(text, 11.5, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 0, wrap: true));
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 12.0, 14.0, 12.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel2, 1);
		grid.Children.Add(image);
		grid.Children.Add(stackPanel2);
		FrameworkElement frameworkElement2 = BrowserHeaderAction(shared, shared2, isEmbedded);
		if ((object)frameworkElement2 != null)
		{
			frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
			frameworkElement2.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			Grid.SetColumn(frameworkElement2, 2);
			grid.Children.Add(frameworkElement2);
		}
		return grid;
	}

	private static FrameworkElement? BrowserHeaderAction(ClickyBrowserBridge bridge, ChromeSetupFlow flow, bool isEmbedded)
	{
		if (bridge.ActiveCount > 0)
		{
			GelButton gelButton = new GelButton("Stop", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, "\ue71a", null, null);
			gelButton.Click += delegate
			{
				bridge.StopAccessForUser("settings");
			};
			return gelButton;
		}
		if (!bridge.IsConnected && flow.IsBusy)
		{
			GelButton gelButton2 = new GelButton("Stop", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton2.Click += delegate
			{
				TrackBrowserSettingsAction("settings_setup_stopped", isEmbedded);
				flow.Stop();
			};
			return gelButton2;
		}
		if (bridge.IsConnected)
		{
			return null;
		}
		StackPanel stackPanel = Ui.H(8.0);
		if (flow.CanDoItForThem)
		{
			GelButton gelButton3 = new GelButton("Get new extension", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton3.Click += delegate
			{
				TrackBrowserSettingsAction("settings_setup_pressed", isEmbedded, new JsonObject((JsonNodeOptions?)null) { ["choice"] = "do_it_for_me" });
				flow.Choose(ChromeSetupFlow.Choice.DoItForMe, ChromeSetupFlow.Target.None);
			};
			stackPanel.Children.Add(gelButton3);
		}
		GelButton gelButton4 = new GelButton(flow.CanDoItForThem ? "I'll do it" : (flow.AddsAgain ? "Show me how" : ("Add to " + flow.BrowserName)), flow.CanDoItForThem ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
		gelButton4.Click += delegate
		{
			TrackBrowserSettingsAction("settings_setup_pressed", isEmbedded, new JsonObject((JsonNodeOptions?)null)
			{
				["choice"] = "show_me_how",
				["do_it_for_me_offered"] = flow.CanDoItForThem,
				["adds_again"] = flow.AddsAgain
			});
			flow.Choose(ChromeSetupFlow.Choice.ShowMeHow, ChromeSetupFlow.Target.None);
		};
		stackPanel.Children.Add(gelButton4);
		return stackPanel;
	}

	private static void TrackBrowserSettingsAction(string action, bool isEmbedded, JsonObject? properties = null)
	{
		ClickyBrowserBridge shared = ClickyBrowserBridge.Shared;
		if (properties == null)
		{
			properties = new JsonObject((JsonNodeOptions?)null);
		}
		properties["action"] = action;
		properties["is_connected"] = shared.IsConnected;
		properties["helper_registered"] = shared.IsEnabled;
		properties["is_embedded"] = isEmbedded;
		ClickyAnalytics.TrackProductEvent("browser_connection_action", null, null, properties);
	}

	private static FrameworkElement? BrowserStatusBadge(ClickyBrowserBridge bridge, ChromeSetupFlow flow)
	{
		(string, Color, bool)? tuple = ((bridge.ActiveCount > 0) ? new(string, Color, bool)?(("Working", DS.Hex(3111935u), true)) : (bridge.IsConnected ? new(string, Color, bool)?(("Connected", SK.Positive, false)) : ((bridge.SetupFailureMessage != null && !flow.IsBusy) ? new(string, Color, bool)?(("Setup failed", SK.Destructive, false)) : ((bridge.IsAwaitingExtension || flow.IsBusy) ? new(string, Color, bool)?(("Waiting", SK.Warning, false)) : (((string, Color, bool)?)null)))));
		if (tuple.HasValue)
		{
			(string, Color, bool) valueOrDefault = tuple.GetValueOrDefault();
			Ellipse ellipse = new Ellipse
			{
				Width = 6.0,
				Height = 6.0,
				Fill = Ui.Brush(valueOrDefault.Item2),
				VerticalAlignment = VerticalAlignment.Center
			};
			if (valueOrDefault.Item3 && !MotionPreference.Reduced)
			{
				Anim.LoopScalar(ellipse, "Opacity", 1.4, delegate(double t)
				{
					double num = ((t < 0.7) ? (t / 0.7) : (2.0 - t / 0.7));
					double num2 = (1.0 - Math.Cos(Math.PI * num)) / 2.0;
					return (float)(1.0 - 0.7 * num2);
				});
			}
			TextBlock textBlock = Ui.Text(valueOrDefault.Item1, 10.5, FontWeights.SemiBold, valueOrDefault.Item2);
			Border border = new Border();
			border.Padding = new Thickness(7.0, 2.5, 7.0, 2.5);
			border.CornerRadius = new CornerRadius(100.0);
			border.Background = Ui.Brush(valueOrDefault.Item2.WithAlpha(0.14));
			border.Child = Ui.H(5.0, ellipse, textBlock);
			return border.Capsule();
		}
		return null;
	}

	private FrameworkElement BrowserDisconnectRow(string browserName, bool isEmbedded)
	{
		if (isConfirmingBrowserDisconnect)
		{
			GelButton gelButton = new GelButton("Cancel", GelAppearance.SecondaryWhite, GelButton.Sizing.Compact, 15.0, null, null, null);
			gelButton.Click += delegate
			{
				isConfirmingBrowserDisconnect = false;
				TrackBrowserSettingsAction("settings_disconnect_cancelled", isEmbedded);
				FillBrowserCard();
			};
			GelButton gelButton2 = new GelButton("Disconnect", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 15.0, null, null, null);
			gelButton2.Click += delegate
			{
				isConfirmingBrowserDisconnect = false;
				ClickyBrowserBridge.Shared.Disconnect();
				FillBrowserCard();
			};
			return SK.Row("Disconnect " + browserName + "?", "HeyClicky stops using it. You can connect again anytime.", Ui.H(8.0, gelButton, gelButton2), SK.Destructive);
		}
		return SK.ActionRow("Disconnect " + browserName, null, delegate
		{
			isConfirmingBrowserDisconnect = true;
			TrackBrowserSettingsAction("settings_disconnect_asked", isEmbedded);
			FillBrowserCard();
		}, null, null, destructive: true);
	}

	private static void LoadBrowserIcon(Image image)
	{
		string name = ClickyBrowserBridge.Shared.SetupBrowserName;
		if (browserIcons.TryGetValue(name, out ImageSource value))
		{
			image.Source = value;
			return;
		}
		string text = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
		if (File.Exists(text))
		{
			image.Source = new BitmapImage(new Uri(text));
		}
		string executable;
		if (browserIconsLookedUp.Add(name))
		{
			executable = ChromiumBrowsers.ExecutablePathFor(name);
			if (executable != null && executable.Length > 0)
			{
				LoadAsync();
			}
		}
		async Task LoadAsync()
		{
			_ = 2;
			try
			{
				using StorageItemThumbnail thumb = await (await StorageFile.GetFileFromPathAsync(executable)).GetThumbnailAsync(ThumbnailMode.SingleItem, 64u, ThumbnailOptions.UseCurrentScale);
				BitmapImage bitmap = new BitmapImage();
				await bitmap.SetSourceAsync(thumb);
				browserIcons[name] = bitmap;
				image.Source = bitmap;
			}
			catch (Exception ex)
			{
				Log.Warn("settings", "browser icon: " + ex.Message);
			}
		}
	}

	private static List<(string Key, string Value)> KeyValueLines(string text, char[] separators)
	{
		List<(string, string)> list = new List<(string, string)>();
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length != 0 && !text2.StartsWith('#'))
			{
				int num = text2.IndexOfAny(separators);
				if (num > 0)
				{
					string item = text2.Substring(0, num).Trim();
					string text3 = text2;
					int num2 = num + 1;
					list.Add((item, text3.Substring(num2, text3.Length - num2).Trim()));
				}
			}
		}
		return list;
	}

	internal static List<string> ParseArguments(string line)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		char? c = null;
		foreach (char c2 in line)
		{
			if (flag2)
			{
				stringBuilder.Append(c2);
				flag = true;
				flag2 = false;
			}
			else if (c2 == '\\' && c != '\'')
			{
				flag2 = true;
			}
			else if (c.HasValue)
			{
				char valueOrDefault = c.GetValueOrDefault();
				if (c2 == valueOrDefault)
				{
					c = null;
				}
				else
				{
					stringBuilder.Append(c2);
				}
			}
			else if ((c2 == '"' || c2 == '\'') ? true : false)
			{
				c = c2;
				flag = true;
			}
			else if (char.IsWhiteSpace(c2))
			{
				if (flag)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
					flag = false;
				}
			}
			else
			{
				stringBuilder.Append(c2);
				flag = true;
			}
		}
		if (flag)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	internal static string JoinArguments(IEnumerable<string> args)
	{
		return string.Join(' ', args.Select((string a) => (a.Length != 0 && !a.Any(delegate(char c)
		{
			bool flag = char.IsWhiteSpace(c);
			if (!flag)
			{
				bool flag2 = ((c == '"' || c == '\'' || c == '\\') ? true : false);
				flag = flag2;
			}
			return flag;
		})) ? a : ("\"" + a.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")));
	}

	private void ParkConnectorDraft()
	{
		if (connectorDraft != null)
		{
			parkedConnectorDraft = connectorDraft;
			parkedConnectorDraftAt = DateTime.UtcNow;
			connectorDraft = null;
			Log.Info("settings", "custom connector draft parked");
		}
	}

	private void RestoreParkedConnectorDraftIfFresh()
	{
		if (this.connectorDraft != null)
		{
			return;
		}
		ConnectorDraft connectorDraft = parkedConnectorDraft;
		if (connectorDraft == null)
		{
			return;
		}
		if (!(DateTime.UtcNow - parkedConnectorDraftAt > ParkedConnectorDraftRetention) && host.IsSignedIn)
		{
			string id = connectorDraft.EditingId;
			if (id == null || !CustomConnectorStore.Load().All((CustomConnector c) => c.Id != id))
			{
				this.connectorDraft = connectorDraft;
				parkedConnectorDraft = null;
				Log.Info("settings", "custom connector draft restored");
				return;
			}
		}
		ClearParkedConnectorDraft();
	}

	public static void ClearParkedConnectorDraft()
	{
		parkedConnectorDraft = null;
		parkedConnectorDraftAt = default(DateTime);
	}

	private void HookConnectorEvents()
	{
		if (connectorEventsHooked)
		{
			return;
		}
		connectorEventsHooked = true;
		ConnectorAuth.Changed += delegate
		{
			dq.TryEnqueue(delegate
			{
				if (page == SettingsWindow.Page.Integrations && isShown)
				{
					FillIntegrationList();
				}
			});
		};
		ObsidianVault.Changed += delegate
		{
			dq.TryEnqueue(delegate
			{
				if (page == SettingsWindow.Page.Integrations && isShown)
				{
					FillIntegrationList();
				}
			});
		};
	}

	public void DemoOpenConnectorEditor()
	{
		if (connectorDraft == null)
		{
			OpenConnectorEditor(null);
		}
	}

	private void OpenConnectorEditor(CustomConnector? existing)
	{
		if ((object)existing == null)
		{
			ConnectorDraft connectorDraft = this.connectorDraft;
			if (connectorDraft != null && connectorDraft.EditingId == null)
			{
				this.connectorDraft = null;
				Render();
				return;
			}
		}
		this.connectorDraft = new ConnectorDraft();
		if ((object)existing != null)
		{
			CustomConnectorSecrets secrets = CustomConnectorStore.ReadSecrets(existing.Id);
			this.connectorDraft = new ConnectorDraft
			{
				EditingId = existing.Id,
				Existing = existing,
				Name = existing.Name,
				Remote = existing.IsRemote,
				Url = (existing.Url ?? ""),
				Command = (existing.Command ?? ""),
				Arguments = JoinArguments(existing.Arguments),
				Auth = (existing.UsesPlainBearerToken ? "apiKey" : "browser"),
				ApiKey = (existing.UsesPlainBearerToken ? (secrets.BearerToken ?? "") : ""),
				OAuthClientId = existing.TrimmedOAuthClientId,
				Headers = string.Join("\n", existing.HeaderNames.Select((string n) => n + ": " + secrets.Headers.GetValueOrDefault(n, ""))),
				Environment = string.Join("\n", existing.EnvironmentNames.Select((string n) => n + "=" + secrets.Environment.GetValueOrDefault(n, "")))
			};
			this.connectorDraft.ShowAdvanced = this.connectorDraft.UsesAdvanced;
		}
		Log.Info("settings", ((object)existing == null) ? "custom connector: add" : ("custom connector: edit " + existing.Name));
		Render();
	}

	private FrameworkElement? ConnectorEditor()
	{
		ConnectorDraft d = connectorDraft;
		if (d == null)
		{
			return null;
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Padding = new Thickness(14.0)
		};
		string label = ((d.EditingId == null) ? "New connector" : ("Edit " + ((d.Name.Trim().Length == 0) ? "connector" : d.Name.Trim())));
		stackPanel.Children.Add(Field("Name", "e.g. Meta Ads", d.Name, delegate(string t)
		{
			d.Name = t;
		}));
		if (d.Remote)
		{
			stackPanel.Children.Add(Field("Server URL", "e.g. https://mcp.example.com/mcp", d.Url, delegate(string t)
			{
				d.Url = t;
			}));
			StackPanel control = Ui.H(6.0, SK.Chip("Sign in with browser", d.Auth == "browser", gelSelection: false, delegate
			{
				d.Auth = "browser";
				Render();
			}), SK.Chip("API key", d.Auth == "apiKey", gelSelection: false, delegate
			{
				d.Auth = "apiKey";
				Render();
			}), SK.Chip("None", d.Auth == "none", gelSelection: false, delegate
			{
				d.Auth = "none";
				Render();
			}));
			stackPanel.Children.Add(Labeled("Authentication", control, (d.Auth == "browser") ? "You'll sign in through your browser after adding it." : null));
			if (d.Auth == "apiKey")
			{
				stackPanel.Children.Add(Field("API key", "Paste your key", d.ApiKey, delegate(string t)
				{
					d.ApiKey = t;
				}, secret: true));
			}
		}
		else
		{
			stackPanel.Children.Add(Field("Command", "e.g. npx", d.Command, delegate(string t)
			{
				d.Command = t;
			}));
			stackPanel.Children.Add(Field("Arguments", "e.g. -y some-mcp-server --flag value", d.Arguments, delegate(string t)
			{
				d.Arguments = t;
			}, secret: false, multiline: false, "Everything after the command, quoted like a shell."));
		}
		Pressable pressable = new Pressable
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
		TextBlock textBlock = Icons.Glyph("\ue76c", 9.0, SK.TextSecondary);
		textBlock.FontWeight = FontWeights.Bold;
		textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
		textBlock.RenderTransform = new RotateTransform
		{
			Angle = (d.ShowAdvanced ? 90 : 0)
		};
		pressable.Children.Add(Ui.H(6.0, textBlock, Ui.Text("Advanced", 12.0, FontWeights.SemiBold, SK.TextSecondary)));
		pressable.Click += delegate
		{
			d.ShowAdvanced = !d.ShowAdvanced;
			Render();
		};
		stackPanel.Children.Add(pressable);
		if (d.ShowAdvanced)
		{
			stackPanel.Children.Add(Labeled("Type", Ui.H(6.0, SK.Chip("Remote URL", d.Remote, gelSelection: false, delegate
			{
				d.Remote = true;
				Render();
			}), SK.Chip("Command on this PC", !d.Remote, gelSelection: false, delegate
			{
				d.Remote = false;
				Render();
			})), null));
			if (d.Remote)
			{
				stackPanel.Children.Add(Field("OAuth client ID", "e.g. 123456789012345", d.OAuthClientId, delegate(string t)
				{
					d.OAuthClientId = t;
				}, secret: false, multiline: false, "Only for servers that make you register an app with them. For Meta Ads, paste your Meta App ID and add this redirect URL to the app's Facebook Login settings: " + McpOAuth.RedirectUri()));
				stackPanel.Children.Add(Field("Extra headers", "e.g. X-Api-Key: …", d.Headers, delegate(string t)
				{
					d.Headers = t;
				}, secret: false, multiline: true, "One per line, like X-Api-Key: value."));
			}
			else
			{
				stackPanel.Children.Add(Field("Environment variables", "e.g. API_TOKEN=…", d.Environment, delegate(string t)
				{
					d.Environment = t;
				}, secret: false, multiline: true, "One per line, like API_TOKEN=value. Kept encrypted for your Windows account."));
			}
		}
		if (d.AttemptedSave)
		{
			string text = d.ValidationProblem ?? d.Problem;
			if (text != null)
			{
				stackPanel.Children.Add(SK.Alert(text, SK.Destructive));
				goto IL_0541;
			}
		}
		string advisoryNote = d.AdvisoryNote;
		if (advisoryNote != null)
		{
			stackPanel.Children.Add(SK.Alert(advisoryNote, SK.Warning));
		}
		goto IL_0541;
		IL_0541:
		GelButton gelButton = new GelButton("Cancel", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
		gelButton.Click += delegate
		{
			connectorDraft = null;
			ClearParkedConnectorDraft();
			Render();
		};
		GelButton gelButton2 = new GelButton((d.EditingId == null) ? "Add connector" : "Save changes", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
		gelButton2.Click += SaveConnectorDraft;
		StackPanel stackPanel2 = Ui.H(8.0, gelButton, gelButton2);
		stackPanel2.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel.Children.Add(stackPanel2);
		return SK.Group(label, stackPanel);
	}

	private static FrameworkElement Labeled(string label, FrameworkElement control, string? detail)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0
		};
		stackPanel.Children.Add(Ui.Text(label, 12.0, FontWeights.SemiBold, SK.TextPrimary));
		stackPanel.Children.Add(control);
		if (detail != null)
		{
			stackPanel.Children.Add(Ui.Text(detail, 10.5, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 0, wrap: true));
		}
		return stackPanel;
	}

	private static FrameworkElement Field(string label, string placeholder, string value, Action<string> changed, bool secret = false, bool multiline = false, string? detail = null)
	{
		FrameworkElement control;
		if (secret)
		{
			PasswordBox box = new PasswordBox
			{
				Password = value,
				PlaceholderText = placeholder,
				FontSize = 12.0,
				CornerRadius = new CornerRadius(9.0),
				BorderThickness = new Thickness(0.0),
				MinHeight = 0.0,
				Padding = new Thickness(11.0, 6.0, 9.0, 6.0)
			};
			ResourceDictionary resources = box.Resources;
			ResourceDictionary resources2 = box.Resources;
			object obj = (box.Resources["TextControlBackgroundFocused"] = Ui.Brush(SK.Ink.WithAlpha(0.07)));
			object value2 = (resources2["TextControlBackgroundPointerOver"] = obj);
			resources["TextControlBackground"] = value2;
			box.PasswordChanged += delegate
			{
				changed(box.Password);
			};
			control = box;
		}
		else
		{
			(Grid Box, TextBox Input) tuple = SK.SearchField(placeholder, value, changed, magnifier: false, 12.5, SK.TextTertiary.WithAlpha(0.7));
			Grid item = tuple.Box;
			TextBox item2 = tuple.Input;
			item2.FontFamily = DS.Fonts.Mono;
			item2.Padding = new Thickness(11.0, 7.0, 11.0, 7.0);
			if (multiline)
			{
				item2.AcceptsReturn = true;
				item2.TextWrapping = TextWrapping.Wrap;
				item2.MinHeight = 48.0;
			}
			control = item;
		}
		return Labeled(label, control, detail);
	}

	private void SaveConnectorDraft()
	{
		ConnectorDraft connectorDraft = this.connectorDraft;
		if (connectorDraft == null)
		{
			return;
		}
		connectorDraft.AttemptedSave = true;
		connectorDraft.Problem = null;
		string validationProblem = connectorDraft.ValidationProblem;
		if (validationProblem != null)
		{
			Log.Info("settings", "custom connector not saved: " + validationProblem);
			Render();
			return;
		}
		CustomConnectorSecrets customConnectorSecrets = new CustomConnectorSecrets();
		if (connectorDraft.Remote)
		{
			if (connectorDraft.Auth == "apiKey")
			{
				customConnectorSecrets.BearerToken = connectorDraft.ApiKey.Trim();
			}
			else
			{
				CustomConnector existing = connectorDraft.Existing;
				if ((object)existing != null && existing.UsesBrowserSignIn && existing.HasApiKey && connectorDraft.Auth == "browser")
				{
					customConnectorSecrets.BearerToken = CustomConnectorStore.ReadSecrets(existing.Id).BearerToken;
				}
			}
			foreach (var (key, text) in connectorDraft.ParsedHeaders)
			{
				if (text.Length > 0)
				{
					customConnectorSecrets.Headers[key] = text;
				}
			}
		}
		else
		{
			foreach (var (key2, text2) in connectorDraft.ParsedEnvironment)
			{
				if (text2.Length > 0)
				{
					customConnectorSecrets.Environment[key2] = text2;
				}
			}
		}
		string text3 = connectorDraft.OAuthClientId.Trim();
		CustomConnector obj = new CustomConnector(connectorDraft.EditingId ?? Guid.NewGuid().ToString("N").Substring(0, 12), connectorDraft.Name.Trim(), connectorDraft.Remote ? "remote" : "stdio", connectorDraft.Remote ? connectorDraft.Url.Trim() : null, connectorDraft.Remote ? null : connectorDraft.Command.Trim(), connectorDraft.Remote ? ((IReadOnlyList<string>)new List<string>()) : ((IReadOnlyList<string>)ParseArguments(connectorDraft.Arguments)), Array.Empty<string>(), HasApiKey: false)
		{
			OAuthClientId = ((connectorDraft.Remote && text3.Length > 0) ? text3 : null),
			DerivedCapabilityDescription = connectorDraft.Existing?.DerivedCapabilityDescription
		};
		CustomConnector? existing2 = connectorDraft.Existing;
		obj.TokenFromSignIn = (object)existing2 != null && existing2.TokenFromSignIn && connectorDraft.Auth == "browser";
		CustomConnector customConnector = obj;
		bool isNew;
		string text4 = CustomConnectorStore.Save(customConnector, customConnectorSecrets, out isNew);
		if (text4 != null)
		{
			Log.Info("settings", "custom connector not saved: " + text4);
			connectorDraft.Problem = text4;
			Render();
			return;
		}
		Log.Info("settings", $"custom connector {(isNew ? "added" : "edited")}: {customConnector.Name} (auth {connectorDraft.Auth}{((connectorDraft.Auth == "apiKey") ? $", {connectorDraft.ApiKey.Trim().Length}-char key" : "")})");
		ClickyAnalytics.TrackProductEvent("custom_connector_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = (isNew ? "add" : "edit"),
			["connector_id"] = customConnector.Id,
			["transport"] = (connectorDraft.Remote ? "remote" : "local")
		});
		if (isNew)
		{
			ClickyAnalytics.TrackIntegrationConnected(customConnector.IntegrationId, customConnector.Name, new global::_003C_003Ez__ReadOnlySingleElementList<string>(customConnector.ServerKey), "agent_space_settings", "connected", 1, 1);
		}
		this.connectorDraft = null;
		ClearParkedConnectorDraft();
		ConnectorAuth.AfterSave(customConnector.Id);
		Render(resetScroll: true);
	}

	private List<FrameworkElement> CustomConnectorRows()
	{
		List<FrameworkElement> list = new List<FrameworkElement>();
		foreach (CustomConnector item in from c in CustomConnectorStore.Load()
			orderby c.CreatedAt
			select c)
		{
			list.Add(CustomConnectorRow(item));
		}
		return list;
	}

	private static ConnectorSignInState EffectiveState(CustomConnector c)
	{
		ConnectorSignInState connectorSignInState = ConnectorAuth.StateFor(c.Id);
		if (!c.UsesPlainBearerToken)
		{
			return connectorSignInState;
		}
		ConnectorSignInKind kind = connectorSignInState.Kind;
		if ((kind != ConnectorSignInKind.Checking && kind != ConnectorSignInKind.NotNeeded && (uint)(kind - 7) > 1u) || 1 == 0)
		{
			return new ConnectorSignInState(ConnectorSignInKind.NotNeeded);
		}
		return connectorSignInState;
	}

	private static (string Text, Color Color) ConnectorStatus(CustomConnector c, ConnectorSignInState s)
	{
		switch (s.Kind)
		{
		case ConnectorSignInKind.NeedsSignIn:
			return (Text: "Needs sign-in", Color: SK.Warning);
		case ConnectorSignInKind.Checking:
			return (Text: "Checking", Color: SK.Warning);
		case ConnectorSignInKind.AwaitingBrowser:
			return (Text: "Signing in", Color: SK.Warning);
		case ConnectorSignInKind.NotNeeded:
		case ConnectorSignInKind.SignedIn:
			return (Text: "Connected", Color: SK.Positive);
		case ConnectorSignInKind.Failed:
			return (Text: c.UsesPlainBearerToken ? "Check failed" : "Sign-in failed", Color: SK.Destructive);
		case ConnectorSignInKind.SignInUnavailable:
			return (Text: "Needs a token", Color: SK.Warning);
		case ConnectorSignInKind.TokenRejected:
			return (Text: "Token rejected", Color: SK.Destructive);
		default:
			if (c.UsesBrowserSignIn && c.HasApiKey)
			{
				return (Text: "Connected", Color: SK.Positive);
			}
			if (c.TrimmedOAuthClientId.Length > 0 || c.BrowserSignInSupport == "supported")
			{
				return (Text: "Needs sign-in", Color: SK.Warning);
			}
			if (c.BrowserSignInSupport == "unsupported")
			{
				return (Text: "Needs a token", Color: SK.Warning);
			}
			return (Text: "Connected", Color: SK.Positive);
		}
	}

	private FrameworkElement CustomConnectorRow(CustomConnector c)
	{
		ConnectorSignInState connectorSignInState = EffectiveState(c);
		(string Text, Color Color) tuple = ConnectorStatus(c, connectorSignInState);
		string item = tuple.Text;
		Color item2 = tuple.Color;
		FrameworkElement frameworkElement = ConnectorGlyph(c, 26.0);
		StackPanel item3 = Ui.H(7.0, Ui.Text(c.Name, 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 1), StatusChip(item, item2));
		ConnectorSignInKind kind = connectorSignInState.Kind;
		bool flag = ((kind == ConnectorSignInKind.SignInUnavailable || (uint)(kind - 7) <= 1u) ? true : false);
		string text = (flag ? connectorSignInState.Text : null);
		string effectiveCapabilityDescription = c.EffectiveCapabilityDescription;
		string text2 = ((connectorSignInState.Kind == ConnectorSignInKind.AwaitingBrowser) ? "Finish connecting in your browser." : ((effectiveCapabilityDescription.Length == 0) ? c.LocationSummary : (effectiveCapabilityDescription + " · " + c.LocationSummary)));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)item3,
				(UIElement)Ui.Text(text2, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 2)
			}
		};
		if (text != null)
		{
			stackPanel.Children.Add(Ui.Text(text, 10.5, FontWeights.Medium, SK.Destructive, TextAlignment.Left, 0, wrap: true));
		}
		FrameworkElement frameworkElement2;
		if (confirmRemoveConnector == c.Id)
		{
			GelButton gelButton = new GelButton("Remove", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton.Click += delegate
			{
				confirmRemoveConnector = null;
				if (connectorDraft?.EditingId == c.Id)
				{
					connectorDraft = null;
				}
				ClickyAnalytics.TrackProductEvent("custom_connector_action", null, null, new JsonObject((JsonNodeOptions?)null)
				{
					["action"] = "remove",
					["connector_id"] = c.Id
				});
				ClickyAnalytics.TrackIntegrationDisconnected(c.IntegrationId, c.Name, new global::_003C_003Ez__ReadOnlySingleElementList<string>(c.ServerKey), "agent_space_settings", ConnectorAuth.NeedsReconnect(c.Id) ? "needs_reconnect" : "connected", 1, 1, 1);
				CustomConnectorStore.Remove(c.Id);
				ConnectorAuth.Removed(c.Id);
				Render();
			};
			GelButton gelButton2 = new GelButton("Keep", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton2.Click += delegate
			{
				confirmRemoveConnector = null;
				Render();
			};
			frameworkElement2 = Ui.H(6.0, gelButton, gelButton2);
		}
		else
		{
			StackPanel stackPanel2 = Ui.H(8.0);
			FrameworkElement frameworkElement3 = SignInControl(c, connectorSignInState);
			if ((object)frameworkElement3 != null)
			{
				stackPanel2.Children.Add(frameworkElement3);
			}
			GelButton gelButton3 = new GelButton("Edit", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton3.Click += delegate
			{
				OpenConnectorEditor(c);
			};
			GelButton gelButton4 = new GelButton("Remove", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton4.Click += delegate
			{
				confirmRemoveConnector = c.Id;
				Render();
			};
			stackPanel2.Children.Add(gelButton3);
			stackPanel2.Children.Add(gelButton4);
			frameworkElement2 = stackPanel2;
		}
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			ColumnSpacing = 12.0,
			Tag = "connector:" + c.Id
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(26.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(frameworkElement);
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetColumn(frameworkElement2, 2);
		grid.Children.Add(frameworkElement2);
		Grid hover = new Grid
		{
			Background = Ui.Transparent,
			Children = { (UIElement)grid }
		};
		hover.PointerEntered += delegate
		{
			hover.Background = Ui.Brush(look.Ink.WithAlpha(0.04));
		};
		hover.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				hover.Background = Ui.Transparent;
			}
		};
		return hover;
	}

	private FrameworkElement? SignInControl(CustomConnector c, ConnectorSignInState s)
	{
		bool usesBrowserSignIn = c.UsesBrowserSignIn;
		bool flag = c.UsesPlainBearerToken;
		if (flag)
		{
			ConnectorSignInKind kind = s.Kind;
			bool flag2 = (uint)(kind - 7) <= 1u;
			flag = flag2;
		}
		bool flag3 = flag;
		if (!usesBrowserSignIn && !flag3)
		{
			return null;
		}
		switch (s.Kind)
		{
		case ConnectorSignInKind.Checking:
			return Ui.Text(c.UsesPlainBearerToken ? "Checking…" : "Checking sign-in…", 11.0, FontWeights.Medium, SK.TextTertiary);
		case ConnectorSignInKind.AwaitingBrowser:
			return Ui.Text("Finish in your browser…", 11.0, FontWeights.Medium, SK.TextTertiary);
		case ConnectorSignInKind.SignedIn:
			return Button("Sign in again", SignIn);
		case ConnectorSignInKind.NotNeeded:
			return null;
		case ConnectorSignInKind.SignInUnavailable:
			return Button("Add token", Edit);
		case ConnectorSignInKind.TokenRejected:
			return Button("Edit token", Edit);
		case ConnectorSignInKind.Failed:
			return Button("Try again", c.UsesPlainBearerToken ? ((Action)delegate
			{
				ConnectorAuth.VerifyToken(c.Id, userInitiated: true);
			}) : new Action(SignIn));
		case ConnectorSignInKind.NeedsSignIn:
			return Button("Sign in", SignIn);
		default:
			if (c.HasApiKey)
			{
				return Button("Sign in again", SignIn);
			}
			if (c.TrimmedOAuthClientId.Length > 0 || c.BrowserSignInSupport == "supported")
			{
				return Button("Sign in", SignIn);
			}
			if (c.BrowserSignInSupport == "unsupported")
			{
				return Button("Add token", Edit);
			}
			return null;
		}
		static GelButton Button(string title, Action click)
		{
			GelButton gelButton = new GelButton(title, GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton.Click += click;
			return gelButton;
		}
		void Edit()
		{
			OpenConnectorEditor(c);
		}
		void SignIn()
		{
			ConnectorAuth.SignIn(c.Id);
		}
	}

	private FrameworkElement ConnectorGlyph(CustomConnector c, double size)
	{
		Border border = new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(size * 0.27),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.1)),
			Child = Icons.Glyph(c.IsRemote ? "\ue774" : "\ue756", size * 0.46, SK.TextSecondary)
		};
		string text = c.LogoUrl(host.WorkerBaseUrl);
		if (text == null)
		{
			return border;
		}
		return IntegrationLogos.Remote(text, size, border);
	}

	private FrameworkElement CursorPage()
	{
		AppSettings current = host.Current;
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0
		};
		(string, Color)[] array = new(string, Color)[4]
		{
			("Red", DS.Colors.CursorRed),
			("Blue", DS.Colors.CursorBlue),
			("Yellow", DS.Colors.CursorYellow),
			("Green", DS.Colors.CursorGreen)
		};
		for (int i = 0; i < array.Length; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			(string, Color) tuple = array[i];
			string name = tuple.Item1;
			Color item = tuple.Item2;
			bool selected = string.Equals(DS.ToHex(item), DS.ToHex(DS.CursorColor(current.CursorColorHex)), StringComparison.OrdinalIgnoreCase);
			Grid grid2 = new Grid
			{
				Width = 30.0,
				Height = 30.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			grid2.Children.Add(Ui.GlowSpot(item, 0.45, 66.0, 0.3).Pad(-18.0));
			grid2.Children.Add(IntroMark.Cursor(30.0, item, -20.0));
			string hex = DS.ToHex(item);
			Pressable tile = SK.Tile(grid2, selected, 20.0, delegate
			{
				host.Set("cursor_color", name.ToLowerInvariant(), delegate(AppSettings x)
				{
					x.CursorColorHex = hex;
				});
				s.Cursor?.SetColor(hex);
			});
			tile.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				if (double.IsNaN(tile.Height) || Math.Abs(tile.Height - e.NewSize.Width) > 0.5)
				{
					tile.Height = e.NewSize.Width;
				}
			};
			ToolTipService.SetToolTip(tile, name + " cursor. Sets the cursor and app accent color");
			Grid.SetColumn(tile, i);
			grid.Children.Add(tile);
		}
		Grid grid3 = new Grid();
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MaxWidth = 520.0
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(0.0)
		});
		grid3.Children.Add(grid);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Children = 
			{
				(UIElement)SK.MicroLabel("Color", 10.0, FontWeights.SemiBold, 110),
				(UIElement)grid3
			}
		};
		return SK.Scaffold("Cursor", "The HeyClicky cursor that lives on your screen.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
		{
			stackPanel,
			SK.Group("Visibility", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
			{
				SK.ToggleRow("Hide floating cursor", "Hides the cursor and stops it from following you.", current.CursorDocked, delegate(bool v)
				{
					quietWrites++;
					try
					{
						Log.Info("settings", $"dock_cursor = {v}");
						host.App?.SetCursorDocked(v);
						SettingsHost.TrackSettingChanged("dock_cursor", v);
					}
					finally
					{
						quietWrites--;
					}
					Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
					dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1.2);
					dispatcherQueueTimer.IsRepeating = false;
					dispatcherQueueTimer.Tick += delegate
					{
						if (page == SettingsWindow.Page.Cursor)
						{
							RenderSoon();
						}
					};
					dispatcherQueueTimer.Start();
				}),
				SK.ToggleRow("Screen highlight", "Draw on screen while holding push-to-talk.", current.ScreenHighlightEnabled, delegate(bool v)
				{
					Quiet("screen_highlight", v, delegate(AppSettings x)
					{
						x.ScreenHighlightEnabled = v;
					});
				}),
				SK.ToggleRow("Shake to detach", "Shake the mouse to leave HeyClicky where it is; click it to talk.", current.ShakeToDetachEnabled, delegate(bool v)
				{
					Quiet("shake_to_detach", v, delegate(AppSettings x)
					{
						x.ShakeToDetachEnabled = v;
					});
				})
			}))
		}));
	}

	private FrameworkElement FreshSessionRow()
	{
		Pressable row = null;
		row = SK.ActionRow("Start a fresh session", "Drops this session's earlier turns, so your next turn starts from turn 1 with an empty conversation. The session that ends still gets its note and memory written.", delegate
		{
			host.App?.Real?.Companion.StartFreshSession();
			oneBrainFreshSessionStartedAt = DateTime.Now;
			Log.Info("settings", "one brain: fresh session");
			if ((object)row != null)
			{
				TextBlock textBlock = TrailingTextOf(row);
				if ((object)textBlock != null)
				{
					textBlock.Text = Trailing();
				}
			}
		}, Trailing(), null);
		return row;
		static string Trailing()
		{
			DateTime? dateTime = oneBrainFreshSessionStartedAt;
			if (!dateTime.HasValue)
			{
				return "";
			}
			return "Started " + dateTime.GetValueOrDefault().ToString("t");
		}
	}

	private static TextBlock? TrailingTextOf(DependencyObject element)
	{
		if (element is TextBlock { MaxWidth: 220.0 } textBlock)
		{
			return textBlock;
		}
		IEnumerable<UIElement> enumerable = ((element is Panel panel) ? panel.Children : ((!(element is Border { Child: { } child })) ? ((IEnumerable<UIElement>)Array.Empty<UIElement>()) : ((IEnumerable<UIElement>)new global::_003C_003Ez__ReadOnlySingleElementList<UIElement>(child))));
		foreach (UIElement item in enumerable)
		{
			TextBlock textBlock2 = TrailingTextOf(item);
			if ((object)textBlock2 != null)
			{
				return textBlock2;
			}
		}
		return null;
	}

	private FrameworkElement NewAgentFeaturesGroup()
	{
		List<FrameworkElement> list = BrowserConnectionRows().ToList();
		if (host.ShowsDeveloperSection)
		{
			list.Add(SK.ActionRow("Preview HeyClicky drive", "The card, the sounds, the pill and the glow, with nothing clicked.", delegate
			{
				host.App?.PreviewClickyDrive();
			}, null, "\ue768"));
		}
		return SK.Group("Chrome and HeyClicky drive", null, list);
	}

	private FrameworkElement DeveloperPage()
	{
		AppSettings c = host.Current;
		Pressable trailing = SK.MenuPicker(BrainModels.Select(((string Id, string Name) m) => m.Name).ToList(), Math.Max(0, Array.FindIndex(BrainModels, ((string Id, string Name) m) => m.Id == (c.OneBrainModel ?? "gpt-6.1-sol-fast-medium"))), delegate(int index)
		{
			string id = BrainModels[index].Id;
			Quiet("one_brain_model", id, delegate(AppSettings x)
			{
				x.OneBrainModel = ((id == "gpt-6.1-sol-fast-medium") ? null : id);
			});
		});
		IReadOnlyList<AgentHarnessModel> harnessModels = AgentHarnessNames.AllModels;
		Pressable trailing2 = SK.MenuPicker(harnessModels.Select((AgentHarnessModel model) => model.DisplayName()).ToList(), harnessModels.ToList().IndexOf(AgentHarnessPreferences.Model), delegate(int index)
		{
			AgentDefaults.Set("ClickyAgentHarnessModel", harnessModels[index].RawValue());
		});
		string text = "Live (use Worker)";
		string[] names = PlanFixtures.Names;
		int num = 0;
		string[] array = new string[1 + names.Length];
		array[num] = text;
		num++;
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(names);
		readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		global::_003C_003Ez__ReadOnlyArray<string> options = new global::_003C_003Ez__ReadOnlyArray<string>(array);
		string current = PlanFixtures.Current;
		int selected = ((current != null) ? (Array.IndexOf(PlanFixtures.Names, current) + 1) : 0);
		Action<int> picked = delegate(int index)
		{
			PlanFixtures.Apply(host.Billing, (index <= 0) ? null : PlanFixtures.Names[index - 1]);
		};
		text = "Live";
		array = PlanFixtures.Names;
		num = 0;
		names = new string[1 + array.Length];
		names[num] = text;
		num++;
		readOnlySpan = new ReadOnlySpan<string>(array);
		readOnlySpan.CopyTo(new Span<string>(names).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		Pressable trailing3 = SK.MenuPicker(options, selected, picked, new global::_003C_003Ez__ReadOnlyArray<string>(names));
		List<FrameworkElement> list = new List<FrameworkElement>();
		list.Add(NewAgentFeaturesGroup());
		list.Add(SK.Group("Drawing on screen", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
		{
			SK.ToggleRow("Drawing sounds", "Every sound a drawing makes: a photo or card landing, a button appearing, the click target, typing, the model's sfx() accents, and the sweep when it's cleared. Off silences them all, saved drawings too. The same setting as Voice → Drawing sounds.", c.DrawingSoundsEnabled, delegate(bool v)
			{
				Quiet("screen_drawing_sounds_developer", v, delegate(AppSettings x)
				{
					x.DrawingSoundsEnabled = v;
				});
			}),
			SK.ToggleRow("Pen sounds", "Also a soft felt-tip, pencil or handwriting sound under every stroke and word, following the pen's speed. Off by default.", c.DrawingPenSoundsEnabled, delegate(bool v)
			{
				Quiet("screen_drawing_pen_sounds", v, delegate(AppSettings x)
				{
					x.DrawingPenSoundsEnabled = v;
				});
			}),
			SK.ToggleRow("JavaScript JIT", "V8 compiles the drawing engine's JavaScript and the 3D kernel's three.js to machine code. Off runs them interpreted, to compare. Takes effect after you quit and reopen HeyClicky.", c.DrawingJavaScriptJit, delegate(bool v)
			{
				Quiet("screen_drawing_javascript_jit", v, delegate(AppSettings x)
				{
					x.DrawingJavaScriptJit = v;
				});
			})
		})));
		list.Add(SK.Group("One brain", "The Worker honors these for internal testers only.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[5]
		{
			SK.Row("One brain model", "The model one brain answers every turn with (sent as brain_model).", trailing, null),
			SK.ToggleRow("One brain reads apps", "One brain reads your connected apps itself for a quick look (an email, an event, a doc) and hands bigger digs to a job. Read only: after a read the rest of that turn only talks. GPT-6.1 Sol models only. The local Worker needs its HTTPS tunnel (npm run dev:mcp).", c.OneBrainReadsApps, delegate(bool v)
			{
				Quiet("one_brain_reads_apps", v, delegate(AppSettings x)
				{
					x.OneBrainReadsApps = v;
				});
			}),
			SK.ToggleRow("Keep screens in history", "Each turn's screenshot of the display with your cursor stays in the conversation, so HeyClicky can look back at what was on screen a few turns ago. It's read back from the cache at about a tenth of the price; compaction folds old ones away, and they're removed when the session ends.", c.OneBrainKeepsScreensInHistory, delegate(bool v)
			{
				Quiet("one_brain_keeps_screens", v, delegate(AppSettings x)
				{
					x.OneBrainKeepsScreensInHistory = v;
				});
			}),
			SK.ToggleRow("Show one brain stats", "A small panel in the top-left corner: tokens in the conversation and how many until compaction, kept screenshots, the prompt cache on the last turn (read, written, fresh), each step's timing from key release to first voice, and session events. Hidden from screenshots, so Clicky never sees it.", c.ShowOneBrainStats, delegate(bool v)
			{
				Quiet("one_brain_stats_hud", v, delegate(AppSettings x)
				{
					x.ShowOneBrainStats = v;
				});
			}),
			FreshSessionRow()
		})));
		list.Add(SK.Group("Agent harness", "The Worker honors this for internal testers only.", new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(SK.Row("H1 model", "New jobs start on it at the server's effort; a job keeps the model it started on. Default is the server's model, what everyone else runs on.", trailing2, null))));
		list.AddRange(ProactiveDeveloperGroups());
		list.Add(SK.Group("Debug toggles", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
		{
			SK.ToggleRow("Show agent names", "The notch's activity strip shows what each agent is working on. Off shows only the working Clicky's face.", c.ShowAgentLabelsInActivityStrip, delegate(bool v)
			{
				Quiet("show_agent_names", v, delegate(AppSettings x)
				{
					x.ShowAgentLabelsInActivityStrip = v;
				});
			}),
			SK.ToggleRow("Preview app update", "Home's header and the notch peek show the orange Update app pill as if an update were downloaded. Resets on relaunch.", HeaderPills.PreviewUpdateReady, delegate(bool v)
			{
				HeaderPills.PreviewUpdateReady = v;
			}),
			SK.Row("Force plan state", "Shows Settings → Account in each payment state without a Stripe trip. Resets on relaunch.", trailing3, null)
		})));
		list.Add(SK.Group("Debug actions", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[9]
		{
			SK.ActionRow("Preview onboarding tutorial", null, actions.StartTutorial, null, null),
			SK.ActionRow("Preview paywall", "The plan picker the tour ends with, Free option included. Your plan and offer are untouched.", delegate
			{
				host.App?.PreviewOnboardingPaywall();
			}, null, null),
			SK.ActionRow("Reset paywall video", "The limit-hit paywall plays its intro video again.", delegate
			{
				host.App?.ResetPaywallIntroVideo();
			}, null, null),
			SK.ActionRow("Show the intro", "The pre-sign-in welcome, squad and color picker.", actions.ShowIntro, null, null),
			SK.ActionRow("Dry-run the intro reel's cues", "Fires the reel's demo cues at their timestamps without the video (they open pages and launch the demo agents).", delegate
			{
				host.App?.StartOnboardingVideoDemoDryRun();
			}, null, null),
			SK.ActionRow("Reset onboarding", "The notch's welcome and permission cards show again.", delegate
			{
				host.Set("reset_onboarding", true, delegate(AppSettings x)
				{
					x.Onboarding.HasCompletedOnboarding = false;
					x.Onboarding.HasAcknowledgedPermissionIntro = false;
					x.Onboarding.TutorialCompletedAtEpochSeconds = null;
				});
			}, null, null),
			SK.ActionRow("Reset intro", "The pre-sign-in intro shows again on the next launch.", delegate
			{
				host.Set("reset_intro", true, delegate(AppSettings x)
				{
					x.Onboarding.PreSignInIntroDismissed = false;
				});
			}, null, null),
			SK.ActionRow("Open logs folder", Log.Directory, delegate
			{
				host.OpenUrl(Log.Directory);
			}, null, null),
			SK.ActionRow("Relaunch HeyClicky", "Quits and opens HeyClicky again, e.g. after switching the JIT.", actions.Relaunch, null, null)
		})));
		return SK.Scaffold("Developer", "Previews and debug tools.", new global::_003C_003Ez__ReadOnlyList<FrameworkElement>(list));
	}

	[IteratorStateMachine(typeof(_003CDemoAgentMomentRows_003Ed__65))]
	private IEnumerable<FrameworkElement> DemoAgentMomentRows()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDemoAgentMomentRows_003Ed__65(-2)
		{
			_003C_003E4__this = this
		};
	}

	private FrameworkElement DictationPage()
	{
		AppSettings c = host.Current;
		bool flag = c.DictationLanguageCode == "multi";
		string pinned = (Languages.Any(((string Code, string Name, string Native) l) => l.Code == c.DictationSpecificLanguageCode) ? c.DictationSpecificLanguageCode : "en");
		Grid item = SK.SearchField("Search languages", languageSearch, delegate(string t)
		{
			languageSearch = t;
			FillLanguageGrid(pinned);
		}, magnifier: true, 12.0, null).Box;
		languageGridHost = new Grid();
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(14.0),
			Opacity = (flag ? 0.35 : 1.0),
			IsHitTestVisible = !flag,
			Children = 
			{
				(UIElement)item,
				(UIElement)languageGridHost
			}
		};
		FillLanguageGrid(pinned);
		StackPanel stackPanel2 = SK.Group("Language", "Auto-detect follows you across languages mid-sentence. Pin one language for the most accurate results.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
		{
			SK.ToggleRow("Auto-detect language", "HeyClicky figures out the language as you speak.", flag, delegate(bool v)
			{
				host.Set("auto_detect_language", v, delegate(AppSettings x)
				{
					x.DictationLanguageCode = (v ? "multi" : x.DictationSpecificLanguageCode);
				});
			}),
			stackPanel
		}));
		List<string> dictationCustomWords = c.DictationCustomWords;
		var (item2, wordInput) = SK.SearchField("Add a word", draftWord, delegate(string t)
		{
			draftWord = t;
		}, magnifier: false, 12.5, null);
		wordInput.KeyDown += delegate(object _, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				CommitDraftWord();
			}
		};
		GelButton add = new GelButton("Add", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, null, null)
		{
			Opacity = (string.IsNullOrWhiteSpace(draftWord) ? 0.5 : 1.0)
		};
		wordInput.TextChanged += delegate
		{
			add.Opacity = (string.IsNullOrWhiteSpace(wordInput.Text) ? 0.5 : 1.0);
		};
		add.Click += CommitDraftWord;
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(add, 1);
		grid.Children.Add(item2);
		grid.Children.Add(add);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 12.0,
			Padding = new Thickness(14.0),
			Children = { (UIElement)grid }
		};
		if (dictationCustomWords.Count > 0)
		{
			WrapPanel wrapPanel = SK.Wrap(6.0);
			foreach (string item3 in dictationCustomWords)
			{
				wrapPanel.Children.Add(WordChip(item3));
			}
			stackPanel3.Children.Add(wrapPanel);
		}
		if (focusWordInputAfterRender)
		{
			focusWordInputAfterRender = false;
			wordInput.Loaded += delegate
			{
				wordInput.Focus(FocusState.Programmatic);
			};
		}
		return SK.Scaffold("Dictation", "Languages and the words HeyClicky should spell right.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
		{
			SK.Group("Dictionary", "Up to 50 words. HeyClicky leans toward these spellings — names, jargon, product words.", new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(stackPanel3)),
			stackPanel2
		}), 32.0);
	}

	private void FillLanguageGrid(string pinned)
	{
		using (MessagesPalette.Use(look))
		{
			if ((object)languageGridHost == null)
			{
				return;
			}
			languageGridHost.Children.Clear();
			string q = languageSearch.Trim().ToLowerInvariant();
			(string, string, string)[] array = ((string, string, string)[])((q.Length == 0) ? ((Array)LanguagesForGrid) : ((Array)LanguagesForGrid.Where(delegate((string Code, string Name, string Native) l)
			{
				if (!l.Name.ToLowerInvariant().Contains(q))
				{
					string item = l.Native;
					if (item == null || !item.ToLowerInvariant().Contains(q))
					{
						return l.Code == q;
					}
				}
				return true;
			}).ToArray()));
			if (array.Length == 0)
			{
				languageGridHost.Children.Add(Ui.Text("No languages match \"" + languageSearch + "\".", 11.5, FontWeights.Medium, SK.TextTertiary));
				return;
			}
			Grid grid = new Grid
			{
				ColumnSpacing = 8.0,
				RowSpacing = 8.0
			};
			for (int i = 0; i < 4; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition());
			}
			for (int j = 0; j < (array.Length + 3) / 4; j++)
			{
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLength.Auto
				});
			}
			for (int k = 0; k < array.Length; k++)
			{
				(string, string, string) tuple = array[k];
				string code = tuple.Item1;
				string item2 = tuple.Item2;
				string item3 = tuple.Item3;
				bool flag = code == pinned;
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 3.0,
					VerticalAlignment = VerticalAlignment.Center,
					Children = { (UIElement)Ui.Text(item2, 11.5, flag ? FontWeights.SemiBold : FontWeights.Medium, flag ? SK.TextPrimary : SK.TextSecondary, TextAlignment.Left, 1) }
				};
				if (item3 != null)
				{
					stackPanel.Children.Add(Ui.Text(item3, 10.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 1));
				}
				Grid grid2 = new Grid
				{
					Padding = new Thickness(13.0, 12.0, 13.0, 12.0),
					MinHeight = 54.0,
					ColumnSpacing = 6.0
				};
				grid2.ColumnDefinitions.Add(new ColumnDefinition());
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(18.0)
				});
				grid2.Children.Add(stackPanel);
				if (flag)
				{
					FrameworkElement frameworkElement = IntroWindow.GelCheckmark(18.0);
					frameworkElement.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(frameworkElement, 1);
					grid2.Children.Add(frameworkElement);
				}
				Pressable pressable = SK.Tile(grid2, flag, 9.0, delegate
				{
					host.Set("dictation_language", code, delegate(AppSettings x)
					{
						x.DictationSpecificLanguageCode = code;
						if (x.DictationLanguageCode != "multi")
						{
							x.DictationLanguageCode = code;
						}
					});
				}, 20.0, topRightCheck: false);
				ToolTipService.SetToolTip(pressable, (item3 == null) ? item2 : (item2 + ", " + item3));
				Grid.SetColumn(pressable, k % 4);
				Grid.SetRow(pressable, k / 4);
				grid.Children.Add(pressable);
			}
			languageGridHost.Children.Add(grid);
		}
	}

	private void CommitDraftWord()
	{
		string word = draftWord.Trim();
		if (word.Length == 0)
		{
			return;
		}
		List<string> words = host.Read((AppSettings x) => x.DictationCustomWords.ToList());
		if (word.Length <= 60 && words.Count < 50 && !words.Any((string w) => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
		{
			words.Add(word);
			host.Set("dictation_dictionary", $"add ({words.Count} words)", delegate(AppSettings x)
			{
				x.DictationCustomWords = words;
			});
		}
		draftWord = "";
		focusWordInputAfterRender = true;
		Render();
	}

	private FrameworkElement WordChip(string word)
	{
		Border chip = new Border
		{
			CornerRadius = new CornerRadius(100.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.08)),
			Padding = new Thickness(10.0, 5.5, 10.0, 5.5)
		}.Capsule();
		Pressable pressable = new Pressable
		{
			Width = 12.0,
			Height = 12.0
		};
		TextBlock textBlock = Icons.Glyph("\ue711", 7.5, SK.TextTertiary);
		textBlock.FontWeight = FontWeights.Bold;
		pressable.Children.Add(textBlock);
		pressable.Click += delegate
		{
			List<string> words = host.Read((AppSettings s) => s.DictationCustomWords.Where((string w) => !string.Equals(w, word, StringComparison.OrdinalIgnoreCase)).ToList());
			host.Set("dictation_dictionary", "remove", delegate(AppSettings s)
			{
				s.DictationCustomWords = words;
			});
		};
		chip.Child = Ui.H(5.0, Ui.Text(word, 11.5, FontWeights.Medium, SK.TextPrimary), pressable);
		chip.PointerEntered += delegate
		{
			chip.Background = Ui.Brush(look.Ink.WithAlpha(0.12));
		};
		chip.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				chip.Background = Ui.Brush(look.Ink.WithAlpha(0.08));
			}
		};
		return chip;
	}

	private FrameworkElement GeneralPage()
	{
		AppSettings current = host.Current;
		AppConfigLinks links = SettingsHost.Links;
		return SK.Scaffold("General", "How HeyClicky behaves on your PC.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[5]
		{
			SK.Group("Behavior", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[4]
			{
				SK.ToggleRow("Show in taskbar", "Turn off to keep HeyClicky notch only.", current.ShowInTaskbar, delegate(bool v)
				{
					Quiet("show_in_taskbar", v, delegate(AppSettings x)
					{
						x.ShowInTaskbar = v;
					});
				}),
				SK.ToggleRow("Show in screen recordings", "Let screen sharing and recording tools capture HeyClicky.", current.ShowInScreenRecordings, delegate(bool v)
				{
					Quiet("show_in_screen_recordings", v, delegate(AppSettings x)
					{
						x.ShowInScreenRecordings = v;
					});
				}),
				SK.ToggleRow("Launch at login", "Open HeyClicky when you sign in to Windows.", s.Settings.LaunchAtLogin, delegate(bool v)
				{
					quietWrites++;
					try
					{
						Log.Info("settings", $"launch_at_login = {v}");
						s.Settings.LaunchAtLogin = v;
						SettingsHost.TrackSettingChanged("launch_at_login", v);
					}
					finally
					{
						quietWrites--;
					}
				}),
				SK.ToggleRow("Pause HeyClicky", "Ignore the shortcuts until you resume.", s.Settings.IsPaused, delegate(bool v)
				{
					quietWrites++;
					try
					{
						Log.Info("settings", $"paused = {v}");
						actions.SetPaused(v);
						SettingsHost.TrackSettingChanged("paused", v);
					}
					finally
					{
						quietWrites--;
					}
				})
			})),
			UpdatesGroup(),
			SK.Group("Community", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				BrandLinkRow("WhatsApp community", "whatsapp-logo.png", delegate
				{
					if (!string.IsNullOrEmpty(links.CommunityWhatsApp))
					{
						ClickyAnalytics.TrackCommunityLinkOpened("whatsapp");
					}
					host.OpenUrl(links.CommunityWhatsApp);
				}),
				BrandLinkRow("Reddit", "reddit-logo.svg", delegate
				{
					if (!string.IsNullOrEmpty(links.CommunityReddit))
					{
						ClickyAnalytics.TrackCommunityLinkOpened("reddit");
					}
					host.OpenUrl(links.CommunityReddit);
				})
			})),
			SK.Group("Support", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				SK.ActionRow("Request a feature", null, delegate
				{
					ClickyAnalytics.TrackFeatureRequestOpened();
					host.OpenUrl(links.FeatureRequest);
				}, null, "arrow.up.right"),
				SK.ActionRow("Report a bug", "Sends diagnostics along so we can actually fix it.", delegate
				{
					BugReport.Run(host);
				}, null, null)
			})),
			SK.Group("Welcome tour", null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(SK.ActionRow("Replay the welcome tour", "Watch the intro video again, then redo the hands-on tour and the interview. HeyClicky suggests three new jobs from your answers; the ones you have stay.", delegate
			{
				Log.Info("settings", "replay_welcome_tour");
				SettingsHost.TrackSettingChanged("replay_welcome_tour", null);
				if (actions is AppHost appHost)
				{
					appHost.ReplayOnboardingExperienceFromSettings();
				}
				else
				{
					actions.StartTutorial();
				}
			}, null, "play.fill")))
		}));
	}

	private FrameworkElement BrandLinkRow(string title, string asset, Action onClick)
	{
		Pressable p = new Pressable();
		string uriString = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", asset);
		FrameworkElement item = ((!asset.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) ? new Image
		{
			Width = 16.0,
			Height = 16.0,
			Source = new BitmapImage(new Uri(uriString))
		} : new Image
		{
			Width = 16.0,
			Height = 16.0,
			Source = new SvgImageSource(new Uri(uriString))
			{
				RasterizePixelWidth = 32.0,
				RasterizePixelHeight = 32.0
			}
		});
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 11.0, 14.0, 11.0),
			ColumnSpacing = 10.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		TextBlock textBlock = Ui.Text(title, 13.0, FontWeights.Medium, SK.TextPrimary);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		FrameworkElement frameworkElement = SK.TrailingSymbol("arrow.up.right");
		Grid.SetColumn(textBlock, 1);
		Grid.SetColumn(frameworkElement, 2);
		grid.Children.Add(item);
		grid.Children.Add(textBlock);
		grid.Children.Add(frameworkElement);
		p.Children.Add(grid);
		p.HoverChanged += delegate(bool h)
		{
			p.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.05 : 0.0));
		};
		p.Click += onClick;
		return p;
	}

	private async Task RefreshIntegrationsAsync()
	{
		if (host.Agents == null)
		{
			AgentIntegrationCatalog demo = DemoCatalog;
			if ((object)demo != null && host.IsSignedIn)
			{
				dq.TryEnqueue(delegate
				{
					catalog = demo;
					integrationError = null;
					loadingIntegrations = false;
					if (page == SettingsWindow.Page.Integrations)
					{
						Render();
					}
				});
				return;
			}
		}
		AgentService agents = host.Agents;
		if (agents == null || !host.IsSignedIn)
		{
			dq.TryEnqueue(delegate
			{
				if (page == SettingsWindow.Page.Integrations)
				{
					Render();
				}
			});
			return;
		}
		loadingIntegrations = true;
		try
		{
			AgentIntegrationCatalog result = await agents.ListIntegrationsAsync();
			ConnectedApps.SetCatalog(result);
			dq.TryEnqueue(delegate
			{
				catalog = result;
				string text = integrationError;
				integrationError = (result.ComposioConfigured ? null : "Integrations aren't set up on this server.");
				loadingIntegrations = false;
				Log.Info("settings", $"integrations: {result.Integrations.Count} in the catalog, {result.Integrations.Count((AgentIntegration i) => i.IsActiveConnection)} connected");
				CheckAwaitedConnection();
				if (page == SettingsWindow.Page.Integrations)
				{
					if (text == integrationError && (object)integrationListHost != null)
					{
						FillIntegrationList();
					}
					else
					{
						Render();
					}
				}
			});
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "integrations fetch failed: " + ex.Message);
			dq.TryEnqueue(delegate
			{
				loadingIntegrations = false;
				integrationError = "HeyClicky couldn't load your integrations. Check your connection and try again.";
				if (page == SettingsWindow.Page.Integrations)
				{
					Render();
				}
			});
		}
	}

	public void OnIntegrationCallback(string? toolkit)
	{
		Log.Info("settings", "integration callback (" + (toolkit ?? "?") + ")");
		RefreshIntegrationsAsync();
	}

	private void CheckAwaitedConnection()
	{
		(string, int, DateTime)? tuple = awaitingConnection;
		if (!tuple.HasValue)
		{
			return;
		}
		(string Id, int Baseline, DateTime Started) waiting = tuple.GetValueOrDefault();
		if ((object)catalog != null)
		{
			AgentIntegration agentIntegration = catalog.Integrations.FirstOrDefault((AgentIntegration i) => i.Id == waiting.Id);
			if ((object)agentIntegration != null && agentIntegration.ActiveAccounts.Count > waiting.Baseline)
			{
				Log.Info("settings", $"integration connected: {waiting.Id} ({agentIntegration.ActiveAccounts.Count} account(s))");
				TrackIntegration("connected", agentIntegration, "agent_space_settings", null);
				awaitingConnection = null;
				justConnected = waiting.Id;
				host.Agents?.InvalidateConnectedApps();
			}
			else if (DateTime.UtcNow - waiting.Started > TimeSpan.FromMinutes(5L))
			{
				awaitingConnection = null;
			}
		}
	}

	private async Task PollAwaitedConnectionAsync()
	{
		while (true)
		{
			(string, int, DateTime)? tuple = awaitingConnection;
			if (!tuple.HasValue)
			{
				break;
			}
			await Task.Delay(3000);
			tuple = awaitingConnection;
			if (!tuple.HasValue)
			{
				break;
			}
			await RefreshIntegrationsAsync();
		}
	}

	private void ScheduleConnectorStateRefresh()
	{
		if (CustomConnectorStore.Load().Count == 0)
		{
			return;
		}
		Task.Run(async delegate
		{
			await Task.Delay(4000);
			if (page == SettingsWindow.Page.Integrations && isShown)
			{
				await ConnectorAuth.RefreshStatesAsync();
			}
		});
	}

	private FrameworkElement IntegrationsPage()
	{
		HookConnectorEvents();
		RestoreParkedConnectorDraftIfFresh();
		List<FrameworkElement> list = new List<FrameworkElement>();
		list.Add(BrowserConnectionSection());
		Grid item = SK.SearchField("Search integrations", integrationSearch, delegate(string t)
		{
			integrationSearch = t;
			FillIntegrationList();
			FillBrowserCard();
		}, magnifier: true, 12.0, null).Box;
		GelButton gelButton = new GelButton("Add custom connector", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
		gelButton.Click += delegate
		{
			OpenConnectorEditor(null);
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(gelButton, 1);
		grid.Children.Add(item);
		grid.Children.Add(gelButton);
		StackPanel stackPanel = new StackPanel
		{
			Children = { (UIElement)grid }
		};
		FrameworkElement frameworkElement = ConnectorEditor();
		if ((object)frameworkElement != null)
		{
			frameworkElement.Margin = new Thickness(0.0, 20.0, 0.0, 0.0);
			stackPanel.Children.Add(frameworkElement);
		}
		list.Add(stackPanel);
		string text = integrationError;
		if (text != null)
		{
			list.Add(SK.Alert(text, SK.Destructive));
		}
		integrationListHost = new StackPanel
		{
			Spacing = 20.0
		};
		FillIntegrationList();
		list.Add(integrationListHost);
		return SK.Scaffold("Integrations", "Apps your agents can reach into.", list);
	}

	private void ResetDepartures()
	{
		departures.Clear();
		departureRows.Clear();
		lastConnected = new List<(string, string, Func<FrameworkElement>)>();
	}

	private void TrackDepartures(List<(string Id, string Name, Func<FrameworkElement> Logo)> connected, bool retain)
	{
		if (!retain)
		{
			departures.Clear();
			lastConnected = connected;
			return;
		}
		HashSet<string> hashSet = connected.Select<(string, string, Func<FrameworkElement>), string>(((string Id, string Name, Func<FrameworkElement> Logo) c) => c.Id).ToHashSet();
		foreach (string item in departures.Keys.Where(hashSet.Contains).ToList())
		{
			departures.Remove(item);
		}
		for (int i = 0; i < lastConnected.Count; i++)
		{
			(string, string, Func<FrameworkElement>) tuple = lastConnected[i];
			if (!hashSet.Contains(tuple.Item1) && !departures.ContainsKey(tuple.Item1))
			{
				Guid token = Guid.NewGuid();
				departures[tuple.Item1] = new Departure(tuple.Item1, tuple.Item2, tuple.Item3, i, token);
				Log.Info("settings", "integration departing: " + tuple.Item1);
				FinishDepartureAsync(tuple.Item1, token);
			}
		}
		lastConnected = connected;
	}

	private async Task FinishDepartureAsync(string id, Guid token)
	{
		await Task.Delay(1200);
		dq.TryEnqueue(async delegate
		{
			if (departures.TryGetValue(id, out Departure value) && !(value.Token != token))
			{
				if (departureRows.TryGetValue(id, out FrameworkElement value2) && !Anim.ReduceMotion)
				{
					Anim.Opacity(value2, 0f, 0.26);
					await Task.Delay(260);
				}
				if (departures.TryGetValue(id, out Departure value3) && !(value3.Token != token))
				{
					departures.Remove(id);
					departureRows.Remove(id);
					FillIntegrationList();
				}
			}
		});
	}

	private FrameworkElement DepartedRow(Departure d)
	{
		StackPanel item = Ui.H(7.0, Ui.Text(d.Name, 13.0, FontWeights.Medium, SK.TextPrimary), Ui.H(3.0, Icons.Glyph("\ue73e", 9.0, SK.TextSecondary), Ui.Text("Disconnected", 10.0, FontWeights.SemiBold, SK.TextSecondary)).Align(HorizontalAlignment.Left, VerticalAlignment.Center));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)item,
				(UIElement)Ui.Text("HeyClicky no longer has access.", 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 2)
			}
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0,
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			Tag = "departed:" + d.Id
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(26.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		FrameworkElement frameworkElement = d.Logo();
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(frameworkElement);
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		departureRows[d.Id] = grid;
		return grid;
	}

	private void FillIntegrationList()
	{
		using (MessagesPalette.Use(look))
		{
			if ((object)integrationListHost == null)
			{
				return;
			}
			integrationListHost.Children.Clear();
			bool flag = integrationSearch.Trim().Length > 0;
			if (!host.IsSignedIn)
			{
				integrationListHost.Children.Add(EmptyText("Sign in to connect integrations."));
				return;
			}
			List<AgentIntegration> source = IntegrationSearch.Sorted((catalog?.Integrations ?? Array.Empty<AgentIntegration>()).Where((AgentIntegration i) => !i.IsLocalIntegration).Append(ObsidianVault.Row()));
			List<AgentIntegration> connected = source.Where((AgentIntegration i) => i.IsActiveConnection || i.Status == "needs_reconnect" || (i.Id == awaitingConnection?.Id && i.Accounts.Count > 0)).ToList();
			List<CustomConnector> source2 = (from c in CustomConnectorStore.Load()
				orderby c.CreatedAt
				select c).ToList();
			List<(string, string, Func<FrameworkElement>, Func<FrameworkElement>)> source3 = connected.Select((AgentIntegration i) => ((string Id, string DisplayName, Func<FrameworkElement>, Func<FrameworkElement>))(Id: i.Id, DisplayName: i.DisplayName, () => IntegrationLogo(i, 26.0), () => IntegrationRow(i))).Concat<(string, string, Func<FrameworkElement>, Func<FrameworkElement>)>(source2.Select((CustomConnector c) => ((string IntegrationId, string Name, Func<FrameworkElement>, Func<FrameworkElement>))(IntegrationId: c.IntegrationId, Name: c.Name, () => ConnectorGlyph(c, 26.0), () => CustomConnectorRow(c)))).ToList();
			TrackDepartures(source3.Select(((string, string, Func<FrameworkElement>, Func<FrameworkElement>) e) => (e.Item1, e.Item2, e.Item3)).ToList(), !flag && (object)catalog != null);
			List<FrameworkElement> list = source3.Select(((string, string, Func<FrameworkElement>, Func<FrameworkElement>) e) => e.Item4()).ToList();
			foreach (Departure item in departures.Values.OrderBy((Departure d) => d.Index))
			{
				list.Insert(Math.Min(item.Index, list.Count), DepartedRow(item));
			}
			if (!flag && list.Count > 0)
			{
				integrationListHost.Children.Add(SK.Group("Connected", null, list, 52.0));
			}
			if ((object)catalog == null && (loadingIntegrations || integrationError == null))
			{
				integrationListHost.Children.Add(IntegrationsLoadingShimmer());
				return;
			}
			List<AgentIntegration> list2 = (flag ? source.Where((AgentIntegration i) => IntegrationSearch.Matches(i, integrationSearch)) : source.Where((AgentIntegration i) => !connected.Contains(i) && !departures.ContainsKey(i.Id))).ToList();
			if (list2.Count == 0)
			{
				integrationListHost.Children.Add(EmptyText(flag ? ("Nothing matches \"" + integrationSearch + "\".") : ((connected.Count > 0) ? "No more integrations to connect." : "No integrations available yet.")));
				return;
			}
			integrationListHost.Children.Add(SK.Group(flag ? "Search results" : "Available integrations", "Custom connectors are any MCP server: a hosted URL or a command on this PC. Tokens stay encrypted for your Windows account and are handed only to HeyClicky's agents.", list2.Select((AgentIntegration i) => IntegrationRow(i)).ToList(), 52.0));
		}
	}

	private static FrameworkElement EmptyText(string text)
	{
		TextBlock textBlock = Ui.Text(text, 12.0, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 0, wrap: true);
		textBlock.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		return textBlock;
	}

	private static FrameworkElement IntegrationLogo(AgentIntegration i, double size)
	{
		return IntegrationLogos.Artwork(i.PrimaryToolkitSlug, i.DisplayName, size, 12.0, i.LogoUrl);
	}

	private FrameworkElement IntegrationRow(AgentIntegration i)
	{
		bool flag = integrationBusy.Contains(i.Id);
		bool flag2 = awaitingConnection?.Id == i.Id;
		bool flag3 = !i.IsLocalIntegration && i.Accounts.Count > 0 && (i.IsActiveConnection || i.Status == "needs_reconnect");
		bool shouldDisconnect = !i.IsLocalIntegration && i.ConnectedCount > 0 && i.Status != "needs_reconnect";
		bool flag4 = justConnected == i.Id;
		StackPanel stackPanel = Ui.H(7.0, Ui.Text(i.DisplayName, 13.0, FontWeights.Medium, SK.TextPrimary));
		if (flag2)
		{
			stackPanel.Children.Add(ConnectionBadge("Checking…", SK.Warning, connected: false));
		}
		else if (flag4)
		{
			stackPanel.Children.Add(ConnectionBadge("Connected", SK.Positive, connected: true));
		}
		else if ((i.IsActiveConnection || i.Status == "needs_reconnect") && !flag3)
		{
			(string, Color) tuple;
			switch (i.Status)
			{
			case "connected":
				tuple = ((!i.IsLocalIntegration) ? ("Connected", SK.Positive) : ("Configured", SK.Positive));
				break;
			case "partial":
				tuple = ("Partial", SK.Warning);
				break;
			case "needs_reconnect":
				tuple = ("Reconnect required", SK.Warning);
				break;
			case "unavailable":
			case "error":
				tuple = ((i.Status == "error") ? "Error" : "Unavailable", SK.Destructive);
				break;
			default:
				tuple = ("Not connected", SK.TextTertiary);
				break;
			}
			var (text, color) = tuple;
			stackPanel.Children.Add(StatusChip(text, color));
		}
		string text2 = (flag2 ? ("Finish signing in to " + i.DisplayName + " in your browser, then come back.") : ((flag4 && !i.IsLocalIntegration) ? "You're all set. HeyClicky can use it now." : i.Description));
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 1.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)Ui.Text(text2, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 2)
			}
		};
		FrameworkElement frameworkElement = null;
		if (flag2)
		{
			GelButton gelButton = new GelButton("Refresh", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton.Click += delegate
			{
				RefreshIntegrationsAsync();
			};
			frameworkElement = gelButton;
		}
		else if (flag3)
		{
			frameworkElement = null;
		}
		else if (flag)
		{
			frameworkElement = Ui.Text(shouldDisconnect ? "Disconnecting…" : ((i.Status == "needs_reconnect") ? "Reconnecting…" : "Connecting…"), 11.0, FontWeights.Medium, SK.Warning);
		}
		else if (confirmDisconnectIntegration == i.Id)
		{
			GelButton gelButton2 = new GelButton("Disconnect", GelAppearance.SecondaryOutline, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton2.Click += delegate
			{
				DisconnectAsync(i, null);
			};
			GelButton gelButton3 = new GelButton("Keep", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton3.Click += delegate
			{
				confirmDisconnectIntegration = null;
				Render();
			};
			frameworkElement = Ui.H(6.0, gelButton2, gelButton3);
		}
		else if (i.IsLocalIntegration || i.Status != "unavailable")
		{
			string title;
			if (i.IsLocalIntegration)
			{
				string status = i.Status;
				string text3 = ((status == "connected") ? "Change" : ((!(status == "needs_reconnect")) ? "Configure" : "Reconnect"));
				title = text3;
			}
			else
			{
				title = ((i.Status == "needs_reconnect") ? "Reconnect" : (shouldDisconnect ? "Disconnect" : "Connect"));
			}
			GelButton gelButton4 = new GelButton(title, shouldDisconnect ? GelAppearance.SecondaryOutline : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
			gelButton4.Click += delegate
			{
				if (shouldDisconnect)
				{
					confirmDisconnectIntegration = i.Id;
					Render();
				}
				else
				{
					ConnectAsync(i);
				}
			};
			frameworkElement = gelButton4;
			if (i.Status == "needs_reconnect" && i.Accounts.Count == 0)
			{
				GelButton gelButton5 = new GelButton("", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 15.0, "\ue711", null, null);
				gelButton5.Click += delegate
				{
					DisconnectAsync(i, null);
				};
				gelButton5.Tip("Remove this expired connection");
				frameworkElement = Ui.H(6.0, gelButton4, gelButton5);
			}
		}
		else
		{
			frameworkElement = Ui.Text("Unavailable", 11.0, FontWeights.Medium, SK.TextTertiary);
		}
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(26.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		FrameworkElement frameworkElement2 = IntegrationLogo(i, 26.0);
		frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(frameworkElement2);
		Grid.SetColumn(stackPanel2, 1);
		grid.Children.Add(stackPanel2);
		if ((object)frameworkElement != null)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			Grid.SetColumn(frameworkElement, 2);
			grid.Children.Add(frameworkElement);
		}
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 8.0,
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			Children = { (UIElement)grid }
		};
		if (flag3)
		{
			stackPanel3.Children.Add(AccountsCard(i));
		}
		Grid hover = new Grid
		{
			Background = Ui.Transparent,
			Children = { (UIElement)stackPanel3 }
		};
		hover.PointerEntered += delegate
		{
			hover.Background = Ui.Brush(look.Ink.WithAlpha(0.04));
		};
		hover.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				hover.Background = Ui.Transparent;
			}
		};
		return hover;
	}

	private static FrameworkElement ConnectionBadge(string title, Color tint, bool connected)
	{
		FrameworkElement frameworkElement = (connected ? new Microsoft.UI.Xaml.Shapes.Path
		{
			Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M1,6 L4.5,9 L11,2"),
			Stroke = Ui.Brush(tint),
			StrokeThickness = 1.8,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			Width = 12.0,
			Height = 12.0
		} : (Anim.ReduceMotion ? ((FrameworkElement)Icons.Glyph("\ue823", 10.0, tint)) : ((FrameworkElement)new ProgressRing
		{
			Width = 9.0,
			Height = 9.0,
			MinWidth = 0.0,
			MinHeight = 0.0,
			IsActive = true,
			Foreground = Ui.Brush(tint)
		})));
		Grid grid = new Grid
		{
			Width = 12.0,
			Height = 12.0,
			Children = { (UIElement)frameworkElement }
		};
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		TextBlock textBlock = Ui.Text(title, 10.0, FontWeights.SemiBold, tint);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Border border = new Border();
		border.Background = Ui.Brush(tint.WithAlpha(0.1));
		border.Padding = new Thickness(7.0, 4.0, 7.0, 4.0);
		border.VerticalAlignment = VerticalAlignment.Center;
		border.Child = Ui.H(5.0, grid, textBlock);
		return border.Capsule();
	}

	private static FrameworkElement StatusChip(string text, Color color)
	{
		return Ui.H(4.0, new Ellipse
		{
			Width = 5.0,
			Height = 5.0,
			Fill = Ui.Brush(color),
			VerticalAlignment = VerticalAlignment.Center
		}, Ui.Text(text, 10.0, FontWeights.SemiBold, color).Align(HorizontalAlignment.Left, VerticalAlignment.Center));
	}

	private FrameworkElement AccountsCard(AgentIntegration i)
	{
		StackPanel stackPanel = new StackPanel();
		bool flag = i.Accounts.Count > 1;
		List<string> freeQuickLabels = new string[2] { "Work", "Personal" }.Where((string l) => !i.Accounts.Any((AgentIntegrationAccount a) => string.Equals(a.TrimmedLabel, l, StringComparison.OrdinalIgnoreCase))).ToList();
		for (int j = 0; j < i.Accounts.Count; j++)
		{
			if (j > 0)
			{
				stackPanel.Children.Add(SK.Divider(52.0));
			}
			stackPanel.Children.Add(AccountRow(i, i.Accounts[j], j, !flag, freeQuickLabels));
		}
		if (i.Status != "needs_reconnect")
		{
			stackPanel.Children.Add(SK.Divider(0.0));
			if (i.HasReachedActiveAccountLimit)
			{
				stackPanel.Children.Add(Ui.H(10.0, Icons.Glyph("\ue946", 11.0, SK.TextTertiary).Size(30.0, 30.0), Ui.Text($"{5} accounts is the most one app can have. Disconnect one to add another.", 11.0, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 0, wrap: true)).Pad(10.0, 7.0, 10.0, 7.0));
			}
			else
			{
				Pressable pressable = new Pressable();
				bool flag2 = integrationBusy.Contains(i.Id) || awaitingConnection?.Id == i.Id;
				Grid grid = new Grid
				{
					Width = 30.0,
					Height = 30.0
				};
				grid.Children.Add(new Ellipse
				{
					Stroke = Ui.Brush(SK.Ink.WithAlpha(0.22)),
					StrokeThickness = 1.0,
					StrokeDashArray = new DoubleCollection { 3.0, 2.5 }
				});
				grid.Children.Add(flag2 ? ((FrameworkElement)new ProgressRing
				{
					Width = 12.0,
					Height = 12.0,
					IsActive = true
				}) : ((FrameworkElement)Icons.Glyph("\ue710", 10.5, SK.TextSecondary)));
				TextBlock label = Ui.Text(flag2 ? ("Checking for your new " + i.DisplayName + " account…") : ("Add another " + i.DisplayName + " account"), 12.0, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 1);
				label.VerticalAlignment = VerticalAlignment.Center;
				pressable.Children.Add(Ui.H(10.0, grid, label).Pad(10.0, 7.0, 10.0, 7.0));
				pressable.HoverChanged += delegate(bool h)
				{
					label.Foreground = Ui.Brush(h ? look.ThemeTextPrimary : look.ThemeTextSecondary);
				};
				pressable.Tip("Connect another " + i.DisplayName + " account, like a work and a personal one");
				if (!flag2)
				{
					pressable.Click += delegate
					{
						ConnectAsync(i, addingAnother: true);
					};
				}
				stackPanel.Children.Add(pressable);
			}
		}
		return new Border
		{
			Margin = new Thickness(38.0, 0.0, 0.0, 0.0),
			CornerRadius = new CornerRadius(11.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.04)),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.07)),
			BorderThickness = new Thickness(1.0),
			Child = stackPanel
		};
	}

	private FrameworkElement AccountRow(AgentIntegration i, AgentIntegrationAccount a, int position, bool isOnly, List<string> freeQuickLabels)
	{
		string text = a.TrimmedAccountName ?? a.TrimmedLabel;
		string text2 = text ?? (isOnly ? (i.DisplayName + " account") : $"{i.DisplayName} account {position + 1}");
		Grid grid = new Grid
		{
			Width = 30.0,
			Height = 30.0,
			Opacity = (a.IsActive ? 1.0 : 0.75)
		};
		if (text != null)
		{
			grid.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(a.IsOrganization ? 7.800000000000001 : 15.0),
				Background = Ui.Brush(InitialColor(text))
			});
			grid.Children.Add(Ui.Text(text.Substring(0, 1).ToUpperInvariant(), 13.5, FontWeights.SemiBold, Colors.White).Center());
		}
		else
		{
			grid.Children.Add(new Ellipse
			{
				Fill = Ui.Brush(SK.Ink.WithAlpha(0.1))
			});
			grid.Children.Add(IntegrationLogo(i, 17.0).Center());
		}
		string avatarUrl = a.AvatarUrl;
		if (avatarUrl != null && Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri result) && result.Scheme == "https")
		{
			grid.Children.Add(new Ellipse
			{
				Fill = new ImageBrush
				{
					ImageSource = new BitmapImage(result),
					Stretch = Stretch.UniformToFill
				}
			});
		}
		if (!a.IsActive)
		{
			grid.Children.Add(new Ellipse
			{
				Width = 9.0,
				Height = 9.0,
				Fill = Ui.Brush(SK.Warning),
				Stroke = Ui.Brush(DS.Black(0.6)),
				StrokeThickness = 1.5,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0.0, 0.0, -1.0, -1.0)
			});
		}
		StackPanel stackPanel = Ui.H(6.0, Ui.Text(text2, 13.0, FontWeights.SemiBold, SK.TextPrimary, TextAlignment.Left, 1));
		bool flag = editingLabelAccount == a.ConnectedAccountId;
		if (flag)
		{
			(Grid, TextBox) tuple = SK.SearchField("Label, like Work", a.TrimmedLabel ?? "", delegate
			{
			}, magnifier: false, 11.5, null);
			Grid item = tuple.Item1;
			TextBox input = tuple.Item2;
			item.Width = 120.0;
			input.Padding = new Thickness(7.0, 1.5, 7.0, 1.5);
			input.CornerRadius = new CornerRadius(6.0);
			input.Loaded += delegate
			{
				input.Focus(FocusState.Programmatic);
			};
			input.KeyDown += delegate(object _, KeyRoutedEventArgs e)
			{
				if (e.Key == VirtualKey.Enter)
				{
					e.Handled = true;
					_ = SaveLabelAsync(i, a, input.Text);
				}
				else if (e.Key == VirtualKey.Escape)
				{
					editingLabelAccount = null;
					Render();
				}
			};
			input.LostFocus += delegate
			{
				if (editingLabelAccount == a.ConnectedAccountId)
				{
					SaveLabelAsync(i, a, input.Text);
				}
			};
			stackPanel.Children.Add(item);
		}
		else if (a.TrimmedAccountName != null && !isOnly)
		{
			string trimmedLabel = a.TrimmedLabel;
			if (trimmedLabel != null)
			{
				Pressable badge = new Pressable
				{
					CornerRadius = new CornerRadius(100.0),
					Background = Ui.Brush(DS.Rgb(0.36, 0.58, 1.0, 0.18))
				};
				badge.Children.Add(Ui.Text(trimmedLabel, 10.5, FontWeights.SemiBold, DS.Rgb(0.62, 0.78, 1.0)).Pad(7.0, 2.0, 7.0, 2.0));
				badge.HoverChanged += delegate(bool h)
				{
					badge.Background = Ui.Brush(DS.Rgb(0.36, 0.58, 1.0, h ? 0.26 : 0.18));
				};
				badge.Tip("Rename this label");
				badge.Click += delegate
				{
					editingLabelAccount = a.ConnectedAccountId;
					Render();
				};
				stackPanel.Children.Add(badge);
			}
		}
		FrameworkElement item2;
		if (!a.IsActive)
		{
			item2 = Ui.Text("Sign-in expired", 11.0, FontWeights.SemiBold, SK.Warning);
		}
		else if (flag)
		{
			item2 = Ui.Text("Press Enter to save", 11.0, FontWeights.Normal, SK.TextTertiary);
		}
		else
		{
			StackPanel stackPanel2 = Ui.H(6.0, StatusChip("Connected", SK.Positive));
			if (!isOnly && a.TrimmedLabel == null)
			{
				if (freeQuickLabels.Count == 0)
				{
					stackPanel2.Children.Add(LabelChip("Add label", delegate
					{
						editingLabelAccount = a.ConnectedAccountId;
						Render();
					}));
				}
				else
				{
					stackPanel2.Children.Add(Ui.Text("Label as", 11.0, FontWeights.Normal, SK.TextTertiary).Tip("Label your accounts so HeyClicky knows which one to use."));
					foreach (string quick in freeQuickLabels)
					{
						stackPanel2.Children.Add(LabelChip(quick, delegate
						{
							SaveLabelAsync(i, a, quick);
						}));
					}
					stackPanel2.Children.Add(LabelChip("Other…", delegate
					{
						editingLabelAccount = a.ConnectedAccountId;
						Render();
					}));
				}
			}
			else
			{
				List<string> list = new List<string>();
				if (a.IsOrganization)
				{
					list.Add("Organization");
				}
				DateTimeOffset? connectedDate = a.ConnectedDate;
				if (connectedDate.HasValue)
				{
					DateTimeOffset valueOrDefault = connectedDate.GetValueOrDefault();
					list.Add("since " + valueOrDefault.ToLocalTime().ToString((valueOrDefault.Year == DateTime.Now.Year) ? "MMM d" : "MMM d, yyyy", CultureInfo.InvariantCulture));
				}
				if (list.Count > 0)
				{
					stackPanel2.Children.Add(Ui.Text("· " + string.Join(" · ", list), 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 1));
				}
			}
			item2 = stackPanel2;
		}
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 2.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)stackPanel.Size(0.0, 20.0),
				(UIElement)item2
			}
		};
		bool flag2 = accountBusy.Contains(a.ConnectedAccountId) || (isOnly && integrationBusy.Contains(i.Id));
		FrameworkElement frameworkElement;
		if (a.IsActive)
		{
			if (flag2)
			{
				frameworkElement = new GelButton("Disconnect", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 15.0, null, null, null)
				{
					IsEnabledGel = false
				};
			}
			else if (confirmDisconnectAccount == a.ConnectedAccountId)
			{
				GelButton gelButton = new GelButton("Disconnect", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 15.0, null, null, null);
				gelButton.Click += delegate
				{
					DisconnectAsync(i, isOnly ? null : a);
				};
				GelButton gelButton2 = new GelButton("Keep", GelAppearance.PrimaryBlue, GelButton.Sizing.Compact, 15.0, null, null, null);
				gelButton2.Click += delegate
				{
					confirmDisconnectAccount = null;
					Render();
				};
				frameworkElement = Ui.H(6.0, gelButton, gelButton2);
			}
			else
			{
				GelButton gelButton3 = new GelButton("Disconnect", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 15.0, null, null, null);
				gelButton3.Click += delegate
				{
					confirmDisconnectAccount = a.ConnectedAccountId;
					Render();
				};
				frameworkElement = gelButton3;
			}
		}
		else
		{
			GelButton gelButton4 = new GelButton("", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 15.0, "\ue711", null, null);
			gelButton4.Tip("Remove this expired account");
			gelButton4.Click += delegate
			{
				DisconnectAsync(i, (i.Accounts.Count <= 1) ? null : a);
			};
			GelButton gelButton5 = new GelButton("Reconnect", GelAppearance.PrimaryBlue, GelButton.Sizing.Compact, 15.0, null, null, null);
			gelButton5.Tip("Sign in to this " + i.DisplayName + " account again");
			gelButton5.Click += delegate
			{
				ConnectAsync(i, i.IsActiveConnection);
			};
			frameworkElement = Ui.H(6.0, gelButton4, gelButton5);
		}
		Grid obj = new Grid
		{
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0),
			ColumnSpacing = 10.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition(),
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		grid.VerticalAlignment = VerticalAlignment.Center;
		obj.Children.Add(grid);
		Grid.SetColumn(stackPanel3, 1);
		obj.Children.Add(stackPanel3);
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetColumn(frameworkElement, 2);
		obj.Children.Add(frameworkElement);
		return obj;
	}

	private static Pressable LabelChip(string title, Action onClick)
	{
		MessagesPalette look = MessagesPalette.Current;
		Pressable p = new Pressable
		{
			CornerRadius = new CornerRadius(100.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.07)),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.08)),
			BorderThickness = new Thickness(1.0)
		};
		TextBlock t = Ui.Text(title, 10.5, FontWeights.Medium, SK.TextSecondary);
		p.Children.Add(t.Pad(7.0, 2.0, 7.0, 2.0));
		p.HoverChanged += delegate(bool h)
		{
			p.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.12 : 0.07));
			t.Foreground = Ui.Brush(h ? look.ThemeTextPrimary : look.ThemeTextSecondary);
		};
		p.Click += onClick;
		return p;
	}

	private static Color InitialColor(string identity)
	{
		uint num = 5381u;
		string text = identity.ToLowerInvariant();
		foreach (char c in text)
		{
			num = num * 33 + c;
		}
		double num2 = (double)(num % 360) / 360.0;
		double num3 = 0.42;
		double num4 = 0.58;
		int num5 = (int)(num2 * 6.0);
		double num6 = num2 * 6.0 - (double)num5;
		double num7 = num4 * (1.0 - num3);
		double num8 = num4 * (1.0 - num6 * num3);
		double num9 = num4 * (1.0 - (1.0 - num6) * num3);
		var (r, g, b) = (num5 % 6) switch
		{
			0 => (num4, num9, num7), 
			1 => (num8, num4, num7), 
			2 => (num7, num4, num9), 
			3 => (num7, num8, num4), 
			4 => (num9, num7, num4), 
			_ => (num4, num7, num8), 
		};
		return DS.Rgb(r, g, b);
	}

	private async Task ConnectAsync(AgentIntegration i, bool addingAnother = false)
	{
		if (i.IsLocalIntegration)
		{
			TrackIntegration("initiated", i, "agent_space_settings", null);
			if (ObsidianVault.Configure(Native.GetForegroundWindow()))
			{
				justConnected = i.Id;
				TrackIntegration("connected", ObsidianVault.Row(), "agent_space_settings", null);
			}
			FillIntegrationList();
			return;
		}
		AgentService agents = host.Agents;
		if (agents == null || integrationBusy.Contains(i.Id))
		{
			return;
		}
		integrationBusy.Add(i.Id);
		integrationError = null;
		Render();
		TrackIntegration("initiated", i, "agent_space_settings", null);
		try
		{
			string url = await agents.ConnectIntegrationAsync(i.PrimaryToolkitSlug);
			dq.TryEnqueue(delegate
			{
				integrationBusy.Remove(i.Id);
				awaitingConnection = (i.Id, i.ActiveAccounts.Count, DateTime.UtcNow);
				justConnected = null;
				Log.Info("settings", "integration connect: " + i.Id + (addingAnother ? " (another account)" : "") + " → browser");
				host.App?.FoldNotchForOpenedLink();
				host.OpenUrl(url);
				Render();
				PollAwaitedConnectionAsync();
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Log.Warn("settings", "integration connect " + i.Id + " failed: " + ex3.Message);
			dq.TryEnqueue(delegate
			{
				integrationBusy.Remove(i.Id);
				integrationError = ((ex3 is AgentBackendException ex4) ? AgentErrorText(ex4) : ex3.Message);
				Render();
			});
		}
	}

	private static void TrackIntegration(string kind, AgentIntegration i, string source = "agent_space_settings", int? deletedAccountCount = null)
	{
		List<string> toolkitSlugs = i.Toolkits.Select((AgentIntegrationToolkit t) => t.Slug).ToList();
		if (!(kind == "initiated"))
		{
			if (kind == "connected")
			{
				ClickyAnalytics.TrackIntegrationConnected(i.Id, i.DisplayName, toolkitSlugs, source, i.Status, i.ConnectedCount, i.TotalCount);
			}
			else
			{
				ClickyAnalytics.TrackIntegrationDisconnected(i.Id, i.DisplayName, toolkitSlugs, source, i.Status, i.ConnectedCount, i.TotalCount, deletedAccountCount);
			}
		}
		else
		{
			ClickyAnalytics.TrackIntegrationInitiated(i.Id, i.DisplayName, toolkitSlugs, source, i.Status, i.ConnectedCount, i.TotalCount);
		}
	}

	private static string AgentErrorText(AgentBackendException ex)
	{
		try
		{
			if (JsonNode.Parse(ex.Body, null) is JsonObject jsonObject)
			{
				string text = jsonObject["error"]?.ToString();
				if (text != null && text.Length > 0)
				{
					return text;
				}
			}
		}
		catch
		{
		}
		int length = ex.Body.Length;
		if (length <= 0 || length >= 300 || ex.Body.TrimStart().StartsWith('{'))
		{
			return "Something went wrong. Try again in a moment.";
		}
		return ex.Body;
	}

	private async Task DisconnectAsync(AgentIntegration i, AgentIntegrationAccount? account)
	{
		if (i.IsLocalIntegration)
		{
			confirmDisconnectIntegration = null;
			TrackIntegration("disconnected", i, "agent_space_settings", 1);
			ObsidianVault.Unset();
			if (justConnected == i.Id)
			{
				justConnected = null;
			}
			FillIntegrationList();
			return;
		}
		AgentService agents = host.Agents;
		if (agents == null)
		{
			return;
		}
		confirmDisconnectIntegration = null;
		confirmDisconnectAccount = null;
		if ((object)account == null)
		{
			integrationBusy.Add(i.Id);
		}
		else
		{
			accountBusy.Add(account.ConnectedAccountId);
		}
		Render();
		try
		{
			await agents.DisconnectIntegrationAsync(i.PrimaryToolkitSlug, account?.ConnectedAccountId);
			Log.Info("settings", "integration disconnected: " + i.Id + (((object)account == null) ? "" : (" account " + account.ConnectedAccountId)));
			TrackIntegration("disconnected", i, "settings", ((object)account != null) ? 1 : Math.Max(1, i.ActiveAccounts.Count));
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Log.Warn("settings", "integration disconnect " + i.Id + " failed: " + ex3.Message);
			dq.TryEnqueue(delegate
			{
				integrationError = ((ex3 is AgentBackendException ex4) ? AgentErrorText(ex4) : ex3.Message);
			});
		}
		dq.TryEnqueue(delegate
		{
			integrationBusy.Remove(i.Id);
			if ((object)account != null)
			{
				accountBusy.Remove(account.ConnectedAccountId);
			}
		});
		await RefreshIntegrationsAsync();
	}

	private async Task SaveLabelAsync(AgentIntegration i, AgentIntegrationAccount a, string label)
	{
		editingLabelAccount = null;
		string trimmed = label.Trim();
		if (!(trimmed == (a.TrimmedLabel ?? "")))
		{
			AgentService agents = host.Agents;
			if (agents != null)
			{
				accountBusy.Add(a.ConnectedAccountId);
				Render();
				try
				{
					await agents.LabelIntegrationAccountAsync(i.PrimaryToolkitSlug, a.ConnectedAccountId, trimmed);
					Log.Info("settings", $"integration label: {i.Id} {a.ConnectedAccountId} = \"{trimmed}\"");
				}
				catch (Exception ex)
				{
					Log.Warn("settings", "label save failed: " + ex.Message);
					dq.TryEnqueue(delegate
					{
						integrationError = "Couldn't save that label. Try again.";
					});
				}
				dq.TryEnqueue(delegate
				{
					accountBusy.Remove(a.ConnectedAccountId);
				});
				await RefreshIntegrationsAsync();
				return;
			}
		}
		Render();
	}

	private static FrameworkElement IntegrationsLoadingShimmer()
	{
		Border item = new Border
		{
			Width = 128.0,
			Height = 8.0,
			CornerRadius = new CornerRadius(4.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.08)),
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(4.0, 2.0, 4.0, 2.0)
		};
		StackPanel stackPanel = new StackPanel();
		for (int i = 0; i < ShimmerNameWidths.Length; i++)
		{
			if (i > 0)
			{
				stackPanel.Children.Add(SK.Divider(52.0));
			}
			stackPanel.Children.Add(ShimmerRow(ShimmerNameWidths[i], ShimmerDescriptionFractions[i]));
		}
		Grid card = new Grid
		{
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.05)),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.06)),
			BorderThickness = new Thickness(1.0)
		};
		card.Children.Add(stackPanel);
		Border band = new Border
		{
			Width = 160.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false,
			Background = Ui.Horizontal((0.0, SK.Ink.WithAlpha(0.0)), (0.5, SK.Ink.WithAlpha(0.06)), (1.0, SK.Ink.WithAlpha(0.0)))
		};
		card.Children.Add(band);
		Ui.ClipRounded(card, 14.0);
		if (!Anim.ReduceMotion && !Anim.Instant)
		{
			band.Loaded += delegate
			{
				double span = Math.Max(card.ActualWidth, 400.0) + 160.0;
				Anim.LoopVector3(band, "Translation", 1.6, (double t) => new Vector3((float)(-160.0 + span * (t / 1.6)), 0f, 0f));
			};
		}
		StackPanel obj = new StackPanel
		{
			Spacing = 7.0,
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)item,
				(UIElement)card
			}
		};
		AutomationProperties.SetName(obj, "Loading integrations");
		return obj;
	}

	private static FrameworkElement ShimmerRow(double nameWidth, double descriptionFraction)
	{
		Grid obj = new Grid
		{
			ColumnSpacing = 12.0,
			Padding = new Thickness(14.0, 12.0, 14.0, 12.0),
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition(),
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			},
			Children = { (UIElement)new Border
			{
				Width = 26.0,
				Height = 26.0,
				CornerRadius = new CornerRadius(7.0),
				Background = Ui.Brush(SK.Ink.WithAlpha(0.09))
			} }
		};
		Border description = new Border
		{
			Height = 7.0,
			CornerRadius = new CornerRadius(3.5),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.07)),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)new Border
				{
					Width = nameWidth,
					Height = 9.0,
					CornerRadius = new CornerRadius(4.5),
					Background = Ui.Brush(SK.Ink.WithAlpha(0.1)),
					HorizontalAlignment = HorizontalAlignment.Left
				},
				(UIElement)description
			}
		};
		stackPanel.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			description.Width = Math.Max(0.0, e.NewSize.Width * descriptionFraction);
		};
		Grid.SetColumn(stackPanel, 1);
		obj.Children.Add(stackPanel);
		Border border = new Border
		{
			Width = 64.0,
			Height = 24.0,
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.08)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(border, 2);
		obj.Children.Add(border);
		return obj;
	}

	[IteratorStateMachine(typeof(_003CProactiveDeveloperGroups_003Ed__130))]
	private IEnumerable<FrameworkElement> ProactiveDeveloperGroups()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CProactiveDeveloperGroups_003Ed__130(-2)
		{
			_003C_003E4__this = this
		};
	}

	public void RefreshReferralIfStale(double maximumAgeSeconds = 30.0)
	{
		if (referral != null)
		{
			DateTimeOffset? dateTimeOffset = referralLoadedAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (referralLoadedFor == s.Auth.User?.UserId && DateTimeOffset.UtcNow - valueOrDefault < TimeSpan.FromSeconds(maximumAgeSeconds))
				{
					return;
				}
			}
		}
		RefreshReferralAsync();
	}

	private async Task RefreshReferralAsync()
	{
		WorkerClient worker = host.Worker;
		if (worker == null || !host.IsSignedIn || referralLoading)
		{
			return;
		}
		referralLoading = true;
		ReferralRequestsForTests++;
		string account = s.Auth.User?.UserId;
		try
		{
			JsonObject r = await worker.GetReferralAsync();
			dq.TryEnqueue(delegate
			{
				referralLoading = false;
				if (!(s.Auth.User?.UserId != account))
				{
					bool flag = !JsonNode.DeepEquals(referral, r) || referralFailed != (r == null);
					if (r != null)
					{
						referralLoadedAt = DateTimeOffset.UtcNow;
						referralLoadedFor = account;
					}
					referral = r;
					referralFailed = r == null;
					Log.Info("settings", $"referral: enabled={r?["enabled"]}, code={r?["code"]}{(flag ? "" : " (unchanged)")}");
					if (flag && page == SettingsWindow.Page.Referral)
					{
						Render();
					}
				}
			});
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "/me/referral failed: " + ex.Message);
			dq.TryEnqueue(delegate
			{
				referralLoading = false;
				if (!(s.Auth.User?.UserId != account))
				{
					bool num = !referralFailed;
					referralFailed = true;
					if (num && page == SettingsWindow.Page.Referral)
					{
						Render();
					}
				}
			});
		}
	}

	private void ResetReferralIfAccountChanged()
	{
		if (referralLoadedFor != null && !(referralLoadedFor == s.Auth.User?.UserId))
		{
			referral = null;
			referralFailed = false;
			referralLoadedAt = null;
			referralLoadedFor = null;
		}
	}

	private void NoteReferralPageAppeared()
	{
		referralPageSettled = false;
		int generation = ++referralSettleGeneration;
		Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromMilliseconds(150L, 0L);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (generation == referralSettleGeneration)
			{
				referralPageSettled = true;
				if (page == SettingsWindow.Page.Referral)
				{
					Render();
				}
			}
		};
		dispatcherQueueTimer.Start();
	}

	public void SetReferralForTests(JsonObject body)
	{
		referral = body;
		referralFailed = false;
		if (page == SettingsWindow.Page.Referral)
		{
			Render();
		}
	}

	public void ScrollReferralForTests(double offset)
	{
		scroller.ChangeView(null, offset, null, disableAnimation: true);
	}

	private static string? Str(JsonNode? n)
	{
		if (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			return null;
		}
		return value;
	}

	private static int Int(JsonNode? n)
	{
		if (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<int>(out var value))
		{
			if (!(n is JsonValue jsonValue2) || !jsonValue2.TryGetValue<double>(out var value2))
			{
				return 0;
			}
			return (int)value2;
		}
		return value;
	}

	private static double Dbl(JsonNode? n)
	{
		if (n is JsonValue jsonValue)
		{
			long value2;
			int value3;
			if (!jsonValue.TryGetValue<double>(out var value))
			{
				return jsonValue.TryGetValue<long>(out value2) ? value2 : (jsonValue.TryGetValue<int>(out value3) ? value3 : 0);
			}
			return value;
		}
		return 0.0;
	}

	private static bool Bool(JsonNode? n)
	{
		bool value = default(bool);
		return n is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value;
	}

	private static string Dollars(int cents)
	{
		if (cents % 100 != 0)
		{
			return ((double)cents / 100.0).ToString("$#,0.00", CultureInfo.InvariantCulture);
		}
		return $"${cents / 100:N0}";
	}

	private FrameworkElement ReferralPage()
	{
		if (!host.IsSignedIn)
		{
			return ReferralFrame(new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(ReferralMessage("Refer a friend", "Sign in to get your referral link.")));
		}
		if (ReferralShowsSkeleton)
		{
			referralSkeletonShown = true;
			return ReferralFrame(new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(ReferralLoadingSkeleton()));
		}
		bool num = referralSkeletonShown && !Anim.ReduceMotion;
		referralSkeletonShown = false;
		FrameworkElement built = BuildReferralPage();
		if (num)
		{
			built.Opacity = 0.0;
			built.Loaded += delegate
			{
				Anim.Opacity(built, 1f, 0.2);
			};
		}
		return built;
	}

	private FrameworkElement BuildReferralPage()
	{
		if (referral == null)
		{
			return ReferralFrame(new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				ReferralMessage("Refer a friend", "HeyClicky couldn't load your referral page."),
				SK.PillButton("Try again", delegate
				{
					RefreshReferralAsync();
				})
			}));
		}
		if (!Bool(referral["enabled"]))
		{
			return ReferralFrame(new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(ReferralMessage("Referrals are paused", "We're not taking new referrals right now. Check back soon.")));
		}
		JsonObject terms = (referral["terms"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		JsonObject jsonObject = (referral["stats"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		bool flag = Int(jsonObject["referred_count"]) > 0;
		return ReferralFrame(new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[7]
		{
			HeldCodeCard(terms, jsonObject),
			NegativeBalanceNote(jsonObject),
			TermsCard(terms),
			flag ? EarningsGroup(jsonObject, terms) : null,
			FriendsGroup(jsonObject),
			EstimatorGroup(terms),
			PayoutGroup(jsonObject, terms)
		}));
	}

	private static FrameworkElement ReferralFrame(IEnumerable<FrameworkElement?> sections)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 26.0,
			MaxWidth = 640.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		foreach (FrameworkElement section in sections)
		{
			if ((object)section != null)
			{
				stackPanel.Children.Add(section);
			}
		}
		return new Grid
		{
			Padding = new Thickness(32.0, 0.0, 32.0, 36.0),
			Children = { (UIElement)stackPanel }
		};
	}

	private static FrameworkElement ReferralMessage(string title, string detail)
	{
		return new StackPanel
		{
			Spacing = 5.0,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0),
			Children = 
			{
				(UIElement)Ui.Text(title, 22.0, FontWeights.SemiBold, SK.TextPrimary),
				(UIElement)Ui.Text(detail, 12.0, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 0, wrap: true)
			}
		};
	}

	private static TextBlock CardLabel(string text)
	{
		TextBlock textBlock = Ui.Text(text.ToUpperInvariant(), 9.5, FontWeights.Bold, MessagesPalette.Current.TertiaryText);
		textBlock.CharacterSpacing = 84;
		return textBlock;
	}

	private FrameworkElement TermsCard(JsonObject terms)
	{
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0
		};
		for (int i = 0; i < 3; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}
		int num = Int(terms["commission_months"]);
		int num2 = Int(terms["customer_discount_months"]);
		double num3 = Dbl(terms["commission_percent"]);
		string item = "Of their monthly subscription.";
		JsonObject jsonObject = (referral?["estimator"]?["plans"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault();
		if (jsonObject != null)
		{
			int num4 = Int(jsonObject["monthly_price_usd_cents"]);
			if (num4 > 0)
			{
				int perReferralUsdCents = ReferralEarningsEstimator.Estimate(1, 1, num4, num3, num).PerReferralUsdCents;
				item = $"For example, a {ReferralFormatting.CompactDollars(num4)}/month plan earns you {ReferralFormatting.CompactDollars(perReferralUsdCents)} each month.";
			}
		}
		string value = (Bool(terms["customer_discount_applies_to_yearly_plans"]) ? "Pro or Max" : "Pro or Max on a monthly plan");
		(string, string, string)[] array = new(string, string, string)[3]
		{
			("For every friend", "Earn " + ReferralFormatting.Percent(num3), item),
			("Your friend gets", ReferralFormatting.Percent(Dbl(terms["customer_discount_percent"])) + " off", $"Their {((num2 == 1) ? "first month" : $"first {num2} months")} of {value}."),
			("Earn for up to", (num == 1) ? "1 month" : ((num > 0) ? $"{num} months" : "Forever"), "You earn from their first paid month, for as long as they stay subscribed.")
		};
		for (int j = 0; j < 3; j++)
		{
			(string, string, string) tuple = array[j];
			string item2 = tuple.Item1;
			string item3 = tuple.Item2;
			string item4 = tuple.Item3;
			FrameworkElement item5 = Ui.Shrink(Ui.Text(item3, 24.0, FontWeights.Black, MessagesPalette.Current.PrimaryText));
			TextBlock textBlock = Ui.Text(item4, 11.0, FontWeights.Medium, MessagesPalette.Current.TertiaryText, TextAlignment.Left, 0, wrap: true);
			textBlock.LineHeight = 16.130000000000003;
			textBlock.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
			Border border = new Border
			{
				CornerRadius = new CornerRadius(14.0),
				Background = Ui.Brush(SK.Ink.WithAlpha(0.05)),
				BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.06)),
				BorderThickness = new Thickness(1.0),
				Padding = new Thickness(13.0),
				Child = new StackPanel
				{
					Spacing = 3.0,
					Children = 
					{
						(UIElement)Ui.Text(item2, 12.0, FontWeights.Bold, MessagesPalette.Current.SecondaryText, TextAlignment.Left, 0, wrap: true),
						(UIElement)item5,
						(UIElement)textBlock
					}
				}
			};
			Grid.SetColumn(border, j);
			grid.Children.Add(border);
		}
		return grid;
	}

	private FrameworkElement HeldCodeCard(JsonObject terms, JsonObject stats)
	{
		double? num = base.XamlRoot?.Size.Width;
		double w;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > 0.0)
			{
				w = valueOrDefault;
				goto IL_005a;
			}
		}
		w = 940.0;
		goto IL_005a;
		IL_00ae:
		double h;
		HomeMetrics homeMetrics = HomeMetrics.For(w, h);
		double num2 = Math.Round(homeMetrics.SuggestionCharacterSize * 1.35);
		double num3 = Math.Round(homeMetrics.SuggestionHandWidth * 1.2);
		double num4 = num3 * 20.0 / 28.0;
		double num5 = Math.Round(num2 * 152.0 / 195.0);
		PortraitAppearance portraitAppearance = PortraitAppearance.HomeMascot(s.Settings.CursorColorHex);
		PortraitExpression expression = ((referralCopied == null) ? PortraitExpression.Happy : PortraitExpression.Excited);
		PortraitView face = new PortraitView(portraitAppearance, num2, num2, expression, showsAvatarBackground: false, (Environment.GetEnvironmentVariable("HEYCLICKY_E2E_NOGLOW") == "1") ? ((Color?)null) : new Color?(portraitAppearance.Hair), num2 * 0.08)
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Tag = "referral-hero-clicky"
		};
		if (referralCelebrate && !Anim.Instant && !Anim.ReduceMotion)
		{
			referralCelebrate = false;
			face.Loaded += delegate
			{
				face.Perform(PortraitReaction.Celebrate);
			};
		}
		referralCelebrate = false;
		Grid grid = new Grid
		{
			Height = num5,
			Width = num2,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(0.0, 0.0 - Math.Round(num2 * 0.11), 0.0, 0.0)
		};
		grid.Children.Add(face);
		grid.Clip = new RectangleGeometry
		{
			Rect = new Rect(-40.0, -60.0, num2 + 80.0, num5 + 60.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0
		};
		int num6 = Int(stats["earned_usd_cents"]);
		int num7 = Int(stats["paid_out_usd_cents"]);
		int num8 = Int(stats["available_usd_cents"]);
		bool flag = num6 > 0 || num7 > 0 || num8 < 0;
		stackPanel.Children.Add(flag ? EarnedMoneyBlock(stats, terms) : EarnPitchBlock(terms, homeMetrics));
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Background = Ui.Brush(SK.Ink.WithAlpha(0.07))
		});
		foreach (FrameworkElement item3 in LinkParts(terms))
		{
			stackPanel.Children.Add(item3);
		}
		Border item = new Border
		{
			CornerRadius = new CornerRadius(20.0),
			Background = Ui.Brush(MessagesPalette.Current.IncomingBubble),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.06)),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(homeMetrics.ContentPadding + 2.0),
			Child = stackPanel,
			Tag = "referral-held-card"
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = Math.Max(0.0, num2 / 2.0 - num3),
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(num2 / 4.0 - num3 / 2.0, (0.0 - num4) / 2.0, 0.0, 0.0)
		};
		for (int i = 0; i < 2; i++)
		{
			FrameworkElement frameworkElement = ClickyLedgeHand.Create(portraitAppearance, num3, num4);
			frameworkElement.CenterPoint = new Vector3((float)num3 / 2f, (float)num4 / 2f, 0f);
			frameworkElement.Rotation = ((i == 0) ? (-8) : 8);
			stackPanel2.Children.Add(frameworkElement);
		}
		Grid item2 = new Grid
		{
			Children = 
			{
				(UIElement)item,
				(UIElement)stackPanel2
			}
		};
		return new StackPanel
		{
			Children = 
			{
				(UIElement)grid,
				(UIElement)item2
			},
			Tag = "referral-hero"
		};
		IL_005a:
		num = base.XamlRoot?.Size.Height;
		if (num.HasValue)
		{
			double valueOrDefault2 = num.GetValueOrDefault();
			if (valueOrDefault2 > 0.0)
			{
				h = valueOrDefault2;
				goto IL_00ae;
			}
		}
		h = 595.0;
		goto IL_00ae;
	}

	private FrameworkElement EarnPitchBlock(JsonObject terms, HomeMetrics metrics)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 7.0
		};
		stackPanel.Children.Add(Ui.Text("Invite your friends and earn.", metrics.SuggestionTitleFontSize + 7.0, FontWeights.Medium, MessagesPalette.Current.PrimaryText, TextAlignment.Left, 0, wrap: true));
		string text = PitchSubtitle(terms);
		if (text != null)
		{
			stackPanel.Children.Add(Ui.Text(text, 12.5, FontWeights.Medium, MessagesPalette.Current.SecondaryText, TextAlignment.Left, 0, wrap: true));
		}
		return stackPanel;
	}

	private string? PitchSubtitle(JsonObject terms)
	{
		List<int> list = (from p in ((referral?["estimator"]?["plans"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select Int(p["monthly_price_usd_cents"]) into p
			where p > 0
			select p).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		double commission = Dbl(terms["commission_percent"]);
		int commissionMonths = Int(terms["commission_months"]);
		int yearMonths = ((commissionMonths > 0) ? Math.Min(12, commissionMonths) : 12);
		int num = list.Min((int price) => ReferralEarningsEstimator.Estimate(1, 1, price, commission, commissionMonths).PerReferralUsdCents);
		int num2 = list.Max((int price) => ReferralEarningsEstimator.Estimate(1, yearMonths, price, commission, commissionMonths).PerReferralUsdCents);
		int num3 = num - num % 100;
		int num4 = num2 - num2 % 100;
		string text = ReferralFormatting.WholeDollars(num3);
		string value = ((num4 > num3) ? (text + " to " + ReferralFormatting.WholeDollars(num4)) : ("up to " + text));
		return $"Earn {value} per friend. They get {ReferralFormatting.Percent(Dbl(terms["customer_discount_percent"]))} off.";
	}

	private FrameworkElement EarnedMoneyBlock(JsonObject stats, JsonObject terms)
	{
		int usdCents = Int(stats["earned_usd_cents"]);
		int num = Int(stats["available_usd_cents"]);
		int num2 = Int(stats["on_hold_usd_cents"]);
		int num3 = Int(terms["minimum_payout_usd_cents"]);
		int value = Int(terms["commission_hold_days"]);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(CardLabel("Total earned"));
		stackPanel.Children.Add(Ui.Text(ReferralFormatting.CompactDollars(usdCents), 40.0, FontWeights.Black, MessagesPalette.Current.PrimaryText, TextAlignment.Left, 1));
		StackPanel stackPanel2 = Ui.H(12.0);
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		string text = ReferralFormatting.NegativeBalanceExplanation(num, Int(stats["reversed_payment_count"]), Int(stats["reversed_usd_cents"]));
		stackPanel2.Children.Add(BalancePill(ReferralFormatting.CompactDollars(num), (num < 0) ? "to cover first" : "ready to pay out", (num < 0) ? GelAppearance.Peach : GelAppearance.Mint, text ?? ("Done waiting and all yours. Once it hits " + ReferralFormatting.CompactDollars(num3) + ", you can cash it out (\uff3e▽\uff3e)")));
		if (num2 > 0)
		{
			stackPanel2.Children.Add(BalancePill(ReferralFormatting.CompactDollars(num2), "on its way", GelAppearance.SecondaryWhite, $"Your friends paid, so this is yours. Each payment just sits for {value} days in case it gets refunded, then it moves over to ready to pay out ( \u02d8▽\u02d8)"));
		}
		Grid grid = new Grid
		{
			ColumnSpacing = 16.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel2, 1);
		grid.Children.Add(stackPanel);
		grid.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 12.0,
			Children = { (UIElement)grid }
		};
		if (num > 0 && num >= num3)
		{
			GelButton gelButton = new GelButton("Get paid", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, null, null)
			{
				HorizontalAlignment = HorizontalAlignment.Right
			};
			string email = Str(terms["payout_contact_email"]) ?? "hi@heyclicky.com";
			string code = Str(referral["code"]) ?? "";
			gelButton.Click += delegate
			{
				ClickyAnalytics.TrackReferralPayoutRequested("earnings_card", isAboveMinimum: true);
				host.OpenUrl($"mailto:{email}?subject={Uri.EscapeDataString("Referral payout (" + code + ")")}&body={Uri.EscapeDataString("Hi! I'd like a payout for my referral code " + code + ". The best way to pay me is: ")}");
			};
			stackPanel3.Children.Add(gelButton);
		}
		return stackPanel3;
	}

	private static FrameworkElement BalancePill(string amount, string label, GelAppearance gel, string help)
	{
		Grid grid = new Grid
		{
			Tag = "referral-balance"
		};
		grid.Children.Add(new GelSurface(gel, 13.0));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			Margin = new Thickness(13.0, 8.0, 13.0, 8.0)
		};
		stackPanel.Children.Add(Ui.Text(amount, 17.0, FontWeights.Bold, Colors.Black));
		stackPanel.Children.Add(Ui.H(6.0, Ui.Text(label, 10.5, FontWeights.SemiBold, Colors.Black.WithAlpha(0.6)), Icons.Glyph("\ue946", 10.0, Colors.Black.WithAlpha(0.45))));
		grid.Children.Add(stackPanel);
		ToolTipService.SetToolTip(grid, help);
		return grid;
	}

	[IteratorStateMachine(typeof(_003CLinkParts_003Ed__180))]
	private IEnumerable<FrameworkElement> LinkParts(JsonObject terms)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CLinkParts_003Ed__180(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__terms = terms
		};
	}

	private void CopyReferral(string what, string text)
	{
		DataPackage dataPackage = new DataPackage();
		dataPackage.SetText(text);
		Clipboard.SetContent(dataPackage);
		referralCopied = what;
		referralCelebrate = true;
		Log.Info("settings", "referral " + what + " copied");
		ClickyAnalytics.TrackReferralShareCopied(what);
		Render();
		Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1.6);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (referralCopied == what)
			{
				referralCopied = null;
				if (page == SettingsWindow.Page.Referral)
				{
					Render();
				}
			}
		};
		dispatcherQueueTimer.Start();
	}

	private async Task SaveReferralCodeAsync()
	{
		WorkerClient worker = host.Worker;
		if (worker == null)
		{
			return;
		}
		string draft = referralCodeDraft.Trim();
		try
		{
			ReferralOutcomeResponse result = await worker.ChangeReferralCodeAsync(draft);
			ClickyAnalytics.TrackReferralUsernameChanged(result?.Outcome ?? "request_failed");
			dq.TryEnqueue(delegate
			{
				bool flag;
				if ((object)result != null)
				{
					string outcome = result.Outcome;
					if (outcome == "changed" || outcome == "unchanged")
					{
						flag = true;
						goto IL_0034;
					}
				}
				flag = false;
				goto IL_0034;
				IL_0034:
				if (flag)
				{
					editingReferralCode = false;
					referralCodeProblem = "";
					referralCelebrate = true;
					Log.Info("settings", "referral code = " + draft);
					RefreshReferralAsync();
				}
				else
				{
					referralCodeProblem = result?.Message ?? "That name isn't available. Try another.";
				}
				Render();
			});
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "referral code change failed: " + ex.Message);
			ClickyAnalytics.TrackReferralUsernameChanged("request_failed");
			dq.TryEnqueue(delegate
			{
				referralCodeProblem = "Couldn't save that. Try again in a moment.";
				Render();
			});
		}
	}

	private FrameworkElement EarningsGroup(JsonObject stats, JsonObject terms)
	{
		int num = Int(terms["commission_hold_days"]);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Padding = new Thickness(14.0)
		};
		List<JsonObject> list = ((referral["monthly_earnings"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().ToList();
		if (list.Any((JsonObject m) => Int(m["earned_usd_cents"]) > 0))
		{
			stackPanel.Children.Add(new MoneyBarChart(list.Select((JsonObject m) => Int(m["earned_usd_cents"])).ToList(), list.Select((JsonObject m) => ReferralFormatting.ShortMonthLabel(Str(m["month"]) ?? "")).ToList(), Enumerable.Range(0, list.Count)));
		}
		Grid grid = new Grid();
		for (int i = 0; i < 4; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}
		FrameworkElement item = StatCell("Friends joined", Int(stats["referred_count"]).ToString(CultureInfo.InvariantCulture), null);
		FrameworkElement frameworkElement = StatCell("Paying you", Int(stats["paying_count"]).ToString(CultureInfo.InvariantCulture), null);
		FrameworkElement frameworkElement2 = StatCell("Last 30 days", ReferralFormatting.CompactDollars(Int(stats["earned_last_30_days_usd_cents"])), null);
		FrameworkElement frameworkElement3 = StatCell("Total paid out", ReferralFormatting.CompactDollars(Int(stats["paid_out_usd_cents"])), null);
		Grid.SetColumn(frameworkElement, 1);
		Grid.SetColumn(frameworkElement2, 2);
		Grid.SetColumn(frameworkElement3, 3);
		grid.Children.Add(item);
		grid.Children.Add(frameworkElement);
		grid.Children.Add(frameworkElement2);
		grid.Children.Add(frameworkElement3);
		stackPanel.Children.Add(grid);
		return SK.Group("Your earnings", (num > 0) ? $"Your share of each payment is ready to cash out {num} days after your friend pays. We wait because a payment can still be refunded during that time. Until then, it shows as on its way." : null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(stackPanel));
	}

	private FrameworkElement? NegativeBalanceNote(JsonObject stats)
	{
		string text = ReferralFormatting.NegativeBalanceExplanation(Int(stats["available_usd_cents"]), Int(stats["reversed_payment_count"]), Int(stats["reversed_usd_cents"]));
		if (text == null)
		{
			return null;
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			Children = 
			{
				(UIElement)Ui.Text("Why your balance is below zero", 12.5, FontWeights.Bold, SK.TextPrimary),
				(UIElement)Ui.Text(text, 12.0, FontWeights.Normal, SK.TextSecondary, TextAlignment.Left, 0, wrap: true)
			}
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		TextBlock textBlock = Icons.Glyph("\ue946", 14.0, SK.Warning);
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(textBlock);
		grid.Children.Add(stackPanel);
		return new Border
		{
			CornerRadius = new CornerRadius(16.0),
			Padding = new Thickness(14.0),
			Background = Ui.Brush(MessagesPalette.Current.IncomingBubble),
			BorderBrush = Ui.Brush(SK.Warning.WithAlpha(0.25)),
			BorderThickness = new Thickness(1.0),
			Child = grid
		};
	}

	private FrameworkElement? EstimatorGroup(JsonObject terms)
	{
		if (!(referral["estimator"] is JsonObject jsonObject))
		{
			return null;
		}
		List<ReferralEstimatorCard.Plan> list = (from p in ((jsonObject["plans"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select new ReferralEstimatorCard.Plan(Str(p["key"]) ?? "", Str(p["name"]) ?? "", Int(p["monthly_price_usd_cents"]), Int(p["default_referrals_per_month"])) into p
			where p.Key.Length > 0 && p.MonthlyPriceUsdCents > 0
			select p).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		ReferralEstimatorCard referralEstimatorCard = new ReferralEstimatorCard(list, Int(jsonObject["max_referrals_per_month"]), Int(jsonObject["default_months_retained"]), Dbl(terms["commission_percent"]), Int(terms["commission_months"]), estimatorState)
		{
			Tag = "referral-estimator"
		};
		return SK.Group("What you could make", referralEstimatorCard);
	}

	private static FrameworkElement StatCell(string title, string value, string? detail)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0,
			Children = 
			{
				(UIElement)Ui.Shrink(Ui.Text(value, 17.0, FontWeights.Bold, SK.TextPrimary)),
				(UIElement)Ui.Text(title, 10.5, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 2)
			}
		};
		if (detail != null)
		{
			stackPanel.Children.Add(Ui.Text(detail, 10.5, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 0, wrap: true));
		}
		return stackPanel;
	}

	private FrameworkElement FriendsGroup(JsonObject stats)
	{
		List<JsonObject> friends = ((referral["invited_friends"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().ToList();
		int num = Int(stats["referred_count"]);
		List<FrameworkElement> list = new List<FrameworkElement>();
		if (friends.Count == 0)
		{
			Grid grid = new Grid
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			grid.Children.Add(new GelSurface(GelAppearance.Peach, 100.0));
			TextBlock textBlock = Ui.Text("EXAMPLE", 9.0, FontWeights.Black, Colors.Black.WithAlpha(0.75));
			textBlock.CharacterSpacing = 80;
			textBlock.Margin = new Thickness(7.0, 3.0, 7.0, 3.0);
			grid.Children.Add(textBlock);
			TextBlock textBlock2 = Ui.Text("Nobody has joined yet. Here's how your list will look once friends do.", 12.0, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true);
			textBlock2.VerticalAlignment = VerticalAlignment.Center;
			Grid grid2 = new Grid
			{
				ColumnSpacing = 6.0,
				Padding = new Thickness(14.0, 11.0, 14.0, 11.0),
				Tag = "referral-example"
			};
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition());
			Grid.SetColumn(textBlock2, 1);
			grid2.Children.Add(grid);
			grid2.Children.Add(textBlock2);
			list.Add(grid2);
			(string, string, string, string, string)[] sampleFriends = SampleFriends;
			for (int i = 0; i < sampleFriends.Length; i++)
			{
				(string, string, string, string, string) tuple = sampleFriends[i];
				list.Add(FriendRow(tuple.Item1, SampleFriendPhoto(tuple.Item2), tuple.Item3, tuple.Item4, tuple.Item5, "Example: " + tuple.Item1 + ", " + PlanLabel(tuple.Item3)));
			}
		}
		foreach (JsonObject item in friends.Take(referralVisibleFriends))
		{
			string name = Str(item["display_name"]) ?? "A friend";
			string plan = Str(item["plan"]) ?? "free";
			int? num2 = ((item["subscribed_months"] == null) ? ((int?)null) : new int?(Int(item["subscribed_months"])));
			DateTimeOffset result;
			DateTimeOffset? dateTimeOffset = (DateTimeOffset.TryParse(Str(item["joined_at"]), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result) ? new DateTimeOffset?(result) : ((DateTimeOffset?)null));
			string detail = string.Join(" · ", new string[2]
			{
				dateTimeOffset.HasValue ? ("Joined " + dateTimeOffset.GetValueOrDefault().ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)) : null,
				(num2.HasValue && num2.GetValueOrDefault() > 0) ? $"Subscribed for {num2} month{((num2 == 1) ? "" : "s")}" : "Joined through your link"
			}.Where((string x) => x != null));
			int num3 = Int(item["earned_usd_cents"]);
			list.Add(FriendRow(name, Str(item["avatar_url"]), plan, detail, (num3 > 0) ? ("+" + Dollars(num3)) : null, null));
		}
		string label = ((friends.Count > 0) ? $"People you've invited · {num}" : "People you've invited");
		int num4 = Math.Max(0, friends.Count - referralVisibleFriends);
		StackPanel stackPanel = SK.Group(label, (num4 == 0 && friends.Count > 0 && friends.Count < num) ? $"Showing your latest {friends.Count} of {num}." : null, list, 56.0);
		FrameworkElement frameworkElement = null;
		if (num4 > 0)
		{
			Pressable pressable = SK.PillButton($"Show {Math.Min(10, num4)} more", delegate
			{
				referralVisibleFriends += 10;
				Render();
			});
			frameworkElement = Ui.H(8.0, pressable);
			if (num4 > 10)
			{
				((StackPanel)frameworkElement).Children.Add(SK.PillButton($"Show all {friends.Count}", delegate
				{
					referralVisibleFriends = friends.Count;
					Render();
				}));
			}
		}
		else if (referralVisibleFriends > 10 && friends.Count > 10)
		{
			frameworkElement = Ui.H(8.0, SK.PillButton("Show less", delegate
			{
				referralVisibleFriends = 10;
				Render();
			}));
		}
		if ((object)frameworkElement != null && stackPanel.Children.OfType<Border>().FirstOrDefault()?.Child is StackPanel stackPanel2)
		{
			frameworkElement.Margin = new Thickness(14.0, 10.0, 14.0, 10.0);
			stackPanel2.Children.Add(SK.Divider());
			stackPanel2.Children.Add(frameworkElement);
		}
		return stackPanel;
	}

	private static string? SampleFriendPhoto(string asset)
	{
		string text = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Referral", asset + ".png");
		if (!File.Exists(text))
		{
			return null;
		}
		return new Uri(text).AbsoluteUri;
	}

	private static string PlanLabel(string plan)
	{
		return plan switch
		{
			"max" => "Max", 
			"pro" => "Pro", 
			"canceled" => "Canceled", 
			_ => "Free", 
		};
	}

	private static FrameworkElement FriendRow(string name, string? avatarUrl, string plan, string detail, string? earnedText, string? accessibleName)
	{
		Grid item = HomeParts.AccountAvatar(30.0, name.Substring(0, 1).ToUpperInvariant(), avatarUrl);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text(name, 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 1),
				(UIElement)Ui.Text(detail, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 1)
			}
		};
		StackPanel stackPanel2 = Ui.H(12.0);
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		if (earnedText != null)
		{
			StackPanel stackPanel3 = Ui.H(4.0, Ui.Text("You earned", 11.0, FontWeights.Normal, SK.TextTertiary), Ui.Text(earnedText, 13.0, FontWeights.Medium, SK.TextPrimary));
			stackPanel3.VerticalAlignment = VerticalAlignment.Center;
			stackPanel2.Children.Add(stackPanel3);
		}
		stackPanel2.Children.Add(PlanChip(plan));
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 9.0, 14.0, 9.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(stackPanel2, 2);
		grid.Children.Add(item);
		grid.Children.Add(stackPanel);
		grid.Children.Add(stackPanel2);
		if (accessibleName != null)
		{
			AutomationProperties.SetName(grid, accessibleName);
		}
		return grid;
	}

	private static FrameworkElement PlanChip(string plan)
	{
		GelAppearance gelAppearance = ((!(plan == "pro")) ? ((!(plan == "max")) ? GelAppearance.SecondaryOutline : GelAppearance.Peach) : GelAppearance.PrimaryBlue);
		GelAppearance gelAppearance2 = gelAppearance;
		bool flag = gelAppearance2 == GelAppearance.SecondaryOutline;
		TextBlock textBlock = Ui.Text(PlanLabel(plan), 10.5, FontWeights.Bold, flag ? Colors.White.WithAlpha((plan == "canceled") ? 0.55 : 0.82) : Colors.Black.WithAlpha(0.85), TextAlignment.Center);
		textBlock.MinWidth = 34.0;
		Grid obj = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)new GelSurface(gelAppearance2, 100.0) }
		};
		FrameworkElement frameworkElement = (flag ? ((FrameworkElement)textBlock) : ((FrameworkElement)Ui.Embossed(textBlock, 0.7, 1.0)));
		frameworkElement.Margin = new Thickness(9.0, 4.0, 9.0, 4.0);
		obj.Children.Add(frameworkElement);
		return obj;
	}

	private FrameworkElement PayoutGroup(JsonObject stats, JsonObject terms)
	{
		int num = Int(stats["available_usd_cents"]);
		int num2 = Int(terms["minimum_payout_usd_cents"]);
		bool canRequest = num >= num2 && num2 > 0;
		string email = Str(terms["payout_contact_email"]) ?? "hi@heyclicky.com";
		string code = Str(referral["code"]) ?? "";
		string subtitle = ((num < 0) ? $"Your next earnings cover the {Dollars(-num)} first, then we pay out monthly once you've earned {Dollars(num2)}." : ("Real money, not credits. We pay out monthly once you've earned " + Dollars(num2) + "; smaller balances roll over."));
		Pressable trailing = SK.PillButton(canRequest ? "Request payout" : "Email us", delegate
		{
			OpenPayoutMail(canRequest, $"mailto:{email}?subject={Uri.EscapeDataString("Referral payout (" + code + ")")}&body={Uri.EscapeDataString("Hi! I'd like a payout for my referral code " + code + ". The best way to pay me is: ")}");
		}, canRequest);
		List<FrameworkElement> list = new List<FrameworkElement> { SK.Row("Get paid", subtitle, trailing, null) };
		string termsUrl = Str(terms["terms_url"]);
		if (termsUrl != null)
		{
			list.Add(SK.ActionRow("Referral terms", "What counts, what doesn't, and how payouts work.", delegate
			{
				host.OpenUrl(termsUrl);
			}, null, "arrow.up.right"));
		}
		return SK.Group("Getting paid", null, list);
	}

	private void OpenPayoutMail(bool isAboveMinimum, string mailto)
	{
		ClickyAnalytics.TrackReferralPayoutRequested("getting_paid_row", isAboveMinimum);
		host.OpenUrl(mailto);
	}

	private FrameworkElement FriendCodeGroup()
	{
		(Grid Box, TextBox Input) tuple = SK.SearchField("Friend's code", friendCodeDraft, delegate(string t)
		{
			friendCodeDraft = t;
		}, magnifier: false, 12.5, null);
		Grid item = tuple.Box;
		TextBox item2 = tuple.Input;
		GelButton gelButton = new GelButton("Apply", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, null, null);
		gelButton.Click += delegate
		{
			ClaimFriendCodeAsync();
		};
		item2.KeyDown += delegate(object _, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				_ = ClaimFriendCodeAsync();
			}
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(gelButton, 1);
		grid.Children.Add(item);
		grid.Children.Add(gelButton);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(14.0),
			Children = 
			{
				(UIElement)Ui.Text("Enter their code to get your discount on Pro or Max.", 11.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true),
				(UIElement)grid
			}
		};
		if (friendCodeMessage.Length > 0)
		{
			stackPanel.Children.Add(Ui.Text(friendCodeMessage, 11.0, FontWeights.SemiBold, SK.TextSecondary, TextAlignment.Left, 0, wrap: true));
		}
		return SK.Group("Were you invited by a friend?", stackPanel);
	}

	private async Task ClaimFriendCodeAsync()
	{
		WorkerClient worker = host.Worker;
		if (worker == null || friendCodeDraft.Trim().Length == 0)
		{
			return;
		}
		try
		{
			ReferralOutcomeResponse result = await worker.ClaimReferralAsync(friendCodeDraft.Trim(), "code");
			ClickyAnalytics.TrackReferralFriendCodeClaimResult("code", result?.Outcome ?? "request_failed");
			dq.TryEnqueue(delegate
			{
				friendCodeMessage = ((result?.Outcome == "claimed") ? "Done! Your discount is waiting at checkout." : (result?.Message ?? "That code didn't work."));
				Log.Info("settings", "referral claim: " + result?.Outcome);
				RefreshReferralAsync();
				Render();
			});
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "referral claim failed: " + ex.Message);
			ClickyAnalytics.TrackReferralFriendCodeClaimResult("code", "request_failed");
			dq.TryEnqueue(delegate
			{
				friendCodeMessage = "Couldn't check that code. Try again in a moment.";
				Render();
			});
		}
	}

	private static FrameworkElement ReferralLoadingSkeleton()
	{
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0,
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.Children.Add(new Border
		{
			Height = 40.0,
			CornerRadius = new CornerRadius(20.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.08))
		});
		Border border = new Border
		{
			Width = 130.0,
			Height = 40.0,
			CornerRadius = new CornerRadius(20.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.08))
		};
		Grid.SetColumn(border, 1);
		grid.Children.Add(border);
		Grid grid2 = new Grid
		{
			Padding = new Thickness(22.0)
		};
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.Children.Add(new StackPanel
		{
			Spacing = 12.0,
			Children = 
			{
				(UIElement)SkeletonBar(150.0, 10.0),
				(UIElement)SkeletonBar(260.0, 22.0),
				(UIElement)SkeletonBar(220.0, 9.0)
			}
		});
		Grid.SetRow(grid, 2);
		grid2.Children.Add(grid);
		Grid grid3 = new Grid
		{
			ColumnSpacing = 10.0
		};
		for (int i = 0; i < 3; i++)
		{
			grid3.ColumnDefinitions.Add(new ColumnDefinition());
			FrameworkElement frameworkElement = SkeletonBlock(104.0, 14.0, new StackPanel
			{
				Spacing = 8.0,
				Padding = new Thickness(14.0),
				Children = 
				{
					(UIElement)SkeletonBar(84.0, 8.0),
					(UIElement)SkeletonBar(70.0, 18.0),
					(UIElement)SkeletonBar(110.0, 7.0)
				}
			});
			Grid.SetColumn(frameworkElement, i);
			grid3.Children.Add(frameworkElement);
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0,
			Padding = new Thickness(16.0)
		};
		double[] referralSkeletonNameWidths = ReferralSkeletonNameWidths;
		foreach (double width in referralSkeletonNameWidths)
		{
			Grid grid4 = new Grid
			{
				ColumnSpacing = 12.0
			};
			grid4.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid4.ColumnDefinitions.Add(new ColumnDefinition());
			grid4.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid4.Children.Add(new Ellipse
			{
				Width = 30.0,
				Height = 30.0,
				Fill = Ui.Brush(SK.Ink.WithAlpha(0.09))
			});
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 6.0,
				VerticalAlignment = VerticalAlignment.Center,
				Children = 
				{
					(UIElement)SkeletonBar(width, 9.0),
					(UIElement)SkeletonBar(70.0, 7.0)
				}
			};
			Grid.SetColumn(stackPanel2, 1);
			grid4.Children.Add(stackPanel2);
			Border border2 = new Border
			{
				Width = 48.0,
				Height = 20.0,
				CornerRadius = new CornerRadius(10.0),
				Background = Ui.Brush(SK.Ink.WithAlpha(0.08)),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(border2, 2);
			grid4.Children.Add(border2);
			stackPanel.Children.Add(grid4);
		}
		StackPanel obj = new StackPanel
		{
			Spacing = 18.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			IsHitTestVisible = false,
			Tag = "referral-skeleton",
			Children = 
			{
				(UIElement)SkeletonBlock(250.0, 22.0, grid2),
				(UIElement)grid3,
				(UIElement)SkeletonBlock(170.0, 14.0, stackPanel)
			}
		};
		AutomationProperties.SetName(obj, "Loading Invite & Earn");
		return obj;
	}

	private static FrameworkElement SkeletonBar(double width, double height)
	{
		return new Border
		{
			Width = width,
			Height = height,
			CornerRadius = new CornerRadius(height / 2.0),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.09)),
			HorizontalAlignment = HorizontalAlignment.Left
		};
	}

	private static FrameworkElement SkeletonBlock(double height, double cornerRadius, FrameworkElement content)
	{
		Grid grid = new Grid
		{
			Height = height,
			CornerRadius = new CornerRadius(cornerRadius),
			Background = Ui.Brush(SK.Ink.WithAlpha(0.05)),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.06)),
			BorderThickness = new Thickness(1.0)
		};
		grid.Children.Add(content);
		Border band = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false,
			Background = Ui.Horizontal((0.0, SK.Ink.WithAlpha(0.0)), (0.5, SK.Ink.WithAlpha(0.06)), (1.0, SK.Ink.WithAlpha(0.0)))
		};
		grid.Children.Add(band);
		grid.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			double width = e.NewSize.Width;
			if (!(width <= 0.0))
			{
				band.Width = width * 0.7;
				band.Translation = new Vector3((float)(0.0 - width), 0f, 0f);
				if (!Anim.Instant)
				{
					Anim.LoopVector3(band, "Translation", 1.4, (double t) => new Vector3((float)(width * (-1.0 + 2.3 * (t / 1.4))), 0f, 0f));
				}
			}
		};
		Ui.ClipRounded(grid, cornerRadius);
		return grid;
	}

	private FrameworkElement RoutinesPage()
	{
		AgentService agents = host.Agents;
		if (agents != null && !routinesHooked)
		{
			routinesHooked = true;
			agents.RoutinesChanged += delegate
			{
				dq.TryEnqueue(delegate
				{
					if (page == SettingsWindow.Page.Routines && isShown)
					{
						RenderSoon();
					}
				});
			};
		}
		List<ClickyRoutine> list = (agents?.Routines.Routines ?? Array.Empty<ClickyRoutine>()).OrderBy((ClickyRoutine r) => r.CreatedAt).ToList();
		List<FrameworkElement> list2 = new List<FrameworkElement> { RoutineVoiceHint(list.Count > 0) };
		List<FrameworkElement> list3 = new List<FrameworkElement>();
		foreach (ClickyRoutine item in list)
		{
			if (!routineRows.TryGetValue(item.Id, out RoutineSettingsRow value))
			{
				value = (routineRows[item.Id] = new RoutineSettingsRow(this, item.Id));
			}
			value.Update(item);
			if (value.Parent is Panel panel)
			{
				panel.Children.Remove(value);
			}
			list3.Add(value);
		}
		HashSet<string> live = list.Select((ClickyRoutine r) => r.Id).ToHashSet();
		foreach (string item2 in routineRows.Keys.Where((string k) => !live.Contains(k)).ToList())
		{
			routineRows.Remove(item2);
		}
		if (list3.Count > 0)
		{
			list2.Add(SK.Group(null, "Routines run on this PC while it's awake and HeyClicky is open.", list3));
		}
		return SK.Scaffold("Routines", "Things HeyClicky does on repeat, on this PC.", list2);
	}

	private FrameworkElement RoutineVoiceHint(bool hasRoutines)
	{
		TextBlock textBlock = Ui.Text("Hold", 12.0, FontWeights.Normal, SK.TextTertiary);
		StackPanel stackPanel = SK.KeyCaps(host.Current.EffectiveHotkeys.PushToTalk);
		TextBlock textBlock2 = Ui.Text(hasRoutines ? "and tell HeyClicky to add, change, pause or delete a routine." : "and tell HeyClicky what to do, and when.", 12.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 0, wrap: true);
		Grid grid = new Grid
		{
			ColumnSpacing = 6.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(textBlock2, 2);
		FrameworkElement[] array = new FrameworkElement[3] { textBlock, stackPanel, textBlock2 };
		foreach (FrameworkElement frameworkElement in array)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(frameworkElement);
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 6.0,
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0),
			Children = { (UIElement)grid }
		};
		if (!hasRoutines)
		{
			stackPanel2.Children.Add(Ui.Text("“Every morning at 9, check my inbox for anything from investors and sum it up.”", 12.0, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true));
		}
		return stackPanel2;
	}

	private void SetMonitorRecording(bool on)
	{
		ShortcutMonitor monitor = host.Monitor;
		if (monitor != null)
		{
			if (on && !recordingHooked)
			{
				monitor.RecordingModifiersChanged += OnRecordingModifiers;
				monitor.RecordingKeyPressed += OnRecordingKey;
				recordingHooked = true;
			}
			else if (!on && recordingHooked)
			{
				monitor.RecordingModifiersChanged -= OnRecordingModifiers;
				monitor.RecordingKeyPressed -= OnRecordingKey;
				recordingHooked = false;
			}
			monitor.IsRecording = on;
		}
	}

	private void OnRecordingModifiers(HotkeyModifiers held, uint _)
	{
		dq.TryEnqueue(delegate
		{
			recorder?.IngestModifiers(held);
		});
	}

	private void OnRecordingKey()
	{
		dq.TryEnqueue(delegate
		{
			recorder?.IngestKey();
		});
	}

	private void CancelShortcutRecording()
	{
		recorder?.Cancel();
		conflictRow = null;
		SetMonitorRecording(on: false);
	}

	private FrameworkElement ShortcutsPage()
	{
		HotkeyBindings bindings = host.Read((AppSettings x) => x.EffectiveHotkeys);
		return SK.Scaffold("Shortcuts", "The keys that summon HeyClicky.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[3]
		{
			SK.Group("Talk to HeyClicky", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				RecorderRow(bindings, ShortcutRecorder.Slot.PushToTalk, "Push to talk", "Hold to talk to HeyClicky. Release to send."),
				RecorderRow(bindings, ShortcutRecorder.Slot.TextMode, "Text mode", "Press to type to HeyClicky instead of talking.")
			})),
			SK.Group("Dictation", null, new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
			{
				RecorderRow(bindings, ShortcutRecorder.Slot.Dictation, "Dictate", "Hold and speak. Release to insert what you said at your cursor."),
				RecorderRow(bindings, ShortcutRecorder.Slot.HandsFreeDictation, "Hands-free dictation", "Dictate without holding a key down. Press to start, press again to stop.")
			})),
			SK.Group(null, null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(SK.ActionRow("Reset to defaults", null, delegate
			{
				CancelShortcutRecording();
				host.Set("shortcuts", "reset_defaults", delegate(AppSettings x)
				{
					x.Hotkeys = null;
				});
			}, null, null)))
		}));
	}

	private FrameworkElement RecorderRow(HotkeyBindings bindings, ShortcutRecorder.Slot slot, string title, string subtitle)
	{
		HotkeyBinding binding = HotkeyRules.Get(bindings, slot);
		ShortcutRecorder shortcutRecorder = recorder;
		bool flag = shortcutRecorder != null && shortcutRecorder.Mode == slot;
		StackPanel stackPanel = Ui.H(flag ? 8 : 10);
		if (flag)
		{
			HotkeyBinding captured = shortcutRecorder.Captured;
			if ((object)captured != null)
			{
				stackPanel.Children.Add(SK.KeyCaps(captured, shortcutRecorder.IsLocked));
			}
			else
			{
				stackPanel.Children.Add(Ui.Text("Press your keys…", 11.5, FontWeights.Medium, SK.TextTertiary).Align(HorizontalAlignment.Left, VerticalAlignment.Center));
			}
			if ((object)shortcutRecorder.Captured != null && shortcutRecorder.IsLocked)
			{
				GelButton gelButton = new GelButton("Save", GelAppearance.PrimaryBlue, GelButton.Sizing.Compact, 15.0, null, null, null);
				gelButton.Click += delegate
				{
					SaveRecorded(slot);
				};
				stackPanel.Children.Add(gelButton);
			}
			GelButton gelButton2 = new GelButton("Cancel", GelAppearance.SecondaryWhite, GelButton.Sizing.Compact, 15.0, null, null, null);
			gelButton2.Click += delegate
			{
				conflictRow = null;
				CancelShortcutRecording();
				Render();
			};
			stackPanel.Children.Add(gelButton2);
		}
		else
		{
			stackPanel.Children.Add(SK.KeyCaps(binding));
			GelButton gelButton3 = new GelButton("Change", GelAppearance.SecondaryWhite, GelButton.Sizing.Compact, 15.0, null, null, null);
			gelButton3.Click += delegate
			{
				conflictRow = null;
				Recorder.Begin(slot);
			};
			stackPanel.Children.Add(gelButton3);
		}
		foreach (FrameworkElement item2 in stackPanel.Children.OfType<FrameworkElement>())
		{
			item2.VerticalAlignment = VerticalAlignment.Center;
		}
		StackPanel item = new StackPanel
		{
			Spacing = 2.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text(title, 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 0, wrap: true),
				(UIElement)Ui.Text(subtitle, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 0, wrap: true)
			}
		};
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetColumn(stackPanel, 1);
		Grid grid = new Grid
		{
			ColumnSpacing = 24.0,
			Tag = "shortcut-row",
			Children = 
			{
				(UIElement)item,
				(UIElement)stackPanel
			}
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid grid2 = new Grid
		{
			Padding = new Thickness(14.0, 11.0, 14.0, 11.0),
			Children = { (UIElement)grid }
		};
		object obj;
		if (flag)
		{
			string hint = shortcutRecorder.Hint;
			if (hint != null)
			{
				obj = hint;
				goto IL_03c8;
			}
		}
		if (conflictRow == slot)
		{
			ShortcutRecorder.Slot? slot2 = conflictWith;
			if (slot2.HasValue)
			{
				ShortcutRecorder.Slot valueOrDefault = slot2.GetValueOrDefault();
				HotkeyBinding hotkeyBinding = shortcutRecorder?.Captured;
				if ((object)hotkeyBinding != null)
				{
					obj = ShortcutCaptureRules.ConflictLine(hotkeyBinding, HotkeyRules.Title(valueOrDefault), HotkeyRules.Get(bindings, valueOrDefault));
					goto IL_03c8;
				}
			}
		}
		obj = null;
		goto IL_03c8;
		IL_03c8:
		string text = (string)obj;
		if (text == null)
		{
			return grid2;
		}
		TextBlock textBlock = Ui.Text(text, 10.5, FontWeights.SemiBold, SK.Destructive, TextAlignment.Left, 0, wrap: true);
		textBlock.Margin = new Thickness(14.0, -5.0, 14.0, 11.0);
		return new StackPanel
		{
			Children = 
			{
				(UIElement)grid2,
				(UIElement)textBlock
			}
		};
	}

	private void SaveRecorded(ShortcutRecorder.Slot slot)
	{
		HotkeyBinding hotkeyBinding = recorder?.Captured;
		if ((object)hotkeyBinding == null)
		{
			return;
		}
		HotkeyBindings b = host.Read((AppSettings x) => x.EffectiveHotkeys);
		ShortcutRecorder.Slot? slot2 = HotkeyRules.Conflict(b, slot, hotkeyBinding);
		if (slot2.HasValue)
		{
			ShortcutRecorder.Slot valueOrDefault = slot2.GetValueOrDefault();
			conflictRow = slot;
			conflictWith = valueOrDefault;
			Log.Info("settings", $"shortcut {slot} = {hotkeyBinding.DisplayText} rejected: used by {valueOrDefault}");
			Render();
		}
		else
		{
			conflictRow = null;
			CancelShortcutRecording();
			HotkeyBindings updated = HotkeyRules.With(b, slot, hotkeyBinding);
			host.Set($"shortcut_{slot}", hotkeyBinding.DisplayText, delegate(AppSettings x)
			{
				x.Hotkeys = updated;
			});
		}
	}

	private FrameworkElement UpdatesGroup()
	{
		IUpdateService updates = s.Updates;
		ContentControl rowHost = new ContentControl
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			IsTabStop = false
		};
		Fill();
		Microsoft.UI.Dispatching.DispatcherQueue dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		rowHost.Loaded += delegate
		{
			updates.Changed -= OnChanged;
			updates.Changed += OnChanged;
			Fill();
		};
		rowHost.Unloaded += delegate
		{
			updates.Changed -= OnChanged;
		};
		Pressable pressable = SK.ActionRow("What's new", "The changelog of everything shipping in HeyClicky.", delegate
		{
			ClickyAnalytics.TrackChangelogOpened("agent_space_settings");
			host.OpenUrl(SettingsHost.Links.Changelog ?? AppHost.ChangelogUrl);
		}, null, "arrow.up.right");
		return SK.Group("Updates", rowHost, pressable);
		void Fill()
		{
			using (MessagesPalette.Use(look))
			{
				GelButton gelButton = new GelButton(updates.ButtonTitle, (!updates.ButtonIsPrimary) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 15.0, null, null, null);
				AutomationProperties.SetName(gelButton, updates.ButtonTitle);
				GelButton gelButton2 = gelButton;
				bool flag;
				switch (updates.State)
				{
				case UpdateState.Unavailable:
				case UpdateState.Checking:
				case UpdateState.Downloading:
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				gelButton2.Enabled = !flag;
				gelButton.Click += delegate
				{
					updates.PerformRowAction("agent_space_settings");
				};
				rowHost.Content = SK.Row(updates.RowTitle, updates.RowSubtitle, gelButton, null);
			}
		}
		void OnChanged(object? sender, EventArgs e)
		{
			dq.TryEnqueue(Fill);
		}
	}

	private static VoiceLanguage? LanguageForPreviews(string? stored)
	{
		return VoiceCatalog.LanguageForPreviews(stored);
	}

	private FrameworkElement VoicePage()
	{
		HookAudioDeviceChanges();
		AppSettings current = host.Current;
		StackPanel stackPanel = Ui.H(6.0);
		(double, string)[] speeds = Speeds;
		for (int j = 0; j < speeds.Length; j++)
		{
			var (multiplier, label) = speeds[j];
			stackPanel.Children.Add(SK.Chip(label, Math.Abs(current.SpeechSpeed - multiplier) < 0.001, gelSelection: true, delegate
			{
				try
				{
					if ((object)voicePreview != null)
					{
						voicePreview.PlaybackSession.PlaybackRate = multiplier;
					}
				}
				catch
				{
				}
				host.Set("voice_speed", multiplier, delegate(AppSettings x)
				{
					x.SpeechSpeed = multiplier;
				});
			}));
		}
		stackPanel.Margin = new Thickness(14.0);
		previewSpinners.Clear();
		IEnumerable<(string, string, string, string, string, Color)> voices = Voices.Select(((string Id, string Name, string Subtitle, string Fill, string Stroke, Color Color) v) => (Id: v.Id, Name: v.Name, Subtitle: v.Subtitle, Fill: v.Fill, Stroke: v.Stroke, Color: v.Color));
		IEnumerable<(string, string, string, string, string, Color)> voices2 = VoiceCatalog.ExpressiveVoices.Select((ExpressiveVoice v, int i) => (Id: v.Id, Name: v.Name, Subtitle: v.Subtitle, Fill: ExpressiveGlyphs[i].Fill, Stroke: ExpressiveGlyphs[i].Stroke, Color: ExpressiveGlyphs[i].Color));
		FrameworkElement frameworkElement = VoiceSection("More voices", "Good at English", voices, current.VoiceIdentifier);
		FrameworkElement frameworkElement2 = VoiceSection("Voices", "Expressive, with a native accent in most languages", voices2, current.VoiceIdentifier);
		StartPreviewSpinnerTimer();
		PrefetchExpressivePreviews();
		return SK.Scaffold("Voice", "How HeyClicky speaks. Changes apply from the next spoken reply.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[6]
		{
			SK.Group("Captions", null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(SK.ToggleRow("Captions while HeyClicky talks", "Show what HeyClicky says at the bottom of your screen, word by word.", current.DrawingVoiceCaptions, delegate(bool v)
			{
				Quiet("screen_drawing_voice_captions", v, delegate(AppSettings x)
				{
					x.DrawingVoiceCaptions = v;
				});
			}))),
			SK.Group("Language", null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(LanguagePicker(current.VoiceLanguageCode)), 14.0, $"{VoiceCatalog.Languages.Count} supported"),
			SK.Group("Speech speed", null, new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(stackPanel)),
			frameworkElement2,
			frameworkElement,
			OutputDeviceGroup(current.OutputDeviceId)
		}), 32.0);
	}

	private FrameworkElement VoiceSection(string label, string caption, IEnumerable<(string Id, string Name, string Subtitle, string? Fill, string? Stroke, Color Color)> voices, string selectedId)
	{
		List<(string, string, string, string, string, Color)> list = voices.ToList();
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0,
			RowSpacing = 12.0,
			MaxWidth = 650.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		for (int i = 0; i < 5; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}
		for (int j = 0; j < (list.Count + 4) / 5; j++)
		{
			grid.RowDefinitions.Add(new RowDefinition());
		}
		for (int k = 0; k < list.Count; k++)
		{
			(string, string, string, string, string, Color) tuple = list[k];
			bool selected = string.Equals(selectedId, tuple.Item1, StringComparison.OrdinalIgnoreCase);
			Grid grid2 = new Grid
			{
				Height = 28.0,
				Width = 28.0,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			grid2.Children.Add(Ui.GlowSpot(tuple.Item6, 0.22, 46.0, 0.25));
			grid2.Children.Add(Icons.Vector(26.0, tuple.Item6, tuple.Item4, tuple.Item5, 1.9));
			StackPanel item = new StackPanel
			{
				Spacing = 8.0,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				Padding = new Thickness(10.0),
				Children = 
				{
					(UIElement)grid2,
					(UIElement)new StackPanel
					{
						Spacing = 3.0,
						Children = 
						{
							(UIElement)Ui.Text(tuple.Item2, 12.5, FontWeights.SemiBold, SK.TextPrimary, TextAlignment.Center, 1),
							(UIElement)Ui.Text(tuple.Item3, 10.5, FontWeights.Medium, SK.TextTertiary, TextAlignment.Center, 2)
						}
					}
				}
			};
			string id = tuple.Item1;
			Grid grid3 = new Grid
			{
				Children = { (UIElement)item }
			};
			if (VoiceCatalog.IsGemini(id))
			{
				ProgressRing progressRing = new ProgressRing
				{
					Width = 12.0,
					Height = 12.0,
					MinWidth = 0.0,
					MinHeight = 0.0,
					IsActive = true,
					Foreground = Ui.Brush(SK.TextTertiary),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(9.0, 9.0, 0.0, 0.0),
					IsHitTestVisible = false,
					Visibility = Visibility.Collapsed
				};
				grid3.Children.Add(progressRing);
				previewSpinners[id] = progressRing;
			}
			Pressable tile = SK.Tile(grid3, selected, 20.0, delegate
			{
				host.Set("voice", id, delegate(AppSettings x)
				{
					x.VoiceIdentifier = id;
				});
				PlayVoicePreview(id);
			});
			tile.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				if (double.IsNaN(tile.Height) || Math.Abs(tile.Height - e.NewSize.Width) > 0.5)
				{
					tile.Height = e.NewSize.Width;
				}
			};
			ToolTipService.SetToolTip(tile, tuple.Item2 + ", " + tuple.Item3);
			Grid.SetColumn(tile, k % 5);
			Grid.SetRow(tile, k / 5);
			grid.Children.Add(tile);
		}
		StackPanel stackPanel = Ui.H(8.0, SK.MicroLabel(label.ToUpperInvariant(), 10.0, FontWeights.SemiBold, 110), Ui.Text(caption, 11.0, FontWeights.Normal, SK.TextSecondary));
		stackPanel.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
		return new StackPanel
		{
			Spacing = 10.0,
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)grid
			}
		};
	}

	private FrameworkElement LanguagePicker(string? stored)
	{
		IReadOnlyList<string> readOnlyList = VoiceCatalog.PickedLanguages(stored);
		string text = ((readOnlyList.Count == 0) ? "Automatic" : VoiceCatalog.SpokenList(readOnlyList));
		StackPanel item = new StackPanel
		{
			Spacing = 2.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text("Speak to me in", 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 1),
				(UIElement)Ui.Text((readOnlyList.Count == 0) ? "Automatic: the language you talk to it in" : ((readOnlyList.Count == 1) ? text : "Whichever of these you use"), 11.5, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 1)
			}
		};
		TextBlock textBlock = Icons.Glyph(languageListOpen ? "\ue70e" : "\ue70d", 10.0, SK.TextTertiary);
		StackPanel stackPanel = Ui.H(6.0, Ui.Text(text, 12.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 1), textBlock);
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(item);
		grid.Children.Add(stackPanel);
		Pressable toggle = new Pressable
		{
			Children = { (UIElement)grid }
		};
		toggle.HoverChanged += delegate(bool h)
		{
			toggle.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.04 : 0.0));
		};
		toggle.Click += delegate
		{
			languageListOpen = !languageListOpen;
			voiceLanguageSearch = "";
			voiceLanguageLimitReached = false;
			if (!languageListOpen)
			{
				choosingVoiceLanguages = false;
			}
			Render();
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Children = { (UIElement)toggle }
		};
		if (!languageListOpen)
		{
			return stackPanel2;
		}
		bool flag = readOnlyList.Count == 0 && !choosingVoiceLanguages;
		Grid item2 = SK.ToggleRow("Automatic", "Answer in the language you talk to it in. Turn off to pick one or more.", flag, delegate(bool on)
		{
			voiceLanguageLimitReached = false;
			if (on)
			{
				choosingVoiceLanguages = false;
				SetVoiceLanguages(Array.Empty<string>());
			}
			else
			{
				choosingVoiceLanguages = true;
			}
			Render();
		});
		Grid item3 = SK.SearchField($"Search {VoiceCatalog.Languages.Count} languages", voiceLanguageSearch, delegate(string t)
		{
			voiceLanguageSearch = t;
			FillLanguageList(stored);
		}, magnifier: true, 12.0, null).Box;
		languageListHost = new Grid();
		FillLanguageList(stored);
		ScrollViewer item4 = new ScrollViewer
		{
			Height = 260.0,
			Content = languageListHost,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(14.0),
			Opacity = (flag ? 0.35 : 1.0),
			IsHitTestVisible = !flag,
			Children = { (UIElement)item3 }
		};
		if (voiceLanguageLimitReached)
		{
			stackPanel3.Children.Add(Ui.Text($"Up to {5} languages. Take one off to add another.", 11.5, FontWeights.Medium, SK.TextTertiary));
		}
		stackPanel3.Children.Add(item4);
		stackPanel2.Children.Add(SK.Divider(0.0));
		stackPanel2.Children.Add(item2);
		stackPanel2.Children.Add(SK.Divider(0.0));
		stackPanel2.Children.Add(stackPanel3);
		return stackPanel2;
	}

	private void FillLanguageList(string? stored)
	{
		using (MessagesPalette.Use(look))
		{
			if ((object)languageListHost == null)
			{
				return;
			}
			languageListHost.Children.Clear();
			languageListHost.RowDefinitions.Clear();
			languageListHost.ColumnDefinitions.Clear();
			IReadOnlyList<string> source = VoiceCatalog.PickedLanguages(stored);
			IReadOnlyList<VoiceLanguage> readOnlyList = VoiceCatalog.PickerList(voiceLanguageSearch);
			if (readOnlyList.Count == 0)
			{
				languageListHost.Children.Add(Ui.Text("No languages match \"" + voiceLanguageSearch + "\".", 11.5, FontWeights.Medium, SK.TextTertiary));
				return;
			}
			languageListHost.ColumnSpacing = 8.0;
			languageListHost.RowSpacing = 8.0;
			for (int i = 0; i < 4; i++)
			{
				languageListHost.ColumnDefinitions.Add(new ColumnDefinition());
			}
			for (int j = 0; j < (readOnlyList.Count + 3) / 4; j++)
			{
				languageListHost.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLength.Auto
				});
			}
			for (int k = 0; k < readOnlyList.Count; k++)
			{
				VoiceLanguage voiceLanguage = readOnlyList[k];
				bool flag = source.Contains(voiceLanguage.Code);
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 3.0,
					VerticalAlignment = VerticalAlignment.Center,
					Children = { (UIElement)Ui.Text(voiceLanguage.Name, 11.5, flag ? FontWeights.SemiBold : FontWeights.Medium, flag ? SK.TextPrimary : SK.TextSecondary, TextAlignment.Left, 1) }
				};
				string nativeName = voiceLanguage.NativeName;
				if (nativeName != null)
				{
					stackPanel.Children.Add(Ui.Text(nativeName, 10.5, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 1));
				}
				Grid grid = new Grid
				{
					Padding = new Thickness(13.0, 12.0, 13.0, 12.0),
					MinHeight = 46.0,
					ColumnSpacing = 6.0
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(18.0)
				});
				grid.Children.Add(stackPanel);
				if (flag)
				{
					FrameworkElement frameworkElement = IntroWindow.GelCheckmark(18.0);
					frameworkElement.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(frameworkElement, 1);
					grid.Children.Add(frameworkElement);
				}
				string code = voiceLanguage.Code;
				Pressable pressable = SK.Tile(grid, flag, 9.0, delegate
				{
					ToggleVoiceLanguage(code);
				}, 20.0, topRightCheck: false);
				ToolTipService.SetToolTip(pressable, voiceLanguage.Name);
				Grid.SetColumn(pressable, k % 4);
				Grid.SetRow(pressable, k / 4);
				languageListHost.Children.Add(pressable);
			}
		}
	}

	private void ToggleVoiceLanguage(string code)
	{
		List<string> list = VoiceCatalog.PickedLanguages(host.Read((AppSettings x) => x.VoiceLanguageCode)).ToList();
		string text;
		if (list.Remove(code))
		{
			voiceLanguageLimitReached = false;
			text = "remove";
		}
		else
		{
			if (list.Count >= 5)
			{
				voiceLanguageLimitReached = true;
				Render();
				return;
			}
			list.Add(code);
			text = "add";
		}
		ClickyAnalytics.TrackProductEvent("setting_changed", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["setting"] = "voice_language",
			["action"] = text,
			["value"] = code,
			["surface"] = "home_space_settings"
		});
		choosingVoiceLanguages = list.Count == 0;
		SetVoiceLanguages(list);
	}

	private void SetVoiceLanguages(IReadOnlyList<string> picks)
	{
		string stored = VoiceCatalog.StoredLanguages(picks);
		string text = host.Read((AppSettings x) => x.VoiceLanguageCode);
		if (stored == text)
		{
			Render();
			return;
		}
		bool num = LanguageForPreviews(stored)?.Code != LanguageForPreviews(text)?.Code;
		if (num)
		{
			StopVoicePreview();
			voicePreviewRequest++;
		}
		Log.Info("settings", "voice_language = " + (stored ?? "automatic"));
		host.Store.Update(delegate(AppSettings x)
		{
			x.VoiceLanguageCode = stored;
		});
		if (num)
		{
			string text2 = host.Read((AppSettings x) => x.VoiceIdentifier);
			if ((object)LanguageForPreviews(stored) != null && VoiceCatalog.IsGemini(text2))
			{
				PlayVoicePreview(text2);
			}
			PrefetchExpressivePreviews();
		}
	}

	private void PrefetchExpressivePreviews()
	{
		VoicePreviewStore shared = VoicePreviewStore.Shared;
		if (shared == null)
		{
			return;
		}
		AppSettings c = host.Current;
		VoiceLanguage voiceLanguage = LanguageForPreviews(c.VoiceLanguageCode);
		if ((object)voiceLanguage != null)
		{
			List<string> voiceIds = (from v in VoiceCatalog.ExpressiveVoices
				select v.Id into id
				orderby (!(id == c.VoiceIdentifier)) ? 1 : 0
				select id).ToList();
			shared.PrefetchAsync(voiceIds, voiceLanguage.Code);
		}
	}

	private void StartPreviewSpinnerTimer()
	{
		if ((object)previewSpinnerTimer == null)
		{
			previewSpinnerTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(400L, 0L)
			};
			previewSpinnerTimer.Tick += delegate
			{
				RefreshPreviewSpinners();
			};
			base.Unloaded += delegate
			{
				previewSpinnerTimer?.Stop();
			};
		}
		RefreshPreviewSpinners();
		previewSpinnerTimer.Start();
	}

	private void RefreshPreviewSpinners()
	{
		using (MessagesPalette.Use(look))
		{
			if (!isShown || page != SettingsWindow.Page.Voice)
			{
				previewSpinnerTimer?.Stop();
				return;
			}
			VoiceLanguage voiceLanguage = LanguageForPreviews(host.Read((AppSettings x) => x.VoiceLanguageCode));
			VoicePreviewStore shared = VoicePreviewStore.Shared;
			foreach (KeyValuePair<string, FrameworkElement> previewSpinner in previewSpinners)
			{
				previewSpinner.Deconstruct(out var key, out var value);
				string voiceId = key;
				FrameworkElement frameworkElement = value;
				Visibility visibility = (((object)voiceLanguage == null || shared == null || !shared.IsMaking(voiceId, voiceLanguage.Code)) ? Visibility.Collapsed : Visibility.Visible);
				if (frameworkElement.Visibility != visibility)
				{
					frameworkElement.Visibility = visibility;
				}
			}
		}
	}

	private FrameworkElement OutputDeviceGroup(string? picked)
	{
		List<FrameworkElement> list = new List<FrameworkElement> { DeviceRow("System Default", DefaultName(renderDevices), picked == null, available: true, delegate
		{
			PickOutput(null);
		}) };
		foreach (AudioDeviceInfo d2 in renderDevices)
		{
			list.Add(DeviceRow(d2.Name, null, string.Equals(d2.Id, picked, StringComparison.OrdinalIgnoreCase), available: true, delegate
			{
				PickOutput(d2.Id);
			}));
		}
		if (picked != null && !renderDevices.Any((AudioDeviceInfo d) => string.Equals(d.Id, picked, StringComparison.OrdinalIgnoreCase)) && renderDevices.Count > 0)
		{
			list.Add(DeviceRow("Unavailable output", "Disconnected", selected: true, available: false, delegate
			{
			}));
		}
		Pressable trailing = SK.PillButton("Play a test sound", delegate
		{
			Log.Info("settings", "output test sound");
			WasapiAudioOutput wasapiAudioOutput = host.App?.Real?.Audio;
			if (wasapiAudioOutput != null)
			{
				wasapiAudioOutput.PlayEarcon("textOpen", 0.8f);
			}
			else
			{
				s.Audio?.PlayEarcon("textOpen");
			}
		});
		list.Add(SK.Row("Test", "Plays a chime through the picked output.", trailing, null));
		return SK.Group("Output device", "HeyClicky's voice and sounds play here. System Default follows Windows.", list);
	}

	private static string? DefaultName(IReadOnlyList<AudioDeviceInfo> devices)
	{
		return devices.FirstOrDefault((AudioDeviceInfo d) => d.IsDefault)?.Name;
	}

	private void PickOutput(string? id)
	{
		host.Set("output_device", id ?? "system_default", delegate(AppSettings x)
		{
			x.OutputDeviceId = id;
		});
	}

	private void PlayVoicePreview(string id)
	{
		StopVoicePreview();
		int request = ++voicePreviewRequest;
		if (Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1")
		{
			return;
		}
		string bundled = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "VoicePreviews", VoiceCatalog.IsGemini(id) ? ("gemini-voice-preview-" + VoiceCatalog.GeminiName(id).ToLowerInvariant() + ".mp3") : ("realtime-voice-preview-" + id + ".mp3"));
		VoiceLanguage language;
		VoicePreviewStore store;
		if (VoiceCatalog.IsGemini(id))
		{
			language = LanguageForPreviews(host.Read((AppSettings x) => x.VoiceLanguageCode));
			if ((object)language != null)
			{
				store = VoicePreviewStore.Shared;
				if (store != null)
				{
					string text = store.Cached(id, language.Code);
					if (text != null)
					{
						PlayPreviewClip(text, id);
						return;
					}
					RefreshPreviewSpinners();
					MakeAndPlayAsync();
					return;
				}
			}
		}
		PlayPreviewClip(bundled, id);
		async Task MakeAndPlayAsync()
		{
			string made = await store.PreviewAsync(id, language.Code);
			dq.TryEnqueue(delegate
			{
				RefreshPreviewSpinners();
				if (request == voicePreviewRequest && isShown && page == SettingsWindow.Page.Voice)
				{
					PlayPreviewClip(made ?? bundled, id);
				}
			});
		}
	}

	private void PlayPreviewClip(string path, string id)
	{
		if (!File.Exists(path))
		{
			Log.Warn("settings", "voice preview missing: " + path);
			return;
		}
		voicePreview = new MediaPlayer
		{
			Source = MediaSource.CreateFromUri(new Uri(path)),
			Volume = 0.95,
			AudioCategory = MediaPlayerAudioCategory.Speech
		};
		double rate = host.Read((AppSettings x) => x.SpeechSpeed);
		try
		{
			voicePreview.PlaybackSession.PlaybackRate = rate;
		}
		catch
		{
		}
		voicePreview.MediaOpened += delegate(MediaPlayer player, object _)
		{
			try
			{
				player.PlaybackSession.PlaybackRate = rate;
			}
			catch
			{
			}
		};
		voicePreview.Play();
		Log.Info("settings", "voice preview " + id);
	}

	private void StopVoicePreview()
	{
		if ((object)voicePreview != null)
		{
			try
			{
				voicePreview.Pause();
				voicePreview.Dispose();
			}
			catch
			{
			}
			voicePreview = null;
		}
	}

	private FrameworkElement MicrophonePage()
	{
		HookAudioDeviceChanges();
		string picked = host.Read((AppSettings x) => x.MicrophoneDeviceId);
		List<FrameworkElement> list = new List<FrameworkElement> { DeviceRow("System Default", DefaultName(captureDevices), picked == null, available: true, delegate
		{
			PickMicrophone(null);
		}) };
		foreach (AudioDeviceInfo d2 in captureDevices)
		{
			list.Add(DeviceRow(d2.Name, null, string.Equals(d2.Id, picked, StringComparison.OrdinalIgnoreCase), available: true, delegate
			{
				PickMicrophone(d2.Id);
			}));
		}
		if (picked != null && captureDevices.Count > 0 && !captureDevices.Any((AudioDeviceInfo d) => string.Equals(d.Id, picked, StringComparison.OrdinalIgnoreCase)))
		{
			list.Add(DeviceRow("Unavailable Microphone", "Disconnected", selected: true, available: false, delegate
			{
			}));
		}
		micTestButton = new GelButton((micTest == null) ? "Test microphone" : "Stop", GelAppearance.SecondaryWhite, GelButton.Sizing.Normal, 100.0, null, null, null);
		micTestButton.Click += delegate
		{
			if (micTest == null)
			{
				StartMicrophoneTest();
			}
			else
			{
				StopMicrophoneTest();
			}
		};
		micBars = Ui.H(3.0);
		for (int i = 0; i < 16; i++)
		{
			micBars.Children.Add(new Rectangle
			{
				Width = 6.0,
				Height = 16.0,
				RadiusX = 2.0,
				RadiusY = 2.0,
				Fill = Ui.Brush(SK.Ink.WithAlpha(0.12))
			});
		}
		micStatusText = Ui.Text(micTestStatus, 11.0, FontWeights.Medium, (micTestStatusColor == default(Color)) ? SK.TextTertiary : micTestStatusColor);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Spacing = 10.0;
		stackPanel.Padding = new Thickness(14.0);
		stackPanel.Children.Add(Ui.H(14.0, micTestButton, micBars));
		stackPanel.Children.Add(micStatusText);
		StackPanel item = stackPanel;
		UpdateMicBars();
		return SK.Scaffold("Microphone", "Which microphone HeyClicky listens through.", new global::_003C_003Ez__ReadOnlyArray<FrameworkElement>(new FrameworkElement[2]
		{
			SK.Group("Input device", null, list),
			SK.Group("Test", "Speak normally — the bars should move as HeyClicky hears you.", new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(item))
		}));
	}

	private void PickMicrophone(string? id)
	{
		StopMicrophoneTest();
		host.Set("microphone", id ?? "system_default", delegate(AppSettings x)
		{
			x.MicrophoneDeviceId = id;
		});
	}

	private FrameworkElement DeviceRow(string name, string? subtitle, bool selected, bool available, Action onPick)
	{
		Pressable p = new Pressable
		{
			Opacity = (available ? 1.0 : 0.45)
		};
		FrameworkElement item = (selected ? IntroWindow.GelCheckmark(18.0) : new Ellipse
		{
			Width = 18.0,
			Height = 18.0,
			Stroke = Ui.Brush(SK.TextTertiary),
			StrokeThickness = 1.0
		});
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)Ui.Text(name, 13.0, FontWeights.Medium, SK.TextPrimary, TextAlignment.Left, 1) }
		};
		if (subtitle != null)
		{
			stackPanel.Children.Add(Ui.Text(subtitle, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 1));
		}
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(item);
		grid.Children.Add(stackPanel);
		p.Children.Add(grid);
		p.HoverChanged += delegate(bool h)
		{
			if (available)
			{
				p.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.05 : 0.0));
			}
		};
		if (available)
		{
			p.Click += onPick;
		}
		return p;
	}

	private void HookAudioDeviceChanges()
	{
		if (audioDeviceChangesHooked)
		{
			return;
		}
		audioDeviceChangesHooked = true;
		AudioTopologyMonitor.Shared.Changed += OnAudioTopologyChanged;
		if (!audioDeviceUnloadHooked)
		{
			audioDeviceUnloadHooked = true;
			base.Unloaded += delegate
			{
				AudioTopologyMonitor.Shared.Changed -= OnAudioTopologyChanged;
				audioDeviceChangesHooked = false;
			};
		}
	}

	private void OnAudioTopologyChanged(AudioTopologySnapshot snapshot)
	{
		dq.TryEnqueue(delegate
		{
			bool flag = !isShown;
			if (!flag)
			{
				SettingsWindow.Page page = this.page;
				bool flag2 = (uint)(page - 3) <= 1u;
				flag = !flag2;
			}
			if (!flag)
			{
				Log.Info("settings", "audio devices changed: re-listing");
				RefreshAudioDevicesAsync();
			}
		});
	}

	private async Task RefreshAudioDevicesAsync()
	{
		var (capture, render) = await Task.Run(() => (AudioDevices.List(capture: true), AudioDevices.List(capture: false)));
		dq.TryEnqueue(delegate
		{
			captureDevices = capture;
			renderDevices = render;
			Log.Info("settings", $"audio devices: {capture.Count} microphones, {render.Count} outputs");
			SettingsWindow.Page page = this.page;
			if ((uint)(page - 3) <= 1u)
			{
				Render();
			}
		});
	}

	private void StartMicrophoneTest()
	{
		using (MessagesPalette.Use(look))
		{
			StopMicrophoneTest();
			try
			{
				micTest = new WasapiMicrophoneCapture();
				micTest.Start();
			}
			catch (Exception ex)
			{
				Log.Warn("settings", "mic test failed: " + ex.Message);
				micTest = null;
				SetMicStatus("Could not start microphone test", SK.Warning);
				return;
			}
			micTestHeard = false;
			micTestLevel = 0.0;
			micTestStarted = DateTime.UtcNow;
			SetMicStatus("Listening...", SK.TextSecondary);
			if ((object)micTestButton != null)
			{
				micTestButton.Title = "Stop";
			}
			micTestTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(60L, 0L)
			};
			micTestTimer.Tick += delegate
			{
				using (MessagesPalette.Use(look))
				{
					if (micTest != null)
					{
						micTestLevel = Math.Clamp(Math.Max(micTest.Level, micTestLevel * 0.68), 0.0, 1.0);
						if (micTestLevel >= 0.045 && !micTestHeard)
						{
							micTestHeard = true;
							SetMicStatus("Receiving audio", MessagesPalette.Current.SelectionBlue);
							Log.Info("settings", "mic test: receiving audio (" + micTest.CurrentDeviceId + ")");
						}
						else if (!micTestHeard && DateTime.UtcNow - micTestStarted > TimeSpan.FromSeconds(2.2) && micTestStatus != "HeyClicky can't hear you. Try different microphone.")
						{
							SetMicStatus("HeyClicky can't hear you. Try different microphone.", SK.Warning);
						}
						UpdateMicBars();
					}
				}
			};
			micTestTimer.Start();
			Log.Info("settings", "mic test: started");
		}
	}

	private void StopMicrophoneTest()
	{
		using (MessagesPalette.Use(look))
		{
			micTestTimer?.Stop();
			micTestTimer = null;
			WasapiMicrophoneCapture wasapiMicrophoneCapture = micTest;
			if (wasapiMicrophoneCapture != null)
			{
				micTest = null;
				Task.Run((Action)wasapiMicrophoneCapture.Dispose);
				Log.Info("settings", "mic test: stopped");
			}
			micTestLevel = 0.0;
			SetMicStatus("Tap to test selected input", default(Color));
			if ((object)micTestButton != null)
			{
				micTestButton.Title = "Test microphone";
			}
			UpdateMicBars();
		}
	}

	private void SetMicStatus(string text, Color color)
	{
		micTestStatus = text;
		micTestStatusColor = color;
		if ((object)micStatusText != null)
		{
			micStatusText.Text = text;
			micStatusText.Foreground = Ui.Brush((color == default(Color)) ? SK.TextTertiary : color);
		}
	}

	private void UpdateMicBars()
	{
		using (MessagesPalette.Use(look))
		{
			if ((object)micBars != null)
			{
				for (int i = 0; i < micBars.Children.Count; i++)
				{
					bool flag = micTestLevel * 16.0 > (double)i;
					((Rectangle)micBars.Children[i]).Fill = Ui.Brush((!flag) ? SK.Ink.WithAlpha(0.12) : ((i >= 13) ? SK.Destructive : ((i >= 10) ? SK.Warning : SK.Positive)));
				}
			}
		}
	}

	public SettingsContent(AppServices services, IAppActions actions, Action? close = null)
	{
		s = services;
		this.actions = actions;
		host = new SettingsHost(services, actions);
		dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		base.ColumnDefinitions.Add(sidebarColumn);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		sidebar = new Grid
		{
			Padding = new Thickness(14.0, 6.0, 16.0, 14.0)
		};
		sidebar.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength((close != null) ? 40 : 0)
		});
		sidebar.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		sidebar.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		if (close != null)
		{
			Pressable pressable = new Pressable
			{
				Width = 24.0,
				Height = 24.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
			};
			pressable.Children.Add(new Ellipse
			{
				Fill = Ui.Brush(DS.White(0.1))
			});
			TextBlock textBlock = Icons.Glyph("\ue711", 9.0, DS.White(0.75));
			textBlock.FontWeight = FontWeights.Bold;
			pressable.Children.Add(textBlock);
			pressable.Click += close;
			pressable.Tip("Close");
			sidebar.Children.Add(pressable);
		}
		ScrollViewer scrollViewer = new ScrollViewer
		{
			Content = sidebarItems,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			Padding = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		Grid.SetRow(scrollViewer, 1);
		sidebar.Children.Add(scrollViewer);
		TextBlock textBlock2 = Ui.Text(host.BuildInfoLine, 9.5, FontWeights.Medium, SK.TextTertiary.WithAlpha(0.8));
		textBlock2.Margin = new Thickness(10.0, 12.0, 10.0, 0.0);
		Grid.SetRow(textBlock2, 2);
		sidebar.Children.Add(textBlock2);
		Border border = new Border
		{
			Background = Ui.Brush(SK.Ink.WithAlpha(0.07))
		};
		Grid.SetColumn(border, 1);
		scroller = new ScrollViewer
		{
			Content = content,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollMode = ScrollMode.Disabled
		};
		Grid.SetColumn(scroller, 2);
		Grid.SetColumn(overlayLayer, 0);
		Grid.SetColumnSpan(overlayLayer, 3);
		base.Children.Add(sidebar);
		base.Children.Add(border);
		base.Children.Add(scroller);
		base.Children.Add(overlayLayer);
		base.Loaded += delegate
		{
			Subscribe();
		};
		base.Unloaded += delegate
		{
			Unsubscribe();
			OnPageLeft();
		};
	}

	private void Subscribe()
	{
		if (!subscribed)
		{
			subscribed = true;
			if (!(s.Settings is CoreAppSettings))
			{
				s.Settings.Changed += OnSettingsChanged;
			}
			host.Store.Changed += OnSettingsChanged;
			s.Auth.Changed += OnAuthChanged;
			HookBilling();
			if (loadedBefore)
			{
				RenderWhenShown();
			}
			loadedBefore = true;
		}
	}

	private void Unsubscribe()
	{
		if (!subscribed)
		{
			return;
		}
		subscribed = false;
		if (!(s.Settings is CoreAppSettings))
		{
			s.Settings.Changed -= OnSettingsChanged;
		}
		host.Store.Changed -= OnSettingsChanged;
		s.Auth.Changed -= OnAuthChanged;
		if (billingHooked)
		{
			BillingUi billing = host.Billing;
			if (billing != null)
			{
				billing.Changed -= OnBillingChanged;
			}
		}
		billingHooked = false;
	}

	private void OnSettingsChanged(object? sender, EventArgs e)
	{
		if (quietWrites == 0)
		{
			dq.TryEnqueue(RenderWhenShown);
		}
	}

	private void OnAuthChanged(object? sender, EventArgs e)
	{
		dq.TryEnqueue(delegate
		{
			ResetReferralIfAccountChanged();
			RenderWhenShown();
		});
	}

	private void RenderWhenShown()
	{
		if (isShown)
		{
			RenderSoon();
		}
	}

	public void UseNotchGrid(double sidebarLeadingInset, double footerBottomInset)
	{
		sidebar.Padding = new Thickness(sidebarLeadingInset, 6.0, 16.0, footerBottomInset);
	}

	public void Show(SettingsWindow.Page? at = null)
	{
		if (at.HasValue)
		{
			SettingsWindow.Page valueOrDefault = at.GetValueOrDefault();
			if (valueOrDefault != page)
			{
				OnPageLeft();
				page = valueOrDefault;
			}
		}
		isShown = true;
		Render(resetScroll: true);
		OnPageShown();
	}

	public void Hidden()
	{
		isShown = false;
		lastBuiltPage = null;
		OnPageLeft();
	}

	private void Quiet(string key, object? value, Action<AppSettings> change)
	{
		quietWrites++;
		try
		{
			host.Set(key, value, change);
		}
		finally
		{
			quietWrites--;
		}
	}

	private void Render()
	{
		Render(resetScroll: false);
	}

	private void RenderSoon()
	{
		if (!renderQueued)
		{
			renderQueued = true;
			dq.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, delegate
			{
				renderQueued = false;
				Render();
			});
		}
	}

	private void Render(bool resetScroll)
	{
		using (MessagesPalette.Use(look))
		{
			SyncSidebar();
			double num = (resetScroll ? 0.0 : scroller.VerticalOffset);
			content.Content = BuildPage(page);
			if (page == SettingsWindow.Page.Account)
			{
				shownPlanSignature = PlanSignature();
			}
			if (num > 0.0)
			{
				scroller.UpdateLayout();
				scroller.ChangeView(null, num, null, disableAnimation: true);
			}
			else if (resetScroll)
			{
				scroller.ChangeView(null, 0.0, null, disableAnimation: true);
			}
		}
	}

	private FrameworkElement BuildPage(SettingsWindow.Page p)
	{
		if (p == SettingsWindow.Page.Referral && lastBuiltPage != SettingsWindow.Page.Referral)
		{
			NoteReferralPageAppeared();
		}
		lastBuiltPage = p;
		try
		{
			return p switch
			{
				SettingsWindow.Page.Account => AccountPage(), 
				SettingsWindow.Page.General => GeneralPage(), 
				SettingsWindow.Page.Referral => ReferralPage(), 
				SettingsWindow.Page.Voice => VoicePage(), 
				SettingsWindow.Page.Microphone => MicrophonePage(), 
				SettingsWindow.Page.Dictation => DictationPage(), 
				SettingsWindow.Page.Shortcuts => ShortcutsPage(), 
				SettingsWindow.Page.Cursor => CursorPage(), 
				SettingsWindow.Page.Agents => AgentsPage(), 
				SettingsWindow.Page.Routines => RoutinesPage(), 
				SettingsWindow.Page.Integrations => IntegrationsPage(), 
				SettingsWindow.Page.Developer => DeveloperPage(), 
				_ => AccountPage(), 
			};
		}
		catch (Exception ex)
		{
			Log.Error("settings", $"{p} page failed to build", ex);
			return SK.Scaffold(p.ToString(), "This page couldn't load.", new global::_003C_003Ez__ReadOnlySingleElementList<FrameworkElement>(Ui.Text(ex.Message, 11.0, FontWeights.Normal, SK.TextTertiary, TextAlignment.Left, 0, wrap: true)));
		}
	}

	private void SyncSidebar()
	{
		string text = $"{host.ShowsDeveloperSection}|{s.Auth.User?.AvatarUrl}";
		if (text != sidebarSignature)
		{
			sidebarSignature = text;
			sidebarRows.Clear();
			sidebarRowSelected.Clear();
			sidebarItems.Children.Clear();
			AddSidebarGroup(null, (SettingsWindow.Page.Account, "Account", "M12,2.5 A4.75,4.75 0 1 1 12,12 A4.75,4.75 0 1 1 12,2.5 Z M4,20.2 C4,16.4 7.6,13.8 12,13.8 C16.4,13.8 20,16.4 20,20.2 C20,21 19.5,21.5 18.7,21.5 L5.3,21.5 C4.5,21.5 4,21 4,20.2 Z"), (SettingsWindow.Page.General, "General", "\ue713"), (SettingsWindow.Page.Referral, "Invite & Earn", "\uf133"));
			AddSidebarGroup("HEYCLICKY", (SettingsWindow.Page.Voice, "Voice", "M3,10 h2 v4 h-2 z M7,7 h2 v10 h-2 z M11,3 h2 v18 h-2 z M15,6 h2 v12 h-2 z M19,9 h2 v6 h-2 z"), (SettingsWindow.Page.Microphone, "Microphone", "\ue720"), (SettingsWindow.Page.Dictation, "Dictation", "M7.5,3 L16.5,3 L16.5,5 L13,5 L13,19 L16.5,19 L16.5,21 L7.5,21 L7.5,19 L11,19 L11,5 L7.5,5 Z"), (SettingsWindow.Page.Shortcuts, "Shortcuts", "⌘"), (SettingsWindow.Page.Cursor, "Cursor", "M6,2.5 L6,19.5 L10.2,15.6 L13,21.8 L15.6,20.6 L12.9,14.6 L18.6,14.6 Z"));
			AddSidebarGroup("WORK", (SettingsWindow.Page.Integrations, "Integrations", "\uea86"), (SettingsWindow.Page.Agents, "Agents", HexGridPath), (SettingsWindow.Page.Routines, "Routines", "\ue72c"));
			if (host.ShowsDeveloperSection)
			{
				AddSidebarGroup("INTERNAL", (SettingsWindow.Page.Developer, "Developer", "\uec7a"));
			}
		}
		foreach (var (p, parts) in sidebarRows)
		{
			StyleSidebarRow(p, parts);
		}
	}

	private void StyleSidebarRow(SettingsWindow.Page p, (Pressable Row, Grid IconBox, TextBlock Text, string Glyph) parts)
	{
		bool flag = p == page;
		if (sidebarRowSelected.TryGetValue(p, out var value) && value == flag)
		{
			return;
		}
		sidebarRowSelected[p] = flag;
		Color color = (flag ? SK.TextPrimary : SK.TextSecondary);
		if (parts.IconBox.Children.Count > 0)
		{
			parts.IconBox.Children.RemoveAt(0);
		}
		parts.IconBox.Children.Insert(0, SidebarIcon(parts.Glyph, color));
		parts.Text.FontWeight = (flag ? FontWeights.SemiBold : FontWeights.Medium);
		parts.Text.Foreground = Ui.Brush(color);
		parts.Row.Background = Ui.Brush(SK.Ink.WithAlpha(flag ? 0.12 : (parts.Row.IsHovered ? 0.07 : 0.0)));
		if (flag)
		{
			if (parts.Row.IsLoaded)
			{
				parts.Row.StartBringIntoView(new BringIntoViewOptions
				{
					AnimationDesired = false
				});
			}
			else
			{
				parts.Row.Loaded += BringIntoViewOnce;
			}
		}
		void BringIntoViewOnce(object sender, RoutedEventArgs e)
		{
			parts.Row.Loaded -= BringIntoViewOnce;
			if (p == page)
			{
				parts.Row.StartBringIntoView(new BringIntoViewOptions
				{
					AnimationDesired = false
				});
			}
		}
	}

	private static BitmapImage AvatarImage(Uri uri)
	{
		if (!AvatarImages.TryGetValue(uri.AbsoluteUri, out BitmapImage value))
		{
			Dictionary<string, BitmapImage> avatarImages = AvatarImages;
			string absoluteUri = uri.AbsoluteUri;
			BitmapImage obj = new BitmapImage
			{
				DecodePixelType = DecodePixelType.Logical,
				DecodePixelWidth = 18,
				UriSource = uri
			};
			value = obj;
			avatarImages[absoluteUri] = obj;
		}
		return value;
	}

	private void AddSidebarGroup(string? header, params (SettingsWindow.Page Page, string Title, string Glyph)[] rows)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0
		};
		if (header != null)
		{
			TextBlock textBlock = Ui.Text(header, 9.5, FontWeights.Bold, SK.TextTertiary);
			textBlock.CharacterSpacing = 100;
			textBlock.Margin = new Thickness(10.0, 0.0, 0.0, 4.0);
			stackPanel.Children.Add(textBlock);
		}
		for (int i = 0; i < rows.Length; i++)
		{
			var (p, title, glyph) = rows[i];
			stackPanel.Children.Add(SidebarRow(p, title, glyph));
		}
		sidebarItems.Children.Add(stackPanel);
	}

	private FrameworkElement SidebarRow(SettingsWindow.Page p, string title, string glyph)
	{
		Pressable row = new Pressable
		{
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(10.0, 12.0, 10.0, 12.0)
		};
		Grid grid = new Grid
		{
			Width = 18.0,
			Height = 18.0,
			Children = { (UIElement)SidebarIcon(glyph, SK.TextSecondary) }
		};
		if (p == SettingsWindow.Page.Account)
		{
			string text = s.Auth.User?.AvatarUrl;
			if (text != null && text.Length > 0 && Uri.TryCreate(text, UriKind.Absolute, out Uri result))
			{
				Ellipse item = new Ellipse
				{
					Width = 18.0,
					Height = 18.0,
					Fill = new ImageBrush
					{
						ImageSource = AvatarImage(result),
						Stretch = Stretch.UniformToFill
					}
				};
				grid.Children.Add(item);
			}
		}
		TextBlock textBlock = Ui.Text(title, 12.5, FontWeights.Medium, SK.TextSecondary);
		row.Children.Add(Ui.H(9.0, grid, textBlock));
		sidebarRows[p] = (row, grid, textBlock, glyph);
		row.HoverChanged += delegate(bool h)
		{
			if (p != page)
			{
				row.Background = Ui.Brush(look.Ink.WithAlpha(h ? 0.07 : 0.0));
			}
		};
		row.Click += delegate
		{
			if (page != p)
			{
				ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
				{
					["action"] = "settings_" + SidebarSectionRawValue(p),
					["via"] = "sidebar"
				});
				if (p == SettingsWindow.Page.Referral)
				{
					ClickyAnalytics.NoteReferralPageEntryPoint("settings_sidebar");
				}
				OnPageLeft();
				page = p;
				Log.Info("settings", $"page {p}");
				Render(resetScroll: true);
				OnPageShown();
				this.PageChanged?.Invoke(p);
			}
		};
		return row;
	}

	private static string SidebarSectionRawValue(SettingsWindow.Page p)
	{
		return Enum.Parse<NotchSettingsSection>(p.ToString()).RawValue();
	}

	private static FrameworkElement SidebarIcon(string glyph, Color color)
	{
		if (glyph == "⌘")
		{
			return new TextBlock
			{
				Text = glyph,
				FontFamily = new FontFamily("Segoe UI Symbol"),
				FontSize = 14.0,
				FontWeight = FontWeights.SemiBold,
				Foreground = Ui.Brush(color),
				IsTextScaleFactorEnabled = false,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}
		if (glyph.StartsWith('M'))
		{
			double size = ((glyph == "M3,10 h2 v4 h-2 z M7,7 h2 v10 h-2 z M11,3 h2 v18 h-2 z M15,6 h2 v12 h-2 z M19,9 h2 v6 h-2 z" || glyph == HexGridPath) ? 13.0 : 12.5);
			return Icons.PathIcon(glyph, size, color);
		}
		TextBlock textBlock = Icons.Glyph(glyph, 11.5, color);
		textBlock.FontWeight = FontWeights.SemiBold;
		return textBlock;
	}

	public static FrameworkElement Switch(bool on, Action<bool> changed)
	{
		return SK.Switch(on, changed);
	}

	private void ShowOverlay(FrameworkElement card)
	{
		overlayLayer.Children.Clear();
		Border item = new Border
		{
			Background = Ui.Brush(DS.Black(0.55))
		};
		overlayLayer.Children.Add(item);
		card.HorizontalAlignment = HorizontalAlignment.Center;
		card.VerticalAlignment = VerticalAlignment.Center;
		overlayLayer.Children.Add(card);
	}

	private void HideOverlay()
	{
		overlayLayer.Children.Clear();
	}

	private void HookBilling()
	{
		if (!billingHooked && subscribed)
		{
			BillingUi billing = host.Billing;
			if (billing != null)
			{
				billingHooked = true;
				billing.Changed += OnBillingChanged;
			}
		}
	}

	private void OnBillingChanged()
	{
		dq.TryEnqueue(delegate
		{
			if (page == SettingsWindow.Page.Account && !(PlanSignature() == shownPlanSignature))
			{
				RenderWhenShown();
			}
		});
	}

	private string? PlanSignature()
	{
		BillingPlanCard billingPlanCard = host.Billing?.PlanCard;
		if ((object)billingPlanCard == null)
		{
			return null;
		}
		global::_003C_003Ey__InlineArray7<string> buffer = default(global::_003C_003Ey__InlineArray7<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 0) = billingPlanCard.Title;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 1) = billingPlanCard.Description;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 2) = billingPlanCard.TeamLabel;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 3) = billingPlanCard.ErrorText;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 4) = billingPlanCard.CancellationLabel;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 5) = string.Join(",", billingPlanCard.Meters);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 6) = string.Join(",", billingPlanCard.Actions);
		return string.Join("|", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray7<string>, string>(in buffer, 7));
	}

	private void OnPageShown()
	{
		switch (page)
		{
		case SettingsWindow.Page.Account:
			HookBilling();
			if (host.IsSignedIn && PlanFixtures.Current == null)
			{
				host.Billing?.RefreshAsync();
			}
			break;
		case SettingsWindow.Page.General:
			RefreshLinksThenRender();
			break;
		case SettingsWindow.Page.Integrations:
			ResetDepartures();
			RefreshIntegrationsAsync();
			ScheduleConnectorStateRefresh();
			break;
		case SettingsWindow.Page.Referral:
			RefreshReferralIfStale();
			ClickyAnalytics.TrackReferralPageOpened(null);
			break;
		case SettingsWindow.Page.Microphone:
			RefreshAudioDevicesAsync();
			break;
		case SettingsWindow.Page.Voice:
			RefreshAudioDevicesAsync();
			break;
		case SettingsWindow.Page.Dictation:
		case SettingsWindow.Page.Shortcuts:
		case SettingsWindow.Page.Cursor:
		case SettingsWindow.Page.Agents:
		case SettingsWindow.Page.Routines:
			break;
		}
	}

	private void OnPageLeft()
	{
		StopMicrophoneTest();
		StopVoicePreview();
		CancelShortcutRecording();
		ParkConnectorDraft();
	}

	private async Task RefreshLinksThenRender()
	{
		await host.RefreshLinksAsync();
		dq.TryEnqueue(delegate
		{
			if (page == SettingsWindow.Page.General)
			{
				Render();
			}
		});
	}
}
