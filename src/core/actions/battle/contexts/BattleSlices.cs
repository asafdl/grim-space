using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct BattleSlices
{
	private readonly BattleBoard _board;
	private readonly string _actorId;
	private readonly TurnState _turnState;
	private readonly Timeline _timeline;

	public BattleSlices(BattleBoard board, string actorId, TurnState turnState, Timeline timeline)
	{
		_board = board;
		_actorId = actorId;
		_turnState = turnState;
		_timeline = timeline;
	}

	public static BattleSlices For(
		BattleBoard board,
		string actorId,
		TurnState turnState,
		Timeline timeline) =>
		new(board, actorId, turnState, timeline);

	public BattleBoard Board => _board;

	public TurnState TurnState => _turnState;

	public Timeline Timeline => _timeline;

	public ApContext Ap => new(_board.StateOf(_actorId));

	public MoveContext Move => new(_board.StateOf(_actorId), _board.UnitOf(_actorId));

	public OrientationContext Orientation => new(_board.StateOf(_actorId));

	public HazardContext Hazards => new(_board, _actorId);

	public DamageContext Damage => new(_board);

	public MissileContext Missiles => new(_board.StateOf(_actorId));
}
