using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Tests.Units;

public sealed class ManeuverabilitySpecTests
{
	[Fact]
	public void ExposesConfiguredBudgetsActionsAndCosts()
	{
		var spec = CreateSpec();

		Assert.Equal(4, spec.MaxActionPoints);
		Assert.Equal(3, spec.MaxManeuverPoints);
		Assert.Contains(ESpatialOrientation.Forward, spec.SupportedTranslations);
		Assert.Contains(EHeadingTurn.YawRight, spec.SupportedHeadingTurns);
		Assert.Contains(ERollDirection.Clockwise, spec.SupportedRolls);
		Assert.True(spec.TryGetTranslationApCost(ESpatialOrientation.Forward, out var translationCost));
		Assert.True(spec.TryGetHeadingMpCost(EHeadingTurn.YawRight, out var headingCost));
		Assert.True(spec.TryGetRollMpCost(ERollDirection.Clockwise, out var rollCost));
		Assert.Equal(1, translationCost);
		Assert.Equal(1, headingCost);
		Assert.Equal(1, rollCost);
	}

	[Fact]
	public void MissingActionsAreUnsupported()
	{
		var spec = CreateSpec();

		Assert.False(spec.TryGetTranslationApCost(ESpatialOrientation.Retro, out _));
		Assert.False(spec.TryGetHeadingMpCost(EHeadingTurn.PitchDown, out _));
		Assert.False(spec.TryGetRollMpCost(ERollDirection.CounterClockwise, out _));
	}

	[Fact]
	public void DefensivelyCopiesCostMaps()
	{
		var translations = new Dictionary<ESpatialOrientation, int>
		{
			[ESpatialOrientation.Forward] = 1,
		};
		var headings = new Dictionary<EHeadingTurn, int>
		{
			[EHeadingTurn.YawRight] = 1,
		};
		var rolls = new Dictionary<ERollDirection, int>
		{
			[ERollDirection.Clockwise] = 1,
		};
		var spec = new ManeuverabilitySpec(4, 3, translations, headings, rolls);

		translations[ESpatialOrientation.Forward] = 9;
		headings.Clear();
		rolls.Clear();

		Assert.True(spec.TryGetTranslationApCost(ESpatialOrientation.Forward, out var translationCost));
		Assert.True(spec.TryGetHeadingMpCost(EHeadingTurn.YawRight, out var headingCost));
		Assert.True(spec.TryGetRollMpCost(ERollDirection.Clockwise, out var rollCost));
		Assert.Equal(1, translationCost);
		Assert.Equal(1, headingCost);
		Assert.Equal(1, rollCost);
	}

	[Fact]
	public void RejectsNegativeBudgets()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSpec(maxActionPoints: -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSpec(maxManeuverPoints: -1));
	}

	[Fact]
	public void RejectsNonPositiveCosts()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSpec(translationCost: 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSpec(headingCost: -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSpec(rollCost: 0));
	}

	[Fact]
	public void RejectsUndefinedActions()
	{
		var headings = new Dictionary<EHeadingTurn, int>
		{
			[(EHeadingTurn)999] = 1,
		};

		Assert.Throws<ArgumentOutOfRangeException>(() => new ManeuverabilitySpec(
			4,
			3,
			new Dictionary<ESpatialOrientation, int>(),
			headings,
			new Dictionary<ERollDirection, int>()));
	}

	[Fact]
	public void WithBudgetsPreservesActionCosts()
	{
		var adjusted = CreateSpec().WithBudgets(6, 2);

		Assert.Equal(6, adjusted.MaxActionPoints);
		Assert.Equal(2, adjusted.MaxManeuverPoints);
		Assert.True(adjusted.TryGetTranslationApCost(ESpatialOrientation.Forward, out var translationCost));
		Assert.True(adjusted.TryGetHeadingMpCost(EHeadingTurn.YawRight, out var headingCost));
		Assert.True(adjusted.TryGetRollMpCost(ERollDirection.Clockwise, out var rollCost));
		Assert.Equal(1, translationCost);
		Assert.Equal(1, headingCost);
		Assert.Equal(1, rollCost);
	}

	[Theory]
	[InlineData(EType.Fighter, 4, 3)]
	[InlineData(EType.Carrier, 3, 1)]
	[InlineData(EType.RepurposedMiner, 4, 2)]
	[InlineData(EType.VoidBomb, 4, 0)]
	public void ChassisOwnsExpectedResourceBudgets(
		EType chassis,
		int expectedActionPoints,
		int expectedManeuverPoints)
	{
		var maneuverability = ShipCatalog.SpecFor(chassis).Maneuverability;

		Assert.Equal(expectedActionPoints, maneuverability.MaxActionPoints);
		Assert.Equal(expectedManeuverPoints, maneuverability.MaxManeuverPoints);
		Assert.Same(maneuverability, ShipCatalog.SpecFor(chassis).Maneuverability);
	}

	[Theory]
	[InlineData(EType.Fighter)]
	[InlineData(EType.Carrier)]
	[InlineData(EType.RepurposedMiner)]
	public void RegularChassisUseInitialStandardCosts(EType chassis)
	{
		var maneuverability = ShipCatalog.SpecFor(chassis).Maneuverability;

		foreach (var direction in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.True(maneuverability.TryGetTranslationApCost(direction, out var cost));
			Assert.Equal(direction == ESpatialOrientation.Retro ? 2 : 1, cost);
		}

		Assert.True(maneuverability.TryGetHeadingMpCost(EHeadingTurn.Yaw180, out var yaw180Cost));
		Assert.Equal(2, yaw180Cost);
		foreach (var turn in Enum.GetValues<EHeadingTurn>().Where(turn => turn != EHeadingTurn.Yaw180))
		{
			Assert.True(maneuverability.TryGetHeadingMpCost(turn, out var cost));
			Assert.Equal(1, cost);
		}

		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			Assert.True(maneuverability.TryGetRollMpCost(direction, out var cost));
			Assert.Equal(1, cost);
		}
	}

	[Fact]
	public void VoidBombSupportsOnlyLauncherTranslations()
	{
		var maneuverability = ShipCatalog.SpecFor(EType.VoidBomb).Maneuverability;

		Assert.True(maneuverability.TryGetTranslationApCost(ESpatialOrientation.Forward, out var forwardCost));
		Assert.Equal(1, forwardCost);
		foreach (var direction in new[]
			{
				ESpatialOrientation.Port,
				ESpatialOrientation.Starboard,
				ESpatialOrientation.Dorsal,
				ESpatialOrientation.Ventral,
			})
		{
			Assert.True(maneuverability.TryGetTranslationApCost(direction, out var cost));
			Assert.Equal(2, cost);
		}

		Assert.False(maneuverability.TryGetTranslationApCost(ESpatialOrientation.Retro, out _));
		Assert.Empty(maneuverability.SupportedHeadingTurns);
		Assert.Empty(maneuverability.SupportedRolls);
	}

	private static ManeuverabilitySpec CreateSpec(
		int maxActionPoints = 4,
		int maxManeuverPoints = 3,
		int translationCost = 1,
		int headingCost = 1,
		int rollCost = 1) =>
		new(
			maxActionPoints,
			maxManeuverPoints,
			new Dictionary<ESpatialOrientation, int>
			{
				[ESpatialOrientation.Forward] = translationCost,
			},
			new Dictionary<EHeadingTurn, int>
			{
				[EHeadingTurn.YawRight] = headingCost,
			},
			new Dictionary<ERollDirection, int>
			{
				[ERollDirection.Clockwise] = rollCost,
			});
}
