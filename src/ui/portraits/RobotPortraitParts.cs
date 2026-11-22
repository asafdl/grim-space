namespace GrimSpace.Ui.Portraits;

public enum RobotEyes { Tired, Friendly, Suspicious, Angry, Clinical, Patchwork, Smug, Hateful, Strange }
public enum RobotMouth { Tired, Friendly, Suspicious, Angry, Clinical, Patchwork, Smug, Hateful, Strange }
public enum RobotScreen { Chamfered, Round, Triangle, Coffin, Egg, Skewed }
public enum RobotEars { Radiators, Speakers, Fins, HearingCups, Salvaged }
public enum RobotAntenna { None, Beacon, Whip, Forks }
public enum RobotShoulders { Workhorse, Rounded, Slim, Armored, Salvaged }
public enum RobotCracks { None, Hairline, Branching }
public enum RobotTape { None, Crown, Shoulder }
public enum RobotRust { None, Seams, Drips }
public enum RobotWearLocation { Screen, Casing, Chest, LeftShoulder, RightShoulder }

public readonly record struct RobotWearPlacement(
	RobotWearLocation Location, float X, float Y, float RotationDegrees, float Scale);

public readonly record struct RobotPortraitBody(float Width, float Height)
{
	public const float ThinWidth = .7f;
	public const float WideWidth = 1.3f;
	public const float ShortHeight = .7f;
	public const float TallHeight = 1.3f;

	public float HeadOffset => (1f - Height) * 89f;

	public RobotWearPlacement TransformWear(RobotWearPlacement wear) => wear.Location switch
	{
		RobotWearLocation.Screen or RobotWearLocation.Casing => wear with { Y = wear.Y + HeadOffset },
		RobotWearLocation.Chest or RobotWearLocation.LeftShoulder or RobotWearLocation.RightShoulder =>
			wear with { X = 160f + (wear.X - 160f) * Width, Y = 310f + (wear.Y - 310f) * Height },
		_ => throw new ArgumentOutOfRangeException(nameof(wear)),
	};
}

public sealed record RobotPortraitParts(
	RobotEyes Eyes = RobotEyes.Tired,
	RobotMouth Mouth = RobotMouth.Tired,
	RobotScreen Screen = RobotScreen.Chamfered,
	RobotEars Ears = RobotEars.Radiators,
	RobotAntenna Antenna = RobotAntenna.Beacon,
	RobotShoulders Shoulders = RobotShoulders.Workhorse,
	RobotCracks Cracks = RobotCracks.None,
	RobotTape Tape = RobotTape.None,
	RobotRust Rust = RobotRust.None,
	RobotWearPlacement? CrackPlacement = null,
	RobotWearPlacement? TapePlacement = null);
