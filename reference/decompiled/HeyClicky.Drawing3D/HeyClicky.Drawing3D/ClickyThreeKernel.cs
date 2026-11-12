using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;

namespace HeyClicky.Drawing3D;

public sealed class ClickyThreeKernel
{
	private const long CacheByteBudget = 50331648L;

	private static readonly TimeSpan IdleRelease = TimeSpan.FromSeconds(Scene3D.EnvFloat("CANVAS_3D_KERNEL_IDLE", 300f));

	private readonly object gate = new object();

	private readonly Dictionary<string, LinkedListNode<(string key, Mesh3D mesh)>> shapes = new Dictionary<string, LinkedListNode<(string, Mesh3D)>>();

	private readonly LinkedList<(string key, Mesh3D mesh)> useOrder = new LinkedList<(string, Mesh3D)>();

	private long cachedBytes;

	private readonly HashSet<string> failed = new HashSet<string>();

	private readonly Dictionary<string, TaskCompletionSource<Mesh3D?>> queued = new Dictionary<string, TaskCompletionSource<Mesh3D>>();

	private readonly BlockingCollection<(string key, string kind, string recipe)> work = new BlockingCollection<(string, string, string)>();

	private Thread? thread;

	private V8ScriptEngine? engine;

	private ScriptObject? buildFunction;

	private bool loadFailed;

	private const string WarmUpKind = "\0warm";

	public static ClickyThreeKernel Shared { get; } = new ClickyThreeKernel();

	public static string? ScriptDirectory { get; set; }

	public int BuildCount { get; private set; }

	public double BuildMilliseconds { get; private set; }

	public double LoadMilliseconds { get; private set; }

	public long CachedBytes
	{
		get
		{
			lock (gate)
			{
				return cachedBytes;
			}
		}
	}

	public long HeapBytes
	{
		get
		{
			try
			{
				return (long)(engine?.GetRuntimeHeapInfo().TotalHeapSize ?? 0);
			}
			catch
			{
				return 0L;
			}
		}
	}

	private ClickyThreeKernel()
	{
	}

	public static string Key(string kind, string recipe)
	{
		return kind + "|" + recipe;
	}

	public void Prepare(string kind, string recipeJson)
	{
		Build(kind, recipeJson);
	}

	public void WarmUp()
	{
		lock (gate)
		{
			if (engine == null && !loadFailed)
			{
				EnsureThread();
				work.Add(("", "\0warm", ""));
			}
		}
	}

	public Task<Mesh3D?> Build(string kind, string recipeJson)
	{
		string text = Key(kind, recipeJson);
		lock (gate)
		{
			if (shapes.TryGetValue(text, out LinkedListNode<(string, Mesh3D)> value))
			{
				Touch(value);
				return Task.FromResult(value.Value.Item2);
			}
			if (failed.Contains(text))
			{
				return Task.FromResult<Mesh3D>(null);
			}
			if (queued.TryGetValue(text, out TaskCompletionSource<Mesh3D> value2))
			{
				return value2.Task;
			}
			TaskCompletionSource<Mesh3D> taskCompletionSource = new TaskCompletionSource<Mesh3D>(TaskCreationOptions.RunContinuationsAsynchronously);
			queued[text] = taskCompletionSource;
			EnsureThread();
			work.Add((text, kind, recipeJson));
			return taskCompletionSource.Task;
		}
	}

	public Mesh3D? Mesh(string kind, string recipeJson)
	{
		return Build(kind, recipeJson).GetAwaiter().GetResult();
	}

	public Mesh3D? CachedMesh(string kind, string recipeJson)
	{
		lock (gate)
		{
			if (!shapes.TryGetValue(Key(kind, recipeJson), out LinkedListNode<(string, Mesh3D)> value))
			{
				return null;
			}
			Touch(value);
			return value.Value.Item2;
		}
	}

	private void Touch(LinkedListNode<(string key, Mesh3D mesh)> node)
	{
		if (node != useOrder.Last)
		{
			useOrder.Remove(node);
			useOrder.AddLast(node);
		}
	}

	private void EnsureThread()
	{
		if (thread == null)
		{
			thread = new Thread(Run)
			{
				IsBackground = true,
				Name = "Clicky 3D kernel",
				Priority = ThreadPriority.AboveNormal
			};
			thread.Start();
		}
	}

