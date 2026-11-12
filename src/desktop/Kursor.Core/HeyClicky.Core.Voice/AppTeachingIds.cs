using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public static class AppTeachingIds
{
	private static readonly Dictionary<string, string> ByProcess = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["Ableton Live 12 Suite"] = "com.ableton.live",
		["Ableton Live 12 Standard"] = "com.ableton.live",
		["Ableton Live 12 Intro"] = "com.ableton.live",
		["Ableton Live 11 Suite"] = "com.ableton.live",
		["Ableton Live 12 Lite"] = "com.ableton.live",
		["Acrobat"] = "com.adobe.Acrobat.Pro",
		["AfterFX"] = "com.adobe.AfterEffects",
		["studio64"] = "com.google.android.studio",
		["Antigravity"] = "com.google.antigravity",
		["Archi"] = "com.archimatetool.editor",
		["Arduino IDE"] = "cc.arduino.IDE2",
		["acad"] = "com.autodesk.AutoCAD2026",
		["blender"] = "org.blenderfoundation.blender",
		["Canva"] = "com.canva.canvaeditor",
		["CapCut"] = "com.lemon.lvoverseas",
		["CaptureOne"] = "com.captureone.captureone16",
		["ChatGPT"] = "com.openai.chat",
		["claude"] = "com.anthropic.claudefordesktop",
		["Codex"] = "com.openai.codex",
		["comet"] = "ai.perplexity.comet",
		["Cursor"] = "com.todesktop.230313mzl4w4u92",
		["Resolve"] = "com.blackmagic-design.DaVinciResolve",
		["Discord"] = "com.hnc.Discord",
		["Docker Desktop"] = "com.electron.dockerdesktop",
		["MailClient"] = "com.emclient.mail.client",
		["EXCEL"] = "com.microsoft.Excel",
		["Figma"] = "com.figma.Desktop",
		["FL64"] = "com.image-line.flstudio",
		["FL"] = "com.image-line.flstudio",
		["Framer"] = "com.framer.electron",
		["Fusion360"] = "com.autodesk.fusion360",
		["GitHubDesktop"] = "com.github.GitHubClient",
		["Godot"] = "org.godotengine.godot",
		["Illustrator"] = "com.adobe.illustrator",
		["InDesign"] = "com.adobe.InDesign",
		["Lightroom"] = "com.adobe.lightroomCC",
		["Linear"] = "com.linear",
		["LM Studio"] = "ai.elementlabs.lmstudio",
		["MATLAB"] = "org.cef.MATLABWindow",
		["MATLABWindow"] = "org.cef.MATLABWindow",
		["Notion"] = "notion.id",
		["obs64"] = "com.obsproject.obs-studio",
		["Obsidian"] = "md.obsidian",
		["OUTLOOK"] = "com.microsoft.Outlook",
		["olk"] = "com.microsoft.Outlook",
		["PacketTracer"] = "com.netacad.PacketTracer9.0.0",
		["Photoshop"] = "com.adobe.Photoshop",
		["POWERPNT"] = "com.microsoft.Powerpoint",
		["Adobe Premiere Pro"] = "com.adobe.PremierePro",
		["Rive"] = "app.rive.editor",
		["slack"] = "com.tinyspeck.slackmacgap",
		["Superhuman"] = "com.superhuman.electron",
		["Spotify"] = "com.spotify.client",
		["ms-teams"] = "com.microsoft.teams2",
		["Telegram"] = "ru.keepcoder.Telegram",
		["TouchDesigner"] = "ca.derivative.TouchDesigner",
		["TradingView"] = "com.tradingview.tradingviewapp.desktop",
		["Unity"] = "com.unity3d.UnityEditor5.x",
		["Code"] = "com.microsoft.VSCode",
		["Code - Insiders"] = "com.microsoft.VSCodeInsiders",
		["warp"] = "dev.warp.Warp-Stable",
		["Webull Desktop"] = "com.webull.desktop.v1",
		["WhatsApp"] = "net.whatsapp.WhatsApp",
		["WhatsApp.Root"] = "net.whatsapp.WhatsApp",
		["Windsurf"] = "com.exafunction.windsurf",
		["WINWORD"] = "com.microsoft.Word",
		["wps"] = "com.kingsoft.wpsoffice.mac.global"
	};

	public static string FrontAppIdentifier(string? processName)
	{
		if (string.IsNullOrWhiteSpace(processName))
		{
			return "";
		}
		if (!ByProcess.TryGetValue(processName.Trim(), out string value))
		{
			return processName.Trim();
		}
		return value;
	}
}
