using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace HeyClicky.App.Portraits;

public static class PortraitData
{
	public static readonly PortraitPack[] VisiblePacks = new PortraitPack[3]
	{
		PortraitPack.ClassicClouds,
		PortraitPack.PuffPals,
		PortraitPack.StorybookSpirits
	};

	public static readonly Color StorybookFaceInk = Rgb(4338479u);

	public static readonly Color ClassicCloudEyeColor = Rgb(1.0, 1.0, 1.0, 0.95);

	public static readonly Color Ink = Rgb(0.055, 0.07, 0.09);

	public static Color Rgb(uint hex)
	{
		return Color.FromArgb(byte.MaxValue, (byte)(hex >> 16), (byte)(hex >> 8), (byte)hex);
	}

	public static Color Rgb(double r, double g, double b, double a = 1.0)
	{
		return Color.FromArgb((byte)Math.Round(Math.Clamp(a, 0.0, 1.0) * 255.0), (byte)Math.Round(Math.Clamp(r, 0.0, 1.0) * 255.0), (byte)Math.Round(Math.Clamp(g, 0.0, 1.0) * 255.0), (byte)Math.Round(Math.Clamp(b, 0.0, 1.0) * 255.0));
	}

	public static bool IsCloud(this PortraitPack p)
	{
		if ((uint)(p - 3) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static string Id(this PortraitPack p)
	{
		return p switch
		{
			PortraitPack.PuffPals => "puffPals", 
			PortraitPack.KiCrew => "kiCrew", 
			PortraitPack.ArcadeAllStars => "arcadeAllStars", 
			PortraitPack.CloudClub => "cloudClub", 
			PortraitPack.ClassicClouds => "classicClouds", 
			_ => "storybookSpirits", 
		};
	}

	public static PortraitPack? PackFromId(string? id)
	{
		return Enum.GetValues<PortraitPack>().Cast<PortraitPack?>().FirstOrDefault((PortraitPack? p) => p.Value.Id() == id);
	}

	public static string Title(this PortraitPack p)
	{
		return p switch
		{
			PortraitPack.PuffPals => "Puff Pals", 
			PortraitPack.KiCrew => "Ki Crew", 
			PortraitPack.ArcadeAllStars => "Arcade All-Stars", 
			PortraitPack.CloudClub => "Cloud Club", 
			PortraitPack.ClassicClouds => "Dreamy Cloud", 
			_ => "Storybook Spirits", 
		};
	}

	public static IReadOnlyList<Hairstyle> Hairstyles(this PortraitPack p)
	{
		return (from h in Enum.GetValues<Hairstyle>()
			where h.Pack() == p
			select h).ToList();
	}

	public static IReadOnlyList<Outfit> Outfits(this PortraitPack p)
	{
		return p switch
		{
			PortraitPack.StorybookSpirits => new Outfit[5]
			{
				Outfit.ForestPoncho,
				Outfit.CourierDress,
				Outfit.GardenApron,
				Outfit.TravelCoat,
				Outfit.LanternRobe
			}, 
			PortraitPack.PuffPals => new Outfit[5]
			{
				Outfit.Classic,
				Outfit.Hoodie,
				Outfit.Dress,
				Outfit.Overalls,
				Outfit.Jacket
			}, 
			PortraitPack.KiCrew => new Outfit[5]
			{
				Outfit.Gi,
				Outfit.BattleArmor,
				Outfit.TrainingSuit,
				Outfit.MonkRobe,
				Outfit.FlightSuit
			}, 
			PortraitPack.ArcadeAllStars => new Outfit[5]
			{
				Outfit.Adventurer,
				Outfit.KnightArmor,
				Outfit.RoyalDress,
				Outfit.SpaceSuit,
				Outfit.MascotSuit
			}, 
			_ => new Outfit[1], 
		};
	}

	public static PortraitPack Pack(this Hairstyle h)
	{
		if (h <= Hairstyle.CloudWisp)
		{
			if (h <= Hairstyle.HaloBob)
			{
				if (h <= Hairstyle.Puff)
				{
					return PortraitPack.PuffPals;
				}
				return PortraitPack.KiCrew;
			}
			if (h <= Hairstyle.Pirate)
			{
				return PortraitPack.ArcadeAllStars;
			}
			return PortraitPack.CloudClub;
		}
		if (h <= Hairstyle.ClassicMoonbeam)
		{
			return PortraitPack.ClassicClouds;
		}
		return PortraitPack.StorybookSpirits;
	}

	public static string RawValue(this Hairstyle h)
	{
		return h switch
		{
			Hairstyle.Bob => "Soft bob", 
			Hairstyle.Topknot => "Spiral bun", 
			Hairstyle.Pixie => "Wild pixie", 
			Hairstyle.TwinBuns => "Space buns", 
			Hairstyle.Waves => "Long waves", 
			Hairstyle.Curls => "Curly crop", 
			Hairstyle.Ponytail => "High pony", 
			Hairstyle.Undercut => "Undercut", 
			Hairstyle.Braids => "Twin braids", 
			Hairstyle.Puff => "Cloud puff", 
			Hairstyle.Flame => "Flame spikes", 
			Hairstyle.Comet => "Comet sweep", 
			Hairstyle.PartedSpikes => "Split spikes", 
			Hairstyle.BladeTail => "Blade ponytail", 
			Hairstyle.Crest => "Rising crest", 
			Hairstyle.Crescent => "Crescent crop", 
			Hairstyle.Storm => "Storm mane", 
			Hairstyle.MonkKnot => "Temple knot", 
			Hairstyle.WindTails => "Wind tails", 
			Hairstyle.HaloBob => "Orbit bob", 
			Hairstyle.HeroCap => "Hero cap", 
			Hairstyle.FoxHood => "Fox hood", 
			Hairstyle.KnightHelm => "Knight helm", 
			Hairstyle.Crown => "Star crown", 
			Hairstyle.Robot => "Robot shell", 
			Hairstyle.LeafCap => "Leaf cap", 
			Hairstyle.Pilot => "Pilot helmet", 
			Hairstyle.BearHood => "Bear hood", 
			Hairstyle.Jester => "Jester hat", 
			Hairstyle.Pirate => "Captain hat", 
			Hairstyle.CloudCumulus => "Cumulus", 
			Hairstyle.CloudCotton => "Cotton", 
			Hairstyle.CloudRipple => "Ripple", 
			Hairstyle.CloudBillow => "Billow", 
			Hairstyle.CloudBloom => "Bloom", 
			Hairstyle.CloudTuft => "Tuft", 
			Hairstyle.CloudDrift => "Drift", 
			Hairstyle.CloudPillow => "Pillow", 
			Hairstyle.CloudNimbus => "Nimbus", 
			Hairstyle.CloudWisp => "Wisp", 
			Hairstyle.ClassicSky => "Sky wash", 
			Hairstyle.ClassicPeach => "Peach haze", 
			Hairstyle.ClassicMeadow => "Meadow mist", 
			Hairstyle.ClassicSunbeam => "Sunbeam blend", 
			Hairstyle.ClassicTwilight => "Twilight swirl", 
			Hairstyle.ClassicApricot => "Apricot glow", 
			Hairstyle.ClassicPetal => "Petal drift", 
			Hairstyle.ClassicBluebell => "Bluebell breeze", 
			Hairstyle.ClassicSherbet => "Sherbet skies", 
			Hairstyle.ClassicMoonbeam => "Moonbeam mist", 
			Hairstyle.SeedHood => "Seed hood", 
			Hairstyle.RibbonBob => "Ribbon bob", 
			Hairstyle.SleepyEars => "Sleepy ears", 
			Hairstyle.MeadowHat => "Meadow hat", 
			Hairstyle.CatKerchief => "Cat kerchief", 
			Hairstyle.WindPart => "Wind part", 
			Hairstyle.CandleTuft => "Candle tuft", 
			Hairstyle.BerryBraids => "Berry braids", 
			Hairstyle.AcornCap => "Acorn cap", 
			_ => "Moon mantle", 
		};
	}

	public static string RawValue(this Colorway c)
	{
		return c.ToString();
	}

	public static string RawValue(this Outfit o)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToLowerInvariant(o.ToString()[0]));
		string text = o.ToString();
		return string.Concat(readOnlySpan, text.Substring(1, text.Length - 1));
	}

