using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string ActorId,
	EHeadingTurn? Heading = null,
	ERollDirection? Roll = null) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MoveDef.Instance;
}

public readonly record struct MoveTransition(
	GridBasis HeadingBasis,
	Coord Destination,
	GridBasis ArrivalBasis);

public sealed class MoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public const int ApCost = 1;

	public static MoveDef Instance { get; } = new();

	private static readonly EHeadingTurn?[] HeadingChoices =
	[
		null,
		EHeadingTurn.YawLeft,
		EHeadingTurn.YawRight,
		EHeadingTurn.PitchUp,
		EHeadingTurn.PitchDown,
	];

	private static readonly ERollDirection?[] RollChoices =
	[
		null,
		ERollDirection.Clockwise,
		ERollDirection.CounterClockwise,
	];

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var heading in HeadingChoices)
		{
			foreach (var roll in RollChoices)
			{
				var action = Bind(actorId, heading, roll);
				if (IsPossible(action, world, runtime))
					yield return action;
			}
		}
	}

	public MoveStepAction Bind(
		string actorId,
		EHeadingTurn? heading = null,
		ERollDirection? roll = null) =>
		new(actorId, heading, roll);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world);

	public bool IsPossible(MoveStepAction action, BattleWorld world)
	{
		if (!TryCalculate(action, world, out var transition))
			return false;

		var actor = world.StateOf(action.ActorId);
		if (actor.Type == EType.Torpedo)
			return false;

		var blocked = world.BlockedFor(action.ActorId);
		return world.Grid.IsInBounds(transition.Destination)
			&& !blocked.Contains(transition.Destination);
	}

	public bool IsLegal(MoveStepAction action, BattleWorld world) =>
		IsPossible(action, world)
		&& world.StateOf(action.ActorId).ActionPoints >= ApCost;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		MoveStepAction action,
		BattleWorld world)
	{
		if (!TryCalculate(action, world, out var transition))
			throw new InvalidOperationException("Cannot resolve an unsupported maneuver.");

		var effects = new List<IEffect<BattleWorld, ActorRuntime>>(5);
		if (action.Heading is { } heading)
			effects.Add(new HeadingTurnEffect(heading));
		effects.Add(new MoveEffect(transition.Destination));
		if (action.Roll is { } roll)
			effects.Add(new RollEffect(roll));
		effects.Add(new ApChangeEffect(-ApCost));
		effects.Add(new HazardCellEntryEffect(transition.Destination));
		return effects;
	}

	public bool TryCalculate(
		MoveStepAction action,
		BattleWorld world,
		out MoveTransition transition)
	{
		if (!IsSupportedHeading(action.Heading) || !IsSupportedRoll(action.Roll))
		{
			transition = default;
			return false;
		}

		var actor = world.StateOf(action.ActorId);
		var basis = GridBasis.From(actor.Fore, actor.Dorsal, actor.Starboard);
		var headingBasis = action.Heading is { } heading
			? Orientation.HeadingTurn(basis, heading)
			: basis;
		var destination = actor.Position + headingBasis.Forward;
		var arrivalBasis = action.Roll is { } roll
			? Orientation.Roll(headingBasis, roll)
			: headingBasis;
		transition = new MoveTransition(headingBasis, destination, arrivalBasis);
		return true;
	}

	private static bool IsSupportedHeading(EHeadingTurn? heading) =>
		heading is null
			or EHeadingTurn.YawLeft
			or EHeadingTurn.YawRight
			or EHeadingTurn.PitchUp
			or EHeadingTurn.PitchDown;

	private static bool IsSupportedRoll(ERollDirection? roll) =>
		roll is null or ERollDirection.Clockwise or ERollDirection.CounterClockwise;

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
