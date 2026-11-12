using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Home;
using HeyClicky.App.Portraits;

namespace HeyClicky.App.Onboarding;

public static class StarterCast
{
	public sealed record Member(string Slug, string Name, string Role, string OneLiner, IReadOnlyList<string> SuggestedAsks);

	public static readonly Member ResearchBuddy = new Member("research-buddy", "Research Buddy", "Researcher", "Digs into companies, markets, and competitors and hands you a tidy report.", new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "Research my top three competitors and what they charge", "Find out who is winning in my space and why", "Put together a one page brief on this company" }));

	public static readonly Member Shippy = new Member("shippy", "Shippy", "Web builder", "Builds and ships websites and small web projects right on your PC.", new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "Build me a simple landing page for my idea", "Make a one page site with a waitlist form", "Ship a little web app that does one thing well" }));

	public static readonly IReadOnlyList<Member> Members = new global::_003C_003Ez__ReadOnlyArray<Member>(new Member[2] { ResearchBuddy, Shippy });

	public static bool IsCast(string slug)
	{
		return Members.Any((Member m) => m.Slug == slug);
	}

	public static IReadOnlyList<string> PresentSlugs()
	{
		return (from m in Members
			select m.Slug into s
			where (object)ClickyRoster.Shared.Clicky(s) != null
			select s).ToList();
	}

	public static void Outfit()
	{
		ClickyRoster.Shared.OutfitStarterCast(Members.Select((Member m) => m.Slug));
	}

	public static void Unlock()
	{
		ClickyRoster shared = ClickyRoster.Shared;
		foreach (Member member in Members)
		{
			IHomeSource? source = shared.Source;
			if ((source == null || !source.KnowsClicky(member.Slug)) && (object)shared.Clicky(member.Slug) == null)
			{
				RosterClicky rosterClicky = shared.CreateClicky(member.Name, member.Role, member.OneLiner, Array.Empty<string>(), member.SuggestedAsks, null, null, null);
				Log.Info("onboarding", $"starter cast: {rosterClicky.Name} ({rosterClicky.Slug})");
			}
		}
	}
}
