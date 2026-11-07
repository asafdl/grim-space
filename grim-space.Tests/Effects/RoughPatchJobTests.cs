using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
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
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class RoughPatchJobTests
{
	[Fact]
	public void StatusPass_MultipliesHealRetainPercent()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(player);
		InstallRoughPatchJob(world, player);
		IAction action = new ScrapDroneSwarmAction(player.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { Coord.Zero },
			ScrapDroneSwarmSpec.Baseline.Heal);

		var modified = Assert.IsType<ApplyAreaAllyHealEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.RoughPatchJob.HealTakenPercent,
			modified.HealRetainPercent);
	}

	[Fact]
	public void Apply_ReducesHealAmountMultiplicatively()
	{
		var healer = BattleTestFixture.Player(Coord.Zero);
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			new Coord(1, 0, 0),
			EBattleAgentKind.Player);
		var emptyShields = ally.State.Loadout.BaseShieldPoints.Clone();
		emptyShields.Fill(0);
		ally.State.Loadout = ally.State.Loadout with { BaseShieldPoints = emptyShields };
		ally.State.CurrentShieldPoints.Fill(0);
		ally.State.CurrentHullPoints = ally.State.Loadout.BaseHullPoints - 150;
		var world = BattleWorldFor(healer, ally);

		var baseHeal = 100;
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { ally.State.Position },
			baseHeal).WithMultipliedHealRetainPercent(
				PilotSkillPercents.RoughPatchJob.HealTakenPercent);

		effect.Apply(world, new ActorRuntime(), healer.State.Id);

		var expected = (int)System.Math.Round(
			baseHeal * PilotSkillPercents.RoughPatchJob.HealTakenPercent / 100m,
			MidpointRounding.AwayFromZero);
		Assert.Equal(
			ally.State.Loadout.BaseHullPoints - 150 + expected,
			ally.State.CurrentHullPoints);
	}

	[Fact]
	public void Apply_StacksRetainMultiplicatively()
	{
		var effect = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord>(),
			100)
			.WithMultipliedHealRetainPercent(PilotSkillPercents.RoughPatchJob.HealTakenPercent)
			.WithMultipliedHealRetainPercent(PilotSkillPercents.RoughPatchJob.HealTakenPercent);

		Assert.Equal(64, effect.HealRetainPercent);
	}

	private static void InstallRoughPatchJob(BattleWorld world, Unit player) =>
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			player.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromQuirks(
					"pilot",
					[EPilotQuirkId.RoughPatchJob],
					BattlePilotCapabilityPipelineFactory.Supports))));

	private static BattleWorld BattleWorldFor(params Unit[] units) =>
		BattleWorld.FromLive(
			units,
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"rough-patch-job-test",
			EObjective.EliminateOpponents,
			units.Select(unit => unit.State.Id).ToHashSet(StringComparer.Ordinal),
			new Timeline());
}
