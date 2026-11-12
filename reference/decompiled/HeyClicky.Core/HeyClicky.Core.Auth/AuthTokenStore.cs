using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace HeyClicky.Core.Auth;

public sealed class AuthTokenStore
{
	private static readonly byte[] Entropy = "HeyClicky.Auth.v1"u8.ToArray();

	private static readonly byte[] PendingSignInEntropy = "HeyClicky.PendingSignIn.v1"u8.ToArray();

	private readonly string filePath;

	private readonly string pendingSignInFilePath;

	public string FilePath => filePath;

	public string PendingSignInFilePath => pendingSignInFilePath;

	public AuthTokenStore(string directory, bool isLocalBackend)
	{
		filePath = Path.Combine(directory, isLocalBackend ? "auth.localdev.bin" : "auth.bin");
		pendingSignInFilePath = Path.Combine(directory, isLocalBackend ? "signin-pending.localdev.bin" : "signin-pending.bin");
	}

	public StoredAuthSession? Load()
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return null;
			}
			return JsonSerializer.Deserialize(ProtectedData.Unprotect(File.ReadAllBytes(filePath), Entropy, DataProtectionScope.CurrentUser), CoreJsonContext.Default.StoredAuthSession);
		}
		catch (Exception ex)
		{
			Log.Warn("Auth", "stored session unreadable (" + ex.GetType().Name + "); starting signed out");
			return null;
		}
	}

	public bool Save(StoredAuthSession session)
	{
		try
		{
			byte[] array = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(session, CoreJsonContext.Default.StoredAuthSession), Entropy, DataProtectionScope.CurrentUser);
			AtomicFile.WriteAllBytes(filePath, array);
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "session save failed", ex);
			return false;
		}
	}

	public bool Delete()
	{
		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "session delete failed", ex);
			return false;
		}
	}

	public StoredPendingSignIn? LoadPendingSignIn()
	{
		try
		{
			if (!File.Exists(pendingSignInFilePath))
			{
				return null;
			}
			return JsonSerializer.Deserialize(ProtectedData.Unprotect(File.ReadAllBytes(pendingSignInFilePath), PendingSignInEntropy, DataProtectionScope.CurrentUser), CoreJsonContext.Default.StoredPendingSignIn);
		}
		catch (Exception ex)
		{
			Log.Warn("Auth", "pending sign-in unreadable (" + ex.GetType().Name + ")");
			return null;
		}
	}

	public bool SavePendingSignIn(StoredPendingSignIn pendingSignIn)
	{
		try
		{
			byte[] array = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(pendingSignIn, CoreJsonContext.Default.StoredPendingSignIn), PendingSignInEntropy, DataProtectionScope.CurrentUser);
			AtomicFile.WriteAllBytes(pendingSignInFilePath, array);
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "pending sign-in save failed; keeping it for this run only", ex);
			return false;
		}
	}

	public bool DeletePendingSignIn()
	{
		try
		{
			if (File.Exists(pendingSignInFilePath))
			{
				File.Delete(pendingSignInFilePath);
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "pending sign-in delete failed", ex);
			return false;
		}
	}
}
