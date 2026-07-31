using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

/// <summary>
/// Independent search + extraction mirroring <see cref="GrimSpace.Battle.Presentation.Domains.Move.MoveUi"/>.
/// Used to verify the turn-scoped cache returns the same options as a fresh search.
/// </summary>
internal static class MoveUiExpectations
{
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	public static IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> CaptureTurnStartFrames(
		BattleOrchestrator battle) =>
		battle.Sim
			.Search(battle.PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities)
			.ToList();

	public static IReadOnlyList<Option> FromFrames(
		string actorId,
		IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> frames,
		IReadOnlyList<IAction> committed)
	{
		if (committed.Any(action => action is FlakAction or RailgunAction))
			return [];

		if (!TryFindNode(frames, PrefixKey(committed), out var originNode))
			return [];

		return ExtractMoveOptions(actorId, frames, committed.Count, originNode);
	}

	public static IReadOnlyList<Option> FromFreshSearch(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId,
		IReadOnlyList<IAction> committed)
	{
		var frames = sim
			.Search(actorId, MovementActionDefs, BattleSearchVisit.ForCapabilities)
			.ToList();

		return FromFrames(actorId, frames, committed);
	}

	public static void AssertEquivalent(IReadOnlyList<Option> expected, IReadOnlyList<Option> actual)
	{
		var expectedByEnd = expected.ToDictionary(option => option.EndPosition);
		var actualByEnd = actual.ToDictionary(option => option.EndPosition);

		Assert.Equal(
			expectedByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z),
			actualByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z));

		foreach (var end in expectedByEnd.Keys)
		{
			Assert.Equal(expectedByEnd[end].ApCost, actualByEnd[end].ApCost);
			Assert.Equal(expectedByEnd[end].Path, actualByEnd[end].Path);
		}
	}

	private static bool TryFindNode(
		IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> frames,
		string prefixKey,
		out SearchFrame<BattleWorld, ActorRuntime> node)
	{
		foreach (var frame in frames)
		{
			if (PrefixKey(frame.Actions) == prefixKey)
			{
				node = frame;
				return true;
			}
		}

		node = default!;
		return false;
	}

	private static IReadOnlyList<Option> ExtractMoveOptions(
		string actorId,
		IReadOnlyList<SearchFrame<BattleWorld, ActorRuntime>> frames,
		int startCount,
		SearchFrame<BattleWorld, ActorRuntime> originNode)
	{
		var actor = originNode.World.StateOf(actorId);
		var origin = actor.Position;
		var frame = BodyFrame.From(actor);
		var committed = originNode.Actions;
		var baselinePathApSpent = originNode.Runtimes.For(actorId).PathApSpent;
		var results = new Dictionary<Coord, Option>();

		foreach (var searchFrame in frames)
		{
			if (searchFrame.Actions.Count <= startCount)
				continue;

			if (!PrefixStartsWith(searchFrame.Actions, committed))
				continue;

			var steps = searchFrame.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.Where(step => step.ActorId == actorId)
				.ToList();
			if (steps.Count == 0)
				continue;

			var runtime = searchFrame.Runtimes.For(actorId);
			var option = MovePathRules.ToEndpointOption(
				origin,
				frame,
				steps,
				runtime,
				baselinePathApSpent);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing)
				|| MovePathRules.PreferEndpointOption(option, existing))
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}

	private static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	private static bool PrefixStartsWith(IReadOnlyList<IAction> actions, IReadOnlyList<IAction> prefix)
	{
		if (actions.Count < prefix.Count)
			return false;

		for (var i = 0; i < prefix.Count; i++)
		{
			if (ActionKey(actions[i]) != ActionKey(prefix[i]))
				return false;
		}

		return true;
	}

	private static string ActionKey(IAction action) =>
		action switch
		{
			MoveStepAction move => $"move:{move.ActorId}:{move.Direction}",
			HeadingTurnAction heading => $"heading:{heading.ActorId}:{heading.Turn}",
			RollAction roll => $"roll:{roll.ActorId}:{roll.Direction}",
			FlakAction flak => $"flak:{flak.ActorId}:{flak.Mount}",
			RailgunAction railgun => $"railgun:{railgun.ActorId}",
			_ => action.GetType().FullName ?? "action",
		};
}
