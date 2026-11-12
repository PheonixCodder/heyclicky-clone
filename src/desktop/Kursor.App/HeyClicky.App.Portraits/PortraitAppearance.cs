using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using Windows.UI;

namespace HeyClicky.App.Portraits;

[JsonConverter(typeof(PortraitAppearanceJsonConverter))]
public sealed record PortraitAppearance(Hairstyle Hairstyle, Colorway HairColor, Outfit OutfitStyle, Colorway OutfitColor, Colorway AccentColor, SkinTone SkinTone, Colorway EyeColor, Colorway BackgroundColor)
{
	public PortraitPack Pack => Hairstyle.Pack();

	public Color Hair => Pack switch
	{
		PortraitPack.ClassicClouds => HairColor.ClassicCloudTint(), 
		PortraitPack.CloudClub => HairColor.CloudTint(), 
		PortraitPack.StorybookSpirits => HairColor.StorybookHair(), 
		_ => HairColor.Hair(), 
	};

	public Color Outfit
	{
		get
		{
			if (Pack != PortraitPack.StorybookSpirits)
			{
				return OutfitColor.OutfitColor();
			}
			return OutfitColor.StorybookFabric();
		}
	}

	public Color Accent => AccentColor.Accent();

	public Color Skin => SkinTone.SkinColor();

	public Color Eyes => Pack switch
	{
		PortraitPack.ClassicClouds => PortraitData.ClassicCloudEyeColor, 
		PortraitPack.CloudClub => EyeColor.CloudTint(), 
		PortraitPack.StorybookSpirits => EyeColor.StorybookHair(), 
		_ => EyeColor.OutfitColor(), 
	};

	public Color Background => BackgroundColor.AvatarBackground();

