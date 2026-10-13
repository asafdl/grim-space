using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class CapabilitiesAbilityDefsTests
{
	[Fact]
	public void AbilityDefsFor_AlignsHudAndLoadoutDiscovery()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Coord.Zero);

		var runtime = ActorRuntime.From(null);
		var abilityDefs = Capabilities.AbilityDefsFor(state, runtime);
		var hudDefs = AbilityHudCatalog.ForActor(state, runtime).Select(spec => spec.Def).ToArray();

		Assert.Equal(abilityDefs, hudDefs);
	}

	[Fact]
	public void For_IncludesMovementAndAbilityDefsFor()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Coord.Zero);

		var all = Capabilities.For(state, ActorRuntime.From(null));
		var abilities = Capabilities.AbilityDefsFor(state, ActorRuntime.From(null));

		Assert.Equal(Capabilities.Movement, all.Take(Capabilities.Movement.Count).ToArray());
		Assert.Equal(abilities, all.Skip(Capabilities.Movement.Count).ToArray());
	}

	[Fact]
	public void AbilityDefsFor_IncludesEquippedPilotSkillDefsInHudOrder()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Coord.Zero,
			Coord.Forward,
			Coord.Up);
		var runtime = ActorRuntime.From(new PilotBattleSnapshot("pilot", [EPilotSkillId.ShieldsUp]));
		var abilityDefs = Capabilities.AbilityDefsFor(state, runtime);
		var hudDefs = AbilityHudCatalog.ForActor(state, runtime).Select(spec => spec.Def).ToArray();

		Assert.Contains(ShieldsUpDef.Instance, abilityDefs);
		Assert.Equal(abilityDefs, hudDefs);
	}
}
