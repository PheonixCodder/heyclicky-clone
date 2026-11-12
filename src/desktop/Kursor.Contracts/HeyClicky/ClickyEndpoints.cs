using System;

namespace HeyClicky;

public sealed record ClickyEndpoints(Uri WorkerBaseUrl, Uri SupabaseUrl, string SupabaseAnonKey)
{
	public const string DefaultWorkerBaseUrl = "http://127.0.0.1:8787";

	public const string DefaultSupabaseUrl = "http://127.0.0.1:8787";

	public const string DefaultSupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

	public static ClickyEndpoints Default { get; } = new ClickyEndpoints(new Uri(DefaultWorkerBaseUrl), new Uri(DefaultSupabaseUrl), DefaultSupabaseAnonKey);
}
