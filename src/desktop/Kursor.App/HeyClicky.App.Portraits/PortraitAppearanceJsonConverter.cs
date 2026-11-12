using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeyClicky.App.Portraits;

public sealed class PortraitAppearanceJsonConverter : JsonConverter<PortraitAppearance>
{
	public override PortraitAppearance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonElement o;
		using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
		{
			o = jsonDocument.RootElement;
			PortraitAppearance portraitAppearance = PortraitAppearance.Make();
			Hairstyle hairstyle = Opt<Hairstyle>("hairstyle", portraitAppearance.Hairstyle, (Hairstyle h) => h.RawValue());
			Colorway colorway = Opt<Colorway>("hairColor", portraitAppearance.HairColor, (Colorway c) => c.RawValue());
			Outfit outfitStyle = Opt<Outfit>("outfitStyle", hairstyle.Pack().Outfits()[0], (Outfit x) => x.RawValue());
			Colorway colorway2 = Opt<Colorway>("outfitColor", colorway, (Colorway c) => c.RawValue());
			Colorway accentColor = Opt<Colorway>("accentColor", colorway2, (Colorway c) => c.RawValue());
			SkinTone skinTone = Opt<SkinTone>("skinTone", colorway.DefaultSkinTone(), (SkinTone s) => s.RawValue());
			Colorway eyeColor = Opt<Colorway>("eyeColor", colorway2, (Colorway c) => c.RawValue());
			Colorway backgroundColor = Opt<Colorway>("backgroundColor", (hairstyle.Pack() == PortraitPack.ClassicClouds) ? colorway.ClassicCloudBackground() : colorway, (Colorway c) => c.RawValue());
			return new PortraitAppearance(hairstyle, colorway, outfitStyle, colorway2, accentColor, skinTone, eyeColor, backgroundColor);
			T Opt<T>(string key, T fallback, Func<T, string> raw) where T : struct, Enum
			{
				string s2 = S(key);
				if (s2 == null)
				{
					return fallback;
				}
				return Enum.GetValues<T>().Cast<T?>().FirstOrDefault((T? v) => raw(v.Value) == s2) ?? fallback;
			}
		}
		string? S(string k)
		{
			if (!o.TryGetProperty(k, out var value) || value.ValueKind != JsonValueKind.String)
			{
				return null;
			}
			return value.GetString();
		}
	}

	public override void Write(Utf8JsonWriter w, PortraitAppearance a, JsonSerializerOptions options)
	{
		w.WriteStartObject();
		w.WriteString("hairstyle", a.Hairstyle.RawValue());
		w.WriteString("hairColor", a.HairColor.RawValue());
		w.WriteString("outfitStyle", a.OutfitStyle.RawValue());
		w.WriteString("outfitColor", a.OutfitColor.RawValue());
		w.WriteString("accentColor", a.AccentColor.RawValue());
		w.WriteString("skinTone", a.SkinTone.RawValue());
		w.WriteString("eyeColor", a.EyeColor.RawValue());
		w.WriteString("backgroundColor", a.BackgroundColor.RawValue());
		w.WriteNumber("backgroundPaletteVersion", 1);
		w.WriteEndObject();
	}
}
