namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct BattleSlices
{
	private readonly BattleBoard _board;
	private readonly string _actorId;
	private readonly TurnState _turnState;

	public BattleSlices(BattleBoard board, string actorId, TurnState turnState)
	{
		_board = board;
		_actorId = actorId;
		_turnState = turnState;
	}

	public static BattleSlices For(BattleBoard board, string actorId, TurnState turnState) =>
		new(board, actorId, turnState);

	public TurnState TurnState => _turnState;

	public ApContext Ap => new(_board.StateOf(_actorId));

	public MoveContext Move => new(_board.StateOf(_actorId), _board.UnitOf(_actorId));

	public OrientationContext Orientation => new(_board.StateOf(_actorId));

	public HazardContext Hazards => new(_board, _actorId);

	public DamageContext Damage => new(_board);

	public MissileContext Missiles => new(_board.StateOf(_actorId));
}
