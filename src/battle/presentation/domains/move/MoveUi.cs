using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

/// <summary>
/// Turn-scoped movement preview: prefix-keyed option index for fast lookup as the player queues actions.
/// </summary>
public sealed class MoveUi
{
	private readonly MoveOptionIndex _index;

	private MoveUi(MoveOptionIndex index) => _index = index;

	public static MoveUi Build(BattleOrchestrator battle)
	{
		var actorId = battle.PlayerId;
		var searchTimer = Stopwatch.StartNew();
		var index = MoveOptionIndex.FromSimulation(battle.Sim, actorId);
		searchTimer.Stop();

		GameLog.Log(
			$"MoveUi.Build ({actorId}): prefixes={index.PrefixCount} "
			+ $"search={searchTimer.Elapsed.TotalMilliseconds:F1}ms");

		return new MoveUi(index);
	}

	public IReadOnlyList<Option> GetMoveOptions(IReadOnlyList<IAction> committed) =>
		_index.GetOptions(committed);

	public bool TryLocate(IReadOnlyList<IAction> committed) =>
		_index.ContainsPrefix(committed);

	public static IReadOnlyList<Option> GetMoveOptions(BattleOrchestrator battle, Unit? actor)
	{
		if (actor is null || !battle.CanAct(actor))
			return [];

		return battle.MoveUi.GetMoveOptions(battle.Sim.Actions);
	}

	public static bool TryApply(BattleOrchestrator battle, Interaction.InteractionState state, Option option)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var actorState = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		IReadOnlyList<MoveStepAction> steps;
		try
		{
			steps = MoveDef.StepsFromPath(
				battle.PlayerId,
				BodyFrame.From(actorState),
				actorState.Position,
				option.Path);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		if (!battle.Sim.TryEnqueue(actions: [..steps]))
			return false;

		state.CommittedMovePath = option.Path;
		state.ClearHovers();
		return true;
	}

	public static (IReadOnlyList<Coord> Path, Coord? Target) GetPathHighlights(
		IReadOnlyList<Option> options,
		int? hoveredIndex,
		IReadOnlyList<Coord> committedPath)
	{
		if (hoveredIndex is int i)
			return (options[i].Path, options[i].EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1]);

		return ([], null);
	}
}
