using System;

namespace HeyClicky;

public sealed record ClickyEndpoints(Uri WorkerBaseUrl, Uri SupabaseUrl, string SupabaseAnonKey);
