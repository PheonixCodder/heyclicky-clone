using System.Buffers;
using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
internal static class _003CRegexGenerator_g_003EF438A85ABF99E47EA4FBA640CE8836B84C8719F8A5025C90CE820D2BC67EAC31D__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	internal static readonly SearchValues<char> s_ascii_FF0300000000FEFFFF07 = SearchValues.Create("0123456789abcdefghijklmnopqrstuvwxyz");
}
