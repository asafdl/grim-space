using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class HydraulicAbuserTests
{
	[Fact]
	public void StatusPass_ReducesIncomingHitDamageMultiplicatively()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		InstallHydraulicAbuser(world, player);

		var hit = BattleStatuses.RewriteIncomingHit(
			player.State.Statuses,
			world,
			player,
			Coord.Zero,
			EImpactCause.LightningCannonBurst,
			ESpatialOrientation.Forward,
			new IncomingHit(100, 0));

		Assert.Equal(95, hit.Damage);
		Assert.Equal(0, hit.ShieldDamageBonus);
	}

	[Fact]
	public void StatusPass_StacksDamageResistanceMultiplicatively()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		player.State.Statuses.Add(
			BattlePassiveStatusInstaller.Create(EPilotCapabilityId.DamageResistance, player.State.Id));
		var second = BattlePassiveStatusInstaller.Create(EPilotCapabilityId.DamageResistance, "stack-2");
		player.State.Statuses.Add(second);

		var hit = BattleStatuses.RewriteIncomingHit(
			player.State.Statuses,
			world,
			player,
			Coord.Zero,
			EImpactCause.LightningCannonBurst,
			ESpatialOrientation.Forward,
			new IncomingHit(100, 0));

		Assert.Equal(90, hit.Damage);
	}

	private static void InstallHydraulicAbuser(BattleWorld world, Unit player) =>
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			player.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.HydraulicAbuser,
					BattlePilotCapabilityPipelineFactory.Supports))));

	private static BattleWorld BattleWorldFor(Unit player) =>
		BattleWorld.FromLive(
			[player],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"hydraulic-abuser-test",
			EObjective.EliminateOpponents,
			new HashSet<string> { player.State.Id },
			new Timeline());
}
