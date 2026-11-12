using System;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Notch;

public static class NotchGeometry
{
	public sealed class SizeContext
	{
		public int TextInputExtraLines;

		public int TextInputAttachmentCount;

		public int FollowUpExtraLines;

		public int FollowUpAttachmentCount;

		public bool ResponseFinishedStreaming;

		public bool ResponseHasGallery;

		public bool IntegrationHasUseCases = true;

		public bool MicrophoneShowsSettings = true;

		public bool TutorialResumeShowsContinue = true;

		public Func<(double W, double H)>? PeekSize;

		public Func<NotchLiveEvent.ClickyUpdate, double>? ClickyUpdateHeight;

		public bool AgentLabelsHidden = true;

		public bool AgentDriving;

		public double ScreenHeightDip = 1000.0;
	}

	public const double CollapsedTopRadiusPadding = 20.0;

	public const double CollapsedWidth = 60.0;

	public const double CollapsedHeight = 8.0;

	public const double FloatingSliverTopGap = 4.0;

	public const double MaxExpandedHeight = 280.0;

	public const double ExpandedWidth = 532.0;

	public const double OnboardingExpandedWidth = 440.0;

	public const double StripHeight = 32.0;

	public const double EnvelopeWidth = 820.0;

	public const double EnvelopeHeightCap = 900.0;

	public const double TopCornerRadius = 10.0;

	public const double LibraryModeControlBandHeight = 30.0;

	public const double ContentHorizontalPadding = 18.0;

	public const double MeetingContentTopGap = 10.0;

	public const double MeetingContentBottomGap = 16.0;

	public const double TextInputBottomGap = 16.0;

	public const double ComposerInternalVerticalPadding = 8.0;

	public const double ActionButtonSize = 24.0;

	public const double AgentLabelWidthCap = 200.0;

	public const double AgentFaceBadgeSize = 26.0;

	public const double StripMinWidth = 220.0;

	public const double CompactStripMinWidth = 200.0;

	public static double InputLineHeight = 16.0;

	public const double HardwareNotchTopInset = 30.0;

	private static TextBlock? measurer;

	public static bool HasHardwareNotchEar => true;

	public static double EnvelopeHeight(double screenHeightDip, double contentScale = 1.0)
	{
		return Math.Min(900.0 * contentScale, Math.Max(120.0, screenHeightDip - 80.0)) / contentScale;
	}

	public static double ContentScale(bool streamMode)
	{
		if (!streamMode)
		{
			return 1.0;
		}
		return 1.5;
	}

	public static NotchPresentation Shown(NotchPresentation p)
	{
		if (!(p is NotchPresentation.TextResponse r) || !IsAwaitingProse(r))
		{
			return p;
		}
		return new NotchPresentation.LiveEvent(new NotchLiveEvent.ActivityThinking());
	}

	public static bool IsAwaitingProse(NotchPresentation.TextResponse r)
	{
		if (!r.IsThinking)
		{
			return string.IsNullOrWhiteSpace(r.Answer);
		}
		return true;
	}

	public static double BottomCornerRadius(NotchPresentation p)
	{
		int num;
		if (IsWorkingBar(p))
		{
			num = 14;
		}
		else
		{
			NotchPresentation notchPresentation = Shown(p);
			int num2 = ((notchPresentation is NotchPresentation.Idle) ? 14 : ((notchPresentation is NotchPresentation.LiveEvent) ? 18 : ((!(notchPresentation is NotchPresentation.OnboardingPinned)) ? 22 : 16)));
			num = num2;
		}
		return num;
	}

	public static bool IsFloatingSliver(NotchPresentation p)
	{
		if (!(p is NotchPresentation.Idle))
		{
			return IsWorkingBar(p);
		}
		return true;
	}

	public static bool IsWorkingBar(NotchPresentation p)
	{
		if (NotchPresenter.RestingMarkIsGlowingBar)
		{
			if (p is NotchPresentation.LiveEvent liveEvent)
			{
				return liveEvent.Event is NotchLiveEvent.RestingMark;
			}
			return false;
		}
		return false;
	}