	public static readonly IReadOnlyList<string> HomeCloudColorNames = new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "red", "blue", "yellow", "green" });

	public static IReadOnlyList<PortraitAppearance> HomeClouds { get; } = HomeCloudColorNames.Select(HomeCloud).ToList();

	public bool IsHomeCloud => HomeClouds.Any(SameCloud);

	public string? HomeCloudColorName
	{
		get
		{
			for (int i = 0; i < HomeClouds.Count; i++)
			{
				if (SameCloud(HomeClouds[i]))
				{
					return HomeCloudColorNames[i];
				}
			}
			return null;
		}
	}

	public static PortraitAppearance Make(Hairstyle hairstyle = Hairstyle.Bob, Colorway colorway = Colorway.Blue)
	{
		PortraitPack portraitPack = hairstyle.Pack();
		return new PortraitAppearance(hairstyle, colorway, portraitPack.Outfits()[0], colorway, colorway, colorway.DefaultSkinTone(), colorway, (portraitPack == PortraitPack.ClassicClouds) ? colorway.ClassicCloudBackground() : colorway);
	}

	public static PortraitAppearance Seeded(string seed)
	{
		return PortraitCharacter.For(seed).Appearance;
	}

	public static PortraitAppearance Varied(IReadOnlyList<PortraitAppearance> existing, PortraitPack pack, Random? rng = null)
	{
		if (rng == null)
		{
			rng = Random.Shared;
		}
		if (pack == PortraitPack.ClassicClouds)
		{
			return VariedClassicCloud(existing, rng);
		}
		List<Hairstyle> list2 = LeastUsed<Hairstyle>((PortraitAppearance a) => a.Hairstyle, pack.Hairstyles());
		List<Colorway> list3 = LeastUsed<Colorway>((PortraitAppearance a) => a.HairColor, Enum.GetValues<Colorway>());
		List<Outfit> list4 = LeastUsed<Outfit>((PortraitAppearance a) => a.OutfitStyle, pack.Outfits());
		List<Colorway> list5 = LeastUsed<Colorway>((PortraitAppearance a) => a.OutfitColor, Enum.GetValues<Colorway>());
		List<SkinTone> list6 = LeastUsed<SkinTone>((PortraitAppearance a) => a.SkinTone, Enum.GetValues<SkinTone>());
		List<Colorway> list7 = LeastUsed<Colorway>((PortraitAppearance a) => a.EyeColor, Enum.GetValues<Colorway>());
		PortraitAppearance result = Make();
		int num = -1;
		for (int j = 0; j < 32; j++)
		{
			PortraitAppearance candidate = Make(Pick<Hairstyle>(list2), Pick<Colorway>(list3));
			if (pack.IsCloud())
			{
				candidate = candidate with
				{
					EyeColor = Pick<Colorway>(list7)
				};
			}
			else
			{
				candidate = candidate with
				{
					OutfitStyle = Pick<Outfit>(list4),
					OutfitColor = Pick<Colorway>(list5),
					SkinTone = Pick<SkinTone>(list6),
					EyeColor = Pick<Colorway>(list7)
				};
				candidate = candidate with
				{
					AccentColor = candidate.OutfitColor
				};
			}
			int num2 = ((existing.Count == 0) ? 14 : existing.Min((PortraitAppearance e) => candidate.VisualDifference(e)));
			if (num2 > num)
			{
				result = candidate;
				num = num2;
			}
		}
		return result;
		List<T> LeastUsed<T>(Func<PortraitAppearance, T> key, IReadOnlyList<T> options) where T : struct
		{
			List<int> counts = options.Select((T o) => existing.Count((PortraitAppearance a) => EqualityComparer<T>.Default.Equals(key(a), o))).ToList();
			int lowest = ((counts.Count != 0) ? counts.Min() : 0);
			return options.Where((T o, int i) => counts[i] == lowest).ToList();
		}
		T Pick<T>(List<T> list)
		{
			return list[rng.Next(list.Count)];
		}
	}

	private static PortraitAppearance VariedClassicCloud(IReadOnlyList<PortraitAppearance> existing, Random rng)
	{
		Colorway[] colors = Enum.GetValues<Colorway>();
		IReadOnlyList<Hairstyle> styles = PortraitPack.ClassicClouds.Hairstyles();
		List<List<PortraitAppearance>> source = (from offset in Enumerable.Range(0, colors.Length)
			select styles.Select((Hairstyle style, int index) => Make(style, colors[(index + offset) % colors.Length])).ToList()).ToList();
		List<List<PortraitAppearance>> list = (from r in source
			select r.Where((PortraitAppearance c) => Uses(c) == 0).ToList() into r
			where r.Count > 0
			select r).ToList();
		if (list.Count > 0)
		{
			int smallest = list.Min((List<PortraitAppearance> r) => r.Count);
			List<List<PortraitAppearance>> list2 = list.Where((List<PortraitAppearance> r) => r.Count == smallest).ToList();
			List<PortraitAppearance> list3 = list2[rng.Next(list2.Count)];
			return list3[rng.Next(list3.Count)];
		}
		List<PortraitAppearance> source2 = source.SelectMany((List<PortraitAppearance> r) => r).ToList();
		int least = ((IEnumerable<PortraitAppearance>)source2).Min((Func<PortraitAppearance, int>)Uses);
		List<PortraitAppearance> list4 = source2.Where((PortraitAppearance c) => Uses(c) == least).ToList();
		return list4[rng.Next(list4.Count)];
		int Uses(PortraitAppearance c)
		{
			return existing.Count((PortraitAppearance e) => e.Hairstyle == c.Hairstyle && e.HairColor == c.HairColor);
		}
	}

	private int VisualDifference(PortraitAppearance other)
	{
		int num = 0;
		if (Hairstyle != other.Hairstyle)
		{
			num += 3;
		}
		if (HairColor != other.HairColor)
		{
			num += 3;
		}
		if (!Pack.IsCloud() && SkinTone != other.SkinTone)
		{
			num += 3;
		}
		if (!Pack.IsCloud() && OutfitStyle != other.OutfitStyle)
		{
			num += 2;
		}
		if (!Pack.IsCloud() && OutfitColor != other.OutfitColor)
		{
			num += 2;
		}
		if (Pack != PortraitPack.ClassicClouds && EyeColor != other.EyeColor)
		{
			num++;
		}
		return num;
	}

	public static PortraitAppearance HomeMascot(string cursorHex)
	{
		return cursorHex.ToUpperInvariant() switch
		{
			"#FF4C4C" => Make(Hairstyle.ClassicPeach, Colorway.Coral), 
			"#26C281" => Make(Hairstyle.ClassicMeadow, Colorway.Mint), 
			"#D4A017" => Make(Hairstyle.ClassicSunbeam, Colorway.Honey), 
			_ => Make(Hairstyle.ClassicSky), 
		};
	}

	public static PortraitAppearance HomeCloud(string colorName)
	{
		return colorName switch
		{
			"red" => Make(Hairstyle.ClassicPeach, Colorway.Coral), 
			"green" => Make(Hairstyle.ClassicMeadow, Colorway.Mint), 
			"yellow" => Make(Hairstyle.ClassicSunbeam, Colorway.Honey), 
			_ => Make(Hairstyle.ClassicSky), 
		};
	}

	private bool SameCloud(PortraitAppearance other)
	{
		if (Hairstyle == other.Hairstyle)
		{
			return HairColor == other.HairColor;
		}
		return false;
	}

	public static PortraitAppearance RandomHomeCloud(IEnumerable<PortraitAppearance> existing, Random? rng = null)
	{
		List<PortraitAppearance> looks = existing.ToList();
		int least = ((IEnumerable<PortraitAppearance>)HomeClouds).Min((Func<PortraitAppearance, int>)Uses);
		List<PortraitAppearance> list = HomeClouds.Where((PortraitAppearance c) => Uses(c) == least).ToList();
		return list[(rng ?? Random.Shared).Next(list.Count)];
		int Uses(PortraitAppearance cloud)
		{
			return looks.Count(cloud.SameCloud);
		}
	}

	public static PortraitAppearance SeededHomeCloud(string seed)
	{
		return HomeClouds[(int)(SeedHash(seed) % (ulong)HomeClouds.Count)];
	}

	public static ulong SeedHash(string seed)
	{
		ulong num = 5381uL;
		foreach (Rune item in seed.EnumerateRunes())
		{
			num = num * 33 + (ulong)item.Value;
		}
		return num;
	}

	[CompilerGenerated]
	private PortraitAppearance(PortraitAppearance original)
	{
		Hairstyle = original.Hairstyle;
		HairColor = original.HairColor;
		OutfitStyle = original.OutfitStyle;
		OutfitColor = original.OutfitColor;
		AccentColor = original.AccentColor;
		SkinTone = original.SkinTone;
		EyeColor = original.EyeColor;
		BackgroundColor = original.BackgroundColor;
	}
}