	public static string RawValue(this SkinTone s)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToLowerInvariant(s.ToString()[0]));
		string text = s.ToString();
		return string.Concat(readOnlySpan, text.Substring(1, text.Length - 1));
	}

	public static Color Hair(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 4692924u, 
			Colorway.Coral => 16152689u, 
			Colorway.Mint => 1417081u, 
			Colorway.Honey => 15052328u, 
			Colorway.Violet => 8415159u, 
			Colorway.Ember => 9977643u, 
			Colorway.Rose => 14114962u, 
			Colorway.Cobalt => 3423096u, 
			Colorway.Peach => 5847875u, 
			_ => 12305883u, 
		});
	}

	public static Color AvatarBackground(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 16372148u, 
			Colorway.Coral => 14274293u, 
			Colorway.Mint => 16313784u, 
			Colorway.Honey => 12379384u, 
			Colorway.Violet => 13888974u, 
			Colorway.Ember => 12380127u, 
			Colorway.Rose => 13229559u, 
			Colorway.Cobalt => 16243893u, 
			Colorway.Peach => 15912926u, 
			_ => 14537461u, 
		});
	}

	public static Color OutfitColor(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 7392501u, 
			Colorway.Coral => 16750992u, 
			Colorway.Mint => 6869673u, 
			Colorway.Honey => 16175720u, 
			Colorway.Violet => 11772129u, 
			Colorway.Ember => 15573092u, 
			Colorway.Rose => 15641551u, 
			Colorway.Cobalt => 9215723u, 
			Colorway.Peach => 15970451u, 
			_ => 12704971u, 
		});
	}

	public static Color Accent(this Colorway c)
	{
		uint hex;
		switch (c)
		{
		case Colorway.Blue:
		case Colorway.Cobalt:
			hex = 16370807u;
			break;
		case Colorway.Coral:
		case Colorway.Rose:
			hex = 12576711u;
			break;
		case Colorway.Mint:
		case Colorway.Moon:
			hex = 15842523u;
			break;
		case Colorway.Honey:
		case Colorway.Ember:
			hex = 10148817u;
			break;
		default:
			hex = 16373131u;
			break;
		}
		return Rgb(hex);
	}

	public static SkinTone DefaultSkinTone(this Colorway c)
	{
		switch (c)
		{
		case Colorway.Blue:
		case Colorway.Coral:
			return SkinTone.Peach;
		case Colorway.Mint:
		case Colorway.Violet:
			return SkinTone.WarmTan;
		case Colorway.Honey:
		case Colorway.Rose:
			return SkinTone.Sand;
		case Colorway.Ember:
		case Colorway.Peach:
			return SkinTone.Caramel;
		case Colorway.Cobalt:
			return SkinTone.Cocoa;
		default:
			return SkinTone.Deep;
		}
	}

	public static Color SkinColor(this SkinTone s)
	{
		return Rgb(s switch
		{
			SkinTone.Porcelain => 16244689u, 
			SkinTone.Peach => 16763309u, 
			SkinTone.Sand => 16107419u, 
			SkinTone.WarmTan => 15380879u, 
			SkinTone.Caramel => 12154449u, 
			SkinTone.Cocoa => 9065282u, 
			SkinTone.Umber => 7685935u, 
			_ => 6897975u, 
		});
	}

	public static Color CloudTint(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 7585001u, 
			Colorway.Coral => 15637144u, 
			Colorway.Mint => 7456172u, 
			Colorway.Honey => 15188068u, 
			Colorway.Violet => 11507931u, 
			Colorway.Ember => 14592638u, 
			Colorway.Rose => 14391741u, 
			Colorway.Cobalt => 9018845u, 
			Colorway.Peach => 15708826u, 
			_ => 11913694u, 
		});
	}

	public static Color CloudFeatureInk(this Colorway c)
	{
		Color color = c.CloudTint();
		return Rgb(0.12 + (double)(int)color.R / 255.0 * 0.3, 0.12 + (double)(int)color.G / 255.0 * 0.3, 0.12 + (double)(int)color.B / 255.0 * 0.3);
	}

	public static Color CloudFill(this Colorway c, double lightness)
	{
		Color color = c.CloudTint();
		double num = (double)(int)color.R / 255.0;
		double num2 = (double)(int)color.G / 255.0;
		double num3 = (double)(int)color.B / 255.0;
		return Rgb(num + (1.0 - num) * lightness, num2 + (1.0 - num2) * lightness, num3 + (1.0 - num3) * lightness);
	}

	public static (uint Base, uint Shade, uint Highlight) ClassicCloudPalette(this Colorway c)
	{
		return c switch
		{
			Colorway.Blue => (Base: 7980525u, Shade: 4427738u, Highlight: 11856110u), 
			Colorway.Coral => (Base: 15702673u, Shade: 15232633u, Highlight: 16303012u), 
			Colorway.Mint => (Base: 8833440u, Shade: 4630668u, Highlight: 12574367u), 
			Colorway.Honey => (Base: 15387504u, Shade: 14459971u, Highlight: 16312233u), 
			Colorway.Violet => (Base: 11638746u, Shade: 8873411u, Highlight: 14401769u), 
			Colorway.Ember => (Base: 15245678u, Shade: 14120779u, Highlight: 16109203u), 
			Colorway.Rose => (Base: 15113918u, Shade: 13593759u, Highlight: 16041432u), 
			Colorway.Cobalt => (Base: 8623053u, Shade: 5598386u, Highlight: 12109036u), 
			Colorway.Peach => (Base: 15778992u, Shade: 15044522u, Highlight: 16245162u), 
			_ => (Base: 12569302u, Shade: 9807548u, Highlight: 14867949u), 
		};
	}

	public static double ClassicCloudHue(this Colorway c)
	{
		return c switch
		{
			Colorway.Blue => 0.57, 
			Colorway.Coral => 0.02, 
			Colorway.Mint => 0.4, 
			Colorway.Honey => 0.13, 
			Colorway.Violet => 0.75, 
			Colorway.Ember => 0.07, 
			Colorway.Rose => 0.9, 
			Colorway.Cobalt => 0.65, 
			Colorway.Peach => 0.04, 
			_ => 0.52, 
		};
	}

	public static Colorway ClassicCloudBackground(this Colorway c)
	{
		switch (c)
		{
		case Colorway.Coral:
		case Colorway.Ember:
		case Colorway.Rose:
		case Colorway.Peach:
			return Colorway.Honey;
		case Colorway.Blue:
		case Colorway.Violet:
		case Colorway.Cobalt:
			return Colorway.Mint;
		case Colorway.Mint:
			return Colorway.Peach;
		case Colorway.Honey:
			return Colorway.Rose;
		default:
			return Colorway.Blue;
		}
	}

	public static Color ClassicCloudTint(this Colorway c)
	{
		return Rgb(c.ClassicCloudPalette().Base);
	}

	public static Color StorybookHair(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 4352627u, 
			Colorway.Coral => 11166290u, 
			Colorway.Mint => 5466189u, 
			Colorway.Honey => 10253886u, 
			Colorway.Violet => 6708856u, 
			Colorway.Ember => 8735538u, 
			Colorway.Rose => 10380149u, 
			Colorway.Cobalt => 3423562u, 
			Colorway.Peach => 4799029u, 
			_ => 9607564u, 
		});
	}

	public static Color StorybookFabric(this Colorway c)
	{
		return Rgb(c switch
		{
			Colorway.Blue => 8497075u, 
			Colorway.Coral => 13930109u, 
			Colorway.Mint => 9546371u, 
			Colorway.Honey => 14597750u, 
			Colorway.Violet => 11182269u, 
			Colorway.Ember => 12421222u, 
			Colorway.Rose => 13146535u, 
			Colorway.Cobalt => 7899042u, 
			Colorway.Peach => 14401696u, 
			_ => 13094588u, 
		});
	}

	public static double EyeOpenness(this PortraitExpression e, int index)
	{
		switch (e)
		{
		case PortraitExpression.Curious:
			return (index == 0) ? 1.0 : 0.96;
		case PortraitExpression.Listening:
			return 1.06;
		case PortraitExpression.Thinking:
			return 0.86;
		case PortraitExpression.Happy:
			return 0.92;
		case PortraitExpression.Sleepy:
		case PortraitExpression.Laughing:
			return 0.06;
		case PortraitExpression.Surprised:
		case PortraitExpression.Excited:
			return 1.09;
		case PortraitExpression.Skeptical:
			return (index == 0) ? 0.66 : 1.02;
		case PortraitExpression.Shy:
			return 0.86;
		case PortraitExpression.Determined:
			return 0.82;
		case PortraitExpression.Loving:
			return 1.0;
		default:
			return 0.85;
		}
	}

	public static bool HasClosedEyes(this PortraitExpression e)
	{
		if (e == PortraitExpression.Sleepy || e == PortraitExpression.Laughing)
		{
			return true;
		}
		return false;
	}

	public static double HeadTilt(this PortraitExpression e)
	{
		switch (e)
		{
		case PortraitExpression.Curious:
			return -0.035;
		case PortraitExpression.Listening:
			return 0.035;
		case PortraitExpression.Thinking:
			return -0.06;
		case PortraitExpression.Sleepy:
			return 0.07;
		case PortraitExpression.Skeptical:
			return -0.085;
		case PortraitExpression.Shy:
		case PortraitExpression.Sad:
			return 0.075;
		case PortraitExpression.Laughing:
			return -0.03;
		default:
			return 0.0;
		}
	}

	public static (double X, double Y) RestingGaze(this PortraitExpression e)
	{
		return e switch
		{
			PortraitExpression.Thinking => (X: -0.35, Y: -0.25), 
			PortraitExpression.Shy => (X: 0.32, Y: 0.3), 
			PortraitExpression.Sad => (X: 0.0, Y: 0.3), 
			PortraitExpression.Skeptical => (X: -0.3, Y: 0.0), 
			_ => (X: 0.0, Y: 0.0), 
		};
	}

	public static PortraitExpression Expression(this PortraitMood m)
	{
		return m switch
		{
			PortraitMood.Running => PortraitExpression.Thinking, 
			PortraitMood.Happy => PortraitExpression.Happy, 
			PortraitMood.Sad => PortraitExpression.Sad, 
			_ => PortraitExpression.Sleepy, 
		};
	}

	public static PortraitMood MoodFor(AgentTaskStatus status)
	{
		switch (status)
		{
		case AgentTaskStatus.Starting:
		case AgentTaskStatus.Running:
			return PortraitMood.Running;
		case AgentTaskStatus.Finished:
			return PortraitMood.Happy;
		case AgentTaskStatus.Failed:
			return PortraitMood.Sad;
		default:
			return PortraitMood.Sleepy;
		}
	}
}
