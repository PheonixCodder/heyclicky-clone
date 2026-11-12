using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections;
using ABI.System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace WinRT.HeyClickyGenericHelpers;

internal static class GlobalVtableLookup
{
	[ModuleInitializer]
	internal static void InitializeGlobalVtableLookup()
	{
		ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup(LookupVtableEntries);
		ComWrappersSupport.RegisterTypeRuntimeClassNameLookup(LookupRuntimeClassName);
	}

	private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(System.Type type)
	{
		switch (type.ToString())
		{
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Windows.Foundation.Point]":
		case "System.Collections.Generic.List`1[Windows.Foundation.Point]":
			_ = IList_Windows_Foundation_Point.Initialized;
			_ = IReadOnlyList_Windows_Foundation_Point.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_Windows_Foundation_Point.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<Point>.IID,
					Vtable = IListMethods<Point>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<Point>.IID,
					Vtable = IReadOnlyListMethods<Point>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<Point>.IID,
					Vtable = IEnumerableMethods<Point>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.List`1[(System.Collections.Generic.List, System.Boolean)`2[System.Collections.Generic.List`1[Windows.Foundation.Point],System.Boolean]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.CloudFaceTraits+Blob]":
		case "System.Collections.Generic.List`1[HeyClicky.DisplayInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.RosterClicky]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Settings.CustomConnector]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.HairPiece]":
		case "System.Collections.Generic.List`1[HeyClicky.AgentTaskSnapshot]":
		case "System.Collections.Generic.List`1[HeyClicky.App.OneBrain.ClickyTourJob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentTaskInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Worker.InterviewLine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Settings.CustomConnector]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, HeyClicky.App.Ink.ClickyPaperFileKind)`2[System.String,HeyClicky.App.Ink.ClickyPaperFileKind]]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkRoutineControl]":
		case "System.Collections.Generic.List`1[(System.String, HeyClicky.App.Ink.ClickyPaperFileKind)`2[System.String,HeyClicky.App.Ink.ClickyPaperFileKind]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentClickyInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Home.LessonService+Recent]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.AgentNotification]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.AgentThreadSummary]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentThreadInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Services.AppEntry]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.AgentIntegrationInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Clickys.ClickyInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.QueuedProactiveTask]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.DisplayInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.ResultCardLayouts+Arrangement]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.ProactiveSuggestionCard]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkDocument+Block]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Onboarding.TutorialWindow+Step]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentClickyInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Worker.InterviewLine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.StarterTaskCard]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.DockWindowKey]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.AgentNotification]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Home.LessonService+Recent]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Text.Json.Nodes.JsonNode]]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Clickys.ClickyInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Notch.ChatHandOff]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Services.AppEntry]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkRoutineControl]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.HairPiece]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Home.StoredLesson]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.AgentThreadSummary]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Voice.VoiceLanguage]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Runtime.CustomConnectorRegistration]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String Role, System.String Text)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentThreadInfo]":
		case "System.Collections.Generic.List`1[(System.String Role, System.String Text)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.ResultCardLayouts+Arrangement]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentTaskInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.OneBrain.ClickyTourJob]":
		case "System.Collections.Generic.List`1[(System.String, System.String, System.Func)`3[System.String,System.String,System.Func`1[Microsoft.UI.Xaml.FrameworkElement]]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.PortraitAppearance]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String, System.Func)`3[System.String,System.String,System.Func`1[Microsoft.UI.Xaml.FrameworkElement]]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.DockWindowKey]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Jobs.ClickyJob]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkDashboardBoard+Section]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkStreakDay]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkDashboardBoard+Section]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.AgentIntegrationInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkDocument+Block]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.PortraitCharacter]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Voice.VoiceLanguage]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Notch.ChatHandOff]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkStreakDay]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.CloudFaceTraits+Blob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Runtime.CustomConnectorRegistration]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.StarterTaskCard]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.ProactiveSuggestionCard]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Routines.ClickyRoutine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.PortraitAppearance]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.AgentTaskSnapshot]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Home.StoredLesson]":
		case "System.Collections.Generic.List`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.PortraitCharacter]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.QueuedProactiveTask]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.RosterClicky]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Collections.Generic.List, System.Boolean)`2[System.Collections.Generic.List`1[Windows.Foundation.Point],System.Boolean]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Routines.ClickyRoutine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Jobs.ClickyJob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Onboarding.TutorialWindow+Step]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Microsoft.UI.Xaml.FrameworkElement]":
		case "System.Collections.Generic.List`1[Microsoft.UI.Xaml.FrameworkElement]":
			_ = IList_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[17]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<FrameworkElement>.IID,
					Vtable = IListMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<FrameworkElement>.IID,
					Vtable = IReadOnlyListMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IAnimationObject>.IID,
					Vtable = IReadOnlyListMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement2>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<UIElement>.IID,
					Vtable = IReadOnlyListMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<DependencyObject>.IID,
					Vtable = IReadOnlyListMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<FrameworkElement>.IID,
					Vtable = IEnumerableMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IAnimationObject>.IID,
					Vtable = IEnumerableMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement>.IID,
					Vtable = IEnumerableMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement2>.IID,
					Vtable = IEnumerableMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<UIElement>.IID,
					Vtable = IEnumerableMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<DependencyObject>.IID,
					Vtable = IEnumerableMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "HeyClicky.Platform.Audio.WasapiAudioOutput":
		case "System.Threading.Tasks.Task`1[System.Threading.Tasks.Task]":
		case "System.Threading.Tasks.Task`1[System.Boolean]":
		case "HeyClicky.Platform.Input.ShortcutMonitor":
		case "HeyClicky.Platform.Permissions.PermissionService":
		case "HeyClicky.Platform.Capture.ScreenCaptureService":
		case "HeyClicky.Drawing.ScreenDrawingCanvas":
		case "System.IO.MemoryStream":
		case "HeyClicky.Core.Notch.NotchPresenter":
		case "System.Threading.Tasks.Task`1[System.Text.Json.Nodes.JsonObject]":
		case "System.Threading.Tasks.Task`1[HeyClicky.Core.Browser.UploadApprovalAnswer]":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.Dictionary`2[System.String,System.Boolean]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Boolean]":
			_ = IDictionary_string_bool.Initialized;
			_ = IReadOnlyDictionary_string_bool.Initialized;
			_ = KeyValuePair_string_bool.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__bool_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDictionaryMethods<string, bool>.IID,
					Vtable = IDictionaryMethods<string, bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, bool>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, bool>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, bool>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Text.Json.Nodes.JsonObject":
		case "System.Collections.Generic.Dictionary`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "System.Collections.Generic.Dictionary`2+ValueCollection[System.String,HeyClicky.App.Portraits.PortraitAppearance]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.Byte[]]":
		case "System.Collections.Generic.Dictionary`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Byte[]]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Byte[]]":
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Boolean]]":
			_ = KeyValuePair_string_bool.Initialized;
			_ = IEnumerator_System_Collections_Generic_KeyValuePair_string__bool_.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, bool>>.IID,
					Vtable = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, bool>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Byte]]":
			_ = IEnumerable_byte.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_byte_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<byte>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.HashSet`1[System.Guid]":
			_ = IEnumerable_System_Guid.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<Guid>.IID,
					Vtable = IEnumerableMethods<Guid>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.List`1[System.Double]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Double]":
			_ = IList_double.Initialized;
			_ = IReadOnlyList_double.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_double.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<double>.IID,
					Vtable = IListMethods<double>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<double>.IID,
					Vtable = IReadOnlyListMethods<double>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<double>.IID,
					Vtable = IEnumerableMethods<double>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "HeyClicky.App.Portraits.SkinTone[]":
		case "HeyClicky.App.Portraits.Colorway[]":
		case "(System.String Event, System.String[] Keys)[]":
		case "HeyClicky.App.Portraits.PortraitCharacter[]":
		case "(System.Single v, System.Double s)[]":
		case "(System.Single, System.Double)[]":
		case "HeyClicky.App.Portraits.HairPiece[]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.UIElement]":
			_ = IEnumerator_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IEnumerator_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<UIElement>.IID,
					Vtable = IEnumeratorMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IAnimationObject>.IID,
					Vtable = IEnumeratorMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement>.IID,
					Vtable = IEnumeratorMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement2>.IID,
					Vtable = IEnumeratorMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<DependencyObject>.IID,
					Vtable = IEnumeratorMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.Int32,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.Int32,System.String]":
			_ = IDictionary_int_string.Initialized;
			_ = IReadOnlyDictionary_int_string.Initialized;
			_ = KeyValuePair_int_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_int__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDictionaryMethods<int, string>.IID,
					Vtable = IDictionaryMethods<int, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<int, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<int, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<int, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<int, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Int32]]":
			_ = KeyValuePair_string_int.Initialized;
			_ = IEnumerator_System_Collections_Generic_KeyValuePair_string__int_.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, int>>.IID,
					Vtable = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, int>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.Dictionary`2[System.String,System.Int32]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Int32]":
			_ = IDictionary_string_int.Initialized;
			_ = IReadOnlyDictionary_string_int.Initialized;
			_ = KeyValuePair_string_int.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__int_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDictionaryMethods<string, int>.IID,
					Vtable = IDictionaryMethods<string, int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, int>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, int>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, int>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.FrameworkElement]":
			_ = IEnumerator_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IEnumerator_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IEnumerator_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[8]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<FrameworkElement>.IID,
					Vtable = IEnumeratorMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IAnimationObject>.IID,
					Vtable = IEnumeratorMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement>.IID,
					Vtable = IEnumeratorMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement2>.IID,
					Vtable = IEnumeratorMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<UIElement>.IID,
					Vtable = IEnumeratorMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<DependencyObject>.IID,
					Vtable = IEnumeratorMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
			_ = IDictionary_string_string.Initialized;
			_ = IReadOnlyDictionary_string_string.Initialized;
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDictionaryMethods<string, string>.IID,
					Vtable = IDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IList]":
			_ = IEnumerator_System_Collections_IList.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IList>.IID,
					Vtable = IEnumeratorMethods<IList>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IAnimationObject]":
			_ = IEnumerator_Microsoft_UI_Composition_IAnimationObject.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IAnimationObject>.IID,
					Vtable = IEnumeratorMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.String[]":
			_ = IList_string.Initialized;
			_ = IReadOnlyList_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[13]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<string>.IID,
					Vtable = IListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<string>.IID,
					Vtable = IReadOnlyListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<char>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<string>.IID,
					Vtable = IEnumerableMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<char>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]":
		case "System.Collections.Generic.List`1[System.String]":
			_ = IList_string.Initialized;
			_ = IReadOnlyList_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[13]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<string>.IID,
					Vtable = IListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<string>.IID,
					Vtable = IReadOnlyListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<char>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<string>.IID,
					Vtable = IEnumerableMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<char>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IVisualElement]":
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement>.IID,
					Vtable = IEnumeratorMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Int32]":
			_ = IReadOnlyDictionary_string_int.Initialized;
			_ = KeyValuePair_string_int.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__int_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, int>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, int>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, int>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]":
			_ = KeyValuePair_int_string.Initialized;
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = KeyValuePairMethods<int, string>.IID,
					Vtable = KeyValuePairMethods<int, string>.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]":
			_ = IEnumerator_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[6]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<string>.IID,
					Vtable = IEnumeratorMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<char>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "Microsoft.UI.Xaml.FrameworkElement[]":
			_ = IList_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_FrameworkElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[17]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<FrameworkElement>.IID,
					Vtable = IListMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<FrameworkElement>.IID,
					Vtable = IReadOnlyListMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IAnimationObject>.IID,
					Vtable = IReadOnlyListMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement2>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<UIElement>.IID,
					Vtable = IReadOnlyListMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<DependencyObject>.IID,
					Vtable = IReadOnlyListMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<FrameworkElement>.IID,
					Vtable = IEnumerableMethods<FrameworkElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IAnimationObject>.IID,
					Vtable = IEnumerableMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement>.IID,
					Vtable = IEnumerableMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement2>.IID,
					Vtable = IEnumerableMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<UIElement>.IID,
					Vtable = IEnumerableMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<DependencyObject>.IID,
					Vtable = IEnumerableMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]":
			_ = IEnumerator_byte.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<byte>.IID,
					Vtable = IEnumeratorMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.DateTimeOffset]":
			_ = IEnumerator_System_DateTimeOffset.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.DateTimeOffset>.IID,
					Vtable = IEnumeratorMethods<System.DateTimeOffset>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.String]":
		case "System.Collections.Generic.HashSet`1[System.String]":
		case "System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.Char]":
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[6]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<string>.IID,
					Vtable = IEnumerableMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<char>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.DateTimeOffset]":
		case "System.Collections.Generic.List`1[System.DateTimeOffset]":
			_ = IList_System_DateTimeOffset.Initialized;
			_ = IReadOnlyList_System_DateTimeOffset.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_System_DateTimeOffset.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<System.DateTimeOffset>.IID,
					Vtable = IListMethods<System.DateTimeOffset>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<System.DateTimeOffset>.IID,
					Vtable = IReadOnlyListMethods<System.DateTimeOffset>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.DateTimeOffset>.IID,
					Vtable = IEnumerableMethods<System.DateTimeOffset>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Microsoft.UI.Xaml.UIElement]":
		case "System.Collections.Generic.List`1[Microsoft.UI.Xaml.UIElement]":
			_ = IList_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IReadOnlyList_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_UIElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IAnimationObject.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement.Initialized;
			_ = IEnumerable_Microsoft_UI_Composition_IVisualElement2.Initialized;
			_ = IEnumerable_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[15]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<UIElement>.IID,
					Vtable = IListMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<UIElement>.IID,
					Vtable = IReadOnlyListMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IAnimationObject>.IID,
					Vtable = IReadOnlyListMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IVisualElement2>.IID,
					Vtable = IReadOnlyListMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<DependencyObject>.IID,
					Vtable = IReadOnlyListMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<UIElement>.IID,
					Vtable = IEnumerableMethods<UIElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IAnimationObject>.IID,
					Vtable = IEnumerableMethods<IAnimationObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement>.IID,
					Vtable = IEnumerableMethods<IVisualElement>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IVisualElement2>.IID,
					Vtable = IEnumerableMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<DependencyObject>.IID,
					Vtable = IEnumerableMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Byte[][]":
			_ = IReadOnlyList_System_Collections_IList.Initialized;
			_ = IList_byte.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IList_byte_.Initialized;
			_ = IReadOnlyList_byte.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IReadOnlyList_byte_.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IReadOnlyList_object_.Initialized;
			_ = IEnumerable_byte.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_byte_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IEnumerable_System_Collections_IList.Initialized;
			_ = IEnumerable_System_Collections_Generic_IList_byte_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IReadOnlyList_byte_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IReadOnlyList_object_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_byte_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[18]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IList>.IID,
					Vtable = IReadOnlyListMethods<IList>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IList<byte>>.IID,
					Vtable = IReadOnlyListMethods<IList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IReadOnlyList<byte>>.IID,
					Vtable = IReadOnlyListMethods<IReadOnlyList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IReadOnlyList<object>>.IID,
					Vtable = IReadOnlyListMethods<IReadOnlyList<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<byte>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IList>.IID,
					Vtable = IEnumerableMethods<IList>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IList<byte>>.IID,
					Vtable = IEnumerableMethods<IList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IReadOnlyList<byte>>.IID,
					Vtable = IEnumerableMethods<IReadOnlyList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IReadOnlyList<object>>.IID,
					Vtable = IEnumerableMethods<IReadOnlyList<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<byte>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.Int32]":
			_ = KeyValuePair_string_int.Initialized;
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = KeyValuePairMethods<string, int>.IID,
					Vtable = KeyValuePairMethods<string, int>.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Notch.Peek.INotchDocumentRenderer]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Notch.Peek.INotchDocumentRenderer]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Drive.IAgentDriveJob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Drive.IAgentDriveJob]":
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Guid]":
			_ = IEnumerator_System_Guid.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<Guid>.IID,
					Vtable = IEnumeratorMethods<Guid>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IList`1[System.Byte]]":
			_ = IList_byte.Initialized;
			_ = IEnumerator_System_Collections_Generic_IList_byte_.Initialized;
			_ = IEnumerable_byte.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_byte_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IList<byte>>.IID,
					Vtable = IEnumeratorMethods<IList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<byte>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]]":
			_ = KeyValuePair_int_string.Initialized;
			_ = IEnumerator_System_Collections_Generic_KeyValuePair_int__string_.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<int, string>>.IID,
					Vtable = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<int, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Boolean[]":
			_ = IList_bool.Initialized;
			_ = IReadOnlyList_bool.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_bool.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<bool>.IID,
					Vtable = IListMethods<bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<bool>.IID,
					Vtable = IReadOnlyListMethods<bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<bool>.IID,
					Vtable = IEnumerableMethods<bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.String]]":
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerator_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.String]":
			_ = IReadOnlyDictionary_string_string.Initialized;
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.Int32,System.String]":
			_ = IReadOnlyDictionary_int_string.Initialized;
			_ = KeyValuePair_int_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_int__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<int, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<int, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<int, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<int, string>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]":
			_ = IEnumerator_int.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<int>.IID,
					Vtable = IEnumeratorMethods<int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IVisualElement2]":
			_ = IEnumerator_Microsoft_UI_Composition_IVisualElement2.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IVisualElement2>.IID,
					Vtable = IEnumeratorMethods<IVisualElement2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Windows.Foundation.Point]":
			_ = IEnumerator_Windows_Foundation_Point.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<Point>.IID,
					Vtable = IEnumeratorMethods<Point>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Byte[]":
			_ = IList_byte.Initialized;
			_ = IReadOnlyList_byte.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_byte.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<byte>.IID,
					Vtable = IListMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<byte>.IID,
					Vtable = IReadOnlyListMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<byte>.IID,
					Vtable = IEnumerableMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IReadOnlyList`1[System.Byte]]":
			_ = IReadOnlyList_byte.Initialized;
			_ = IEnumerator_System_Collections_Generic_IReadOnlyList_byte_.Initialized;
			_ = IEnumerable_byte.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_byte_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IReadOnlyList<byte>>.IID,
					Vtable = IEnumeratorMethods<IReadOnlyList<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<byte>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<byte>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Object]]":
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]":
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.List`1[System.Text.Json.Nodes.JsonObject]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Text.Json.Nodes.JsonObject]":
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[8]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Char]]":
			_ = IEnumerable_char.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<char>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.HashSet`1[System.Int32]":
			_ = IEnumerable_int.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<int>.IID,
					Vtable = IEnumerableMethods<int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Boolean]":
			_ = IReadOnlyDictionary_string_bool.Initialized;
			_ = KeyValuePair_string_bool.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__bool_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, bool>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, bool>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, bool>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Double]":
			_ = IEnumerator_double.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<double>.IID,
					Vtable = IEnumeratorMethods<double>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IReadOnlyList`1[System.Object]]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IReadOnlyList_object_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IReadOnlyList<object>>.IID,
					Vtable = IEnumeratorMethods<IReadOnlyList<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.Boolean]":
			_ = KeyValuePair_string_bool.Initialized;
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = KeyValuePairMethods<string, bool>.IID,
					Vtable = KeyValuePairMethods<string, bool>.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Boolean]":
			_ = IEnumerator_bool.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<bool>.IID,
					Vtable = IEnumeratorMethods<bool>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.DependencyObject]":
			_ = IEnumerator_Microsoft_UI_Xaml_DependencyObject.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<DependencyObject>.IID,
					Vtable = IEnumeratorMethods<DependencyObject>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IEnumerable]":
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.String]":
			_ = KeyValuePair_string_string.Initialized;
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = KeyValuePairMethods<string, string>.IID,
					Vtable = KeyValuePairMethods<string, string>.AbiToProjectionVftablePtr
				}
			};
		default:
			return null;
		}
	}

	private static string LookupRuntimeClassName(System.Type type)
	{
		switch (type.ToString())
		{
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Windows.Foundation.Point]":
		case "System.Collections.Generic.List`1[Windows.Foundation.Point]":
			return "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Point>";
		case "System.Collections.Generic.List`1[(System.Collections.Generic.List, System.Boolean)`2[System.Collections.Generic.List`1[Windows.Foundation.Point],System.Boolean]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.CloudFaceTraits+Blob]":
		case "System.Collections.Generic.List`1[HeyClicky.DisplayInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.RosterClicky]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Settings.CustomConnector]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.HairPiece]":
		case "System.Collections.Generic.List`1[HeyClicky.AgentTaskSnapshot]":
		case "System.Collections.Generic.List`1[HeyClicky.App.OneBrain.ClickyTourJob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentTaskInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Worker.InterviewLine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Settings.CustomConnector]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, HeyClicky.App.Ink.ClickyPaperFileKind)`2[System.String,HeyClicky.App.Ink.ClickyPaperFileKind]]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkRoutineControl]":
		case "System.Collections.Generic.List`1[(System.String, HeyClicky.App.Ink.ClickyPaperFileKind)`2[System.String,HeyClicky.App.Ink.ClickyPaperFileKind]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentClickyInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Home.LessonService+Recent]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.AgentNotification]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.AgentThreadSummary]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentThreadInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Services.AppEntry]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.AgentIntegrationInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Clickys.ClickyInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.QueuedProactiveTask]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.DisplayInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.ResultCardLayouts+Arrangement]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.ProactiveSuggestionCard]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkDocument+Block]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Onboarding.TutorialWindow+Step]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentClickyInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Worker.InterviewLine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.StarterTaskCard]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Dock.DockWindowKey]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.AgentNotification]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Home.LessonService+Recent]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Text.Json.Nodes.JsonNode]]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Clickys.ClickyInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Notch.ChatHandOff]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Services.AppEntry]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkRoutineControl]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.HairPiece]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Home.StoredLesson]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.AgentThreadSummary]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Voice.VoiceLanguage]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Runtime.CustomConnectorRegistration]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String Role, System.String Text)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Tools.AgentThreadInfo]":
		case "System.Collections.Generic.List`1[(System.String Role, System.String Text)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.ResultCardLayouts+Arrangement]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Tools.AgentTaskInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.OneBrain.ClickyTourJob]":
		case "System.Collections.Generic.List`1[(System.String, System.String, System.Func)`3[System.String,System.String,System.Func`1[Microsoft.UI.Xaml.FrameworkElement]]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.PortraitAppearance]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String, System.Func)`3[System.String,System.String,System.Func`1[Microsoft.UI.Xaml.FrameworkElement]]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.DockWindowKey]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Jobs.ClickyJob]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkDashboardBoard+Section]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkStreakDay]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Ink.ClickyInkDashboardBoard+Section]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.AgentIntegrationInfo]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkDocument+Block]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.PortraitCharacter]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Voice.VoiceLanguage]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Notch.ChatHandOff]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Ink.ClickyInkStreakDay]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.CloudFaceTraits+Blob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Runtime.CustomConnectorRegistration]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.StarterTaskCard]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Core.Proactive.ProactiveSuggestionCard]":
		case "System.Collections.Generic.List`1[HeyClicky.Agents.Routines.ClickyRoutine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.PortraitAppearance]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.AgentTaskSnapshot]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Home.StoredLesson]":
		case "System.Collections.Generic.List`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Portraits.PortraitCharacter]":
		case "System.Collections.Generic.List`1[HeyClicky.Core.Proactive.QueuedProactiveTask]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Portraits.RosterClicky]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Collections.Generic.List, System.Boolean)`2[System.Collections.Generic.List`1[Windows.Foundation.Point],System.Boolean]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Routines.ClickyRoutine]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.Agents.Jobs.ClickyJob]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Onboarding.TutorialWindow+Step]":
			return "Windows.Foundation.Collections.IVectorView`1<Object>";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Microsoft.UI.Xaml.FrameworkElement]":
		case "System.Collections.Generic.List`1[Microsoft.UI.Xaml.FrameworkElement]":
			return "Windows.Foundation.Collections.IVector`1<Microsoft.UI.Xaml.FrameworkElement>";
		case "HeyClicky.Platform.Audio.WasapiAudioOutput":
		case "System.Threading.Tasks.Task`1[System.Threading.Tasks.Task]":
		case "System.Threading.Tasks.Task`1[System.Boolean]":
		case "HeyClicky.Platform.Input.ShortcutMonitor":
		case "HeyClicky.Platform.Permissions.PermissionService":
		case "HeyClicky.Platform.Capture.ScreenCaptureService":
		case "HeyClicky.Drawing.ScreenDrawingCanvas":
		case "System.IO.MemoryStream":
		case "HeyClicky.Core.Notch.NotchPresenter":
		case "System.Threading.Tasks.Task`1[System.Text.Json.Nodes.JsonObject]":
		case "System.Threading.Tasks.Task`1[HeyClicky.Core.Browser.UploadApprovalAnswer]":
			return "Windows.Foundation.IClosable";
		case "System.Collections.Generic.Dictionary`2[System.String,System.Boolean]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Boolean]":
			return "Windows.Foundation.Collections.IMap`2<String, Boolean>";
		case "System.Text.Json.Nodes.JsonObject":
		case "System.Collections.Generic.Dictionary`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "System.Collections.Generic.Dictionary`2+ValueCollection[System.String,HeyClicky.App.Portraits.PortraitAppearance]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.Byte[]]":
		case "System.Collections.Generic.Dictionary`2[System.String,HeyClicky.App.Dock.ArcRect]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Byte[]]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,HeyClicky.App.Dock.ArcCardView]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Byte[]]":
			return "Windows.Foundation.Collections.IIterable`1<Object>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Boolean]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<String, Boolean>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Byte]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<UInt8>>";
		case "System.Collections.Generic.HashSet`1[System.Guid]":
			return "Windows.Foundation.Collections.IIterable`1<System.Guid>";
		case "System.Collections.Generic.List`1[System.Double]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Double]":
			return "Windows.Foundation.Collections.IVector`1<Double>";
		case "HeyClicky.App.Portraits.SkinTone[]":
		case "System.String[]":
		case "HeyClicky.App.Portraits.Colorway[]":
		case "Microsoft.UI.Xaml.FrameworkElement[]":
		case "System.Byte[][]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Notch.Peek.INotchDocumentRenderer]":
		case "System.Boolean[]":
		case "(System.String Event, System.String[] Keys)[]":
		case "HeyClicky.App.Portraits.PortraitCharacter[]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Notch.Peek.INotchDocumentRenderer]":
		case "(System.Single v, System.Double s)[]":
		case "(System.Single, System.Double)[]":
		case "System.Byte[]":
		case "System.Collections.Generic.List`1[HeyClicky.App.Drive.IAgentDriveJob]":
		case "HeyClicky.App.Portraits.HairPiece[]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.App.Drive.IAgentDriveJob]":
			return "Microsoft.UI.Xaml.Interop.IBindableVector";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.UIElement]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.UIElement>";
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.Int32,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.Int32,System.String]":
			return "Windows.Foundation.Collections.IMap`2<Int32, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Int32]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<String, Int32>>";
		case "System.Collections.Generic.Dictionary`2[System.String,System.Int32]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Int32]":
			return "Windows.Foundation.Collections.IMap`2<String, Int32>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.FrameworkElement]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.FrameworkElement>";
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMap`2<String, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IList]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.Interop.IBindableVector>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IAnimationObject]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Composition.IAnimationObject>";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]":
		case "System.Collections.Generic.List`1[System.String]":
			return "Windows.Foundation.Collections.IVector`1<String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IVisualElement]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Composition.IVisualElement>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Int32]":
			return "Windows.Foundation.Collections.IMapView`2<String, Int32>";
		case "System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<Int32, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]":
			return "Windows.Foundation.Collections.IIterator`1<String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]":
			return "Windows.Foundation.Collections.IIterator`1<UInt8>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.DateTimeOffset]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.DateTime>";
		case "System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.String]":
		case "System.Collections.Generic.HashSet`1[System.String]":
		case "System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.Char]":
			return "Windows.Foundation.Collections.IIterable`1<String>";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.DateTimeOffset]":
		case "System.Collections.Generic.List`1[System.DateTimeOffset]":
			return "Windows.Foundation.Collections.IVector`1<Windows.Foundation.DateTime>";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[Microsoft.UI.Xaml.UIElement]":
		case "System.Collections.Generic.List`1[Microsoft.UI.Xaml.UIElement]":
			return "Windows.Foundation.Collections.IVector`1<Microsoft.UI.Xaml.UIElement>";
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.Int32]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<String, Int32>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Guid]":
			return "Windows.Foundation.Collections.IIterator`1<System.Guid>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IList`1[System.Byte]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IVector`1<UInt8>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<Int32, String>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.String]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<String, String>>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMapView`2<String, String>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.Int32,System.String]":
			return "Windows.Foundation.Collections.IMapView`2<Int32, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]":
			return "Windows.Foundation.Collections.IIterator`1<Int32>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Composition.IVisualElement2]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Composition.IVisualElement2>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Windows.Foundation.Point]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Point>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IReadOnlyList`1[System.Byte]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IVectorView`1<UInt8>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Object]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]":
			return "Windows.Foundation.Collections.IIterator`1<Object>";
		case "System.Collections.Generic.List`1[System.Text.Json.Nodes.JsonObject]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Text.Json.Nodes.JsonObject]":
			return "Windows.Foundation.Collections.IVectorView`1<Windows.Foundation.Collections.IIterable`1<Object>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Char]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Char>>";
		case "System.Collections.Generic.HashSet`1[System.Int32]":
			return "Windows.Foundation.Collections.IIterable`1<Int32>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Boolean]":
			return "Windows.Foundation.Collections.IMapView`2<String, Boolean>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Double]":
			return "Windows.Foundation.Collections.IIterator`1<Double>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IReadOnlyList`1[System.Object]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IVectorView`1<Object>>";
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.Boolean]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<String, Boolean>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Boolean]":
			return "Windows.Foundation.Collections.IIterator`1<Boolean>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[Microsoft.UI.Xaml.DependencyObject]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.DependencyObject>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IEnumerable]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.Interop.IBindableIterable>";
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<String, String>";
		default:
			return null;
		}
	}
}
