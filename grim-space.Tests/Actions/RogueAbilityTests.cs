using System.Text.Json;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class RogueAbilityTests
{
	private static readonly Coord Origin = new(5, 5, 5);
	private static readonly AbilityMount JumpMount =
		new(EAbilityKind.RoadsWeDontNeedRoads, ESpatialOrientation.Forward);

	[Fact]
	public void RodTooltipShowsDamageAndCritMultiplierWithUntintedIcon()
	{
		var battle = CreateBattle(Origin + Coord.Up * 2);
		var unit = UnitDisplayState.Capture(
			battle.PlayerAgent.Sim.World.StateOf("player"),
			GrimSpace.Battle.Runtime.ActorRuntime.From(null));

		var tooltip = BattleHudCopy.HighSpeedRodTooltipFor(unit);

		Assert.Equal("[icon: Damage, text: 70] [icon: Crit, text: x3.5]\n[icon: Heat, text: +20]", tooltip.Details);
		Assert.Contains("Crit when hitting hull", tooltip.Body);
		var icon = HudTooltipIconCatalog.Resolve(nameof(Icon.Crit));
		Assert.Equal("res://assets/ui/battle/crit.png", icon.Path);
		Assert.False(icon.TintIcon);
	}

	[Theory]
	[InlineData(0, 245)]
	[InlineData(20, 225)]
	[InlineData(69, 176)]
	[InlineData(70, 0)]
	public void RodUsesDirectionalShieldThresholdAndIndependentCharges(int shields, int hullDamage)
	{
		var battle = CreateBattle(Origin + Coord.Up * 2, enemyHull: 1000, shields: shields);
		var sim = battle.PlayerAgent.Sim;
		Assert.True(sim.TryEnqueue(true, new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));

		Assert.Equal(1000 - hullDamage, sim.World.StateOf("enemy").CurrentHullPoints);
		Assert.Equal(0, sim.World.StateOf("player").UsesRemaining(
			new AbilityMount(EAbilityKind.HighSpeedRod, ESpatialOrientation.Dorsal)));
		Assert.Equal(1, sim.World.StateOf("player").UsesRemaining(
			new AbilityMount(EAbilityKind.HighSpeedRod, ESpatialOrientation.Ventral)));
		Assert.Equal(4, sim.World.StateOf("player").CurrentActionPoints);
		Assert.Equal(4, sim.World.StateOf("player").CurrentManeuverPoints);
		Assert.Equal(20, sim.World.StateOf("player").Heat);
		Assert.False(sim.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));
		Assert.Equal(1000, battle.Engine.World.StateOf("enemy").CurrentHullPoints);
		Assert.True(sim.TryUndoLast());
		Assert.Equal(1000, sim.World.StateOf("enemy").CurrentHullPoints);
		Assert.Equal(2, sim.World.StateOf("player").UsesRemaining(EAbilityKind.HighSpeedRod));
		Assert.Equal(0, sim.World.StateOf("player").Heat);
	}

	[Fact]
	public void WeaponGearBonusUsesExistingPipelineBeforeHullBreachCheck()
	{
		var battle = CreateBattle(Origin + Coord.Up * 2, enemyHull: 1000, shields: 75,
			buffs: [EChassisBuffOffer.WeaponDamage]);
		var sim = battle.PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));

		// 70 * 1.2 = 84 penetrates 75 shields; 84 * 3.5 - 75 = 219 hull damage.
		Assert.Equal(781, sim.World.StateOf("enemy").CurrentHullPoints);
	}

	[Fact]
	public void OverheatBlocksBothAbilities()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var sim = battle.PlayerAgent.Sim;
		var actor = sim.World.StateOf("player");
		actor.Overheat = true;
		BattleStatuses.SyncOverheat(actor);

		Assert.False(sim.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));
		Assert.False(sim.TryEnqueue(new RoadsWeDontNeedRoadsAction("player", Origin + Coord.Up * 4)));
	}

	[Fact]
	public void RodCountsTemporaryShieldsAndStopsAtFirstShip()
	{
		var battle = CreateBattle(Origin + Coord.Up * 2, shields: 50, temporaryShields: 20);
		var sim = battle.PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));
		Assert.Equal(200, sim.World.StateOf("enemy").CurrentHullPoints);
		Assert.Equal(0, Defense.EffectiveShield(sim.World.StateOf("enemy"),
			BodyFrame.From(sim.World.StateOf("enemy")).HitFaceFrom(Origin)));
		var cells = HighSpeedRodDef.Instance.AffectedCells(
			new HighSpeedRodAction("player", ESpatialOrientation.Dorsal), sim.World);
		Assert.Equal(2, cells.Count);
		Assert.DoesNotContain(Origin + Coord.Up * 3, cells);
	}

	[Fact]
	public void RodStopsAtObstacleAndRangeFour()
	{
		var blockedBattle = CreateBattle(Origin + Coord.Up * 3,
			blocked: new HashSet<Coord> { Origin + Coord.Up });
		var sim = blockedBattle.PlayerAgent.Sim;
		Assert.True(sim.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));
		Assert.Equal(200, sim.World.StateOf("enemy").CurrentHullPoints);
		Assert.Empty(HighSpeedRodDef.Instance.AffectedCells(
			new HighSpeedRodAction("player", ESpatialOrientation.Dorsal), sim.World));

		var distant = CreateBattle(Origin + Coord.Up * 5).PlayerAgent.Sim;
		Assert.True(distant.TryEnqueue(new HighSpeedRodAction("player", ESpatialOrientation.Dorsal)));
		Assert.Equal(200, distant.World.StateOf("enemy").CurrentHullPoints);
	}

	[Fact]
	public void JumpBypassesObstaclesPreservesOrientationAndUndoes()
	{
		var destination = Origin + Coord.Forward * 4;
		var battle = CreateBattle(new Coord(1, 1, 1),
			blocked: new HashSet<Coord> { Origin + Coord.Forward * 2 });
		var sim = battle.PlayerAgent.Sim;
		var before = sim.World.StateOf("player").Clone();

		Assert.True(sim.TryEnqueue(true, new RoadsWeDontNeedRoadsAction("player", destination)));
		var after = sim.World.StateOf("player");
		Assert.Equal(destination, after.Position);
		Assert.Equal(before.Fore, after.Fore);
		Assert.Equal(before.Dorsal, after.Dorsal);
		Assert.Equal(before.Starboard, after.Starboard);
		Assert.Equal(4, after.CurrentActionPoints);
		Assert.Equal(4, after.CurrentManeuverPoints);
		Assert.Equal(20, after.Heat);
		Assert.Equal(3, after.CooldownRemaining(JumpMount));
		Assert.True(sim.TryUndoLast());
		Assert.Equal(Origin, sim.World.StateOf("player").Position);
		Assert.Equal(0, sim.World.StateOf("player").CooldownRemaining(JumpMount));
		Assert.Equal(0, sim.World.StateOf("player").Heat);
	}

	[Theory]
	[InlineData(5, 5, 5)]
	[InlineData(6, 6, 5)]
	[InlineData(10, 5, 5)]
	[InlineData(-1, 5, 5)]
	[InlineData(5, 7, 5)]
	[InlineData(5, 5, 7)]
	[InlineData(6, 5, 5)]
	[InlineData(7, 5, 5)]
	[InlineData(8, 5, 5)]
	[InlineData(5, 9, 5)]
	[InlineData(5, 5, 9)]
	public void JumpRejectsInvalidOrOccupiedLanding(int x, int y, int z)
	{
		var battle = CreateBattle(new Coord(5, 9, 5),
			blocked: new HashSet<Coord> { new(5, 5, 9) });
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new RoadsWeDontNeedRoadsAction("player", new Coord(x, y, z))));
	}

	[Fact]
	public void JumpDiscoversAllSixDirectionsAndCooldownExpiresAfterThreeUpkeeps()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For("player");
		Assert.Equal(6, RoadsWeDontNeedRoadsDef.Instance.Discover(world, runtime, "player").Count());
		battle.Engine.Commit(new RoadsWeDontNeedRoadsAction("player", Origin + Coord.Up * 4));
		Assert.Empty(RoadsWeDontNeedRoadsDef.Instance.Discover(world, runtime, "player"));
		for (var remaining = 2; remaining >= 0; remaining--)
		{
			battle.Engine.Commit(new TurnUpkeepAction("player"));
			Assert.Equal(remaining, world.StateOf("player").CooldownRemaining(JumpMount));
		}
		Assert.NotEmpty(RoadsWeDontNeedRoadsDef.Instance.Discover(world, runtime, "player"));
	}

	[Fact]
	public void JumpKillJumpWorksInSimulationUndoAndLiveCommit()
	{
		var landing = Origin + new Coord(4, 0, 0);
		var battle = CreateBattle(landing + Coord.Up * 2);
		var sim = battle.PlayerAgent.Sim;
		var firstJump = new RoadsWeDontNeedRoadsAction("player", landing);
		var rod = new HighSpeedRodAction("player", ESpatialOrientation.Dorsal);
		Assert.True(sim.TryEnqueue(true, firstJump));
		Assert.True(sim.TryEnqueue(true, rod));
		Assert.False(sim.World.StateOf("enemy").IsAlive);
		Assert.Equal(0, sim.World.StateOf("player").CooldownRemaining(JumpMount));
		Assert.Single(sim.RecordsFor(1).OfType<Record<CooldownResetOnKillFacts>>());
		Assert.True(sim.TryUndoLast());
		Assert.True(sim.World.StateOf("enemy").IsAlive);
		Assert.Equal(3, sim.World.StateOf("player").CooldownRemaining(JumpMount));
		Assert.True(sim.TryEnqueue(true, rod));
		var escape = new RoadsWeDontNeedRoadsAction("player", Origin);
		Assert.True(sim.TryEnqueue(true, escape));
		Assert.True(sim.TryCommit(out var actions, out _));
		battle.Engine.Commit(actions.ToArray());
		Assert.Equal(Origin, battle.Engine.World.StateOf("player").Position);
		Assert.Equal(60, battle.Engine.World.StateOf("player").Heat);
		Assert.Equal(3, battle.Engine.World.StateOf("player").CooldownRemaining(JumpMount));
		Assert.False(battle.Engine.World.StateOf("enemy").IsAlive);
	}

	[Fact]
	public void HudChoicesUseOrthogonalMountPosesAndExactJumpDestinations()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var sim = battle.PlayerAgent.Sim;
		var actor = sim.World.StateOf("player");
		var legal = Capabilities.LegalCapabilities(sim, "player");
		var rodSpec = AbilityHudCatalog.ForUnit(EType.Rogue).Single(spec => spec.Mode == EPlayerMode.HighSpeedRod);
		var rodChoices = AbilityActivation.ResolveChoices(rodSpec, actor, legal);
		Assert.Equal(2, rodChoices.Count);
		Assert.All(rodChoices, choice =>
		{
			Assert.Equal(0, Coord.Dot(choice.Fore, choice.Dorsal));
			Assert.IsType<HighSpeedRodAction>(AbilityActivation.CreateExecutionAction(choice));
		});
		var jumpSpec = AbilityHudCatalog.ForUnit(EType.Rogue).Single(spec => spec.Mode == EPlayerMode.RoadsWeDontNeedRoads);
		var jumpChoices = AbilityActivation.ResolveChoices(jumpSpec, actor, legal);
		Assert.Equal(6, jumpChoices.Count);
		Assert.All(jumpChoices, choice =>
		{
			Assert.Equal(choice.Position,
				Assert.IsType<RoadsWeDontNeedRoadsAction>(AbilityActivation.CreateExecutionAction(choice)).Destination);
			Assert.Equal(4, actor.Position.ManhattanDistanceTo(choice.Position));
			var face = Assert.IsType<ESpatialOrientation>(choice.MountedOn);
			Assert.Equal(actor.Position + BodyFrame.From(actor).Step(face) * 4, choice.Position);
			Assert.Equal(choice, jumpChoices[AbilityActivation.IndexOfMountedChoice(jumpChoices, face)!.Value]);
		});
		var legality = PlanningPreview.Abilities(legal);
		Assert.True(legality.RoadsWeDontNeedRoads);
		Assert.True(legality.Weapons.HighSpeedRod);
	}

	[Fact]
	public void JumpDirectionSelectorPreviewsOnlyLandingCell()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var sim = battle.PlayerAgent.Sim;
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(EType.Rogue)
			.Single(entry => entry.Mode == EPlayerMode.RoadsWeDontNeedRoads);
		frames.Interaction.SetMode(spec.Mode, spec);
		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);
		var hover = AbilityActivation.IndexOfMountedChoice(frame.AbilityChoices, ESpatialOrientation.Dorsal);
		Assert.NotNull(hover);
		frames.Interaction.SetAbilityHover(hover, frame.AbilityChoices.Count);

		frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal("Pick jump direction", frame.Instruction.Label);
		var choice = Assert.IsType<AbilityActivationChoice>(frame.HoveredAbilityChoice);
		var preview = Assert.Single(frame.AreaActions.Aim, area => ReferenceEquals(area.Action, choice.Action));
		var destination = Origin + Coord.Up * 4;
		Assert.Equal(destination, Assert.Single(preview.Volume.Cells));
		Assert.Equal(Origin, sim.World.StateOf("player").Position);
		Assert.True(sim.TryEnqueue(AbilityActivation.CreateExecutionAction(choice)));
		Assert.Equal(destination, sim.World.StateOf("player").Position);
		var queued = Assert.Single(new PlanningPreview().AreaPreviews(
			sim, "player", Capabilities.LegalCapabilities(sim, "player")).Queued);
		Assert.Equal(destination, Assert.Single(queued.Volume.Cells));
	}

	[Fact]
	public void JumpSelectorDisablesInvalidLandingsWithoutShortening()
	{
		var battle = CreateBattle(Origin + Coord.Up * 4,
			blocked: new HashSet<Coord> { Origin + Coord.Forward * 4 });
		var sim = battle.PlayerAgent.Sim;
		var actor = sim.World.StateOf("player");
		var spec = AbilityHudCatalog.ForUnit(EType.Rogue)
			.Single(entry => entry.Mode == EPlayerMode.RoadsWeDontNeedRoads);
		var choices = AbilityActivation.ResolveChoices(spec, actor, Capabilities.LegalCapabilities(sim, "player"));

		Assert.Equal(4, choices.Count);
		Assert.Null(AbilityActivation.IndexOfMountedChoice(choices, ESpatialOrientation.Dorsal));
		Assert.Null(AbilityActivation.IndexOfMountedChoice(choices, ESpatialOrientation.Forward));
		Assert.All(choices, choice => Assert.Equal(4, actor.Position.ManhattanDistanceTo(choice.Position)));

		actor.Position = new Coord(0, 0, 0);
		choices = AbilityActivation.ResolveChoices(spec, actor, Capabilities.LegalCapabilities(sim, "player"));
		Assert.Equal(3, choices.Count);
		Assert.All(choices, choice =>
		{
			Assert.True(sim.World.Grid.IsInBounds(choice.Position));
			Assert.Equal(4, actor.Position.ManhattanDistanceTo(choice.Position));
		});
	}

	[Fact]
	public void JumpDirectionChoicesFollowRotatedShipOrientation()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var sim = battle.PlayerAgent.Sim;
		var actor = sim.World.StateOf("player");
		actor.Fore = Coord.Up;
		actor.Dorsal = Coord.Forward;
		actor.Starboard = Coord.Cross(actor.Dorsal, actor.Fore);
		var spec = AbilityHudCatalog.ForUnit(EType.Rogue)
			.Single(entry => entry.Mode == EPlayerMode.RoadsWeDontNeedRoads);
		var choices = AbilityActivation.ResolveChoices(spec, actor, Capabilities.LegalCapabilities(sim, "player"));

		Assert.Equal(6, choices.Count);
		Assert.All(choices, choice =>
		{
			var face = Assert.IsType<ESpatialOrientation>(choice.MountedOn);
			Assert.Equal(actor.Position + BodyFrame.From(actor).Step(face) * 4, choice.Position);
			Assert.Equal(actor.Fore, choice.Fore);
			Assert.Equal(actor.Dorsal, choice.Dorsal);
		});
	}

	[Fact]
	public void BattleStateAndActionsRoundTripWithCooldownAndCharges()
	{
		var battle = CreateBattle(new Coord(1, 1, 1));
		var jump = new RoadsWeDontNeedRoadsAction("player", Origin + Coord.Up * 4);
		battle.Engine.Commit(jump, new HighSpeedRodAction("player", ESpatialOrientation.Ventral));
		var state = battle.Engine.World.StateOf("player");
		var options = PersistenceJson.CreateOptions();
		var json = JsonSerializer.Serialize(SaveDtoMapper.CaptureBattleUnitState(state), options);
		var restored = SaveDtoMapper.RestoreBattleUnitState(
			JsonSerializer.Deserialize<BattleUnitStateDto>(json, options)!);
		Assert.Equal(state.Position, restored.Position);
		Assert.Equal(40, restored.Heat);
		Assert.Equal(3, restored.CooldownRemaining(JumpMount));
		Assert.Equal(1, restored.UsesRemaining(EAbilityKind.HighSpeedRod));
		var registry = PersistenceRegistry.CreateDefault();
		Assert.Equal(jump, registry.Read(registry.Write(jump)));
		var reset = new Record<CooldownResetOnKillFacts>(new("player", JumpMount));
		Assert.Equal(reset, registry.Read(registry.Write(reset)));
	}

	private static BattleOrchestrator CreateBattle(
		Coord enemyPosition, int enemyHull = 200, int shields = 0,
		IReadOnlySet<Coord>? blocked = null, int temporaryShields = 0,
		IReadOnlyList<EChassisBuffOffer>? buffs = null)
	{
		var ship = ShipInstance.FromCatalog("player", EType.Rogue);
		if (buffs is not null)
			ship.Loadout = ChassisBuffCatalog.Apply(ship.Spec, ship.Loadout, buffs);
		var player = Factory.Create(
			ship, ETeam.Player, Origin, new UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(enemyPosition);
		enemy.State.CurrentHullPoints = enemyHull;
		enemy.State.CurrentShieldPoints.Fill(shields);
		if (temporaryShields > 0)
			TemporaryShields.GrantPerFace(enemy.State, "test", temporaryShields);
		return BattleTestFixture.BeginSimulation(player, enemy, blocked: blocked ?? new HashSet<Coord>());
	}
}
