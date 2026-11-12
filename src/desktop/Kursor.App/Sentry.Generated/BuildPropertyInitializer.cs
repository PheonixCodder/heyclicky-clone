using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sentry.CompilerServices;

namespace Sentry.Generated;

[GeneratedCode("Sentry.Compiler.Extensions", "6.11.1.0")]
internal static class BuildPropertyInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		BuildProperties.Initialize(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "cswinrtmergereferencedactivationfactories", "" },
			{ "cswinrtrcwfactoryfallbackgeneratorforceoptout", "" },
			{ "cswin32applocalallowedlibraries", "" },
			{ "rootnamespace", "HeyClicky.App" },
			{ "_mvvmtoolkitisusingwindowsruntimepack", "true" },
			{ "publishselfcontained", "" },
			{ "sentrydisablesourcegenerator", "" },
			{ "projecttypeguids", "9A19103F-16F7-4668-BE54-9A1E7A4F7556" },
			{ "usedotnetnativetoolchain", "" },
			{ "enforceextendedanalyzerrules", "" },
			{ "cswinrtaotwarninglevel", "" },
			{ "cswin32inputmetadatapaths", "C:\\Users\\niles\\.nuget\\packages\\microsoft.windows.sdk.win32metadata\\71.0.30-preview\\Windows.Win32.winmd|C:\\Users\\niles\\.nuget\\packages\\microsoft.windows.wdk.win32metadata\\0.13.25-experimental\\Windows.Wdk.winmd" },
			{ "cswinrtaotexportsenabled", "" },
			{ "cswin32inputdocpaths", "C:\\Users\\niles\\.nuget\\packages\\microsoft.windows.sdk.win32docs\\0.1.42-alpha\\apidocs.msgpack" },
			{ "configuration", "Release" },
			{ "cswinrtaotoptimizerenabled", "true" },
			{ "targetframework", "net9.0-windows10.0.22621.0" },
			{ "targetplatformminversion", "10.0.19041.0" },
			{ "cswin32runasbuildtask", "false" },
			{ "enablecodestyleseverity", "" },
			{ "cswin32platformtarget", "" },
			{ "invariantglobalization", "false" },
			{ "platformneutralassembly", "" },
			{ "effectiveanalysislevelstyle", "9.0" },
			{ "mvvmtoolkitenableinotifypropertychangingsupport", "true" },
			{ "projectdir", "C:\\Users\\niles\\code\\clicky-closed\\windows\\src\\HeyClicky.App\\" },
			{ "_ispublishing", "true" },
			{ "enablecomhosting", "" },
			{ "enablegeneratedcominterfacecomimportinterop", "" },
			{ "publishtrimmed", "" },
			{ "cswinrtccwlookuptablegeneratorenabled", "true" },
			{ "_supportedplatformlist", "Linux,macOS,Windows" },
			{ "cswinrtusewindowsuixamlprojections", "false" },
			{ "cswinrtrcwfactoryfallbackgeneratorforceoptin", "" },
			{ "usingmicrosoftnetsdkweb", "" },
			{ "cswinrtcomponent", "" },
			{ "publishaot", "" }
		});
	}
}