	private void Run()
	{
		while (true)
		{
			if (!work.TryTake(out (string, string, string) item, IdleRelease))
			{
				if (engine != null)
				{
					buildFunction = null;
					engine.Dispose();
					engine = null;
					Log.Info("3D", "kernel: engine released after idle");
				}
				ReleaseCachedGpuBuffers();
				continue;
			}
			if (item.Item2 == "\0warm")
			{
				LoadIfNeeded();
				continue;
			}
			Mesh3D mesh3D = null;
			lock (gate)
			{
				if (shapes.ContainsKey(item.Item1))
				{
					mesh3D = shapes[item.Item1].Value.mesh;
				}
			}
			if (mesh3D == null)
			{
				try
				{
					mesh3D = BuildNow(item.Item2, item.Item3);
				}
				catch (Exception ex)
				{
					Log.Warn("3D", "kernel: " + item.Item2 + " failed: " + ex.Message);
					mesh3D = null;
				}
			}
			TaskCompletionSource<Mesh3D> value;
			lock (gate)
			{
				queued.Remove(item.Item1, out value);
				if (mesh3D != null)
				{
					if (!shapes.ContainsKey(item.Item1))
					{
						shapes[item.Item1] = useOrder.AddLast((item.Item1, mesh3D));
						cachedBytes += mesh3D.ByteSize;
						while (cachedBytes > 50331648 && useOrder.Count > 1)
						{
							LinkedListNode<(string, Mesh3D)> first = useOrder.First;
							useOrder.RemoveFirst();
							shapes.Remove(first.Value.Item1);
							cachedBytes -= first.Value.Item2.ByteSize;
						}
					}
				}
				else
				{
					failed.Add(item.Item1);
				}
			}
			value?.TrySetResult(mesh3D);
		}
	}

	private void ReleaseCachedGpuBuffers()
	{
		List<Mesh3D> list;
		lock (gate)
		{
			if (useOrder.Count == 0)
			{
				return;
			}
			list = useOrder.Select<(string, Mesh3D), Mesh3D>(((string key, Mesh3D mesh) entry) => entry.mesh).ToList();
		}
		GpuDevice shared = GpuDevice.Shared;
		int num = 0;
		lock (shared.ContextLock)
		{
			foreach (Mesh3D item in list)
			{
				if (item.Gpu != null)
				{
					item.Gpu.Dispose();
					item.Gpu = null;
					num++;
				}
			}
		}
		if (ThreeStage.LiveCounts.Stages == 0)
		{
			shared.Trim();
		}
		Log.Info("3D", $"kernel: GPU buffers of {num} cached shapes released after idle; {list.Count} shapes kept");
	}

	private bool LoadIfNeeded()
	{
		if (engine != null)
		{
			return true;
		}
		if (loadFailed)
		{
			return false;
		}
		long timestamp = Stopwatch.GetTimestamp();
		try
		{
			string path = ResolveScriptDirectory();
			engine = new V8ScriptEngine("Clicky 3D kernel", V8ScriptEngineFlags.DisableGlobalMembers);
			string[] array = new string[2] { "clicky-three.js", "clicky-three-parts.js" };
			foreach (string text in array)
			{
				string path2 = Path.Combine(path, text);
				engine.Execute(new DocumentInfo(text), File.ReadAllText(path2));
			}
			buildFunction = engine.Script.clickyThreeBuild as ScriptObject;
			if (buildFunction == null)
			{
				throw new InvalidOperationException("clickyThreeBuild is missing");
			}
			LoadMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
			Log.Info("3D", $"kernel loaded in {LoadMilliseconds:F1} ms");
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("3D", "kernel failed to load", ex);
			engine?.Dispose();
			engine = null;
			loadFailed = true;
			return false;
		}
	}

	private static string ResolveScriptDirectory()
	{
		string scriptDirectory = ScriptDirectory;
		if (scriptDirectory != null)
		{
			return scriptDirectory;
		}
		string[] array = new string[3]
		{
			Path.Combine(AppContext.BaseDirectory, "Assets", "Engine"),
			Path.Combine(AppContext.BaseDirectory, "HeyClicky.Drawing", "Assets", "Engine"),
			Environment.GetEnvironmentVariable("HEYCLICKY_ENGINE_DIR") ?? ""
		};
		foreach (string text in array)
		{
			if (text.Length > 0 && File.Exists(Path.Combine(text, "clicky-three.js")))
			{
				return text;
			}
		}
		throw new FileNotFoundException("clicky-three.js not found (set ClickyThreeKernel.ScriptDirectory)");
	}

