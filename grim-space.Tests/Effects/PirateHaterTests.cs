using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class PirateHaterTests
{
	[Fact]
	public void AppliesTenPercentOnlyAgainstPirateFactionTargets()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var pirate = Factory.Create(
			ShipInstance.FromCatalog("pirate", EType.Fighter),
			ETeam.Enemy,
			EFaction.Pirates,
			new Coord(5, 7, 5),
			new AiController(),
			Coord.Forward,
			Coord.Up);
		var other = BattleTestFixture.Enemy(new Coord(5, 8, 5));
		using var battle = BattleTestFixture.BeginSimulation(
			player,
			[pirate, other],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>
			{
				[player.State.Id] = new PilotBattleSnapshot(
					"pilot",
					PilotCapabilityGrant.FromQuirks(
						"pilot",
						[EPilotQuirkId.PirateHater],
						BattlePilotCapabilityPipelineFactory.Supports)),
			});
		var world = battle.Engine.World;
		foreach (var target in new[] { pirate.State, other.State })
		{
			target.CurrentHullPoints = 1000;
			target.CurrentShieldPoints.Fill(0);
		}

		var area = Assert.IsType<ApplyAreaDamageEffect>(Assert.Single(BattleStatuses.RewriteOutgoing(
			new LightningCannonAction(player.State.Id),
			world,
			[
				new ApplyAreaDamageEffect(
					EImpactCause.LightningCannonBurst,
					new HashSet<Coord> { pirate.State.Position, other.State.Position },
					100,
					player.State.Position),
			])));
		Assert.Equal(PilotSkillPercents.PirateHater.DamageBonusPercent, area.PirateDamageBonusPercent);
		area.Apply(world, battle.Engine.ActorRuntimes.For(player.State.Id), player.State.Id);

		Assert.Equal(890, pirate.State.CurrentHullPoints);
		Assert.Equal(900, other.State.CurrentHullPoints);
	}
}
