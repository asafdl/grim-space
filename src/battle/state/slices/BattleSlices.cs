using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Slices;

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

	public static BattleSlices For(
		BattleBoard board,
		string actorId,
		TurnState turnState) =>
		new(board, actorId, turnState);

	public static BattleSlices ForSystem(BattleBoard board) =>
		new(board, actorId: string.Empty, turnState: new TurnState());

	public BattleBoard Board => _board;

	public TurnState TurnState => _turnState;

	public Timeline Timeline => _board.Timeline;

	public ApContext Ap => new(_board.StateOf(_actorId));

	public OrientationContext Orientation => new(_board.StateOf(_actorId));

	public HazardContext Hazards => new(_board, _actorId);

	public DamageContext Damage => new(_board);

	public MissileContext Missiles => new(_board.StateOf(_actorId));
}
