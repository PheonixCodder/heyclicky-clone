using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace HeyClicky.App.Portraits;

public sealed record PortraitCharacter(string Name, Hairstyle Hairstyle, Colorway Colorway, string Introduction, Outfit OutfitStyle = Outfit.Classic, Colorway? OutfitColor = null, Colorway? AccentColor = null, SkinTone? SkinTone = null, Colorway? EyeColor = null)
{
	public PortraitPack Pack => Hairstyle.Pack();

	public PortraitAppearance Appearance
	{
		get
		{
			PortraitAppearance portraitAppearance = PortraitAppearance.Make(Hairstyle, Colorway);
			Colorway colorway = OutfitColor ?? Colorway;
			return portraitAppearance with
			{
				OutfitStyle = OutfitStyle,
				OutfitColor = colorway,
				AccentColor = (AccentColor ?? colorway),
				SkinTone = (SkinTone ?? Colorway.DefaultSkinTone()),
				EyeColor = (EyeColor ?? Colorway)
			};
		}
	}

	public static readonly PortraitCharacter Sprout = new PortraitCharacter("Sprout", Hairstyle.Pixie, Colorway.Mint, "Probably has a plan. Definitely has a cowlick.", Outfit.Classic, null, null, null, null);

	public static readonly PortraitCharacter Sunny = new PortraitCharacter("Sunny", Hairstyle.TwinBuns, Colorway.Honey, "Pocket-sized sunshine. Two buns of optimism.", Outfit.Classic, null, null, null, null);

	public static readonly PortraitCharacter EmberCharacter = new PortraitCharacter("Ember", Hairstyle.Curls, Colorway.Ember, "A spark of mischief in every curl.", Outfit.Classic, null, null, null, null);

	public static readonly IReadOnlyList<PortraitCharacter> Cast = new PortraitCharacter[10]
	{
		new PortraitCharacter("Blue", Hairstyle.Bob, Colorway.Blue, "A little curious. Very happy you're here.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Poppy", Hairstyle.Topknot, Colorway.Coral, "Big ideas. One very small bun.", Outfit.Classic, null, null, null, null),
		Sprout,
		Sunny,
		new PortraitCharacter("Lumi", Hairstyle.Waves, Colorway.Violet, "Dreamy hair. Surprisingly good focus.", Outfit.Classic, null, null, null, null),
		EmberCharacter,
		new PortraitCharacter("Peppy", Hairstyle.Ponytail, Colorway.Rose, "Always up for it. Ponytail included.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Indie", Hairstyle.Undercut, Colorway.Cobalt, "Quiet confidence. A slightly rebellious fringe.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Coco", Hairstyle.Braids, Colorway.Peach, "Keeps your loose ends neatly braided.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Mochi", Hairstyle.Puff, Colorway.Moon, "Cloud-soft hair. An excellent listener.", Outfit.Classic, null, null, null, null)
	};

	private static readonly IReadOnlyList<PortraitCharacter> PuffPalsDisplay = Cast.Select((PortraitCharacter c, int i) => i switch
	{
		3 => Cast[1], 
		1 => Cast[3], 
		_ => c, 
	}).ToList();

	private static readonly IReadOnlyList<PortraitCharacter> ClassicClouds = new PortraitCharacter[10]
	{
		new PortraitCharacter("Sky", Hairstyle.ClassicSky, Colorway.Blue, "A familiar little face. A sky full of ideas.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Peaches", Hairstyle.ClassicPeach, Colorway.Coral, "A warm welcome, every time.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Meadow", Hairstyle.ClassicMeadow, Colorway.Mint, "A quiet moment and a fresh start.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Sunbeam", Hairstyle.ClassicSunbeam, Colorway.Honey, "A little sunshine for your day.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Twilight", Hairstyle.ClassicTwilight, Colorway.Violet, "Good ideas come in soft colors.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Apricot", Hairstyle.ClassicApricot, Colorway.Ember, "A tiny glow of encouragement.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Petal", Hairstyle.ClassicPetal, Colorway.Rose, "Gentle company. Thoughtful help.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Bluebell", Hairstyle.ClassicBluebell, Colorway.Cobalt, "A daydream with a plan.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Sherbet", Hairstyle.ClassicSherbet, Colorway.Peach, "A little color for the everyday.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Moonbeam", Hairstyle.ClassicMoonbeam, Colorway.Moon, "Keeping you company, day or night.", Outfit.Classic, null, null, null, null)
	};

