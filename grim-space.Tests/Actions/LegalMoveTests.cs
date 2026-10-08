using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Units.Maneuvering;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class LegalMoveTests
{
	[Fact]
	public void BlockedDestinationsIncludeTerrainAndLivingUnitsButNotDeadUnits()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		var terrain = origin + Coord.Up;
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			enemy,
			blocked: new HashSet<Coord> { terrain });
		var world = battle.Engine.World;

		Assert.True(world.IsCellBlocked(terrain));
		Assert.True(world.IsCellBlocked(origin));
		Assert.True(world.IsCellBlocked(enemy.State.Position));
		Assert.False(MoveDef.Instance.IsPossible(
			new MoveStepAction(battle.PlayerId),
			world,
			battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId)));

		enemy.State.HullPoints = 0;
		Assert.False(world.IsCellBlocked(enemy.State.Position));
		Assert.True(MoveDef.Instance.IsPossible(
			new MoveStepAction(battle.PlayerId),
			world,
			battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId)));
	}

	[Fact]
	public void EnqueueMovePathAddsExactCombinedSteps()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var move = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));
		Assert.Equal(move.Steps, battle.PlayerAgent.Sim.Actions);
		Assert.Equal(move.EndPosition, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Equal(1, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
	}

	[Fact]
	public void ConfirmedSegmentCanBeFollowedByWeaponAndAnotherSegment()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var first = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.EndPosition == origin + Coord.Forward);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, first));
		Assert.True(battle.PlayerAgent.TryEnqueue([new LightningCannonAction(battle.PlayerId)]));

		var second = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.ExtensionApCost == 1);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, second));

		Assert.IsType<MoveStepAction>(battle.PlayerAgent.Sim.Actions[0]);
		Assert.IsType<LightningCannonAction>(battle.PlayerAgent.Sim.Actions[1]);
		Assert.All(
			battle.PlayerAgent.Sim.Actions.Skip(2),
			action => Assert.True(action is HeadingTurnAction or RollAction or MoveStepAction));
		Assert.IsType<MoveStepAction>(battle.PlayerAgent.Sim.Actions[^1]);
	}

	[Fact]
	public void UndoRestoresPositionBasisAndApForCombinedSegment()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		IAction[] segment =
		[
			new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight),
			new MoveStepAction(battle.PlayerId),
			new RollAction(battle.PlayerId, ERollDirection.Clockwise),
			new MoveStepAction(battle.PlayerId),
		];
		Assert.True(battle.PlayerAgent.TryEnqueue(segment));

		Assert.True(battle.PlayerAgent.Undo());

		var state = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId);
		Assert.Equal(origin, state.Position);
		Assert.Equal(Coord.Forward, state.Fore);
		Assert.Equal(Coord.Up, state.Dorsal);
		Assert.Equal(4, state.ActionPoints);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port, -1, 0, 0)]
	[InlineData(ESpatialOrientation.Starboard, 1, 0, 0)]
	[InlineData(ESpatialOrientation.Retro, 0, 0, -1)]
	[InlineData(ESpatialOrientation.Dorsal, 0, 1, 0)]
	[InlineData(ESpatialOrientation.Ventral, 0, -1, 0)]
	public void DirectionalStepTranslatesWithoutChangingOrientation(
		ESpatialOrientation direction,
		int x,
		int y,
		int z)
	{
		var origin = new Coord(5, 5, 5);
		var session = BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(new MoveStepAction("player", direction)));

		var state = session.StateOf<ActorState>("player");
		Assert.Equal(origin + new Coord(x, y, z), state.Position);
		Assert.Equal(Coord.Forward, state.Fore);
		Assert.Equal(Coord.Up, state.Dorsal);
	}

	[Fact]
	public void RetroStepCostsTwoAp()
	{
		var origin = new Coord(5, 5, 5);
		var session = BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Starboard)));
		Assert.Equal(3, session.StateOf<ActorState>("player").ActionPoints);

		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Retro)));
		Assert.Equal(1, session.StateOf<ActorState>("player").ActionPoints);
	}

	[Fact]
	public void RetroStepRequiresTwoAp()
	{
		var session = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5), actionPoints: 1),
			BattleTestFixture.Enemy(Coord.Zero)).PlayerAgent.Sim;

		Assert.False(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Retro)));
		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Port)));
	}

	[Fact]
	public void TranslationDiscoveryAndCostUseEffectiveProfile()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		player.State.Maneuverability = new ManeuverabilitySpec(
			4,
			3,
			new Dictionary<ESpatialOrientation, int>
			{
				[ESpatialOrientation.Forward] = 2,
			},
			new Dictionary<EHeadingTurn, int>(),
			new Dictionary<ERollDirection, int>());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var sim = battle.PlayerAgent.Sim;
		var discovered = MoveDef.Instance
			.Discover(sim.World, sim.RuntimeFor(battle.PlayerId), battle.PlayerId)
			.Cast<MoveStepAction>()
			.ToArray();

		Assert.Single(discovered);
		Assert.Equal(ESpatialOrientation.Forward, discovered[0].Direction);
		Assert.False(sim.TryEnqueue(
			new MoveStepAction(battle.PlayerId, ESpatialOrientation.Port)));
		Assert.True(sim.TryEnqueue(new MoveStepAction(battle.PlayerId)));
		Assert.Equal(2, sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
		Assert.Equal(3, sim.StateOf<ActorState>(battle.PlayerId).ManeuverPoints);
	}
}
