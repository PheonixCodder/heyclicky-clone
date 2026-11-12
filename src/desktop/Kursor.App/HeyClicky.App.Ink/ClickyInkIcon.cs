using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkIcon
{
	private static readonly Dictionary<string, char> fluent = new Dictionary<string, char>(StringComparer.Ordinal)
	{
		["pencil"] = '\ue70f',
		["pencil.line"] = '\ue70f',
		["square.and.pencil"] = '\ue70f',
		["checkmark"] = '\ue73e',
		["checkmark.circle"] = '\ue930',
		["checkmark.circle.fill"] = '\uec61',
		["checkmark.seal"] = '\uec61',
		["checkmark.square"] = '\ue73a',
		["checklist"] = '\ue9d5',
		["photo"] = '\ue91b',
		["photo.on.rectangle"] = '\ue91b',
		["camera"] = '\ue722',
		["calendar"] = '\ue787',
		["calendar.badge.plus"] = '\ue787',
		["clock"] = '\ue823',
		["alarm"] = '\ue823',
		["timer"] = '\ue916',
		["link"] = '\ue71b',
		["globe"] = '\ue774',
		["safari"] = '\ue774',
		["cart"] = '\ue7bf',
		["bag"] = '\ue7bf',
		["creditcard"] = '\ue8c7',
		["dollarsign.circle"] = '\ue8c7',
		["banknote"] = '\ue8c7',
		["envelope"] = '\ue715',
		["envelope.fill"] = '\ue715',
		["envelope.open"] = '\ue8c3',
		["tray"] = '\ue7b8',
		["archivebox"] = '\ue7b8',
		["questionmark.bubble"] = '\ue9ce',
		["questionmark.circle"] = '\ue9ce',
		["questionmark"] = '\ue9ce',
		["exclamationmark.triangle"] = '\ue7ba',
		["exclamationmark.circle"] = '\ue783',
		["music.note"] = '\uec4f',
		["music.note.list"] = '\uec4f',
		["headphones"] = '\ue7f6',
		["doc"] = '\ue8a5',
		["doc.text"] = '\ue8a5',
		["doc.text.fill"] = '\ue8a5',
		["doc.richtext"] = '\ue8a5',
		["doc.plaintext"] = '\ue8a5',
		["doc.on.doc"] = '\ue8c8',
		["doc.on.doc.fill"] = '\ue8c8',
		["folder"] = '\ue8b7',
		["folder.fill"] = '\ue8b7',
		["paperclip"] = '\ue723',
		["person"] = '\ue77b',
		["person.2"] = '\ue716',
		["person.3"] = '\ue716',
		["person.crop.circle"] = '\ue77b',
		["mappin"] = '\ue7b7',
		["mappin.and.ellipse"] = '\ue7b7',
		["mappin.circle.fill"] = '\ue7b7',
		["map"] = '\ue826',
		["location"] = '\ue81d',
		["magnifyingglass"] = '\ue721',
		["text.magnifyingglass"] = '\ue721',
		["square.grid.2x2"] = '\ue8a9',
		["square.grid.3x3.fill"] = '\ue8a9',
		["tablecells.fill"] = '\ue8a9',
		["tablecells"] = '\ue8a9',
		["list.bullet"] = '\ue8fd',
		["list.bullet.rectangle"] = '\ue8fd',
		["arrow.triangle.2.circlepath"] = '\ue895',
		["arrow.clockwise"] = '\ue72c',
		["arrow.counterclockwise"] = '\ue7a7',
		["arrow.up.right"] = '\ue8a7',
		["arrow.up.right.square"] = '\ue8a7',
		["square.and.arrow.up"] = '\ue72d',
		["arrow.down.circle"] = '\ue896',
		["keyboard"] = '\ue765',
		["play.fill"] = '\uf5b0',
		["play"] = '\ue768',
		["pause.fill"] = '\ue769',
		["pause"] = '\ue769',
		["stop.fill"] = '\ue71a',
		["powerplug.fill"] = '\ue945',
		["bolt"] = '\ue945',
		["bolt.fill"] = '\ue945',
		["rectangle.on.rectangle"] = '\ue7c4',
		["paintpalette.fill"] = '\ue790',
		["paintpalette"] = '\ue790',
		["paintbrush.pointed.fill"] = '\ue771',
		["paintbrush"] = '\ue771',
		["ticket.fill"] = '\ue8ec',
		["ticket"] = '\ue8ec',
		["tag"] = '\ue8ec',
		["tram.fill"] = '\ue7c0',
		["bus.fill"] = '\ue806',
		["car.fill"] = '\ue804',
		["car"] = '\ue804',
		["airplane"] = '\ue709',
		["engine.combustion"] = '\ue90f',
		["wrench"] = '\ue90f',
		["hammer"] = '\ue90f',
		["sparkles"] = '\uf4a5',
		["star"] = '\ue734',
		["star.fill"] = '\ue735',
		["heart"] = '\ueb51',
		["heart.fill"] = '\ueb52',
		["house"] = '\ue80f',
		["bell"] = '\uea8f',
		["lock"] = '\ue72e',
		["gearshape"] = '\ue713',
		["gear"] = '\ue713',
		["flag"] = '\ue7c1',
		["book"] = '\ue82d',
		["lightbulb"] = '\ue82f',
		["graduationcap"] = '\ue7be',
		["phone"] = '\ue717',
		["video"] = '\ue714',
		["mic"] = '\ue720',
		["trash"] = '\ue74d',
		["bubble.left"] = '\ue8bd',
		["bubble.left.and.bubble.right"] = '\ue8f2',
		["message"] = '\ue8bd',
		["sun.max"] = '\ue706',
		["moon"] = '\ue708',
		["cloud"] = '\ue753',
		["xmark"] = '\ue711',
		["plus"] = '\ue710',
		["minus"] = '\ue738',
		["ellipsis"] = '\ue712',
		["info.circle"] = '\ue946',
		["chart.bar"] = '\ue9d2',
		["chart.line.uptrend.xyaxis"] = '\ue9d2',
		["chart.pie"] = '\ueb05',
		["building.2"] = '\ue825',
		["briefcase"] = '\ue821',
		["cup.and.saucer"] = '\uec32',
		["fork.knife"] = '\ued56',
		["leaf"] = '\uec0a',
		["desktopcomputer"] = '\ue7f4',
		["laptopcomputer"] = '\ue7f8',
		["iphone"] = '\ue8ea',
		["shield"] = '\uea18',
		["eye"] = '\ue7b3',
		["hand.thumbsup"] = '\ue8e1',
		["crown"] = '\ue734',
		["trophy"] = '\ue734'
	};

	private static readonly Dictionary<string, bool> knownSymbols = new Dictionary<string, bool>();

	private static readonly Dictionary<string, string> families = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["figure"] = "person",
		["person"] = "person",
		["people"] = "person.2",
		["brain"] = "lightbulb",
		["face"] = "person.crop.circle",
		["hand"] = "hand.thumbsup",
		["hands"] = "hand.thumbsup",
		["eye"] = "eye",
		["eyes"] = "eye",
		["ear"] = "headphones",
		["mouth"] = "bubble.left",
		["lungs"] = "heart",
		["staroflife"] = "heart",
		["cross"] = "heart",
		["pills"] = "heart",
		["stethoscope"] = "heart",
		["bandage"] = "heart",
		["syringe"] = "heart",
		["waveform"] = "music.note",
		["bed"] = "house",
		["sportscourt"] = "trophy",
		["soccerball"] = "trophy",
		["basketball"] = "trophy",
		["football"] = "trophy",
		["baseball"] = "trophy",
		["tennisball"] = "trophy",
		["volleyball"] = "trophy",
		["tennis"] = "trophy",
		["dumbbell"] = "trophy",
		["medal"] = "trophy",
		["rosette"] = "trophy",
		["gamecontroller"] = "star",
		["dice"] = "star",
		["puzzlepiece"] = "square.grid.2x2",
		["pawprint"] = "leaf",
		["dog"] = "leaf",
		["cat"] = "leaf",
		["bird"] = "leaf",
		["fish"] = "leaf",
		["tortoise"] = "leaf",
		["hare"] = "leaf",
		["ant"] = "leaf",
		["ladybug"] = "leaf",
		["lizard"] = "leaf",
		["tree"] = "leaf",
		["camera.macro"] = "leaf",
		["carrot"] = "leaf",
		["apple"] = "leaf",
		["sun"] = "sun.max",
		["sunrise"] = "sun.max",
		["sunset"] = "sun.max",
		["moon"] = "moon",
		["sparkle"] = "sparkles",
		["cloud"] = "cloud",
		["snowflake"] = "cloud",
		["wind"] = "cloud",
		["umbrella"] = "cloud",
		["thermometer"] = "cloud",
		["drop"] = "cloud",
		["humidity"] = "cloud",
		["tornado"] = "cloud",
		["hurricane"] = "cloud",
		["rainbow"] = "cloud",
		["flame"] = "bolt",
		["mountain"] = "map",
		["globe"] = "globe",
		["fork"] = "fork.knife",
		["takeoutbag"] = "bag",
		["birthday"] = "star",
		["wineglass"] = "cup.and.saucer",
		["mug"] = "cup.and.saucer",
		["cup"] = "cup.and.saucer",
		["frying"] = "fork.knife",
		["refrigerator"] = "house",
		["house"] = "house",
		["building"] = "building.2",
		["storefront"] = "building.2",
		["tent"] = "house",
		["car"] = "car",
		["bus"] = "bus.fill",
		["tram"] = "tram.fill",
		["train"] = "tram.fill",
		["bicycle"] = "car",
		["scooter"] = "car",
		["sailboat"] = "airplane",
		["ferry"] = "airplane",
		["airplane"] = "airplane",
		["fuelpump"] = "car",
		["map"] = "map",
		["mappin"] = "mappin",
		["location"] = "location",
		["signpost"] = "map",
		["suitcase"] = "briefcase",
		["briefcase"] = "briefcase",
		["bag"] = "bag",
		["cart"] = "cart",
		["basket"] = "cart",
		["gift"] = "bag",
		["shippingbox"] = "archivebox",
		["archivebox"] = "archivebox",
		["tray"] = "tray",
		["box"] = "archivebox",
		["creditcard"] = "creditcard",
		["wallet"] = "creditcard",
		["banknote"] = "banknote",
		["dollarsign"] = "dollarsign.circle",
		["eurosign"] = "dollarsign.circle",
		["sterlingsign"] = "dollarsign.circle",
		["yensign"] = "dollarsign.circle",
		["indianrupeesign"] = "dollarsign.circle",
		["bitcoinsign"] = "dollarsign.circle",
		["chart"] = "chart.bar",
		["percent"] = "chart.pie",
		["function"] = "chart.line.uptrend.xyaxis",
		["sum"] = "chart.bar",
		["number"] = "list.bullet",
		["tv"] = "desktopcomputer",
		["display"] = "desktopcomputer",
		["desktopcomputer"] = "desktopcomputer",
		["pc"] = "desktopcomputer",
		["server"] = "desktopcomputer",
		["macbook"] = "laptopcomputer",
		["laptopcomputer"] = "laptopcomputer",
		["iphone"] = "iphone",
		["ipad"] = "iphone",
		["applewatch"] = "iphone",
		["wifi"] = "globe",
		["antenna"] = "globe",
		["network"] = "globe",
		["printer"] = "doc",
		["scanner"] = "doc",
		["keyboard"] = "keyboard",
		["computermouse"] = "desktopcomputer",
		["music"] = "music.note",
		["guitars"] = "music.note",
		["pianokeys"] = "music.note",
		["speaker"] = "music.note",
		["radio"] = "music.note",
		["headphones"] = "headphones",
		["film"] = "video",
		["movieclapper"] = "video",
		["video"] = "video",
		["camera"] = "camera",
		["photo"] = "photo",
		["play"] = "play",
		["pause"] = "pause",
		["mic"] = "mic",
		["phone"] = "phone",
		["message"] = "message",
		["bubble"] = "bubble.left",
		["quote"] = "bubble.left",
		["envelope"] = "envelope",
		["paperplane"] = "envelope",
		["at"] = "envelope",
		["bell"] = "bell",
		["megaphone"] = "bell",
		["newspaper"] = "doc.text",
		["magazine"] = "book",
		["book"] = "book",
		["books"] = "book",
		["text"] = "doc.text",
		["character"] = "doc.text",
		["doc"] = "doc",
		["folder"] = "folder",
		["paperclip"] = "paperclip",
		["list"] = "list.bullet",
		["checklist"] = "checklist",
		["note"] = "doc.text",
		["calendar"] = "calendar",
		["clock"] = "clock",
		["timer"] = "timer",
		["alarm"] = "alarm",
		["hourglass"] = "clock",
		["stopwatch"] = "timer",
		["gear"] = "gear",
		["gearshape"] = "gearshape",
		["wrench"] = "wrench",
		["hammer"] = "hammer",
		["screwdriver"] = "wrench",
		["scissors"] = "pencil",
		["ruler"] = "pencil",
		["pencil"] = "pencil",
		["paintbrush"] = "paintbrush",
		["paintpalette"] = "paintpalette",
		["eyedropper"] = "paintbrush",
		["wand"] = "sparkles",
		["lightbulb"] = "lightbulb",
		["graduationcap"] = "graduationcap",
		["studentdesk"] = "graduationcap",
		["backpack"] = "graduationcap",
		["atom"] = "sparkles",
		["flask"] = "sparkles",
		["testtube"] = "sparkles",
		["lock"] = "lock",
		["key"] = "lock",
		["shield"] = "shield",
		["exclamationmark"] = "exclamationmark.circle",
		["questionmark"] = "questionmark.circle",
		["info"] = "info.circle",
		["checkmark"] = "checkmark",
		["xmark"] = "xmark",
		["plus"] = "plus",
		["minus"] = "minus",
		["trash"] = "trash",
		["flag"] = "flag",
		["tag"] = "tag",
		["ticket"] = "ticket",
		["bookmark"] = "tag",
		["link"] = "link",
		["magnifyingglass"] = "magnifyingglass",
		["arrow"] = "arrow.up.right",
		["arrowshape"] = "arrow.up.right",
		["square"] = "square.grid.2x2",
		["circle"] = "info.circle",
		["star"] = "star",
		["heart"] = "heart",
		["crown"] = "crown",
		["trophy"] = "trophy",
		["bolt"] = "bolt",
		["battery"] = "bolt",
		["powerplug"] = "powerplug.fill",
		["leaf"] = "leaf",
		["safari"] = "safari",
		["person.crop"] = "person.crop.circle"
	};

	private static readonly (string[] Words, string Symbol)[] labelSymbols = new(string[], string)[20]
	{
		(new string[5] { "bug", "error", "fail", "broken", "blocked" }, "exclamationmark.triangle"),
		(new string[6] { "answered", "replied", "done", "passed", "verified", "check" }, "checkmark.seal"),
		(new string[7] { "inbox", "email", "mail", "repl", "message", "thread", "unread" }, "envelope"),
		(new string[3] { "ask", "request", "question" }, "questionmark.bubble"),
		(new string[5] { "album", "track", "song", "music", "playlist" }, "music.note"),
		(new string[4] { "image", "photo", "picture", "cover" }, "photo"),
		(new string[3] { "page", "site", "web" }, "globe"),
		(new string[5] { "pdf", "guide", "doc", "report", "file" }, "doc.richtext"),
		(new string[3] { "source", "link", "citation" }, "link"),
		(new string[5] { "cart", "item", "order", "purchase", "buy" }, "cart"),
		(new string[9] { "price", "cost", "budget", "revenue", "spend", "$", "€", "£", "₹" }, "dollarsign.circle"),
		(new string[7] { "profile", "people", "person", "user", "friend", "team", "contact" }, "person.2"),
		(new string[6] { "closed", "open", "hour", "time", "deadline", "late" }, "clock"),
		(new string[7] { "built", "date", "day", "year", "week", "month", "when" }, "calendar"),
		(new string[2] { "engine", "motor" }, "engine.combustion"),
		(new string[5] { "stroke", "turn", "cycle", "round", "repeat" }, "arrow.triangle.2.circlepath"),
		(new string[4] { "search", "scan", "checked", "found" }, "magnifyingglass"),
		(new string[3] { "change", "edit", "update" }, "pencil"),
		(new string[3] { "type", "kind", "categor" }, "square.grid.2x2"),
		(new string[5] { "place", "city", "map", "location", "trip" }, "mappin.and.ellipse")
	};

	public static IEnumerable<string> KnownNames => fluent.Keys;

	public static string? SymbolName(string? icon, string label, bool inferring = true)
	{
		string text = icon?.Trim();
		if (!string.IsNullOrEmpty(text) && Exists(text))
		{
			return text;
		}
		if (!inferring)
		{
			return null;
		}
		return Inferred(label);
	}

	public static bool Exists(string name)
	{
		lock (knownSymbols)
		{
			if (knownSymbols.TryGetValue(name, out var value))
			{
				return value;
			}
			bool flag = Resolve(name).HasValue;
			knownSymbols[name] = flag;
			return flag;
		}
	}

	public static char? Resolve(string name)
	{
		string text = name;
		while (text.Length > 0)
		{
			if (fluent.TryGetValue(text, out var value))
			{
				return value;
			}
			int num = text.LastIndexOf('.');
			if (num <= 0)
			{
				break;
			}
			text = text.Substring(0, num);
		}
		string[] array = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
		foreach (string key in array)
		{
			if (families.TryGetValue(key, out string value2) && fluent.TryGetValue(value2, out var value3))
			{
				return value3;
			}
		}
		return null;
	}

	public static TextBlock? Glyph(string name, double size, Color color, bool bold = false)
	{
		char? c = Resolve(name);
		if (c.HasValue)
		{
			char valueOrDefault = c.GetValueOrDefault();
			return new TextBlock
			{
				Text = valueOrDefault.ToString(),
				FontFamily = DS.Fonts.Symbols,
				FontSize = size,
				FontWeight = (bold ? FontWeights.Bold : FontWeights.Normal),
				Foreground = Ui.Brush(color),
				IsTextScaleFactorEnabled = false,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				IsHitTestVisible = false
			};
		}
		return null;
	}

	public static string? Inferred(string label)
	{
		string lowered = label.ToLowerInvariant();
		if (lowered.Length == 0)
		{
			return null;
		}
		(string[], string)[] array = labelSymbols;
		for (int i = 0; i < array.Length; i++)
		{
			var (source, result) = array[i];
			if (source.Any((string w) => lowered.Contains(w, StringComparison.Ordinal)))
			{
				return result;
			}
		}
		return null;
	}
}
