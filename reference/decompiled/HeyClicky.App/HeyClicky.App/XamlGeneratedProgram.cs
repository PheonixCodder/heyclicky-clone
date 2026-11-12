using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace HeyClicky.App;

internal static class XamlGeneratedProgram
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	internal static void XamlGeneratedMain()
	{
		ComWrappersSupport.InitializeComWrappers();
		Application.Start(delegate
		{
			SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
			App.XamlGeneratedCreateApplicationInstance();
		});
	}
}
