using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Movement;

public static class MoveDirectionRules
{
	public static int DirectionBit(EStepDirection direction) => 1 << (int)direction;

	public static bool UsesOpposite(int usedMask, EStepDirection direction) =>
		(usedMask & DirectionBit(Opposite(direction))) != 0;

	public static EStepDirection Opposite(EStepDirection direction) =>
		direction switch
		{
			EStepDirection.Forward => EStepDirection.Retro,
			EStepDirection.Retro => EStepDirection.Forward,
			EStepDirection.Dorsal => EStepDirection.Ventral,
			EStepDirection.Ventral => EStepDirection.Dorsal,
			EStepDirection.Port => EStepDirection.Starboard,
			EStepDirection.Starboard => EStepDirection.Port,
			_ => direction,
		};
}
