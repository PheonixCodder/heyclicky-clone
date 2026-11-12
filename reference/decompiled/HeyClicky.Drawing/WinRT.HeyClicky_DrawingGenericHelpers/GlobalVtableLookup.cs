using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClicky_DrawingGenericHelpers;

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
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32 Kind, HeyClicky.RectD Fraction)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.Generic.List`1[(Windows.UI.Composition.CompositionColorBrush, Windows.UI.Color)`2[Windows.UI.Composition.CompositionColorBrush,Windows.UI.Color]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32, System.Int32)`2[System.Int32,System.Int32]]":
		case "System.Collections.Generic.List`1[HeyClicky.RectD]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(Windows.UI.Composition.CompositionColorBrush, Windows.UI.Color)`2[Windows.UI.Composition.CompositionColorBrush,Windows.UI.Color]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32, HeyClicky.RectD)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.Int32, HeyClicky.RectD)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.Generic.List`1[(System.String, System.Int32, System.Int32, System.Text.Json.JsonElement)`4[System.String,System.Int32,System.Int32,System.Text.Json.JsonElement]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String Full, System.String Thumbnail)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.Int32, System.Int32, System.Text.Json.JsonElement)`4[System.String,System.Int32,System.Int32,System.Text.Json.JsonElement]]":
		case "System.Collections.Generic.List`1[(System.Int32 Kind, HeyClicky.RectD Fraction)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.RectD]":
		case "System.Collections.Generic.List`1[(System.String Full, System.String Thumbnail)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.Int32, System.Int32)`2[System.Int32,System.Int32]]":
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
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Numerics.Vector2]":
			_ = IEnumerator_System_Numerics_Vector2.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<Vector2>.IID,
					Vtable = IEnumeratorMethods<Vector2>.AbiToProjectionVftablePtr
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
		case "System.Int32[]":
			_ = IList_int.Initialized;
			_ = IReadOnlyList_int.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_int.Initialized;
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
					IID = IListMethods<int>.IID,
					Vtable = IListMethods<int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<int>.IID,
					Vtable = IReadOnlyListMethods<int>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
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
		case "System.Net.Sockets.NetworkStream":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
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
		case "System.Collections.Generic.Dictionary`2[System.String,System.Byte[]]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Byte[]]":
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
		case "System.Numerics.Vector2[]":
			_ = IList_System_Numerics_Vector2.Initialized;
			_ = IReadOnlyList_System_Numerics_Vector2.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_System_Numerics_Vector2.Initialized;
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
					IID = IListMethods<Vector2>.IID,
					Vtable = IListMethods<Vector2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<Vector2>.IID,
					Vtable = IReadOnlyListMethods<Vector2>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<Vector2>.IID,
					Vtable = IEnumerableMethods<Vector2>.AbiToProjectionVftablePtr
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
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32 Kind, HeyClicky.RectD Fraction)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.Generic.List`1[(Windows.UI.Composition.CompositionColorBrush, Windows.UI.Color)`2[Windows.UI.Composition.CompositionColorBrush,Windows.UI.Color]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32, System.Int32)`2[System.Int32,System.Int32]]":
		case "System.Collections.Generic.List`1[HeyClicky.RectD]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(Windows.UI.Composition.CompositionColorBrush, Windows.UI.Color)`2[Windows.UI.Composition.CompositionColorBrush,Windows.UI.Color]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.Int32, HeyClicky.RectD)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.Int32, HeyClicky.RectD)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.Generic.List`1[(System.String, System.Int32, System.Int32, System.Text.Json.JsonElement)`4[System.String,System.Int32,System.Int32,System.Text.Json.JsonElement]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String Full, System.String Thumbnail)`2[System.String,System.String]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[(System.String, System.Int32, System.Int32, System.Text.Json.JsonElement)`4[System.String,System.Int32,System.Int32,System.Text.Json.JsonElement]]":
		case "System.Collections.Generic.List`1[(System.Int32 Kind, HeyClicky.RectD Fraction)`2[System.Int32,HeyClicky.RectD]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[HeyClicky.RectD]":
		case "System.Collections.Generic.List`1[(System.String Full, System.String Thumbnail)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.String, System.String)`2[System.String,System.String]]":
		case "System.Collections.Generic.List`1[(System.Int32, System.Int32)`2[System.Int32,System.Int32]]":
			return "Windows.Foundation.Collections.IVectorView`1<Object>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Numerics.Vector2]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Numerics.Vector2>";
		case "System.Int32[]":
		case "System.Byte[]":
		case "System.Numerics.Vector2[]":
			return "Microsoft.UI.Xaml.Interop.IBindableVector";
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMap`2<String, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]":
			return "Windows.Foundation.Collections.IIterator`1<UInt8>";
		case "System.Net.Sockets.NetworkStream":
			return "Windows.Foundation.IClosable";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.String]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<String, String>>";
		case "System.Collections.Generic.Dictionary`2[System.String,System.Byte[]]":
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Byte[]]":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Byte[]]":
			return "Windows.Foundation.Collections.IIterable`1<Object>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMapView`2<String, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]":
			return "Windows.Foundation.Collections.IIterator`1<Int32>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]":
			return "Windows.Foundation.Collections.IIterator`1<Object>";
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<String, String>";
		default:
			return null;
		}
	}
}
