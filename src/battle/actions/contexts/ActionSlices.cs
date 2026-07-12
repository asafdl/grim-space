namespace GrimSpace.Battle.Actions.Contexts;

public readonly struct ActionSlices(ActionBoard board)
{
	public static ActionSlices From(ActionBoard board) => new(board);

	public ApContext Ap => new(board.Player);

	public MoveContext Move => new(board.Player, board.PlayerUnit);

	public OrientationContext Orientation => new(board.Player);

	public HazardContext Hazards => new(board.Hazards, board.Grid);

	public DamageContext Damage => new(board.Enemy);
}
