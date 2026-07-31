using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Orientation;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public sealed class BattleUi
{
	public BattleUi(BattleOrchestrator battle) => Battle = battle;

	public BattleOrchestrator Battle { get; }

	public InteractionState State { get; } = new();

	public TurnReplay? CommitAndResolve()
	{
		if (!TurnUi.TryCommit(Battle, out var playerActions))
			return null;

		var replay = Battle.ResolveTurn(playerActions);
		State.ResetAfterTurn();
		return replay;
	}

	public bool Undo() => TurnUi.TryUndo(Battle, State);

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Movement.Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
			return false;

		return MoveUi.TryApply(Battle, State, options[optionIndex]);
	}

	public bool TryQueueFlak(Coord cell) => FlakUi.TryApply(Battle, State, cell);

	public bool TryQueueRailgun(Coord cell) => RailgunUi.TryApply(Battle, State, cell);

	public bool TryQueueRoll(ERollDirection direction) => OrientationUi.TryApplyRoll(Battle, direction);

	public bool TryQueueHeadingTurn(EHeadingTurn turn) => OrientationUi.TryApplyHeadingTurn(Battle, turn);

	public PresentationFrame BuildFrame() => BattleFrameBuilder.Build(Battle, State);
}
