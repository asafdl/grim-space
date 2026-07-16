namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct BattleSlices(BattleBoard board)
{
	public static BattleSlices From(BattleBoard board) => new(board);

	public ApContext Ap => new(board.Player);

	public MoveContext Move => new(board.Player, board.PlayerUnit, board.CommitMomentum);

	public OrientationContext Orientation => new(board.Player);

	public HazardContext Hazards => new(board.Hazards, board.Grid);

	public DamageContext Damage => new(board.Enemy);
}
