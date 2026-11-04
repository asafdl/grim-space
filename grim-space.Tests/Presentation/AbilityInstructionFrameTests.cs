using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class AbilityInstructionFrameTests
{
	[Theory]
	[InlineData(1, 0, true)]
	[InlineData(1000, 0, false)]
	[InlineData(1000, 1000, false)]
	public void AimedAbilityPublishesResolvedDamageWithoutChangingPlan(
		int hull,
		int shields,
		bool lethal)
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		enemy.State.CurrentHullPoints = hull;
		enemy.State.CurrentShieldPoints.Fill(shields);
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var sim = battle.PlayerAgent.Sim;
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(player.State.Type)
			.First(entry => entry.Mode == EPlayerMode.LightningCannon);
		frames.Interaction.SetMode(EPlayerMode.LightningCannon, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		var damage = Assert.Single(frame.AbilityDamage).Value;
		var choice = Assert.Single(frame.AbilityChoices);
		var peek = sim.Peek(choice.Action)!.Value;
		var impacts = peek.Records.OfType<Record<ImpactFacts>>()
			.Select(record => record.Value)
			.Where(impact => impact.TargetId == enemy.State.Id)
			.ToArray();
		Assert.Equal(impacts.Sum(impact => impact.HullDamage), damage.HullDamage);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(
				impacts.Where(impact => impact.Face == face).Sum(impact => impact.ShieldDamage),
				damage.ShieldDamage[face]);
		Assert.True(damage.HullDamage > 0 || damage.ShieldDamage.MaxOnAnyFace > 0);
		Assert.Equal(lethal, damage.IsLethal);
		Assert.Equal(
			lethal,
			impacts.Any(impact => impact.IsLethal));
		Assert.All(impacts, impact =>
		{
			Assert.Equal(hull, impact.HullBefore);
			Assert.Equal(shields, impact.ShieldBefore);
		});
		Assert.Equal(!peek.World.StateOf(enemy.State.Id).IsAlive, damage.IsLethal);
		Assert.Equal(hull, sim.World.StateOf(enemy.State.Id).CurrentHullPoints);
		Assert.Equal(hull, enemy.State.CurrentHullPoints);
		Assert.Equal(shields, sim.World.StateOf(enemy.State.Id).CurrentShieldPoints.MaxOnAnyFace);
		Assert.Empty(sim.Actions);
		Assert.Empty(frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: false).AbilityDamage);

		frames.Interaction.SetMode(EPlayerMode.Move);
		Assert.Empty(frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true).AbilityDamage);
	}

	[Fact]
	public void LightningCannonModeHidesMountInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.LightningCannon);

		frames.Interaction.SetMode(EPlayerMode.LightningCannon, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
		Assert.True(frame.CanUndo);
	}

	[Fact]
	public void ScrapDroneSwarmHidesMountInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginScrapSimulation(origin);
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForActor(
				battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId),
				battle.Engine.ActorRuntimes.For(battle.PlayerId))
			.First(entry => entry.Mode == EPlayerMode.ScrapDroneSwarm);

		frames.Interaction.SetMode(EPlayerMode.ScrapDroneSwarm, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
		Assert.Equal(2, frame.AbilityChoices.Count);
		Assert.Null(frame.HoveredAbilityChoice);
		Assert.Empty(frame.AbilityDamage);
	}

	[Fact]
	public void AbilityHoverPublishesResolvedChoice()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginScrapSimulation(origin);
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForActor(
				battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId),
				battle.Engine.ActorRuntimes.For(battle.PlayerId))
			.First(entry => entry.Mode == EPlayerMode.ScrapDroneSwarm);
		frames.Interaction.SetMode(EPlayerMode.ScrapDroneSwarm, spec);
		frames.Interaction.SetAbilityHover(1, optionCount: 2);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(1, frame.AbilityHoveredIndex);
		var choice = Assert.IsType<AbilityActivationChoice>(frame.HoveredAbilityChoice);
		Assert.Same(frame.AbilityChoices[1], choice);
		Assert.IsType<ScrapDroneSwarmAction>(choice.Action);
	}

	[Fact]
	public void BlockedVoidBombMountIsNotPublishedAsChoice()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var (blockedMount, _, _) = VoidBombMount.LaunchPose(
			player.State,
			ESpatialOrientation.Ventral);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position, blockedMount });
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(player.State.Type)
			.First(entry => entry.Mode == EPlayerMode.VoidBomb);
		frames.Interaction.SetMode(EPlayerMode.VoidBomb, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
		Assert.DoesNotContain(
			frame.AbilityChoices,
			choice => choice.Position == blockedMount);
	}

	[Fact]
	public void ActionFailureShowsUnavailableInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.LightningCannon);
		frames.Interaction.SetMode(EPlayerMode.LightningCannon, spec);
		frames.Interaction.ReportActionFailure();

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(BattleHudCopy.ActionUnavailable, frame.Instruction.Label);
	}

	[Fact]
	public void MoveModeHidesInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var frames = new PresentationFrameBuilder();

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
	}

	[Fact]
	public void InspectingHidesInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.LightningCannon);

		frames.Interaction.SetMode(EPlayerMode.LightningCannon, spec);
		frames.Interaction.FocusUnit(BattleTestFixture.FirstEnemyId(battle));

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
		Assert.Empty(frame.AbilityDamage);
	}
}