	private static readonly IReadOnlyList<PortraitCharacter> CloudClub = new PortraitCharacter[10]
	{
		new PortraitCharacter("Nimbus", Hairstyle.CloudCumulus, Colorway.Blue, "The original daydreamer. Always here to help.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Mallow", Hairstyle.CloudCotton, Colorway.Moon, "Soft landing. Very good listener.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Dewdrop", Hairstyle.CloudRipple, Colorway.Mint, "A fresh little thought, floating your way.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Buttercup", Hairstyle.CloudBillow, Colorway.Honey, "A pocket of sunshine on a cloudy day.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Daydream", Hairstyle.CloudBloom, Colorway.Violet, "Head in the clouds. Heart in the work.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Sorbet", Hairstyle.CloudTuft, Colorway.Peach, "A tiny swirl of optimism.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Drift", Hairstyle.CloudDrift, Colorway.Cobalt, "Keeps things moving at a kinder pace.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Rosie", Hairstyle.CloudPillow, Colorway.Rose, "Small cloud. Warm welcome.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Blush", Hairstyle.CloudNimbus, Colorway.Coral, "A little bashful. A lot of bright ideas.", Outfit.Classic, null, null, null, null),
		new PortraitCharacter("Wisp", Hairstyle.CloudWisp, Colorway.Ember, "Light on the breeze. Quick on the uptake.", Outfit.Classic, null, null, null, null)
	};

	private static readonly IReadOnlyList<PortraitCharacter> Storybook = new PortraitCharacter[10]
	{
		new PortraitCharacter("Fern", Hairstyle.SeedHood, Colorway.Mint, "Knows a quiet shortcut through the woods.", Outfit.ForestPoncho, Colorway.Mint, Colorway.Honey, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Peach),
		new PortraitCharacter("Miri", Hairstyle.RibbonBob, Colorway.Peach, "A small delivery. A whole new adventure.", Outfit.CourierDress, Colorway.Cobalt, Colorway.Coral, HeyClicky.App.Portraits.SkinTone.WarmTan, Colorway.Peach),
		new PortraitCharacter("Bramble", Hairstyle.SleepyEars, Colorway.Moon, "Carries an extra scarf, just in case.", Outfit.ForestPoncho, Colorway.Moon, Colorway.Ember, HeyClicky.App.Portraits.SkinTone.Porcelain, Colorway.Cobalt),
		new PortraitCharacter("Tansy", Hairstyle.MeadowHat, Colorway.Honey, "Plants little ideas and remembers to water them.", Outfit.GardenApron, Colorway.Mint, Colorway.Peach, HeyClicky.App.Portraits.SkinTone.Cocoa, Colorway.Honey),
		new PortraitCharacter("Nori", Hairstyle.CatKerchief, Colorway.Cobalt, "Excellent company for a rainy afternoon.", Outfit.CourierDress, Colorway.Violet, Colorway.Honey, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Mint),
		new PortraitCharacter("Solvi", Hairstyle.WindPart, Colorway.Ember, "Keeps a map of places worth getting lost in.", Outfit.TravelCoat, Colorway.Blue, Colorway.Honey, HeyClicky.App.Portraits.SkinTone.Caramel, Colorway.Blue),
		new PortraitCharacter("Wick", Hairstyle.CandleTuft, Colorway.Ember, "A warm little light when the work runs late.", Outfit.LanternRobe, Colorway.Honey, Colorway.Ember, HeyClicky.App.Portraits.SkinTone.Peach, Colorway.Honey),
		new PortraitCharacter("Pippa", Hairstyle.BerryBraids, Colorway.Peach, "Big pockets for very small discoveries.", Outfit.GardenApron, Colorway.Coral, Colorway.Mint, HeyClicky.App.Portraits.SkinTone.Deep, Colorway.Peach),
		new PortraitCharacter("Yarrow", Hairstyle.AcornCap, Colorway.Honey, "Every long journey starts with a tiny seed.", Outfit.TravelCoat, Colorway.Ember, Colorway.Mint, HeyClicky.App.Portraits.SkinTone.Umber, Colorway.Honey),
		new PortraitCharacter("Lune", Hairstyle.MoonMantle, Colorway.Violet, "Listens to the rain and your unfinished thoughts.", Outfit.LanternRobe, Colorway.Violet, Colorway.Blue, HeyClicky.App.Portraits.SkinTone.Porcelain, Colorway.Violet)
	};

