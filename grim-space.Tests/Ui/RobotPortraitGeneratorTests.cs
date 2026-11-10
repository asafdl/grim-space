using GrimSpace.Ui.Portraits;

namespace GrimSpace.Tests.Ui;

public sealed class RobotPortraitGeneratorTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(42)]
	[InlineData(-1)]
	[InlineData(int.MaxValue)]
	public void SeedReproducesPartsAndColors(int seed) =>
		Assert.Equal(RobotPortraitGenerator.Generate(seed), RobotPortraitGenerator.Generate(seed));

	[Fact]
	public void GenerationUsesValidIndependentPartsAndCompletePalettes()
	{
		var avatars = Enumerable.Range(0, 1000).Select(RobotPortraitGenerator.Generate).ToArray();
		foreach (var avatar in avatars)
		{
			var p = avatar.Parts;
			Assert.True(Enum.IsDefined(p.Eyes) && Enum.IsDefined(p.Mouth) && Enum.IsDefined(p.Screen) &&
				Enum.IsDefined(p.Ears) && Enum.IsDefined(p.Antenna) && Enum.IsDefined(p.Shoulders) &&
				Enum.IsDefined(p.Cracks) && Enum.IsDefined(p.Tape) && Enum.IsDefined(p.Rust));
			Assert.Contains(RobotPortraitGenerator.Palettes, palette => palette.Colors == avatar.Colors);
		}
		Assert.Equal(81, avatars.Select(a => (a.Parts.Eyes, a.Parts.Mouth)).Distinct().Count());
		Assert.Equal(1000, avatars.Distinct().Count());
		Assert.All(avatars, a =>
		{
			Assert.Equal(a.Colors.Casing, a.Colors.Chest);
			Assert.Equal(a.Colors.Ears, a.Colors.Shoulders);
			Assert.NotEqual(a.Colors.Casing, a.Colors.Shoulders);
		});
	}

	[Fact]
	public void FlavorEffectsIncludeCleanPortraitsAndEveryStackingCombination()
	{
		var parts = Enumerable.Range(0, 2000).Select(seed => RobotPortraitGenerator.Generate(seed).Parts).ToArray();
		Assert.Equal(27, parts.Select(p => (p.Cracks, p.Tape, p.Rust)).Distinct().Count());
		Assert.Contains(parts, p => p.Cracks == RobotCracks.None && p.Tape == RobotTape.None && p.Rust == RobotRust.None);
		Assert.Contains(parts, p => p.Cracks != RobotCracks.None && p.Tape != RobotTape.None && p.Rust != RobotRust.None);
		var clean = new RobotPortraitParts();
		Assert.Equal(RobotCracks.None, clean.Cracks);
		Assert.Equal(RobotTape.None, clean.Tape);
		Assert.Equal(RobotRust.None, clean.Rust);
		Assert.Equal(clean with { Cracks = RobotCracks.Hairline },
			(clean with { Cracks = RobotCracks.Hairline, Tape = RobotTape.Crown }) with { Tape = RobotTape.None });
	}

	[Fact]
	public void WearPlacementVariesAcrossSurfacesWithoutChangingPresenceRules()
	{
		var parts = Enumerable.Range(0, 2000).Select(seed => RobotPortraitGenerator.Generate(seed).Parts).ToArray();
		Assert.Equal(5, parts.Where(p => p.CrackPlacement.HasValue)
			.Select(p => p.CrackPlacement!.Value.Location).Distinct().Count());
		Assert.Equal(3, parts.Where(p => p.TapePlacement.HasValue)
			.Select(p => p.TapePlacement!.Value.Location).Distinct().Count());
		foreach (var p in parts)
		{
			Assert.Equal(p.Cracks != RobotCracks.None, p.CrackPlacement.HasValue);
			Assert.Equal(p.Tape != RobotTape.None, p.TapePlacement.HasValue);
			foreach (var placement in new[] { p.CrackPlacement, p.TapePlacement })
				if (placement is { } v)
				{
					Assert.InRange(v.X, 90, 245);
					Assert.InRange(v.Y, 45, 292);
					Assert.InRange(v.RotationDegrees, 0, 359);
					Assert.InRange(v.Scale, .45f, .9751f);
				}
			if (p.CrackPlacement is { } crack)
				Assert.InRange(crack.Scale, .45f * 1.5f, .65f * 1.5f);
			if (p.TapePlacement is { } tape)
				Assert.InRange(tape.Scale, .45f, .65f);
		}
		Assert.True(parts.Where(p => p.CrackPlacement.HasValue)
			.Select(p => p.CrackPlacement).Distinct().Count() > 100);
		foreach (var placements in new[]
		{
			parts.Where(p => p.CrackPlacement.HasValue).Select(p => p.CrackPlacement!.Value),
			parts.Where(p => p.TapePlacement.HasValue).Select(p => p.TapePlacement!.Value),
		})
			Assert.Equal(4, placements.Select(p => (int)(p.RotationDegrees / 90)).Distinct().Count());
	}

	[Fact]
	public void AuthoredPalettesHaveWornHardwareAndReadableLeds()
	{
		foreach (var (_, colors) in RobotPortraitGenerator.Palettes)
		{
			foreach (var metal in new[] { colors.Casing, colors.Ears, colors.Antenna, colors.Shoulders, colors.Chest })
			{
				var channels = Channels(metal);
				Assert.True(channels.Max() - channels.Min() <= .45);
				Assert.InRange(channels.Max(), .30, .80);
			}
			Assert.True((Luminance(colors.Glyph) + .05) / (Luminance(colors.Screen) + .05) >= 4.5,
				$"Insufficient LED contrast: {colors.Screen:X8} / {colors.Glyph:X8}");
		}
	}

	[Fact]
	public void AllTwelvePalettesAreDistinctAndReceiveEqualSelectionWeight()
	{
		Assert.Equal(new[]
			{
				"Naval", "Butcher", "Reactor", "Salvage", "Occult", "Ash",
				"Hazard", "Frost", "Corrosion", "Ivory", "Cobalt", "Ember",
			},
			RobotPortraitGenerator.Palettes.Select(p => p.Name));
		Assert.Equal(12, RobotPortraitGenerator.Palettes.Select(p => p.Colors).Distinct().Count());
		var avatars = Enumerable.Range(0, 12000).Select(RobotPortraitGenerator.Generate).ToArray();
		foreach (var palette in RobotPortraitGenerator.Palettes)
		{
			var matches = avatars.Where(a => a.Colors == palette.Colors).ToArray();
			Assert.InRange(matches.Length, 850, 1150);
			Assert.True(matches.Select(a => a.Parts).Distinct().Count() > 800);
		}
	}

	private static double[] Channels(uint color) =>
		[((color >> 24) & 255) / 255d, ((color >> 16) & 255) / 255d, ((color >> 8) & 255) / 255d];

	private static double Luminance(uint color)
	{
		var channels = Channels(color).Select(c => c <= .04045 ? c / 12.92 : System.Math.Pow((c + .055) / 1.055, 2.4)).ToArray();
		return .2126 * channels[0] + .7152 * channels[1] + .0722 * channels[2];
	}
}
