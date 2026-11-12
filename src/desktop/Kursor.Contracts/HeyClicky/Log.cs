using System;
using System.IO;

namespace HeyClicky;

public static class Log
{
	private static readonly object Gate = new object();

	private static readonly string LogDir = AppPaths.Combine("logs");

	private static StreamWriter? writer;

	public static string Directory => LogDir;

	public static void Info(string area, string message)
	{
		Write("INF", area, message);
	}

	public static void Warn(string area, string message)
	{
		Write("WRN", area, message);
	}

	public static void Error(string area, string message, Exception? ex = null)
	{
		Write("ERR", area, (ex == null) ? message : $"{message}: {ex}");
	}

	private static void Write(string level, string area, string message)
	{
		string value = $"{DateTimeOffset.Now:HH:mm:ss.fff} {level} [{area}] {message}";
		lock (Gate)
		{
			try
			{
				if (writer == null)
				{
					System.IO.Directory.CreateDirectory(LogDir);
					string processPath = Environment.ProcessPath;
					string value2 = ((processPath != null) ? Path.GetFileNameWithoutExtension(processPath) : "heyclicky");
					writer = new StreamWriter(new FileStream(Path.Combine(LogDir, $"{value2}-{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}.log"), FileMode.Append, FileAccess.Write, FileShare.Read | FileShare.Delete))
					{
						AutoFlush = true
					};
				}
				writer.WriteLine(value);
			}
			catch (Exception ex)
			{
				writer = null;
				try
				{
					File.AppendAllText(Path.Combine(LogDir, "logger-failures.txt"), $"{DateTimeOffset.Now:O} pid {Environment.ProcessId}: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
				}
				catch
				{
				}
			}
		}
	}
}
