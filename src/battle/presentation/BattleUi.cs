using System.Threading.Tasks;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public sealed class BattleUi
{
	public BattleUi(BattleOrchestrator battle) => Battle = battle;

	public BattleOrchestrator Battle { get; }

	public InteractionState State { get; } = new();

	private MoveUi? _moveUi;

	public MoveUi MoveUi => _moveUi ??= MoveUi.Build(Battle.Sim, Battle.PlayerId);

	public void ResetMoveUi() => _moveUi = null;

	public TurnReplay? CommitAndResolve()
	{
		if (!TurnUi.TryCommit(Battle, out var playerActions))
			return null;

		var replay = Battle.ResolveTurn(playerActions);
		State.ResetAfterTurn();
		return replay;
	}

	public async Task<TurnReplay?> CommitAndResolveAsync()
	{
		if (!TurnUi.TryCommit(Battle, out var playerActions))
			return null;

		var replay = await Battle.ResolveTurnAsync(playerActions);
		State.ResetAfterTurn();
		return replay;
	}

	public bool Undo() => TurnUi.TryUndo(Battle, State);

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Movement.Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
			return false;

		var actor = Battle.GetActiveActor();
		if (actor is null || !Battle.CanAct(actor))
			return false;

		var option = options[optionIndex];
		var actorState = Battle.Sim.StateOf<ActorState>(Battle.PlayerId);
		var steps = MoveUi.ToMoveActions(Battle.PlayerId, actorState, option);
		if (steps is null || !Battle.Sim.TryEnqueue(actions: [..steps]))
			return false;

		State.CommittedMovePath = option.Path;
		State.ClearHovers();
		return true;
	}

	public PresentationFrame BuildFrame()
	{
		var state = State;
		var activeUnit = Battle.GetActiveActor();
		var moveOptions = activeUnit is not null && Battle.CanAct(activeUnit)
			? MoveUi.GetMoveOptions(Battle.Sim.Actions)
			: [];
		state.ClampMoveHover(moveOptions.Count);

		var previewWorld = TurnUi.GetPreviewWorld(Battle);
		var hazardCells = TurnUi.GetPreviewHazardCells(Battle);
		var validFlakPortCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Port)
			: [];
		var validFlakStarboardCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Starboard)
			: [];
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetPreviewCells(Battle, state)
			: [];
		var railgunCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetBurstCells(Battle)
			: [];
		var railgunPreviewCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetPreviewCells(Battle, state)
			: [];
		var (path, target) = MoveUi.GetPathHighlights(
			moveOptions,
			state.MoveHoveredIndex,
			state.CommittedMovePath);

		var actorId = Battle.PlayerId;
		var actorState = previewWorld.StateOf(actorId);

		return new PresentationFrame
		{
			Mode = state.Mode,
			ActiveUnit = activeUnit,
			MoveOptions = moveOptions,
			PreviewWorld = previewWorld,
			ActorState = actorState,
			PreviewHazardCells = hazardCells,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunCells = railgunCells,
			RailgunPreviewCells = railgunPreviewCells,
			MovePath = path,
			MoveTarget = target,
			HintText = TurnUi.BuildHint(
				Battle,
				state,
				activeUnit,
				actorState,
				Battle.Sim.Actions.Count),
			CanAct = !Battle.IsBattleOver && activeUnit is not null && !Battle.IsResolving,
			FlakAvailable = Capabilities.For(actorState.Type)
				.OfType<FlakDef>()
				.Any(def => Battle.Sim.Peek(new FlakAction(Battle.PlayerId, def.Mount)) is not null),
			RailgunAvailable = Battle.Sim.Peek(new RailgunAction(Battle.PlayerId)) is not null,
			ShowOutcomeOverlay = Battle.IsBattleOver,
			PlayerWon = Battle.IsBattleOver && Battle.WinnerId == Battle.PlayerId,
		};
	}
}
