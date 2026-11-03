using System.Collections.Frozen;
using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Maneuvering;

public sealed class ManeuverabilitySpec
{
	private readonly FrozenDictionary<ESpatialOrientation, int> _translationApCosts;
	private readonly FrozenDictionary<EHeadingTurn, int> _headingMpCosts;
	private readonly FrozenDictionary<ERollDirection, int> _rollMpCosts;

	public int BaseActionPoints { get; }
	public int BaseManeuverPoints { get; }
	public IReadOnlyCollection<ESpatialOrientation> SupportedTranslations => _translationApCosts.Keys;
	public IReadOnlyCollection<EHeadingTurn> SupportedHeadingTurns => _headingMpCosts.Keys;
	public IReadOnlyCollection<ERollDirection> SupportedRolls => _rollMpCosts.Keys;

	public ManeuverabilitySpec(
		int baseActionPoints,
		int baseManeuverPoints,
		IReadOnlyDictionary<ESpatialOrientation, int> translationApCosts,
		IReadOnlyDictionary<EHeadingTurn, int> headingMpCosts,
		IReadOnlyDictionary<ERollDirection, int> rollMpCosts)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(baseActionPoints);
		ArgumentOutOfRangeException.ThrowIfNegative(baseManeuverPoints);

		BaseActionPoints = baseActionPoints;
		BaseManeuverPoints = baseManeuverPoints;
		_translationApCosts = ValidateAndFreeze(translationApCosts);
		_headingMpCosts = ValidateAndFreeze(headingMpCosts);
		_rollMpCosts = ValidateAndFreeze(rollMpCosts);
	}

	public bool TryGetTranslationApCost(ESpatialOrientation direction, out int cost) =>
		_translationApCosts.TryGetValue(direction, out cost);

	public bool TryGetHeadingMpCost(EHeadingTurn turn, out int cost) =>
		_headingMpCosts.TryGetValue(turn, out cost);

	public bool TryGetRollMpCost(ERollDirection direction, out int cost) =>
		_rollMpCosts.TryGetValue(direction, out cost);

	public ManeuverabilitySpec WithBudgets(int baseActionPoints, int baseManeuverPoints) =>
		new(
			baseActionPoints,
			baseManeuverPoints,
			_translationApCosts,
			_headingMpCosts,
			_rollMpCosts);

	public static ManeuverabilitySpec StandardShip(
		int baseActionPoints,
		int baseManeuverPoints,
		int reverseApCost = 2) =>
		new(
			baseActionPoints,
			baseManeuverPoints,
			new Dictionary<ESpatialOrientation, int>
			{
				[ESpatialOrientation.Forward] = 1,
				[ESpatialOrientation.Retro] = reverseApCost,
				[ESpatialOrientation.Port] = 1,
				[ESpatialOrientation.Starboard] = 1,
				[ESpatialOrientation.Dorsal] = 1,
				[ESpatialOrientation.Ventral] = 1,
			},
			new Dictionary<EHeadingTurn, int>
			{
				[EHeadingTurn.YawLeft] = 1,
				[EHeadingTurn.YawRight] = 1,
				[EHeadingTurn.Yaw180] = 2,
				[EHeadingTurn.PitchUp] = 1,
				[EHeadingTurn.PitchDown] = 1,
			},
			new Dictionary<ERollDirection, int>
			{
				[ERollDirection.Clockwise] = 1,
				[ERollDirection.CounterClockwise] = 1,
			});

	public static ManeuverabilitySpec VoidBomb(
		int baseActionPoints,
		int forwardApCost,
		int lateralApCost) =>
		new(
			baseActionPoints,
			0,
			new Dictionary<ESpatialOrientation, int>
			{
				[ESpatialOrientation.Forward] = forwardApCost,
				[ESpatialOrientation.Port] = lateralApCost,
				[ESpatialOrientation.Starboard] = lateralApCost,
				[ESpatialOrientation.Dorsal] = lateralApCost,
				[ESpatialOrientation.Ventral] = lateralApCost,
			},
			new Dictionary<EHeadingTurn, int>(),
			new Dictionary<ERollDirection, int>());

	private static FrozenDictionary<TAction, int> ValidateAndFreeze<TAction>(
		IReadOnlyDictionary<TAction, int> costs)
		where TAction : struct, Enum
	{
		ArgumentNullException.ThrowIfNull(costs);

		foreach (var (action, cost) in costs)
		{
			if (!Enum.IsDefined(action))
				throw new ArgumentOutOfRangeException(nameof(costs), action, "Action must be a defined enum value.");
			if (cost <= 0)
				throw new ArgumentOutOfRangeException(nameof(costs), cost, "Action costs must be positive.");
		}

		return costs.ToFrozenDictionary();
	}
}
