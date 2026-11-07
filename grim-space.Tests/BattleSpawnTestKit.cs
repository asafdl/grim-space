using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests;

internal static class BattleSpawnTestKit
{
	public static BattleSpawn Create(
		string id,
		EType chassis,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		Create(
			chassis == EType.Fighter
				? ShipInstance.FromSpec(id, FighterSpec.Instance, ShipCatalog.FullFighterLoadout())
				: ShipInstance.FromCatalog(id, chassis),
			team,
			position,
			executionAgent);

	public static BattleSpawn Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		new()
		{
			Ship = ship,
			Pilot = null,
			Team = team,
			Faction = GrimSpace.Battle.Units.Factory.DefaultFactionFor(team),
			Position = position,
			ExecutionAgent = executionAgent,
		};

	public static ShipInstance FighterWithInstalledAbilities(
		string id,
		IReadOnlyList<InstalledAbility> installed) =>
		ShipInstance.FromSpec(
			id,
			FighterSpec.Instance,
			ShipLoadout.Create(
				FighterSpec.Instance,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).BaseHullPoints,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).BaseShieldPoints,
				installed));

	public static ShipInstance FighterWithGoopGun(
		string id,
		ESpatialOrientation mountedOn = ESpatialOrientation.Retro,
		GoopGunSpec? spec = null)
	{
		spec ??= new GoopGunSpec();
		var gooper = IndustrialGooperSpec.Instance;
		if (!gooper.Supports(new AbilityMount(EAbilityKind.GoopGun, mountedOn)))
			throw new ArgumentException($"Goop gun mount '{mountedOn}' is not on the industrial gooper.", nameof(mountedOn));

		var baseline = gooper.NewDefaultLoadout();
		var installed = baseline.InstalledAbilities
			.Select(ability => ability.Kind == EAbilityKind.GoopGun
				? new InstalledAbility(spec, ability.MountedOn)
				: ability)
			.ToArray();
		return ShipInstance.FromSpec(
			id,
			gooper,
			ShipLoadout.Create(
				gooper,
				baseline.BaseHullPoints,
				baseline.BaseShieldPoints,
				installed));
	}
}