	public static double ContentTopPadding(NotchPresentation p)
	{
		if (Shown(p) is NotchPresentation.TextInput)
		{
			return 34.0;
		}
		if (NeedsHardwareNotchClearance(p))
		{
			return Math.Max(28.0, 22.0);
		}
		return 6.0;
	}

	public static double ContentBottomPadding(NotchPresentation p)
	{
		if (!(Shown(p) is NotchPresentation.LiveEvent))
		{
			return BottomCornerRadius(p) / 2.0 + 4.0;
		}
		if (!NeedsHardwareNotchClearance(p))
		{
			return ContentTopPadding(p);
		}
		return 2.0;
	}

	public static bool NeedsHardwareNotchClearance(NotchPresentation p)
	{
		bool flag = HasHardwareNotchEar;
		bool flag2;
		if (flag)
		{
			if (Shown(p) is NotchPresentation.LiveEvent liveEvent)
			{
				NotchLiveEvent @event = liveEvent.Event;
				if (@event is NotchLiveEvent.IntegrationSuggestion || @event is NotchLiveEvent.IntegrationConnected)
				{
					flag2 = true;
					goto IL_0035;
				}
			}
			flag2 = false;
			goto IL_0035;
		}
		goto IL_0037;
		IL_0037:
		return flag;
		IL_0035:
		flag = flag2;
		goto IL_0037;
	}

	public static SwiftSpring? SpringFor(NotchPresentation p)
	{
		if (IsWorkingBar(p))
		{
			return NotchSprings.Close;
		}
		NotchPresentation notchPresentation = Shown(p);
		SwiftSpring? result;
		if (!(notchPresentation is NotchPresentation.Idle))
		{
			if (notchPresentation is NotchPresentation.LiveEvent liveEvent)
			{
				if (!(liveEvent.Event is NotchLiveEvent.ActivityListening))
				{
					goto IL_006f;
				}
				result = null;
			}
			else
			{
				if (!(notchPresentation is NotchPresentation.TextInput))
				{
					goto IL_006f;
				}
				result = NotchSprings.TextInputOpen;
			}
		}
		else
		{
			result = NotchSprings.Close;
		}
		goto IL_007a;
		IL_007a:
		return result;
		IL_006f:
		result = NotchSprings.Open;
		goto IL_007a;
	}

	public static (double W, double H) PanelSize(NotchPresentation state, SizeContext ctx)
	{
		state = Shown(state);
		double num = 60.0;
		if (!(state is NotchPresentation.Idle))
		{
			if (state is NotchPresentation.LiveEvent liveEvent)
			{
				NotchLiveEvent @event = liveEvent.Event;
				return LiveSize(@event, ctx);
			}
			if (!(state is NotchPresentation.TextInput textInput))
			{
				if (!(state is NotchPresentation.TextResponse textResponse))
				{
					if (state is NotchPresentation.OnboardingPinned)
					{
						return (W: 440.0, H: Math.Min(170.0, ctx.ScreenHeightDip - 80.0));
					}
					return (W: num, H: 8.0);
				}
				string text = (textResponse.Answer ?? "").Trim();
				if ((textResponse.IsThinking && text.Length == 0) || text.Length == 0)
				{
					return (W: 532.0, H: 70.0);
				}
				int num2 = 0;
				string[] array = text.Split('\n');
				for (int i = 0; i < array.Length; i++)
				{
					int num3 = Math.Max(array[i].Length, 1);
					num2 += Math.Max(1, (num3 + 74 - 1) / 74);
				}
				double num4 = 56 + 18 * Math.Max(3, num2);
				if (ctx.ResponseHasGallery)
				{
					num4 += 100.0;
				}
				num4 += 32.0;
				if (ctx.ResponseFinishedStreaming)
				{
					double val = (double)(Math.Clamp(ctx.FollowUpExtraLines, 0, 7) + 1) * InputLineHeight + 6.0;
					num4 += (double)(16 + ((ctx.FollowUpAttachmentCount > 0) ? 54 : 0)) + Math.Max(24.0, val);
				}
				return (W: 532.0, H: Math.Min(num4, ctx.ScreenHeightDip * 0.6));
			}
			double num5 = 34.0;
			double val2 = (double)(Math.Clamp(ctx.TextInputExtraLines, 0, 7) + 1) * InputLineHeight + 6.0;
			double num6 = ((ctx.TextInputAttachmentCount > 0) ? 54 : 0);
			double num7 = 16.0 + num6 + Math.Max(24.0, val2);
			double num8 = (((object)textInput.AgentFollowUp != null) ? 28 : 0);
			return (W: Math.Max(420.0, num + 220.0), H: num5 + num8 + num7 + 16.0);
		}
		return (W: num, H: 8.0);
	}

