using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.World;

public enum EUnitRelation
{
	Self,
	Ally,
	Opponent,
}

public readonly record struct UnitInArea(Unit Unit, EUnitRelation Relation);
