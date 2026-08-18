using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public static class AbilityHudCatalog
{
	public sealed record Spec(
		EPlayerMode Mode,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Def,
		string? IconPath,
		string Tooltip,
		Func<UnitDisplayState, AbilityLegality, string> Charges,
		Func<AbilityLegality, bool> IsLegal);

	public static IReadOnlyList<Spec> ForUnit(EType type) =>
		Capabilities.AbilitiesFor(type)
			.Select(Resolve)
			.ToList();

	public static AbilityBarSlotState BuildState(Spec spec, UnitDisplayState unit, AbilityLegality legality) =>
		new(spec.Tooltip, spec.Charges(unit, legality), spec.IsLegal(legality));

	private static Spec Resolve(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		def switch
		{
			FlakDef => new(
				EPlayerMode.Flak,
				def,
				"res://assets/ui/abilities/flak.svg",
				BattleHudCopy.FlakTooltip,
				(unit, _) => BattleHudCopy.Charges(unit.FlakRemaining, unit.FlaksPerTurn),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Flak)),
			RailgunDef => new(
				EPlayerMode.Railgun,
				def,
				"res://assets/ui/abilities/railgun.svg",
				BattleHudCopy.RailgunTooltip,
				(unit, _) => BattleHudCopy.Charges(unit.RailgunRemaining, unit.RailgunsPerTurn),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Railgun)),
			TorpedoDef => new(
				EPlayerMode.Torpedo,
				def,
				"res://assets/ui/abilities/torpedo.svg",
				BattleHudCopy.TorpedoTooltip,
				(unit, legality) => BattleHudCopy.Charges(
					CooldownUses(unit.TorpedoCooldownRemaining, legality.Weapons.IsKindLegal(EWeaponKind.Torpedo)),
					1),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Torpedo)),
			DetonateDef => new(
				EPlayerMode.Detonate,
				def,
				"res://assets/ui/abilities/detonate.svg",
				BattleHudCopy.DetonateTooltip,
				(unit, _) => BattleHudCopy.Charges(unit.FuelRemaining, TorpedoConfig.Fuel),
				legality => legality.Detonate),
			SpawnPatrolDef => new(
				EPlayerMode.SpawnPatrol,
				def,
				"res://assets/ui/abilities/patrol.svg",
				BattleHudCopy.SpawnPatrolTooltip,
				(unit, legality) => BattleHudCopy.Charges(
					CooldownUses(unit.PatrolSpawnCooldownRemaining, legality.SpawnPatrol),
					1),
				legality => legality.SpawnPatrol),
			_ => new(
				EPlayerMode.Move,
				def,
				null,
				$"{def.GetType().Name}\n(No HUD metadata yet.)",
				(_, _) => "—",
				_ => false),
		};

	private static int CooldownUses(int cooldownRemaining, bool legal) =>
		legal && cooldownRemaining == 0 ? 1 : 0;
}
