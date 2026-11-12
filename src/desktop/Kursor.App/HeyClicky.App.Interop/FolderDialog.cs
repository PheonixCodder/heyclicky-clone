using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HeyClicky.App.Interop;

public static class FolderDialog
{
	[ComImport]
	[Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
	[ClassInterface(ClassInterfaceType.None)]
	private class FileOpenDialogCoClass
	{
	}

	[ComImport]
	[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItem
	{
		void BindToHandler(nint pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out nint ppv);

		void GetParent(out IShellItem ppsi);

		void GetDisplayName(uint sigdnName, out nint ppszName);

		void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

		void Compare(IShellItem psi, uint hint, out int piOrder);
	}

	[ComImport]
	[Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFileOpenDialog
	{
		[PreserveSig]
		int Show(nint parent);

		void SetFileTypes(uint cFileTypes, nint rgFilterSpec);

		void SetFileTypeIndex(uint iFileType);

		void GetFileTypeIndex(out uint piFileType);

		void Advise(nint pfde, out uint pdwCookie);

		void Unadvise(uint dwCookie);

		void SetOptions(uint fos);

		void GetOptions(out uint pfos);

		void SetDefaultFolder(IShellItem psi);

		void SetFolder(IShellItem psi);

		void GetFolder(out IShellItem ppsi);

		void GetCurrentSelection(out IShellItem ppsi);

		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		void GetResult(out IShellItem ppsi);

		void AddPlace(IShellItem psi, int fdap);

		void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		void Close(int hr);

		void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);

		void ClearClientData();

		void SetFilter(nint pFilter);

		void GetResults(out nint ppenum);

		void GetSelectedItems(out nint ppsai);
	}

	private const uint FOS_NOCHANGEDIR = 8u;

	private const uint FOS_PICKFOLDERS = 32u;

	private const uint FOS_FORCEFILESYSTEM = 64u;

	private const uint FOS_PATHMUSTEXIST = 2048u;

	private const uint FOS_FILEMUSTEXIST = 4096u;

	private const uint SIGDN_FILESYSPATH = 2147844096u;

	public static string? Pick(nint owner, string title, string okLabel, string? startFolder, bool folders = true)
	{
		IFileOpenDialog fileOpenDialog = null;
		try
		{
			fileOpenDialog = (IFileOpenDialog)new FileOpenDialogCoClass();
			fileOpenDialog.GetOptions(out var pfos);
			fileOpenDialog.SetOptions(pfos | (uint)(folders ? 32 : 4096) | 0x40 | 0x800 | 8);
			fileOpenDialog.SetTitle(title);
			fileOpenDialog.SetOkButtonLabel(okLabel);
			if (startFolder != null && Directory.Exists(startFolder) && SHCreateItemFromParsingName(startFolder, 0, typeof(IShellItem).GUID, out IShellItem item) == 0 && item != null)
			{
				fileOpenDialog.SetFolder(item);
				Marshal.ReleaseComObject(item);
			}
			if (fileOpenDialog.Show(owner) != 0)
			{
				return null;
			}
			fileOpenDialog.GetResult(out IShellItem ppsi);
			ppsi.GetDisplayName(2147844096u, out var ppszName);
			try
			{
				return Marshal.PtrToStringUni(ppszName);
			}
			finally
			{
				Marshal.FreeCoTaskMem(ppszName);
				Marshal.ReleaseComObject(ppsi);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("folder-dialog", ex.Message);
			return null;
		}
		finally
		{
			if (fileOpenDialog != null)
			{
				Marshal.ReleaseComObject(fileOpenDialog);
			}
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHCreateItemFromParsingName(string path, nint bindContext, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem? item);
}
