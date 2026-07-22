using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Slices;

public readonly struct BattleSlices
{
	private readonly BattleBoard _board;
	private readonly string _actorId;
	private readonly TurnPhaseContext _phaseContext;

	public BattleSlices(BattleBoard board, string actorId, TurnPhaseContext phaseContext)
	{
		_board = board;
		_actorId = actorId;
		_phaseContext = phaseContext;
	}

	public static BattleSlices For(
		BattleBoard board,
		string actorId,
		TurnPhaseContext phaseContext) =>
		new(board, actorId, phaseContext);

	public static BattleSlices ForSystem(BattleBoard board) =>
		new(board, actorId: string.Empty, phaseContext: new TurnPhaseContext());

	public BattleBoard Board => _board;

	public TurnPhaseContext PhaseContext => _phaseContext;

	public Timeline Timeline => _board.Timeline;

	public ApContext Ap => new(_board.StateOf(_actorId));

	public OrientationContext Orientation => new(_board.StateOf(_actorId));

	public HazardContext Hazards => new(_board, _actorId);

	public DamageContext Damage => new(_board);

	public MissileContext Missiles => new(_board.StateOf(_actorId));
}
