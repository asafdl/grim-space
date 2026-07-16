namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct BattleSlices
{
	private readonly BattleBoard _board;
	private readonly string _actorId;

	public BattleSlices(BattleBoard board, string actorId)
	{
		_board = board;
		_actorId = actorId;
	}

	public static BattleSlices For(BattleBoard board, string actorId) => new(board, actorId);

	public ApContext Ap => new(_board.StateOf(_actorId));

	public MoveContext Move => new(_board.StateOf(_actorId), _board.UnitOf(_actorId));

	public OrientationContext Orientation => new(_board.StateOf(_actorId));

	public HazardContext Hazards => new(_board, _actorId);

	public DamageContext Damage => new(_board);

	public MissileContext Missiles => new(_board.StateOf(_actorId));
}
