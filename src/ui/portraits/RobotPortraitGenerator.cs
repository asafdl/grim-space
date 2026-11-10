namespace GrimSpace.Ui.Portraits;

public sealed record RobotPortraitColors(
	uint Casing, uint Screen, uint Glyph, uint Ears, uint Antenna, uint Shoulders, uint Chest);

public sealed record GeneratedRobotPortrait(RobotPortraitParts Parts, RobotPortraitColors Colors);

public static class RobotPortraitGenerator
{
	private static readonly (string Name, RobotPortraitColors Colors)[] ColorPalettes =
	[
		("Naval", new(0x426b9cff, 0x142738ff, 0xffc247ff,
			0x748a9aff, 0xb09a68ff, 0x748a9aff, 0x426b9cff)),
		("Butcher", new(0x8d4148ff, 0x301b24ff, 0x55deffff,
			0x65565dff, 0xa58c83ff, 0x65565dff, 0x8d4148ff)),
		("Reactor", new(0x42784aff, 0x14291cff, 0xa4ff38ff,
			0x7d8960ff, 0xb1a35aff, 0x7d8960ff, 0x42784aff)),
		("Salvage", new(0x997340ff, 0x142e2cff, 0x31ffc9ff,
			0x596f68ff, 0xb09a72ff, 0x596f68ff, 0x997340ff)),
		("Occult", new(0x785881ff, 0x291a35ff, 0xff65c3ff,
			0x555469ff, 0xa18b9fff, 0x555469ff, 0x785881ff)),
		("Ash", new(0x525963ff, 0x171b22ff, 0xff4444ff,
			0x878b8fff, 0x9b766aff, 0x878b8fff, 0x525963ff)),
		("Hazard", new(0x56574fff, 0x201e14ff, 0xffc247ff,
			0xa18b36ff, 0xb5a16bff, 0xa18b36ff, 0x56574fff)),
		("Frost", new(0xbac4ccff, 0x101d35ff, 0x638fffff,
			0x475976ff, 0x829eaeff, 0x475976ff, 0xbac4ccff)),
		("Corrosion", new(0x468780ff, 0x241b15ff, 0xff7838ff,
			0x805b49ff, 0xad8861ff, 0x805b49ff, 0x468780ff)),
		("Ivory", new(0xc2b99aff, 0x15251bff, 0x39ff77ff,
			0x6a594aff, 0x92976cff, 0x6a594aff, 0xc2b99aff)),
		("Cobalt", new(0x405e9aff, 0x171326ff, 0xbd65ffff,
			0x525360ff, 0x8c7c9fff, 0x525360ff, 0x405e9aff)),
		("Ember", new(0xa3673fff, 0x291b16ff, 0xf1f5dfff,
			0x55514eff, 0xb29b76ff, 0x55514eff, 0xa3673fff)),
	];

	public static IReadOnlyList<(string Name, RobotPortraitColors Colors)> Palettes { get; } =
		Array.AsReadOnly(ColorPalettes);

	public static GeneratedRobotPortrait Generate(int seed)
	{
		var random = new Random(seed);
		var parts = new RobotPortraitParts(
			Pick(random, Enum.GetValues<RobotEyes>()),
			Pick(random, Enum.GetValues<RobotMouth>()),
			Pick(random, Enum.GetValues<RobotScreen>()),
			Pick(random, Enum.GetValues<RobotEars>()),
			Pick(random, Enum.GetValues<RobotAntenna>()),
			Pick(random, Enum.GetValues<RobotShoulders>()));
		var colors = Pick(random, ColorPalettes).Colors;
		parts = parts with
		{
			Cracks = random.Next(2) == 0 ? RobotCracks.None : Pick(random, new[] { RobotCracks.Hairline, RobotCracks.Branching }),
			Tape = random.Next(2) == 0 ? RobotTape.None : Pick(random, new[] { RobotTape.Crown, RobotTape.Shoulder }),
			Rust = random.Next(2) == 0 ? RobotRust.None : Pick(random, new[] { RobotRust.Seams, RobotRust.Drips }),
		};
		parts = parts with
		{
			CrackPlacement = parts.Cracks == RobotCracks.None ? null : PlaceWear(random, parts, false),
			TapePlacement = parts.Tape == RobotTape.None ? null : PlaceWear(random, parts, true),
		};
		return new(parts, colors);
	}

	private static RobotWearPlacement PlaceWear(Random random, RobotPortraitParts parts, bool tape)
	{
		var locations = tape
			? new[] { RobotWearLocation.Casing, RobotWearLocation.LeftShoulder, RobotWearLocation.RightShoulder }
			: Enum.GetValues<RobotWearLocation>();
		var location = Pick(random, locations);
		var (x, y, scale) = location switch
		{
			RobotWearLocation.Screen => (160f, 98f, .55f),
			RobotWearLocation.Casing => (160f, parts.Screen == RobotScreen.Skewed ? 60f : 51f, .45f),
			RobotWearLocation.Chest => (160f, 289f, .6f),
			RobotWearLocation.LeftShoulder => (parts.Shoulders == RobotShoulders.Slim ? 111f : 96f, 287f, .65f),
			RobotWearLocation.RightShoulder => (parts.Shoulders == RobotShoulders.Slim ? 209f :
				parts.Shoulders == RobotShoulders.Salvaged ? 238f : 224f, 288f, .55f),
			_ => throw new ArgumentOutOfRangeException(nameof(location)),
		};
		if (!tape)
		{
			scale *= 1.5f;
			if (location is RobotWearLocation.Chest or RobotWearLocation.LeftShoulder or RobotWearLocation.RightShoulder)
				y = 279f;
		}
		return new(location, x + random.Next(-2, 3), y + random.Next(-2, 3),
			random.Next(360), scale);
	}

	private static T Pick<T>(Random random, T[] pool) => pool[random.Next(pool.Length)];
}
