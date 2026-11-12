using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Connectors;

public sealed class LoopbackCallbackServer : IDisposable
{
	private readonly string path;

	private readonly string expectedState;

	private readonly List<TcpListener> listeners = new List<TcpListener>();

	private readonly TaskCompletionSource<Dictionary<string, string>> result = new TaskCompletionSource<Dictionary<string, string>>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly CancellationTokenSource stop = new CancellationTokenSource();

	public LoopbackCallbackServer(int port, string path, string expectedState)
	{
		this.path = path;
		this.expectedState = expectedState;
		IPAddress[] array = new IPAddress[2]
		{
			IPAddress.Loopback,
			IPAddress.IPv6Loopback
		};
		foreach (IPAddress iPAddress in array)
		{
			try
			{
				TcpListener tcpListener = new TcpListener(iPAddress, port);
				tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, optionValue: true);
				tcpListener.Start();
				listeners.Add(tcpListener);
			}
			catch (SocketException) when (iPAddress.Equals(IPAddress.IPv6Loopback))
			{
			}
			catch (SocketException)
			{
				Dispose();
				throw new McpOAuthException(McpOAuthFailure.CallbackPortUnavailable, $"Port {port} is in use, so the sign-in can't receive its callback.");
			}
		}
		foreach (TcpListener listener in listeners)
		{
			AcceptLoopAsync(listener);
		}
	}

	public async Task<Dictionary<string, string>> WaitAsync(TimeSpan timeout, CancellationToken ct)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout);
		try
		{
			return await result.Task.WaitAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			throw new McpOAuthException(McpOAuthFailure.TimedOut, "The sign-in wasn't completed in time.");
		}
	}

	private async Task AcceptLoopAsync(TcpListener listener)
	{
		while (!stop.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync(stop.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				break;
			}
			HandleAsync(client);
		}
	}

	private async Task HandleAsync(TcpClient client)
	{
		using (client)
		{
			_ = 3;
			try
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[16384];
				int total = 0;
				using CancellationTokenSource read = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
				read.CancelAfter(TimeSpan.FromSeconds(10L));
				while (total < buffer.Length)
				{
					int num = await stream.ReadAsync(buffer.AsMemory(total), read.Token).ConfigureAwait(continueOnCapturedContext: false);
					if (num == 0)
					{
						break;
					}
					total += num;
					if (Encoding.ASCII.GetString(buffer, 0, total).Contains("\r\n\r\n", StringComparison.Ordinal))
					{
						break;
					}
				}
				string text = Encoding.ASCII.GetString(buffer, 0, total).Split("\r\n", 2)[0].Split(' ').ElementAtOrDefault(1) ?? "/";
				Uri uri = new Uri("http://localhost" + (text.StartsWith('/') ? text : ("/" + text)));
				Dictionary<string, string> parameters = ParseQuery(uri.Query);
				bool isCallback = uri.AbsolutePath == path && parameters.GetValueOrDefault("state") == expectedState;
				string text2 = ((!isCallback) ? "<h2>Not found.</h2>" : (parameters.ContainsKey("error") ? "<h2>Sign-in didn't complete.</h2><p>You can close this window and try again in HeyClicky.</p>" : "<h2>Signed in.</h2><p>You can close this window and go back to HeyClicky.</p>"));
				string s = "<!doctype html><html><head><meta charset=\"utf-8\"><title>HeyClicky</title><style>body{font-family:'Segoe UI',system-ui;background:#111;color:#eee;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;text-align:center}</style></head><body><div>" + text2 + "</div></body></html>";
				byte[] bytes = Encoding.UTF8.GetBytes(s);
				string s2 = $"HTTP/1.1 {(isCallback ? "200 OK" : "404 Not Found")}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
				await stream.WriteAsync(Encoding.ASCII.GetBytes(s2)).ConfigureAwait(continueOnCapturedContext: false);
				await stream.WriteAsync(bytes).ConfigureAwait(continueOnCapturedContext: false);
				await stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (isCallback)
				{
					result.TrySetResult(parameters);
				}
			}
			catch
			{
			}
		}
	}

	public static Dictionary<string, string> ParseQuery(string query)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] array = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			string key = Uri.UnescapeDataString(((num < 0) ? text : text.Substring(0, num)).Replace('+', ' '));
			object obj;
			if (num >= 0)
			{
				string text2 = text;
				int num2 = num + 1;
				obj = Uri.UnescapeDataString(text2.Substring(num2, text2.Length - num2).Replace('+', ' '));
			}
			else
			{
				obj = "";
			}
			string value = (string)obj;
			dictionary[key] = value;
		}
		return dictionary;
	}

	public void Dispose()
	{
		stop.Cancel();
		foreach (TcpListener listener in listeners)
		{
			try
			{
				listener.Stop();
			}
			catch
			{
			}
		}
		listeners.Clear();
		result.TrySetCanceled();
	}
}
