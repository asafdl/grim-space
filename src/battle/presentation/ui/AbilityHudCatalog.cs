using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
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
			FlakDef => Flak,
			RailgunDef => Railgun,
			TorpedoDef => Torpedo,
			SpawnPatrolDef => SpawnPatrol,
			DetonateDef => Detonate,
			_ => Unknown(def),
		};

	private static readonly Spec Flak = new(
		EPlayerMode.Flak,
		"res://assets/ui/abilities/flak.svg",
		BattleHudCopy.FlakTooltip,
		(unit, _) => BattleHudCopy.Charges(unit.FlakRemaining, unit.FlaksPerTurn),
		legality => legality.Weapons.IsKindLegal(EWeaponKind.Flak));

	private static readonly Spec Railgun = new(
		EPlayerMode.Railgun,
		"res://assets/ui/abilities/railgun.svg",
		BattleHudCopy.RailgunTooltip,
		(unit, _) => BattleHudCopy.Charges(unit.RailgunRemaining, unit.RailgunsPerTurn),
		legality => legality.Weapons.IsKindLegal(EWeaponKind.Railgun));

	private static readonly Spec Torpedo = new(
		EPlayerMode.Torpedo,
		"res://assets/ui/abilities/torpedo.svg",
		BattleHudCopy.TorpedoTooltip,
		(unit, legality) => BattleHudCopy.Charges(
			CooldownUses(unit.TorpedoCooldownRemaining, legality.Weapons.IsKindLegal(EWeaponKind.Torpedo)),
			1),
		legality => legality.Weapons.IsKindLegal(EWeaponKind.Torpedo));

	private static readonly Spec Detonate = new(
		EPlayerMode.Detonate,
		"res://assets/ui/abilities/detonate.svg",
		BattleHudCopy.DetonateTooltip,
		(unit, _) => BattleHudCopy.Charges(unit.FuelRemaining, TorpedoConfig.Fuel),
		legality => legality.Detonate);

	private static readonly Spec SpawnPatrol = new(
		EPlayerMode.SpawnPatrol,
		"res://assets/ui/abilities/patrol.svg",
		BattleHudCopy.SpawnPatrolTooltip,
		(unit, legality) => BattleHudCopy.Charges(
			CooldownUses(unit.PatrolSpawnCooldownRemaining, legality.SpawnPatrol),
			1),
		legality => legality.SpawnPatrol);

	private static Spec Unknown(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		new(
			EPlayerMode.Move,
			null,
			$"{def.GetType().Name}\n(No HUD metadata yet.)",
			(_, _) => "—",
			_ => false);

	private static int CooldownUses(int cooldownRemaining, bool legal) =>
		legal && cooldownRemaining == 0 ? 1 : 0;
}