	private static readonly IReadOnlyList<PortraitCharacter> Arcade = new PortraitCharacter[10]
	{
		new PortraitCharacter("Dash", Hairstyle.HeroCap, Colorway.Coral, "Ready, set, let's make something.", Outfit.Adventurer, Colorway.Blue, null, HeyClicky.App.Portraits.SkinTone.Peach, Colorway.Honey),
		new PortraitCharacter("Pip", Hairstyle.FoxHood, Colorway.Ember, "Quick paws. Clever ideas.", Outfit.MascotSuit, Colorway.Ember, null, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Mint),
		new PortraitCharacter("Rook", Hairstyle.KnightHelm, Colorway.Moon, "Your brave little first line of defense.", Outfit.KnightArmor, Colorway.Moon, null, HeyClicky.App.Portraits.SkinTone.Deep, Colorway.Blue),
		new PortraitCharacter("Luma", Hairstyle.Crown, Colorway.Honey, "Royal manners. Mischievous plans.", Outfit.RoyalDress, Colorway.Rose, null, HeyClicky.App.Portraits.SkinTone.Caramel, Colorway.Violet),
		new PortraitCharacter("Bolt", Hairstyle.Robot, Colorway.Mint, "Beep. Boop. Beautifully done.", Outfit.SpaceSuit, Colorway.Mint, null, HeyClicky.App.Portraits.SkinTone.Cocoa, Colorway.Honey),
		new PortraitCharacter("Moss", Hairstyle.LeafCap, Colorway.Mint, "Takes the scenic route to good ideas.", Outfit.Adventurer, Colorway.Mint, null, HeyClicky.App.Portraits.SkinTone.WarmTan, Colorway.Peach),
		new PortraitCharacter("Nova", Hairstyle.Pilot, Colorway.Violet, "Every small step is a giant leap.", Outfit.SpaceSuit, Colorway.Violet, null, HeyClicky.App.Portraits.SkinTone.Umber, Colorway.Coral),
		new PortraitCharacter("Bram", Hairstyle.BearHood, Colorway.Peach, "Big hugs. Surprisingly nimble.", Outfit.MascotSuit, Colorway.Peach, null, HeyClicky.App.Portraits.SkinTone.Porcelain, Colorway.Cobalt),
		new PortraitCharacter("Jinx", Hairstyle.Jester, Colorway.Rose, "Has a trick or three up each sleeve.", Outfit.RoyalDress, Colorway.Violet, null, HeyClicky.App.Portraits.SkinTone.Deep, Colorway.Moon),
		new PortraitCharacter("Finn", Hairstyle.Pirate, Colorway.Cobalt, "A pocket-sized captain of possibility.", Outfit.Adventurer, Colorway.Coral, null, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Ember)
	};

