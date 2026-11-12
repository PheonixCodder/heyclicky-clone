using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.UI;

public class AccessibleGrid : Grid
{
	private sealed class AccessibleGridPeer(AccessibleGrid owner) : FrameworkElementAutomationPeer(owner)
	{
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return owner.ControlType;
		}

		protected override string GetClassNameCore()
		{
			return "Group";
		}

		protected override bool IsControlElementCore()
		{
			return true;
		}

		protected override bool IsContentElementCore()
		{
			return !string.IsNullOrEmpty(AutomationProperties.GetName(owner));
		}
	}

	public AutomationControlType ControlType { get; set; } = AutomationControlType.Group;

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new AccessibleGridPeer(this);
	}
}
