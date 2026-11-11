using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class AbilityDisplacementPreviewTests
{
	private static readonly Coord Origin = new(8, 8, 8);

	[Theory]
	[InlineData("ram")]
	[InlineData("roads")]
	[InlineData("goop")]
	public void AimMovesShipAndPreservesOriginWithoutChangingSimulation(string ability)
	{
		using var battle = CreateBattle(ability);
		var sim = battle.PlayerAgent.Sim;
		var action = ActionFor(ability, Origin);
		var expected = sim.Peek(action)!.Value.World.StateOf("player").Position;
		Assert.Equal(ability switch
		{
			"ram" => Origin + Coord.Forward * 2,
			"roads" => Origin + Coord.Up * 4,
			"goop" => Origin + Coord.Forward * 3,
			_ => throw new ArgumentOutOfRangeException(nameof(ability)),
		}, expected);

		var preview = PlanningPreview.AbilityDisplacements(sim, "player", action);

		Assert.Equal(expected, preview.AimResult!.Position);
		Assert.Equal(Origin, Assert.Single(preview.Origins).Position);
		Assert.Empty(preview.MovePaths);
		Assert.Empty(sim.Actions);
		Assert.Equal(Origin, sim.World.StateOf("player").Position);
		Assert.Equal(Origin, battle.Engine.World.StateOf("player").Position);
	}

	[Theory]
	[InlineData("ram")]
	[InlineData("roads")]
	[InlineData("goop")]
	public void QueuedAbilityRetainsOriginAndUndoRemovesIt(string ability)
	{
		using var battle = CreateBattle(ability);
		var sim = battle.PlayerAgent.Sim;
		Assert.True(battle.PlayerAgent.TryEnqueue([ActionFor(ability, Origin)]));
		var result = sim.World.StateOf("player").Position;

		var preview = PlanningPreview.AbilityDisplacements(sim, "player", null);

		Assert.Null(preview.AimResult);
		Assert.Equal(Origin, Assert.Single(preview.Origins).Position);
		Assert.Equal(result, sim.World.StateOf("player").Position);
		Assert.True(battle.PlayerAgent.Undo());
		Assert.Empty(PlanningPreview.AbilityDisplacements(sim, "player", null).Origins);
		Assert.Equal(Origin, sim.World.StateOf("player").Position);
	}

	[Theory]
	[InlineData("ram")]
	[InlineData("roads")]
	[InlineData("goop")]
	public void MovementThenAbilityUsesProjectedOriginAndDoesNotBridgeAbilityJump(string ability)
	{
		using var battle = CreateBattle(ability);
		var sim = battle.PlayerAgent.Sim;
		Assert.True(battle.PlayerAgent.TryEnqueue([new MoveStepAction("player")]));
		var moved = Origin + Coord.Forward;
		var action = ActionFor(ability, moved);

		var aimed = PlanningPreview.AbilityDisplacements(sim, "player", action);
		Assert.Equal(moved, Assert.Single(aimed.Origins).Position);
		Assert.Equal(moved, Assert.Single(aimed.MovePaths)[^1].Position);

		Assert.True(battle.PlayerAgent.TryEnqueue([action]));
		var destination = sim.World.StateOf("player").Position;
		var queued = PlanningPreview.AbilityDisplacements(sim, "player", null);
		Assert.Equal(moved, Assert.Single(queued.Origins).Position);
		Assert.Equal(moved, Assert.Single(queued.MovePaths)[^1].Position);

		var move = MoveDef.Instance.Discover(sim.World, sim.RuntimeFor("player"), "player")
			.First(action => sim.Peek(action) is not null);
		Assert.True(battle.PlayerAgent.TryEnqueue([move]));
		var continued = PlanningPreview.AbilityDisplacements(sim, "player", null);
		Assert.Equal(2, continued.MovePaths.Count);
		Assert.Equal(destination, continued.MovePaths[1][0].Position);
		Assert.Equal(Origin, battle.Engine.World.StateOf("player").Position);
	}

	[Fact]
	public void HeadingBeforeMovementPreservesStartingOrientationForReplayBlend()
	{
		using var battle = CreateBattle("goop");
		var sim = battle.PlayerAgent.Sim;
		var start = sim.World.StateOf("player").Clone();
		var heading = HeadingDef.Instance.Discover(sim.World, sim.RuntimeFor("player"), "player")
			.First(action => sim.Peek(action) is not null);
		Assert.True(battle.PlayerAgent.TryEnqueue([heading, new MoveStepAction("player")]));

		var preview = PlanningPreview.AbilityDisplacements(sim, "player", null);

		var path = Assert.Single(preview.MovePaths);
		Assert.Equal(start.Fore, path[0].Basis.Forward);
		Assert.Equal(sim.World.StateOf("player").Fore, path[^1].Basis.Forward);
		Assert.NotEqual(path[0].Basis.Forward, path[^1].Basis.Forward);
	}

	[Fact]
	public void RamWithoutDisplacementDoesNotCreateDuplicateOrigin()
	{
		using var battle = CreateBattle("ram", enemyOffset: 1);
		var preview = PlanningPreview.AbilityDisplacements(
			battle.PlayerAgent.Sim, "player", new RamAction("player"));
		Assert.Equal(Origin, preview.AimResult!.Position);
		Assert.Empty(preview.Origins);
	}

	[Fact]
	public void QueuedAbilitiesKeepTheirIndividualOriginsAndClearWithUndo()
	{
		using var battle = CreateBattle("goop", enemyOffset: 6);
		var sim = battle.PlayerAgent.Sim;
		Assert.True(battle.PlayerAgent.TryEnqueue([new MoveStepAction("player")]));
		var goopOrigin = sim.World.StateOf("player").Position;
		Assert.True(battle.PlayerAgent.TryEnqueue([ActionFor("goop", goopOrigin)]));
		var ramOrigin = sim.World.StateOf("player").Position;
		Assert.True(battle.PlayerAgent.TryEnqueue([new RamAction("player")]));

		var preview = PlanningPreview.AbilityDisplacements(sim, "player", null);
		Assert.Equal(new[] { goopOrigin, ramOrigin }, preview.Origins.Select(state => state.Position));
		Assert.True(battle.PlayerAgent.Undo());
		Assert.Single(PlanningPreview.AbilityDisplacements(sim, "player", null).Origins);
		while (battle.PlayerAgent.CanUndo)
			Assert.True(battle.PlayerAgent.Undo());
		Assert.Empty(PlanningPreview.AbilityDisplacements(sim, "player", null).Origins);
	}

	private static IAction ActionFor(string ability, Coord origin) => ability switch
	{
		"ram" => new RamAction("player"),
		"roads" => new RoadsWeDontNeedRoadsAction("player", origin + Coord.Up * 4),
		"goop" => GoopGunDef.Instance.Bind("player", ESpatialOrientation.Retro),
		_ => throw new ArgumentOutOfRangeException(nameof(ability)),
	};

	private static BattleOrchestrator CreateBattle(string ability, int? enemyOffset = null)
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", ability == "roads" ? EType.Rogue : EType.IndustrialGooper),
			ETeam.Player, Origin, new UserExecutionAgent());
		return BattleTestFixture.BeginSimulation(
			player, BattleTestFixture.Enemy(Origin + Coord.Forward * (enemyOffset ?? (ability == "ram" ? 3 : 6))),
			BattleTestFixture.Grid(size: 30));
	}
}
