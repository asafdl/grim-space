namespace GrimSpace.Battle.Movement;

public sealed class MomentumConfig
{
	public const int MaxLevel = 3;

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
}
