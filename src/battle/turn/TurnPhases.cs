namespace GrimSpace.Battle.Turn;

public static class TurnPhases
{
	public const int Player = 0;
	public const int Enemy = 1;

	/// <summary>Clock advances here after a turn's orchestration completes.</summary>
	public const int End = 2;
}
