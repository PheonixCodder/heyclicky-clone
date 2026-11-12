using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkInlineAnswerField : Grid
{
	private readonly ClickyInkBlankLine line;

	private readonly ClickyPaperGelButton send;

	public Action<string> OnSend { get; set; } = delegate
	{
	};

	private string Trimmed => line.Text.TrimWsNl();

	public ClickyInkInlineAnswerField(string placeholder, string sendTitle = "Send")
	{
		line = new ClickyInkBlankLine(placeholder, PaperColor.Hex("#3B2A10"));
		send = new ClickyPaperGelButton(sendTitle, Send);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnSpacing = 10.0;
		line.VerticalAlignment = VerticalAlignment.Bottom;
		send.VerticalAlignment = VerticalAlignment.Bottom;
		Grid.SetColumn(send, 1);
		base.Children.Add(line);
		base.Children.Add(send);
		line.Submitted += Send;
		line.TextChanged += delegate
		{
			Refresh();
		};
		base.HorizontalAlignment = HorizontalAlignment.Stretch;
		Refresh();
	}

	private void Refresh()
	{
		bool flag = Trimmed.Length == 0;
		send.Enabled = !flag;
		send.Opacity = (flag ? 0.5 : 1.0);
	}

	private void Send()
	{
		if (Trimmed.Length != 0)
		{
			OnSend(Trimmed);
			line.Text = "";
		}
	}

	public void SetDraft(string text)
	{
		line.Text = text;
	}
}
