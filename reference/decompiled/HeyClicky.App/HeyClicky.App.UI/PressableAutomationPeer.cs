using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.UI;

public sealed class PressableAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IToggleProvider
{
	private readonly Pressable owner;

	public ToggleState ToggleState
	{
		get
		{
			if (owner.AccessibleToggleState != true)
			{
				return ToggleState.Off;
			}
			return ToggleState.On;
		}
	}

	public PressableAutomationPeer(Pressable owner)
		: base(owner)
	{
		this.owner = owner;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		if (owner.AccessibleToggleState.HasValue)
		{
			return AutomationControlType.CheckBox;
		}
		return AutomationControlType.Button;
	}

	protected override string GetClassNameCore()
	{
		if (owner.AccessibleToggleState.HasValue)
		{
			return "ToggleSwitch";
		}
		return "Button";
	}

	protected override bool IsControlElementCore()
	{
		return true;
	}

	protected override bool IsContentElementCore()
	{
		return true;
	}

	protected override bool IsEnabledCore()
	{
		return owner.Enabled;
	}

	protected override string GetNameCore()
	{
		string nameCore = base.GetNameCore();
		if (!string.IsNullOrWhiteSpace(nameCore))
		{
			return nameCore;
		}
		if (ToolTipService.GetToolTip(owner) is string text && !string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return Accessibility.TextWithin(owner);
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		switch (patternInterface)
		{
		case PatternInterface.Invoke:
			if (!owner.AccessibleToggleState.HasValue)
			{
				return this;
			}
			break;
		case PatternInterface.Toggle:
			if (owner.AccessibleToggleState.HasValue)
			{
				return this;
			}
			break;
		}
		return base.GetPatternCore(patternInterface);
	}

	public void Invoke()
	{
		if (owner.Enabled)
		{
			owner.RaiseClick();
		}
	}

	public void Toggle()
	{
		if (owner.Enabled)
		{
			owner.RaiseClick();
		}
	}
}
