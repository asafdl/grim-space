using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
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
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Tests.StatusModel;

[BattleTestSuite]
public sealed class BattleStatusPhase1Tests
{
	private const string PlayerId = "player";

	[Fact]
	public void EffectiveArea_AgreesAcrossTargetingPreviewAiAndResolution()
	{
		var origin = new Coord(5, 5, 5);
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[PlayerId] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.HelpABotterOut]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.ScrapPlayer(origin),
			[BattleTestFixture.Enemy(origin + Coord.Forward * 8)],
			pilotsByActorId: pilots);
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		var world = battle.PlayerAgent.Sim.World;
		var state = world.StateOf(PlayerId);
		var installed = state.FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn);
		Assert.NotNull(installed);
		var baseArea = Assert.IsAssignableFrom<IAreaDamage>(installed.Spec);

		var viaQuery = BattleStatuses.RewriteArea(baseArea, state.Statuses, action.Tags);
		var frame = BodyFrame.From(state);
		var expected = AbilityArea.ApplyBlocking(
			state.Position,
			AbilityArea.CellsInBounds(viaQuery, frame, action.MountedOn, world.Grid),
			ScrapDroneSwarmDef.Instance.IsBlockable,
			world.AbilityBlockingCells);

		var targeting = ScrapDroneSwarmDef.Instance.AffectedCells(action, world);
		var preview = ((IAreaActionDef)ScrapDroneSwarmDef.Instance).AffectedCells(action, world);
		var aiProbe = ScrapDroneSwarmDef.Instance.AffectedCells(
			new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard),
			world);
		var resolved = ScrapDroneSwarmDef.Instance.Resolve(
			action,
			world,
			battle.Engine.ActorRuntimes.For(PlayerId));
		var damage = Assert.IsType<ApplyAreaDamageEffect>(
			resolved.Single(effect => effect is ApplyAreaDamageEffect));

		Assert.Equal(expected, targeting);
		Assert.Equal(expected, preview);
		Assert.Equal(expected, aiProbe);
		Assert.Equal(expected, damage.Cells);
	}

	[Fact]
	public void SubjectProtection_DoesNotChangeOtherSubjectDamage()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var protectedEnemy = CreateEnemy("protected", origin + Coord.Forward);
		protectedEnemy.State.Statuses.Add(IncomingFlatReduction("protected", 100));
		var plainEnemy = CreateEnemy("plain", origin + Coord.Forward * 2);
		EnsureHitFaceShields(protectedEnemy.State, player.State.Position, 300);
		EnsureHitFaceShields(plainEnemy.State, player.State.Position, 300);
		var battle = BattleTestFixture.BeginSimulation(player, [protectedEnemy, plainEnemy]);
		var world = battle.PlayerAgent.Sim.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var protectedHullBefore = world.StateOf("protected").CurrentHullPoints;
		var plainHullBefore = world.StateOf("plain").CurrentHullPoints;
		var protectedShieldsBefore = FaceTotal(world.StateOf("protected").CurrentShieldPoints);
		var plainShieldsBefore = FaceTotal(world.StateOf("plain").CurrentShieldPoints);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord>
			{
				world.StateOf("protected").Position,
				world.StateOf("plain").Position,
			},
			200,
			origin,
			alliesTakeDamage: false);

		_ = effect.Apply(world, runtime, PlayerId);

		Assert.Equal(
			100,
			CombatLost(
				protectedHullBefore,
				protectedShieldsBefore,
				world.StateOf("protected")));
		Assert.Equal(
			200,
			CombatLost(
				plainHullBefore,
				plainShieldsBefore,
				world.StateOf("plain")));
	}

	[Fact]
	public void MixedAttackHealAction_StatusBonusDoesNotAffectHeal()
	{
		var origin = new Coord(5, 5, 5);
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[PlayerId] = new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.ScrapDrone,
					BattlePilotCapabilityPipelineFactory.Supports)),
		};
		var player = BattleTestFixture.ScrapPlayer(origin);
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			origin + Coord.Forward * 4,
			new UserExecutionAgent());
		var distantEnemy = BattleTestFixture.Enemy(origin + Coord.Forward * 8);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[ally, distantEnemy],
			pilotsByActorId: pilots);
		var world = battle.PlayerAgent.Sim.World;
		Assert.Contains(
			world.StateOf(PlayerId).Statuses,
			status => status.InstanceId
				== BattleStatuses.PassiveInstanceId(
					nameof(EPilotCapabilityId.ScrapDroneSwarmDamageBonus),
					PlayerId));

		var allyUnit = UnitRegistry.For(world).All.First(unit => unit.State.Id == "ally");
		var burstCell = BodyFrame.From(world.StateOf(PlayerId)).ToWorld(0, -1, 0);
		allyUnit.State.CurrentHullPoints = 50;
		allyUnit.State.Position = burstCell;
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		Assert.Contains(burstCell, ScrapDroneSwarmDef.Instance.AffectedCells(action, world));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(50 + CatalogExpectations.ScrapDroneSwarmHeal(), allyUnit.State.CurrentHullPoints);
	}

	[Fact]
	public void ApplyAndUndo_RoundTripsIncomingStatusReduction()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		enemy.State.Statuses.Add(IncomingFlatReduction(enemy.State.Id, 50));
		EnsureHitFaceShields(enemy.State, player.State.Position, 300);
		var battle = BattleTestFixture.BeginSimulation(player, [enemy]);
		var world = battle.PlayerAgent.Sim.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var before = world.StateOf(enemy.State.Id).Clone();
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { enemy.State.Position },
			150,
			origin,
			alliesTakeDamage: false);

		_ = effect.Apply(world, runtime, PlayerId);
		Assert.Equal(
			100,
			CombatLost(before.CurrentHullPoints, FaceTotal(before.CurrentShieldPoints), world.StateOf(enemy.State.Id)));

		effect.Undo(world, runtime, PlayerId);

		Assert.Equal(before.CurrentHullPoints, world.StateOf(enemy.State.Id).CurrentHullPoints);
		Assert.Equal(FaceTotal(before.CurrentShieldPoints), FaceTotal(world.StateOf(enemy.State.Id).CurrentShieldPoints));
	}

	[Fact]
	public void StateClone_CopiesStatuses()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		player.State.Statuses.Add(IncomingFlatReduction(player.State.Id, 25));
		var clone = player.State.Clone();

		Assert.Single(clone.Statuses);
		Assert.NotSame(player.State.Statuses[0], clone.Statuses[0]);
		Assert.Equal(75, clone.Statuses[0].RewriteIncomingDamage!(100));
	}

	[Fact]
	public void ExecutionHelper_RunsStatusPassBeforeApply()
	{
		var origin = new Coord(5, 5, 5);
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[PlayerId] = new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.ScrapDrone,
					BattlePilotCapabilityPipelineFactory.Supports)),
		};
		var player = BattleTestFixture.ScrapPlayer(origin);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		var battle = BattleTestFixture.BeginSimulation(player, [enemy], pilotsByActorId: pilots);
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		var world = battle.Engine.World;
		var enemyState = world.StateOf(enemy.State.Id);
		enemyState.Position = ScrapDroneSwarmDef.Instance.AffectedCells(action, world).First();
		EnsureHitFaceShields(enemyState, world.StateOf(PlayerId).Position, 500);
		var hullBefore = enemyState.CurrentHullPoints;
		var shieldsBefore = FaceTotal(enemyState.CurrentShieldPoints);

		var records = ExecutionHelper.Apply(
			action,
			world,
			battle.Engine.ActorRuntimes.For(PlayerId));

		Assert.NotEmpty(records);
		Assert.Equal(
			CatalogExpectations.ScrapDroneSwarmDamage()
				+ PilotSkillPercents.ScrapDrone.SwarmAdditionalDamage,
			CombatLost(hullBefore, shieldsBefore, enemyState));
	}

	private static BattleStatus IncomingFlatReduction(string actorId, int reduction) =>
		new()
		{
			InstanceId = BattleStatuses.PassiveInstanceId("test-flat-incoming", actorId),
			Priority = 100,
			ActionTags = [],
			Polarity = EStatusPolarity.Buff,
			RewriteIncomingDamage = damage => System.Math.Max(0, damage - reduction),
		};

	private static Unit CreateEnemy(string id, Coord position) =>
		Factory.Create(
			ShipInstance.FromSpec(id, GrimSpace.Units.Specs.FighterSpec.Instance, ShipCatalog.FullFighterLoadout()),
			ETeam.Enemy,
			position,
			new AiController());

	private static void EnsureHitFaceShields(State unit, Coord origin, int amount)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		unit.CurrentShieldPoints[face] = amount;
	}

	private static int CombatLost(int hullBefore, int shieldsBefore, State after) =>
		(hullBefore - after.CurrentHullPoints) + (shieldsBefore - FaceTotal(after.CurrentShieldPoints));

	private static int FaceTotal(FaceShieldPoints shields)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += shields[face];
		return total;
	}
}
