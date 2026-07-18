namespace GrimSpace.Battle.Movement;

public sealed class MomentumConfig
{
	public const int MaxLevel = 3;
	public const int ForwardStepsPerMomentumGain = 2;
	public const int MaxGainFromMovementPerTurn = 1;

	public required int Level { get; init; }
	public required float Evasion { get; init; }
	public required int FreeForwardSteps { get; init; }
	public required int ForwardStepCost { get; init; }
	public required int LateralCost { get; init; }
	public required int BrakeCost { get; init; }

	private static readonly MomentumConfig[] Levels =
	[
		new()
		{
			Level = 0,
			Evasion = 0f,
			FreeForwardSteps = 0,
			ForwardStepCost = 1,
			LateralCost = 1,
			BrakeCost = 1,
		},
		new()
		{
			Level = 1,
			Evasion = 0.30f,
			FreeForwardSteps = 1,
			ForwardStepCost = 1,
			LateralCost = 2,
			BrakeCost = 1,
		},
		new()
		{
			Level = 2,
			Evasion = 0.70f,
			FreeForwardSteps = 2,
			ForwardStepCost = 1,
			LateralCost = 3,
			BrakeCost = 2,
		},
		new()
		{
			Level = 3,
			Evasion = 0.90f,
			FreeForwardSteps = 3,
			ForwardStepCost = 1,
			LateralCost = 4,
			BrakeCost = 2,
		},
	];

	public static MomentumConfig ForLevel(int level) =>
		Levels[System.Math.Clamp(level, 0, MaxLevel)];

	public readonly record struct Buildup(int Level, int ForwardStepsTowardGain);

	public static Buildup ApplyStep(Buildup state, Enums.EStepDirection direction)
	{
		if (direction == Enums.EStepDirection.Forward)
		{
			var toward = state.ForwardStepsTowardGain + 1;
			if (toward < ForwardStepsPerMomentumGain)
				return state with { ForwardStepsTowardGain = toward };

			return new Buildup(System.Math.Min(state.Level + 1, MaxLevel), 0);
		}

		if (direction == Enums.EStepDirection.Retro)
			return new Buildup(System.Math.Max(state.Level - 1, 0), 0);

		return state;
	}

	public static Buildup CapMovementGain(
		Buildup state,
		int moveStartLevel,
		int momentumGainedFromMovementThisTurn)
	{
		var maxLevel = System.Math.Min(
			moveStartLevel + MaxGainFromMovementPerTurn - momentumGainedFromMovementThisTurn,
			MaxLevel);
		if (state.Level <= maxLevel)
			return state;

		return new Buildup(maxLevel, 0);
	}

	public static Buildup ApplyMovementStep(
		Buildup state,
		Enums.EStepDirection direction,
		int moveStartLevel,
		int momentumGainedFromMovementThisTurn) =>
		CapMovementGain(ApplyStep(state, direction), moveStartLevel, momentumGainedFromMovementThisTurn);

	public static int MomentumAfterPureForwardPath(int startMomentum, int stepCount)
	{
		var buildup = new Buildup(startMomentum, 0);
		for (var step = 0; step < stepCount; step++)
			buildup = ApplyMovementStep(buildup, Enums.EStepDirection.Forward, startMomentum, 0);

		return buildup.Level;
	}
}