	private static readonly IReadOnlyList<PortraitCharacter> Ki = new PortraitCharacter[10]
	{
		new PortraitCharacter("Kiro", Hairstyle.Flame, Colorway.Cobalt, "Small stance. Unreasonably big spirit.", Outfit.Gi, Colorway.Cobalt, null, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Cobalt),
		new PortraitCharacter("Sora", Hairstyle.Comet, Colorway.Honey, "Always chasing the next horizon.", Outfit.BattleArmor, Colorway.Honey, Colorway.Blue, HeyClicky.App.Portraits.SkinTone.Cocoa, Colorway.Honey),
		new PortraitCharacter("Ren", Hairstyle.PartedSpikes, Colorway.Violet, "Cool head. A perfectly timed comeback.", Outfit.TrainingSuit, Colorway.Violet, null, HeyClicky.App.Portraits.SkinTone.Peach, Colorway.Violet),
		new PortraitCharacter("Aiko", Hairstyle.BladeTail, Colorway.Peach, "A soft smile and a sharp game plan.", Outfit.Gi, Colorway.Peach, null, HeyClicky.App.Portraits.SkinTone.Caramel, Colorway.Peach),
		new PortraitCharacter("Taro", Hairstyle.Crest, Colorway.Coral, "First to practice. Last to give up.", Outfit.BattleArmor, Colorway.Coral, Colorway.Peach, HeyClicky.App.Portraits.SkinTone.Deep, Colorway.Coral),
		new PortraitCharacter("Nami", Hairstyle.Crescent, Colorway.Blue, "Makes the hard stuff look effortless.", Outfit.FlightSuit, Colorway.Blue, null, HeyClicky.App.Portraits.SkinTone.Porcelain, Colorway.Blue),
		new PortraitCharacter("Zeno", Hairstyle.Storm, Colorway.Moon, "Quiet until it's time to power up.", Outfit.TrainingSuit, Colorway.Moon, null, HeyClicky.App.Portraits.SkinTone.Umber, Colorway.Moon),
		new PortraitCharacter("Kuma", Hairstyle.MonkKnot, Colorway.Ember, "Deep breath. Then one good move.", Outfit.MonkRobe, Colorway.Ember, Colorway.Peach, HeyClicky.App.Portraits.SkinTone.WarmTan, Colorway.Ember),
		new PortraitCharacter("Rumi", Hairstyle.WindTails, Colorway.Rose, "Twice the energy. Twice the hair ties.", Outfit.Gi, Colorway.Rose, null, HeyClicky.App.Portraits.SkinTone.Sand, Colorway.Rose),
		new PortraitCharacter("Iona", Hairstyle.HaloBob, Colorway.Mint, "A tiny explorer with cosmic ambition.", Outfit.FlightSuit, Colorway.Mint, null, HeyClicky.App.Portraits.SkinTone.Cocoa, Colorway.Mint)
	};

	public static PortraitCharacter For(string seed)
	{
		ulong num = 5381uL;
		byte[] bytes = Encoding.UTF8.GetBytes(seed);
		foreach (byte b in bytes)
		{
			num = num * 33 + b;
		}
		return Cast[(int)(num % (ulong)Cast.Count)];
	}

	public static IReadOnlyList<PortraitCharacter> CharactersOf(PortraitPack pack)
	{
		return pack switch
		{
			PortraitPack.PuffPals => PuffPalsDisplay, 
			PortraitPack.ClassicClouds => ClassicClouds, 
			PortraitPack.StorybookSpirits => Storybook, 
			PortraitPack.CloudClub => CloudClub, 
			PortraitPack.ArcadeAllStars => Arcade, 
			_ => Ki, 
		};
	}

	[CompilerGenerated]
	private PortraitCharacter(PortraitCharacter original)
	{
		Name = original.Name;
		Hairstyle = original.Hairstyle;
		Colorway = original.Colorway;
		Introduction = original.Introduction;
		OutfitStyle = original.OutfitStyle;
		OutfitColor = original.OutfitColor;
		AccentColor = original.AccentColor;
		SkinTone = original.SkinTone;
		EyeColor = original.EyeColor;
	}
}