	private unsafe Mesh3D? BuildNow(string kind, string recipe)
	{
		if (!LoadIfNeeded())
		{
			return null;
		}
		long timestamp = Stopwatch.GetTimestamp();
		object obj;
		try
		{
			obj = buildFunction.Invoke(false, kind, recipe);
		}
		catch (ScriptEngineException ex)
		{
			Log.Warn("3D", "kernel: " + kind + " failed: " + (ex.ErrorDetails ?? ex.Message));
			return null;
		}
		if (!(obj is ScriptObject scriptObject))
		{
			return null;
		}
		ITypedArray<float> typedArray = scriptObject.GetProperty("positions") as ITypedArray<float>;
		ITypedArray<float> typedArray2 = scriptObject.GetProperty("normals") as ITypedArray<float>;
		ITypedArray<float> typedArray3 = scriptObject.GetProperty("uvs") as ITypedArray<float>;
		ITypedArray<uint> typedArray4 = scriptObject.GetProperty("indices") as ITypedArray<uint>;
		ITypedArray<float> typedArray5 = scriptObject.GetProperty("samples") as ITypedArray<float>;
		if (typedArray == null || typedArray2 == null || typedArray4 == null)
		{
			return null;
		}
		int vertexCount = (int)(typedArray.Length / 3);
		int indexCount = (int)typedArray4.Length;
		if (vertexCount == 0 || (int)typedArray2.Length != vertexCount * 3 || indexCount == 0 || indexCount % 3 != 0)
		{
			return null;
		}
		bool num = typedArray3 != null && (int)typedArray3.Length == vertexCount * 2;
		Mesh3D mesh3D = new Mesh3D
		{
			Vertices = new Vertex3D[vertexCount],
			Indices = new uint[indexCount]
		};
		fixed (Vertex3D* vertices = mesh3D.Vertices)
		{
			Vertex3D* v = vertices;
			typedArray.InvokeWithDirectAccess(delegate(nint pointer)
			{
				for (int i = 0; i < vertexCount; i++)
				{
					v[i].Position = new Vector3(*(float*)(pointer + (nint)(i * 3) * (nint)4), *(float*)(pointer + (nint)(i * 3 + 1) * (nint)4), *(float*)(pointer + (nint)(i * 3 + 2) * (nint)4));
					v[i].Color = Vector4.One;
				}
			});
			typedArray2.InvokeWithDirectAccess(delegate(nint pointer)
			{
				for (int j = 0; j < vertexCount; j++)
				{
					v[j].Normal = new Vector3(*(float*)(pointer + (nint)(j * 3) * (nint)4), *(float*)(pointer + (nint)(j * 3 + 1) * (nint)4), *(float*)(pointer + (nint)(j * 3 + 2) * (nint)4));
				}
			});
			if (num)
			{
				typedArray3.InvokeWithDirectAccess(delegate(nint pointer)
				{
					for (int k = 0; k < vertexCount; k++)
					{
						v[k].UV = new Vector2(*(float*)(pointer + (nint)(k * 2) * (nint)4), *(float*)(pointer + (nint)(k * 2 + 1) * (nint)4));
					}
				});
			}
		}
		bool indicesValid = true;
		fixed (uint* indices = mesh3D.Indices)
		{
			uint* d = indices;
			typedArray4.InvokeWithDirectAccess(delegate(nint pointer)
			{
				for (int l = 0; l < indexCount; l++)
				{
					uint num2 = *(uint*)(pointer + (nint)l * (nint)4);
					if (num2 >= (uint)vertexCount)
					{
						indicesValid = false;
					}
					d[l] = num2;
				}
			});
		}
		if (!indicesValid)
		{
			return null;
		}
		if (typedArray5 != null && typedArray5.Length >= 3)
		{
			int count = (int)(typedArray5.Length / 3);
			Vector3[] points = new Vector3[count];
			typedArray5.InvokeWithDirectAccess(delegate(nint pointer)
			{
				for (int m = 0; m < count; m++)
				{
					points[m] = new Vector3(*(float*)(pointer + (nint)(m * 3) * (nint)4), *(float*)(pointer + (nint)(m * 3 + 1) * (nint)4), *(float*)(pointer + (nint)(m * 3 + 2) * (nint)4));
				}
			});
			mesh3D.SamplePoints = points;
		}
		try
		{
			mesh3D.Gpu = GpuMesh.Create(GpuDevice.Shared.Device, mesh3D);
		}
		catch (Exception ex2)
		{
			Log.Warn("3D", "kernel: GPU buffers for " + kind + " failed: " + ex2.Message);
		}
		double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
		BuildCount++;
		BuildMilliseconds += totalMilliseconds;
		if (totalMilliseconds > 8.0)
		{
			Log.Info("3D", $"kernel: {kind} built in {totalMilliseconds:F1} ms ({vertexCount} vertices)");
		}
		return mesh3D;
	}
}
