using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Browser;

public static class BrowserUploads
{
	public static readonly string[] RefusedFolders = new string[26]
	{
		".ssh", ".gnupg", ".aws", ".azure", ".kube", ".docker", ".config\\gcloud", ".config\\gh", ".codex", "AppData\\Roaming\\Microsoft\\Credentials",
		"AppData\\Local\\Microsoft\\Credentials", "AppData\\Local\\Microsoft\\Vault", "AppData\\Roaming\\Microsoft\\Vault", "AppData\\Roaming\\Microsoft\\Protect", "AppData\\Roaming\\Microsoft\\Crypto", "AppData\\Local\\Microsoft\\Windows\\INetCookies", "AppData\\Local\\Google", "AppData\\Local\\Chromium", "AppData\\Local\\BraveSoftware", "AppData\\Local\\Microsoft\\Edge",
		"AppData\\Local\\Vivaldi", "AppData\\Local\\Packages\\TheBrowserCompany.Arc", "AppData\\Roaming\\Mozilla", "AppData\\Local\\Mozilla", "AppData\\Local\\HeyClicky", "AppData\\Roaming\\HeyClicky"
	};

	public static readonly string[] AllowedInsideRefused = new string[2] { "AppData\\Local\\HeyClicky\\Clickys", "AppData\\Local\\HeyClicky\\projects" };

	public const int MaxFiles = 10;

	public const long MaxBytes = 104857600L;

	public const int ChunkBytes = 524288;

	public const string NeedsNewerExtensionHint = "This upload needs a newer version of the HeyClicky Chrome extension, which Chrome installs by itself (at the latest when Chrome restarts). For now, ask the user to add the file to the page themselves: name the file and the field.";

	public const string TooLargeForOlderExtensionHint = "This Chrome still has an older HeyClicky extension, which can only upload files up to 2 MB. Chrome installs the new one by itself (at the latest when Chrome restarts). For now, ask the user to add the file to the page themselves: name the file and the field.";

	public const long OlderExtensionMaxBytes = 2097152L;

	public const int OlderExtensionChunkBytes = 14700;

	public const string OlderExtensionArmScript = "globalThis.__heyClickyUploadDisarm?.();\nconst meta = __FILES__;\nconst parts = globalThis.__heyClickyUpload || [];\ndelete globalThis.__heyClickyUpload;\nconst files = meta.map((fileMeta, fileIndex) => new File((parts[fileIndex] || []).map(piece => {\n  const binary = atob(piece), bytes = new Uint8Array(binary.length);\n  for (let byteIndex = 0; byteIndex < binary.length; byteIndex++) bytes[byteIndex] = binary.charCodeAt(byteIndex);\n  return bytes;\n}), fileMeta.name, {type: fileMeta.type, lastModified: fileMeta.lastModified}));\nif (files.some((file, fileIndex) => file.size !== meta[fileIndex].size)) return {error: 'The page reloaded while the file was being sent.'};\nconst state = {given: null};\nconst click = HTMLInputElement.prototype.click, picker = HTMLInputElement.prototype.showPicker, openPicker = window.showOpenFilePicker;\nlet disarmed = false, disarmTimer = null;\nconst disarm = () => {\n  if (disarmed) return;\n  disarmed = true;\n  clearTimeout(disarmTimer);\n  window.removeEventListener('click', onClick, true);\n  HTMLInputElement.prototype.click = click;\n  if (picker) HTMLInputElement.prototype.showPicker = picker;\n  if (openPicker) window.showOpenFilePicker = openPicker;\n  if (globalThis.__heyClickyUploadDisarm === disarm) delete globalThis.__heyClickyUploadDisarm;\n};\nconst give = input => {\n  if (state.given) return;\n  if (!input.multiple && files.length > 1) state.given = {error: 'This file field takes one file.'};\n  else {\n    const chosen = new DataTransfer();\n    files.forEach(file => chosen.items.add(file));\n    input.files = chosen.files;\n    input.dispatchEvent(new Event('input', {bubbles: true, composed: true}));\n    input.dispatchEvent(new Event('change', {bubbles: true}));\n    state.given = {sizes: [...input.files].map(file => file.size)};\n  }\n  disarm();\n};\nconst isFileField = node => node instanceof HTMLInputElement && node.type === 'file';\nfunction onClick(event) {\n  const target = event.composedPath()[0];\n  if (isFileField(target)) { event.preventDefault(); give(target); }\n}\nwindow.addEventListener('click', onClick, true);\nHTMLInputElement.prototype.click = function () { return isFileField(this) ? give(this) : click.call(this); };\nif (picker) HTMLInputElement.prototype.showPicker = function () { return isFileField(this) ? give(this) : picker.call(this); };\nif (openPicker) window.showOpenFilePicker = async () => {\n  state.given = {error: 'This page picks files a way HeyClicky can\\'t answer yet.'};\n  disarm();\n  throw new DOMException('The user aborted a request.', 'AbortError');\n};\nglobalThis.__heyClickyUploadDisarm = disarm;\nglobalThis.__heyClickyUploadState = state;\ndisarmTimer = setTimeout(disarm, 8000);\nreturn {armed: true};";

