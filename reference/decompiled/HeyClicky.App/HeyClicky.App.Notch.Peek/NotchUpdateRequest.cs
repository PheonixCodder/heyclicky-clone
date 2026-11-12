using System.Collections.Generic;

namespace HeyClicky.App.Notch.Peek;

public sealed record NotchUpdateRequest(string Title, string Detail, IReadOnlyList<NotchUpdateButton> Buttons, bool IsChromeAsk = false);
