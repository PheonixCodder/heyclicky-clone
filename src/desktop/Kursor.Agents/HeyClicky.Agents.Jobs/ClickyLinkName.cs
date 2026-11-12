using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Jobs;

public static class ClickyLinkName
{
	public sealed record Name(string? Thing, string Site);

	private static readonly Dictionary<string, string> KnownSiteNames = new Dictionary<string, string>
	{
		["github"] = "GitHub",
		["youtube"] = "YouTube",
		["youtu"] = "YouTube",
		["linkedin"] = "LinkedIn",
		["ebay"] = "eBay",
		["airbnb"] = "Airbnb",
		["booking"] = "Booking.com",
		["tripadvisor"] = "Tripadvisor",
		["doordash"] = "DoorDash",
		["ubereats"] = "Uber Eats",
		["instacart"] = "Instacart",
		["bestbuy"] = "Best Buy",
		["homedepot"] = "Home Depot",
		["walmart"] = "Walmart",
		["target"] = "Target",
		["etsy"] = "Etsy",
		["amazon"] = "Amazon",
		["flipkart"] = "Flipkart",
		["swiggy"] = "Swiggy",
		["zomato"] = "Zomato",
		["bigbasket"] = "BigBasket",
		["blinkit"] = "Blinkit",
		["zeptonow"] = "Zepto",
		["myntra"] = "Myntra",
		["nykaa"] = "Nykaa",
		["makemytrip"] = "MakeMyTrip",
		["x"] = "X",
		["twitter"] = "X",
		["reddit"] = "Reddit",
		["figma"] = "Figma",
		["notion"] = "Notion",
		["linear"] = "Linear",
		["stripe"] = "Stripe",
		["openai"] = "OpenAI",
		["chatgpt"] = "ChatGPT",
		["wikipedia"] = "Wikipedia",
		["imdb"] = "IMDb",
		["nytimes"] = "NYTimes",
		["producthunt"] = "Product Hunt",
		["ycombinator"] = "Y Combinator",
		["stackoverflow"] = "Stack Overflow",
		["apple"] = "Apple",
		["spotify"] = "Spotify",
		["netflix"] = "Netflix",
		["google"] = "Google",
		["kayak"] = "Kayak",
		["expedia"] = "Expedia",
		["skyscanner"] = "Skyscanner",
		["zillow"] = "Zillow",
		["craigslist"] = "Craigslist"
	};

	private static readonly HashSet<string> Endings = new HashSet<string>
	{
		"com", "co", "org", "net", "io", "app", "ai", "dev", "in", "uk",
		"au", "ca", "de", "fr", "jp", "us", "so", "site", "me", "be",
		"ly", "tv", "gg"
	};

	public static Name? For(string urlString)
	{
		if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri result) || string.IsNullOrEmpty(result.Host))
		{
			return null;
		}
		string text = result.Host.ToLowerInvariant();
		string host;
		if (!text.StartsWith("www.", StringComparison.Ordinal))
		{
			host = text;
		}
		else
		{
			string text2 = text;
			host = text2.Substring(4, text2.Length - 4);
		}
		string path = Uri.UnescapeDataString(result.AbsolutePath).ToLowerInvariant();
		string query = result.Query.TrimStart('?').ToLowerInvariant();
		string site = SiteName(host);
		return new Name(Thing(host, path, query, site), site);
	}

	private static bool Has(string path, params string[] parts)
	{
		return parts.Any(path.Contains);
	}

	private static string? Thing(string host, string path, string query, string site)
	{
		switch (host)
		{
		case "docs.google.com":
			if (Has(path, "/spreadsheets"))
			{
				return "Google Sheet";
			}
			if (Has(path, "/presentation"))
			{
				return "Google Slides";
			}
			if (Has(path, "/forms"))
			{
				return "Google Form";
			}
			return "Google Doc";
		case "sheets.google.com":
			return "Google Sheet";
		case "slides.google.com":
			return "Google Slides";
		case "drive.google.com":
			return "Drive file";
		case "mail.google.com":
			return "email";
		case "calendar.google.com":
			return "calendar event";
		default:
		{
			if (host.StartsWith("maps.google.", StringComparison.Ordinal) || (host.StartsWith("google.", StringComparison.Ordinal) && Has(path, "/maps")))
			{
				return "map";
			}
			switch (host)
			{
			case "github.com":
				if (Has(path, "/pull/"))
				{
					return "pull request";
				}
				if (Has(path, "/issues/"))
				{
					return "GitHub issue";
				}
				if (Has(path, "/releases"))
				{
					return "GitHub release";
				}
				if (Has(path, "/gist"))
				{
					return "gist";
				}
				if (path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length < 2)
				{
					return null;
				}
				return "GitHub repo";
			case "gist.github.com":
				return "gist";
			case "linear.app":
				if (Has(path, "/issue/"))
				{
					return "Linear issue";
				}
				break;
			}
			bool flag;
			switch (host)
			{
			case "youtube.com":
			case "youtu.be":
			case "m.youtube.com":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				if (!Has(path, "/playlist"))
				{
					return "YouTube video";
				}
				return "YouTube playlist";
			}
			if (host == "figma.com")
			{
				return "Figma file";
			}
			if (host.EndsWith("notion.so", StringComparison.Ordinal) || host.EndsWith("notion.site", StringComparison.Ordinal))
			{
				return "Notion page";
			}
			if (host.EndsWith("vercel.app", StringComparison.Ordinal) || host.EndsWith("netlify.app", StringComparison.Ordinal) || host.EndsWith("pages.dev", StringComparison.Ordinal))
			{
				return "website";
			}
			if (Has(path, "/cart", "/basket", "/bag"))
			{
				return site + " cart";
			}
			if (Has(path, "/checkout"))
			{
				return site + " checkout";
			}
			if (Has(path, "/wishlist", "/registry"))
			{
				return site + " wishlist";
			}
			if (Has(path, "/order", "/your-orders"))
			{
				return site + " order";
			}
			if (Has(path, "/dp/", "/gp/product/", "/product", "/itm/", "/item", "/listing/", "/p/"))
			{
				return site + " product";
			}
			if (Has(path, "/search", "/s/") || query.Contains("q=") || query.Contains("k=") || host.StartsWith("search.", StringComparison.Ordinal))
			{
				return site + " search";
			}
			if (Has(path, "/rooms/", "/listing"))
			{
				return site + " listing";
			}
			if (Has(path, "/booking", "/reservation", "/hotel"))
			{
				return site + " booking";
			}
			if (Has(path, "/flights"))
			{
				return site + " flights";
			}
			if (Has(path, "/maps", "/place/", "/dir/"))
			{
				return "map";
			}
			return null;
		}
		}
	}

	public static string SiteName(string host)
	{
		List<string> list = host.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
		while (list.Count > 1)
		{
			if (!Endings.Contains(list[list.Count - 1]))
			{
				break;
			}
			list.RemoveAt(list.Count - 1);
		}
		string text;
		if (list.Count <= 0)
		{
			text = host;
		}
		else
		{
			text = list[list.Count - 1];
		}
		string text2 = text;
		if (KnownSiteNames.TryGetValue(text2, out string value))
		{
			return value;
		}
		if (text2.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text2[0]));
			string text3 = text2;
			return string.Concat(readOnlySpan, text3.Substring(1, text3.Length - 1));
		}
		return text2;
	}
}
