using System;

namespace HeyClicky.Core.Worker;

public sealed class WorkerHttpException(int status, string body, string path) : Exception($"Worker {path} failed ({status}): {((body.Length > 200) ? body.Substring(0, 200) : body)}")
{
	public int Status { get; } = status;

	public string Body { get; } = body;

	public string Path { get; } = path;
}