	public const string OlderExtensionResultScript = "const given = globalThis.__heyClickyUploadState?.given ?? null;\nif (given || __LAST__) { globalThis.__heyClickyUploadDisarm?.(); delete globalThis.__heyClickyUploadState; }\nreturn given;";

	private static string NeedsPaths => $"upload needs paths: 1 to {10} absolute file paths.";

	public static string OlderExtensionChunkScript(int fileIndex, string base64, bool first)
	{
		return (first ? "globalThis.__heyClickyUploadDisarm?.(); globalThis.__heyClickyUpload = [];\n" : "") + $"(globalThis.__heyClickyUpload ??= [])[{fileIndex}] ??= [];\n" + $"globalThis.__heyClickyUpload[{fileIndex}].push('{base64}');\nreturn true;";
	}

	public static bool IsJobsOwnFile(string path, string? jobFolder)
	{
		if (string.IsNullOrEmpty(jobFolder))
		{
			return false;
		}
		string text;
		string text2;
		try
		{
			text = Resolve(jobFolder);
			text2 = Resolve(path);
		}
		catch
		{
			return false;
		}
		return text2.StartsWith(text + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
	}

	public static bool CarriesUpload(JsonObject arguments, string operation)
	{
		if (!(operation == "upload"))
		{
			if (operation == "batch" && arguments["steps"] is JsonArray source)
			{
				return source.Any((JsonNode s) => s?["operation"]?.ToString() == "upload");
			}
			return false;
		}
		return true;
	}

	public static string ApprovalQuestion(IReadOnlyList<string> fileNames, string? site)
	{
		string text = site ?? "a website";
		if (fileNames.Count == 0)
		{
			return "Add files from your PC to " + text + "?";
		}
		List<string> list = new List<string>();
		int num = 0;
		foreach (string fileName in fileNames)
		{
			string text2 = ((fileName.Length > 40) ? (fileName.Substring(0, 39) + "…") : fileName);
			if (list.Count > 0 && num + text2.Length > 64)
			{
				break;
			}
			list.Add(text2);
			num += text2.Length + 2;
		}
		int num2 = fileNames.Count - list.Count;
		object obj;
		if (num2 <= 0)
		{
			if (list.Count <= 1)
			{
				obj = list[0];
			}
			else
			{
				obj = string.Join(", ", list.SkipLast(1)) + " and " + list[list.Count - 1];
			}
		}
		else
		{
			obj = $"{string.Join(", ", list)} and {num2} more";
		}
		string value = (string)obj;
		return $"Add {value} to {text}?";
	}

	public static string ApprovalNote(string jobTitle)
	{
		string text = jobTitle.Trim();
		return ((text.Length == 0) ? "Your job" : ((text.Length > 40) ? (text.Substring(0, 39) + "…") : text)) + " is asking to add files from your PC. A yes covers this turn.";
	}

	public static string? Refusal(UploadApprovalAnswer answer, string files, string place)
	{
		return answer switch
		{
			UploadApprovalAnswer.Allowed => null, 
			UploadApprovalAnswer.Declined => $"The user said no to adding {files} to {place} this turn. Don't try again or copy the file somewhere else to get around this. Carry on without it and say what's still needed.", 
			UploadApprovalAnswer.Unanswered => $"HeyClicky asked the user about adding {files} to {place} and they haven't answered. Leave the page as it is and end your turn saying you need their yes; HeyClicky's card comes back when you try next turn.", 
			_ => "HeyClicky is asking the user something else right now. Try this again in a moment.", 
		};
	}

	public static string DisplayName(string file)
	{
		string text = ((Path.IsPathRooted(file) || file.StartsWith('~')) ? Path.GetFileName(file.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar)) : file);
		return string.Join(' ', text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
	}

	public static string MimeType(string name)
	{
		string text = Path.GetExtension(name).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					switch (text[3])
					{
					case 'e':
						break;
					case 'b':
						if (!(text == ".webp"))
						{
							goto IL_03f6;
						}
						return "image/webp";
					case 'i':
						if (!(text == ".heic"))
						{
							goto IL_03f6;
						}
						return "image/heic";
					case 'f':
						goto IL_02a0;
					case 'm':
						goto IL_02b5;
					case 'o':
						if (!(text == ".json"))
						{
							goto IL_03f6;
						}
						return "application/json";
					case 'c':
						if (!(text == ".docx"))
						{
							goto IL_03f6;
						}
						return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
					case 's':
						if (!(text == ".xlsx"))
						{
							goto IL_03f6;
						}
						return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
					case 't':
						if (!(text == ".pptx"))
						{
							goto IL_03f6;
						}
						return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
					default:
						goto IL_03f6;
					}
					if (text == ".jpeg")
					{
						goto IL_031e;
					}
				}
			}
			else
			{
				switch (text[1])
				{
				case 'j':
					break;
				case 'p':
					goto IL_0115;
				case 'g':
					if (!(text == ".gif"))
					{
						goto IL_03f6;
					}
					return "image/gif";
				case 'b':
					if (!(text == ".bmp"))
					{
						goto IL_03f6;
					}
					return "image/bmp";
				case 't':
					goto IL_0174;
				case 's':
					if (!(text == ".svg"))
					{
						goto IL_03f6;
					}
					return "image/svg+xml";
				case 'c':
					if (!(text == ".csv"))
					{
						goto IL_03f6;
					}
					return "text/csv";
				case 'h':
					goto IL_01c3;
				case 'z':
					if (!(text == ".zip"))
					{
						goto IL_03f6;
					}
					return "application/zip";
				case 'd':
					if (!(text == ".doc"))
					{
						goto IL_03f6;
					}
					return "application/msword";
				case 'x':
					if (!(text == ".xls"))
					{
						goto IL_03f6;
					}
					return "application/vnd.ms-excel";
				case 'm':
					goto IL_0217;
				case 'w':
					if (!(text == ".wav"))
					{
						goto IL_03f6;
					}
					return "audio/wav";
				default:
					goto IL_03f6;
				}
				if (text == ".jpg")
				{
					goto IL_031e;
				}
			}
		}
		goto IL_03f6;
		IL_01c3:
		if (text == ".htm")
		{
			goto IL_038e;
		}
		goto IL_03f6;
		IL_02b5:
		if (text == ".html")
		{
			goto IL_038e;
		}
		goto IL_03f6;
		IL_031e:
		return "image/jpeg";
		IL_0115:
		switch (text)
		{
		case ".png":
			return "image/png";
		case ".pdf":
			return "application/pdf";
		case ".ppt":
			return "application/vnd.ms-powerpoint";
		}
		goto IL_03f6;
		IL_0217:
		switch (text)
		{
		case ".mp4":
			return "video/mp4";
		case ".mov":
			return "video/quicktime";
		case ".mp3":
			return "audio/mpeg";
		}
		goto IL_03f6;
		IL_038e:
		return "text/html";
		IL_03f6:
		return "";
		IL_0174:
		if (text == ".tif")
		{
			goto IL_0360;
		}
		if (!(text == ".txt"))
		{
			goto IL_03f6;
		}
		return "text/plain";
		IL_0360:
		return "image/tiff";
		IL_02a0:
		if (text == ".tiff")
		{
			goto IL_0360;
		}
		goto IL_03f6;
	}

	public static Task<byte[]?> ReadChunkAsync(string path, int index)
	{
		return Task.Run(delegate
		{
			try
			{
				using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
				fileStream.Seek((long)index * 524288L, SeekOrigin.Begin);
				byte[] array = new byte[524288];
				int i;
				int num;
				for (i = 0; i < array.Length; i += num)
				{
					if ((num = fileStream.Read(array, i, array.Length - i)) <= 0)
					{
						break;
					}
				}
				return array[..i];
			}
			catch
			{
				return (byte[]?)null;
			}
		});
	}

	public static bool IsSecretFileName(string name)
	{
		string text = name.ToLowerInvariant();
		bool flag = text.StartsWith(".env", StringComparison.Ordinal) || text.EndsWith(".pem", StringComparison.Ordinal) || text.EndsWith(".key", StringComparison.Ordinal);
		if (!flag)
		{
			bool flag2;
			switch (text)
			{
			case ".netrc":
			case "_netrc":
			case ".git-credentials":
			case ".npmrc":
			case ".pypirc":
			case ".pgpass":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	public static (IReadOnlyList<string>? Paths, string? Error) ValidatedPaths(JsonNode? raw, string? home = null, string? dataRoot = null)
	{
		if (!(raw is JsonArray { Count: not 0, Count: <=10 } jsonArray) || jsonArray.Any((JsonNode i) => !(i is JsonValue jsonValue) || jsonValue.GetValueKind() != JsonValueKind.String))
		{
			return (Paths: null, Error: NeedsPaths);
		}
		home = Normalize(home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		List<string> list = RefusedFolders.Select((string f) => Path.Combine(home, f)).ToList();
		List<string> list2 = AllowedInsideRefused.Select((string f) => Path.Combine(home, f)).ToList();
		if (dataRoot != null)
		{
			dataRoot = Normalize(dataRoot);
			list.Add(dataRoot);
			list2.Add(Path.Combine(dataRoot, "Clickys"));
		}
		List<string> list3 = new List<string>();
		foreach (JsonNode item in jsonArray)
		{
			string value = item.GetValue<string>();
			string text;
			if (!value.StartsWith("~/", StringComparison.Ordinal) && !value.StartsWith("~\\", StringComparison.Ordinal))
			{
				text = value;
			}
			else
			{
				string? text2 = home;
				string text3 = value;
				text = text2 + "\\" + text3.Substring(2, text3.Length - 2);
			}
			string path2 = text;
			if (!Path.IsPathFullyQualified(path2))
			{
				return (Paths: null, Error: NeedsPaths);
			}
			string path3;
			try
			{
				path3 = Resolve(Normalize(path2));
			}
			catch
			{
				return (Paths: null, Error: "No file at " + value + ".");
			}
			if ((list.Any((string r) => IsInside(path3, r)) && !list2.Any((string a) => IsInside(path3, a))) || IsSecretFileName(Path.GetFileName(path3)))
			{
				return (Paths: null, Error: value + " is in a private place (keys, credentials, browser profiles or app settings), so it is never uploaded.");
			}
			if (Directory.Exists(path3))
			{
				return (Paths: null, Error: value + " is not a regular file; upload takes files.");
			}
			FileInfo fileInfo = new FileInfo(path3);
			if (!fileInfo.Exists)
			{
				return (Paths: null, Error: "No file at " + value + ".");
			}
			if ((fileInfo.Attributes & (FileAttributes.Directory | FileAttributes.Device)) != 0)
			{
				return (Paths: null, Error: value + " is not a regular file; upload takes files.");
			}
			try
			{
				using (File.Open(path3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
				}
			}
			catch
			{
				return (Paths: null, Error: "HeyClicky can't read " + value + ".");
			}
			if (fileInfo.Length > 104857600)
			{
				return (Paths: null, Error: fileInfo.Name + " is larger than 100 MB.");
			}
			list3.Add(path3);
		}
		return (Paths: list3, Error: null);
		static bool IsInside(string path, string root)
		{
			if (!path.Equals(root, StringComparison.OrdinalIgnoreCase))
			{
				return path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	private static string Normalize(string path)
	{
		return Path.GetFullPath(path).TrimEnd('\\', '/');
	}

	private static string Resolve(string path)
	{
		return Resolve(path, 0);
	}

	private static string Resolve(string path, int depth)
	{
		if (depth > 32)
		{
			throw new IOException("Too many links on the way to " + path);
		}
		string fullPath = Path.GetFullPath(path);
		string text;
		string text2 = (text = Path.GetPathRoot(fullPath) ?? throw new IOException("No root in " + path));
		string text3 = fullPath;
		int length = text2.Length;
		string[] array = text3.Substring(length, text3.Length - length).Split(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}, StringSplitOptions.RemoveEmptyEntries);
		foreach (string path2 in array)
		{
			string text4 = Path.Join(text, path2);
			FileSystemInfo fileSystemInfo = (Directory.Exists(text4) ? ((FileSystemInfo)new DirectoryInfo(text4)) : ((FileSystemInfo)new FileInfo(text4)));
			if (fileSystemInfo.LinkTarget != null)
			{
				text4 = Resolve((fileSystemInfo.ResolveLinkTarget(returnFinalTarget: true) ?? throw new IOException("Can't follow the link at " + text4)).FullName, depth + 1);
			}
			text = text4;
		}
		return Normalize(text);
	}

	public static (JsonObject Arguments, string? Error) Validating(JsonObject arguments, string operation, string? home = null, string? dataRoot = null)
	{
		if (operation == "upload")
		{
			var (readOnlyList, item) = ValidatedPaths(arguments["paths"], home, dataRoot);
			if (readOnlyList == null)
			{
				return (Arguments: arguments, Error: item);
			}
			arguments["paths"] = new JsonArray(((IEnumerable<string>)readOnlyList).Select((Func<string, JsonNode>)((string p) => p)).ToArray());
		}
		if (operation == "batch" && arguments["steps"] is JsonArray jsonArray)
		{
			for (int i = 0; i < jsonArray.Count; i++)
			{
				if (jsonArray[i] is JsonObject jsonObject && !(jsonObject["operation"]?.ToString() != "upload"))
				{
					var (readOnlyList2, text) = ValidatedPaths(jsonObject["paths"], home, dataRoot);
					if (readOnlyList2 == null)
					{
						return (Arguments: arguments, Error: $"Step {i + 1}: {text ?? ""} Nothing ran.");
					}
					jsonObject["paths"] = new JsonArray(((IEnumerable<string>)readOnlyList2).Select((Func<string, JsonNode>)((string p) => p)).ToArray());
				}
			}
		}
		return (Arguments: arguments, Error: null);
	}
}
