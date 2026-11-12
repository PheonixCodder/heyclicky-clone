using System;

namespace HeyClicky.App.Notch.Peek;

public sealed record NotchUpdateButton(string Title, bool IsPrimary, Action Action);
