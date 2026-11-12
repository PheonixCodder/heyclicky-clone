using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HeyClicky.App.Services;

public static class UiWork
{
	private static object? current;

	private static long currentSince;

	private static long started;

	public static long Started => Interlocked.Read(in started);

	public static object Pick(SendOrPostCallback callback, object? state)
	{
		bool flag = ((state is Delegate || state is IAsyncStateMachine) ? true : false);
		if (!flag && (state == null || !state.GetType().IsGenericType))
		{
			return callback;
		}
		return state;
	}

	public static (object? Work, long Since) Enter(object work)
	{
		(object?, long) result = (Volatile.Read(in current), Volatile.Read(in currentSince));
		Volatile.Write(ref currentSince, Stopwatch.GetTimestamp());
		Volatile.Write(ref current, work);
		Interlocked.Increment(ref started);
		return result;
	}

	public static void Exit((object? Work, long Since) previous)
	{
		Volatile.Write(ref current, previous.Work);
		Volatile.Write(ref currentSince, previous.Since);
	}

	public static (object? Work, int RunningMs) Snapshot()
	{
		object obj = Volatile.Read(in current);
		long num = Volatile.Read(in currentSince);
		if (obj == null || num == 0L)
		{
			return (Work: null, RunningMs: 0);
		}
		long value = (Stopwatch.GetTimestamp() - num) * 1000 / Stopwatch.Frequency;
		return (Work: obj, RunningMs: (int)Math.Clamp(value, 0L, 2147483647L));
	}

	public static string Describe(object? work)
	{
		if (work == null)
		{
			return "none";
		}
		try
		{
			if (work is Delegate @delegate)
			{
				object target = @delegate.Target;
				if (target != null)
				{
					string text = StateMachineName(target.GetType());
					if (text != null)
					{
						return text;
					}
				}
				if (@delegate.Target is IAsyncStateMachine asyncStateMachine)
				{
					return NameOf(asyncStateMachine.GetType());
				}
				return MethodName(@delegate.Method.DeclaringType, @delegate.Method.Name);
			}
			if (work is IAsyncStateMachine asyncStateMachine2)
			{
				return NameOf(asyncStateMachine2.GetType());
			}
			return StateMachineName(work.GetType()) ?? NameOf(work.GetType());
		}
		catch
		{
			return "unknown";
		}
	}

	private static string? StateMachineName(Type type)
	{
		if (!type.IsGenericType)
		{
			return null;
		}
		Type[] genericArguments = type.GetGenericArguments();
		for (int num = genericArguments.Length - 1; num >= 0; num--)
		{
			if (typeof(IAsyncStateMachine).IsAssignableFrom(genericArguments[num]))
			{
				return NameOf(genericArguments[num]);
			}
		}
		return null;
	}

	private static string NameOf(Type type)
	{
		return MethodName(type.DeclaringType, type.Name);
	}

	public static string MethodName(Type? owner, string name)
	{
		string text = name;
		if (text.StartsWith('<'))
		{
			text = text.TrimStart('<');
			int num = text.IndexOf('>');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
		}
		while ((object)owner != null && owner.Name.StartsWith('<'))
		{
			owner = owner.DeclaringType;
		}
		string text2 = owner?.Name ?? "?";
		int num2 = text2.IndexOf('`');
		if (num2 > 0)
		{
			text2 = text2.Substring(0, num2);
		}
		return text2 + "." + text;
	}
}
