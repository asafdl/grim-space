using GrimSpace.Battle.Abilities;

namespace GrimSpace.Units.Loadouts.Abilities;

public sealed record AbilityDefinition(
	EAbilityKind Id,
	int Damage,
	int UsesPerTurn,
	int Range)
{
	public static AbilityDefinition For(EAbilityKind id) =>
		id switch
		{
			EAbilityKind.Flak => new AbilityDefinition(
				EAbilityKind.Flak,
				CombatConfig.FlakDamage,
				CombatConfig.FlaksPerTurn,
				CombatConfig.FlakRange),
			EAbilityKind.Railgun => new AbilityDefinition(
				EAbilityKind.Railgun,
				CombatConfig.RailgunDamage,
				CombatConfig.RailgunsPerTurn,
				CombatConfig.RailgunLineLength),
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown ability id."),
		};
}
