using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch;

public abstract class NotchSurface
{
	protected INotchHost Host { get; private set; }

	public abstract FrameworkElement View { get; }

	public virtual FrameworkElement? Overlay => null;

	public virtual VerticalAlignment ContentAlignment => VerticalAlignment.Top;

	public virtual bool WantsKeyboard => false;

	public virtual bool HoldsKeyboard => false;

	protected bool StillShowing { get; private set; }

	public virtual bool CanUpdate(NotchPresentation p)
	{
		return false;
	}

	public virtual void Update(NotchPresentation p)
	{
	}

	public virtual void FocusInput()
	{
	}

	public virtual bool AcceptFiles(IReadOnlyList<string> paths)
	{
		return false;
	}

	public virtual void SetDropTargeting(bool on)
	{
	}

	public void Attach(INotchHost host, bool stillShowing = false)
	{
		Host = host;
		StillShowing = stillShowing;
		OnAttached();
	}

	protected virtual void OnAttached()
	{
	}

	public virtual void Detached()
	{
	}
}
