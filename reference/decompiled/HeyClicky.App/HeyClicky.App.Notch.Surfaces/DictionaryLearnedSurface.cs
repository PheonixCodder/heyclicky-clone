using System;
using HeyClicky.App.Design;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class DictionaryLearnedSurface : NotchSurface
{
	public const double AutoHideSeconds = 6.0;

	private readonly Grid view;

	public override FrameworkElement View => view;

	public DictionaryLearnedSurface(NotchLiveEvent.DictionaryWordLearned e)
	{
		DictionaryLearnedSurface dictionaryLearnedSurface = this;
		Grid tile = CardParts.IconTile("\ue82d", 15.0, DS.Hex(2450411u));
		StackPanel lines = CardParts.TwoLines("Added to your dictionary", "“" + e.Word + "”");
		GelActionButton gelActionButton = new GelActionButton("Remove", null, GelAppearance.SecondaryWhite, 30.0, 14.0).Tip("Remove this word from your dictionary");
		gelActionButton.Click += delegate
		{
			Action<string> removeLearnedWord = DictationHooks.RemoveLearnedWord;
			if (removeLearnedWord != null)
			{
				removeLearnedWord(e.Word);
			}
			else if (dictionaryLearnedSurface.Host.Actions is INotchCardActions notchCardActions)
			{
				notchCardActions.RemoveDictionaryWord(e.Word);
			}
			dictionaryLearnedSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.DictionaryWordLearned));
		};
		RingCloseButton ringCloseButton = new RingCloseButton(showRing: true).Tip("Dismiss");
		ringCloseButton.Click += delegate
		{
			dictionaryLearnedSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.DictionaryWordLearned));
		};
		ringCloseButton.StartCountdown(6.0);
		view = CardParts.HeadsUpRow(tile, lines, gelActionButton, ringCloseButton);
	}
}