	private static (double, double) LiveSize(NotchLiveEvent e, SizeContext ctx)
	{
		double num = 60.0;
		double item = 32.0;
		double num2 = 30.0;
		if (!(e is NotchLiveEvent.AgentTask))
		{
			if (!(e is NotchLiveEvent.ActivityListening) && !(e is NotchLiveEvent.ActivityThinking) && !(e is NotchLiveEvent.ActivitySpeaking))
			{
				if (!(e is NotchLiveEvent.AgentMultiActivity agentMultiActivity))
				{
					if (!(e is NotchLiveEvent.ActivityCustomStatus activityCustomStatus))
					{
						if (!(e is NotchLiveEvent.Dictation dictation))
						{
							if (!(e is NotchLiveEvent.IntroHatchPeek))
							{
								if (!(e is NotchLiveEvent.RestingMark))
								{
									if (!(e is NotchLiveEvent.ClickyUpdate clickyUpdate))
									{
										if (!(e is NotchLiveEvent.IntegrationConnected))
										{
											if (!(e is NotchLiveEvent.IntegrationSuggestion))
											{
												if (!(e is NotchLiveEvent.MeetingCountdown))
												{
													if (!(e is NotchLiveEvent.AgentPeek))
													{
														if (!(e is NotchLiveEvent.OnboardingTutorialResume))
														{
															if (!(e is NotchLiveEvent.MorningSuggestions))
															{
																if (!(e is NotchLiveEvent.OnboardingTutorialInvite))
																{
																	if (!(e is NotchLiveEvent.AppUpdated))
																	{
																		if (!(e is NotchLiveEvent.MicrophoneTrouble))
																		{
																			if (!(e is NotchLiveEvent.DictationCopiedToClipboard))
																			{
																				if (!(e is NotchLiveEvent.VoiceReplyMutedCopiedToClipboard))
																				{
																					if (!(e is NotchLiveEvent.HandoffQueued) && !(e is NotchLiveEvent.HandoffSent))
																					{
																						if (!(e is NotchLiveEvent.SidecarLearningSuggestion))
																						{
																							if (!(e is NotchLiveEvent.SpotifyVoiceTip))
																							{
																								if (!(e is NotchLiveEvent.DictionaryWordLearned))
																								{
																									if (!(e is NotchLiveEvent.DocumentFolderAccessDenied documentFolderAccessDenied))
																									{
																										if (!(e is NotchLiveEvent.IntroSkillPing introSkillPing))
																										{
																											if (e is NotchLiveEvent.SkillPinned skillPinned)
																											{
																												return (Math.Max(200.0, num + 70.0 + (double)((Math.Clamp(skillPinned.Count, 1, 3) - 1) * 26)), item);
																											}
																											return (Math.Max(220.0, num + 160.0), item);
																										}
																										return (num, 104.0 + Math.Min(18.0, Math.Max(0.0, introSkillPing.ScaleBias) * 44.0));
																									}
																									return (documentFolderAccessDenied.ShowsDontShowAgain ? Math.Max(630.0, num + 390.0) : Math.Max(500.0, num + 260.0), gapRow(34.0));
																								}
																								return (Math.Max(460.0, num + 230.0), gapRow(34.0));
																							}
																							return (Math.Max(680.0, num + 460.0), 92.0);
																						}
																						return (Math.Max(620.0, num + 420.0), 92.0);
																					}
																					return (Math.Max(420.0, num + 220.0), 86.0);
																				}
																				return (Math.Max(380.0, num + 160.0), gapRow(90.0));
																			}
																			return (Math.Max(360.0, num + 140.0), gapRow(42.0));
																		}
																		return (ctx.MicrophoneShowsSettings ? Math.Max(540.0, num + 300.0) : Math.Max(500.0, num + 260.0), gapRow(34.0));
																	}
																	return (Math.Max(460.0, num + 230.0), gapRow(34.0));
																}
																return (Math.Max(480.0, num + 240.0), gapRow(108.0));
															}
															if (!MorningSuggestionsSurface.ShowsPaper)
															{
																return (Math.Max(440.0, num + 220.0), num2 + 6.0 + 74.0 + 12.0);
															}
															return (Math.Max(540.0, num + 220.0), num2 + 6.0 + 112.0 + 12.0);
														}
														return (Math.Max(480.0, num + 240.0), gapRow(ctx.TutorialResumeShowsContinue ? 108 : 44));
													}
													return ctx.PeekSize?.Invoke() ?? (536.0, NotchPeekLayout.ChatPanelHeight);
												}
												return (Math.Max(440.0, num + 210.0), gapRow(34.0));
											}
											return (Math.Max(620.0, num + 420.0), ctx.IntegrationHasUseCases ? Math.Max(104.0, num2 + 84.0) : Math.Max(92.0, num2 + 58.0));
										}
										return (Math.Max(600.0, num + 380.0), ctx.IntegrationHasUseCases ? Math.Max(92.0, num2 + 84.0) : Math.Max(76.0, num2 + 58.0));
									}
									return (Math.Max(560.0, num + 220.0), ctx.ClickyUpdateHeight?.Invoke(clickyUpdate) ?? ClickyUpdateLayout.PanelHeight(NotchUpdateKindNames.Parse(clickyUpdate.Kind), null, hasPreview: false));
								}
								if (NotchPresenter.RestingMarkIsGlowingBar)
								{
									return (num, 8.0);
								}
								return (Math.Max(220.0, num + 160.0), item);
							}
							return (Math.Max(200.0, num + 70.0), item);
						}
						double num3 = Math.Max(MeasureLabel(dictation.Label), 30.0) + 10.0;
						return (Math.Min(Math.Max(220.0, num + num3 * 2.0), 700.0), item);
					}
					bool agentDriving = ctx.AgentDriving;
					bool flag = agentDriving && ctx.AgentLabelsHidden;
					double num4 = (flag ? 0.0 : MeasureLabel(activityCustomStatus.Label));
					double num5 = ((agentDriving && !flag) ? Math.Min(num4, 200.0) : num4);
					double num6 = (agentDriving ? 36.0 : 0.0);
					double num7 = Math.Max(num5 + num6, 18.0) + (double)(agentDriving ? 32 : 10);
					return (Math.Min(Math.Max(flag ? 200.0 : 220.0, num + num7 * 2.0), agentDriving ? 700 : 820), item);
				}
				bool agentLabelsHidden = ctx.AgentLabelsHidden;
				double num8 = (agentLabelsHidden ? 0.0 : Math.Min(MeasureLabel(agentMultiActivity.LongestLabel), 200.0));
				double num9 = 36.0;
				return (Math.Min(Math.Max(val2: num + (Math.Max(num8 + num9, 18.0) + 32.0) * 2.0, val1: agentLabelsHidden ? 200.0 : 220.0), 700.0), item);
			}
			return (Math.Max(220.0, num + 160.0), item);
		}
		return (Math.Max(220.0, num + 160.0), item);
		static double gapRow(double row)
		{
			return 40.0 + row + 16.0;
		}
	}

	public static double MeasureLabel(string label)
	{
		if (string.IsNullOrEmpty(label))
		{
			return 0.0;
		}
		label += "...";
		if ((object)measurer == null)
		{
			measurer = new TextBlock
			{
				FontSize = 11.0,
				FontWeight = FontWeights.SemiBold,
				FontFamily = DS.Fonts.For(11.0),
				IsTextScaleFactorEnabled = false
			};
		}
		measurer.Text = label;
		measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return Math.Ceiling(measurer.DesiredSize.Width);
	}
}
