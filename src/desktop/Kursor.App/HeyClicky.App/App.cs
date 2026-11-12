using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.App.HeyClicky_App_XamlTypeInfo;
using HeyClicky.App.Services;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Telemetry;
using HeyClicky.Drawing;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using WinRT;
using WinRT.HeyClickyVtableClasses;

namespace HeyClicky.App;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IApplicationOverrides")]
[WinRTExposedType(typeof(HeyClicky_App_AppWinRTTypeDetails))]
public class App : Application, IXamlMetadataProvider
{
	private AppHost? host;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	private XamlMetaDataProvider __appProvider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	private XamlMetaDataProvider _AppProvider
	{
		get
		{
			if (__appProvider == null)
			{
				__appProvider = new XamlMetaDataProvider();
			}
			return __appProvider;
		}
	}

	public App()
	{
		InitializeComponent();
		base.UnhandledException += delegate(object _, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			CrashReporting.ReportUiException(e.Exception);
			e.Handled = true;
			if (IsLayoutCycle(e.Exception))
			{
				host?.RecoverFromLayoutCycle();
			}
		};
	}

	private static bool IsLayoutCycle(Exception? exception)
	{
		if (exception != null)
		{
			if (exception.HResult != -2144665580 && !(exception.GetType().Name == "LayoutCycleException"))
			{
				return exception.Message.Contains("Layout cycle detected", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		string value = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
		Log.Info("app", $"launched {value} (+{Program.MillisecondsSinceProcessStart():0} ms since process start) args=[{string.Join(' ', Program.Args)}]");
		AppOptions appOptions = AppOptions.Parse(Program.Args);
		if (appOptions.AllowCapture)
		{
			ScreenDrawingCanvas.ExcludeFromCapture = false;
			CaptureExclusion.Enabled = false;
		}
		DispatcherQueue forCurrentThread = DispatcherQueue.GetForCurrentThread();
		(AppServices Services, RealStack? Real) tuple = CompositionRoot.Build(appOptions, forCurrentThread, SynchronizationContext.Current);
		AppServices item = tuple.Services;
		RealStack item2 = tuple.Real;
		AppHost started = (host = new AppHost(item, appOptions, item2));
		Program.ArgumentsForwarded += delegate(string[] forwarded)
		{
			started.DispatcherQueue.TryEnqueue(delegate
			{
				started.HandleForwardedArgs(forwarded);
			});
		};
		try
		{
			started.Start();
		}
		catch (Exception ex)
		{
			Log.Error("startup", "HeyClicky failed to start", ex);
			CrashReporting.ReportUiException(ex);
			try
			{
				CrashReporter.Current.FlushAsync(TimeSpan.FromSeconds(3L)).Wait(TimeSpan.FromSeconds(4L));
			}
			catch
			{
			}
			MessageBoxW(0, $"HeyClicky couldn't start.\n\n{ex.GetType().Name}: {ex.Message}\n\nWe've been sent the details. If it keeps happening, email hi@heyclicky.com with a screenshot of this message.", "HeyClicky", 65552u);
			Exit();
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBoxW(nint owner, string text, string caption, uint type);

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	internal static void XamlGeneratedCreateApplicationInstance()
	{
		_XamlGeneratedCreateApplicationInstance();
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	private static void _XamlGeneratedCreateApplicationInstance()
	{
		new App();
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///App.xaml");
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(Type type)
	{
		return _AppProvider.GetXamlType(type);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(string fullName)
	{
		return _AppProvider.GetXamlType(fullName);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
	[DebuggerNonUserCode]
	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return _AppProvider.GetXmlnsDefinitions();
	}
}
