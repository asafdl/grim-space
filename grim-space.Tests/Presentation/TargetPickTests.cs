using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Targeting;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class TargetPickTests
{
	private static readonly Coord PlayerCell = new(1, 1, 1);

	[Fact]
	public void TeamFilterMatchesRelativeToActor()
	{
		var world = World().Engine.World;
		var enemy = new RelationFilter(EUnitRelation.Opponent);
		var ally = new RelationFilter(EUnitRelation.Ally);

		Assert.True(enemy.Matches(world, "player", "enemy", []));
		Assert.False(enemy.Matches(world, "player", "ally", []));
		Assert.True(ally.Matches(world, "player", "ally", []));
		Assert.False(ally.Matches(world, "player", "player", []));
		Assert.False(ally.Matches(world, "player", "enemy", []));
		Assert.True(new RelationFilter(EUnitRelation.Self, EUnitRelation.Ally)
			.Matches(world, "player", "player", []));
	}

	[Fact]
	public void RangeAndPickedFiltersMatch()
	{
		var world = World().Engine.World;

		Assert.True(new MaxRangeFilter(1).Matches(world, "player", "enemy", []));
		Assert.False(new MaxRangeFilter(1).Matches(world, "player", "ally", []));
		Assert.False(new ExcludeAlreadyPickedFilter().Matches(world, "player", "enemy", ["enemy"]));
		Assert.True(new ExcludeAlreadyPickedFilter().Matches(world, "player", "enemy", ["ally"]));
	}

	[Fact]
	public void LegalTargetsAreOrderedByDistanceThenId()
	{
		var world = World().Engine.World;
		var spec = new TargetPickSpec(1, 3, [new RelationFilter(EUnitRelation.Self, EUnitRelation.Ally, EUnitRelation.Opponent)]);

		Assert.Equal(
			["player", "enemy", "ally", "far-enemy"],
			spec.LegalTargets(world, "player", []));
	}

	[Fact]
	public void LegalTargetsApplyEveryFilterAndSkipDeadUnits()
	{
		var battle = World();
		var spec = new TargetPickSpec(
			1,
			2,
			[new RelationFilter(EUnitRelation.Opponent), new ExcludeAlreadyPickedFilter()]);

		Assert.Equal(["enemy", "far-enemy"], spec.LegalTargets(battle.Engine.World, "player", []));
		Assert.Equal(["far-enemy"], spec.LegalTargets(battle.Engine.World, "player", ["enemy"]));

		battle.Engine.World.StateOf("enemy").HullPoints = 0;
		Assert.Equal(["far-enemy"], spec.LegalTargets(battle.Engine.World, "player", []));
	}

	[Fact]
	public void MinAndMaxGateConfirmAndLockIn()
	{
		var spec = new TargetPickSpec(2, 3, []);

		Assert.False(spec.CanLockIn(1));
		Assert.True(spec.CanLockIn(2));
		Assert.True(spec.CanLockIn(3));
		Assert.True(spec.CanPickMore(2));
		Assert.False(spec.CanPickMore(3));
		Assert.Throws<ArgumentOutOfRangeException>(() => new TargetPickSpec(0, 1, []));
		Assert.Throws<ArgumentOutOfRangeException>(() => new TargetPickSpec(3, 2, []));
	}

	[Fact]
	public void SessionStartsOnNearestLegalUnit()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));

		var frame = Frame(battle, frames);

		Assert.Equal("enemy", frame.TargetPick!.CandidateId);
		Assert.Equal("enemy", frame.RangeEnemy!.Value.EnemyId);
		Assert.True(frame.TargetPick.CanConfirmPick);
		Assert.False(frame.TargetPick.CanLockIn);
		Assert.Empty(frame.AbilityChoices);
	}

	[Fact]
	public void ReticleFollowsAllyCandidate()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 1, [new RelationFilter(EUnitRelation.Ally)]));

		var frame = Frame(battle, frames);

		Assert.Equal("ally", frame.RangeEnemy!.Value.EnemyId);
	}

	[Fact]
	public void ConfirmPickAdvancesCandidateAndEnablesLockIn()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		Frame(battle, frames);

		Assert.True(frames.Interaction.ConfirmTargetPick());
		var frame = Frame(battle, frames);

		Assert.Equal(["enemy"], frame.TargetPick!.PickedIds);
		Assert.Equal("far-enemy", frame.TargetPick.CandidateId);
		Assert.True(frame.TargetPick.CanLockIn);
		Assert.True(frame.TargetPick.CanConfirmPick);
	}

	[Fact]
	public void ConfirmIsRefusedAtMaxPicks()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 1, EnemyOnly()));
		Frame(battle, frames);
		frames.Interaction.ConfirmTargetPick();

		var frame = Frame(battle, frames);

		Assert.False(frame.TargetPick!.CanConfirmPick);
		Assert.False(frames.Interaction.ConfirmTargetPick());
		Assert.Single(frames.Interaction.PickedTargetIds);
	}

	[Fact]
	public void UndoPopsLastPickAndRestoresCandidate()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		Frame(battle, frames);
		frames.Interaction.ConfirmTargetPick();
		Frame(battle, frames);

		Assert.True(frames.Interaction.UndoTargetPick());
		var frame = Frame(battle, frames);

		Assert.Empty(frame.TargetPick!.PickedIds);
		Assert.Equal("enemy", frame.TargetPick.CandidateId);
		Assert.False(frames.Interaction.UndoTargetPick());
	}

	[Fact]
	public void CandidateThatStopsMatchingFallsBackToNearestLegal()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		Frame(battle, frames);
		frames.Interaction.SetTargetCandidate("far-enemy");
		Assert.Equal("far-enemy", Frame(battle, frames).TargetPick!.CandidateId);

		battle.PlayerAgent.Sim.StateOf<ActorState>("far-enemy").HullPoints = 0;
		var frame = Frame(battle, frames);

		Assert.Equal("enemy", frame.TargetPick!.CandidateId);
	}

	[Fact]
	public void DeadPickIsDropped()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		Frame(battle, frames);
		frames.Interaction.ConfirmTargetPick();
		battle.PlayerAgent.Sim.StateOf<ActorState>("enemy").HullPoints = 0;

		var frame = Frame(battle, frames);

		Assert.Empty(frame.TargetPick!.PickedIds);
	}

	[Fact]
	public void LeavingModeDropsEveryPick()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		Frame(battle, frames);
		frames.Interaction.ConfirmTargetPick();

		frames.Interaction.SetMode(EPlayerMode.Move);

		Assert.Empty(frames.Interaction.PickedTargetIds);
		Assert.Null(frames.Interaction.TargetCandidateId);
		Assert.Null(Frame(battle, frames).TargetPick);
	}

	[Fact]
	public void IdleRangeEnemyIsUntouchedBySession()
	{
		var battle = World();
		var frames = BeginSession(battle, new TargetPickSpec(1, 2, EnemyOnly()));
		frames.Interaction.SetRangeEnemy("far-enemy");
		Frame(battle, frames);

		frames.Interaction.SetMode(EPlayerMode.Move);
		var frame = Frame(battle, frames);

		Assert.Equal("far-enemy", frame.RangeEnemy!.Value.EnemyId);
	}

	[Fact]
	public void TargetedSpecRequiresTargetedDef()
	{
		var spec = AbilityHudCatalog.ForUnit(EType.Fighter)[0] with
		{
			TargetPick = new TargetPickSpec(1, 1, []),
		};

		Assert.Throws<InvalidOperationException>(() => spec.TargetedDef);
	}

	private static IReadOnlyList<ITargetFilter> EnemyOnly() => [new RelationFilter(EUnitRelation.Opponent), new ExcludeAlreadyPickedFilter()];

	private static BattleOrchestrator World() =>
		BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(PlayerCell),
			[
				BattleTestFixture.Enemy(new Coord(2, 1, 1)),
				BattleTestFixture.Ally(new Coord(3, 1, 1)),
				BattleTestFixture.RepurposedMiner(new Coord(8, 8, 8), "far-enemy"),
			]);

	private static PresentationFrameBuilder BeginSession(BattleOrchestrator battle, TargetPickSpec pick)
	{
		var frames = BattleTestFixture.FrameBuilder(battle);
		var spec = AbilityHudCatalog.ForUnit(EType.Fighter)[0] with
		{
			Def = new FakeTargetedDef(),
			TargetPick = pick,
		};
		frames.Interaction.SetMode(spec.Mode, spec);
		return frames;
	}

	private static PresentationFrame Frame(BattleOrchestrator battle, PresentationFrameBuilder frames) =>
		frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

	private sealed class FakeTargetedDef
		: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
			ITargetedActionDef
	{
		public IAction Bind(string actorId, IReadOnlyList<string> pickedIds) =>
			new FakeAction(actorId, pickedIds, this);

		public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

		public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

		public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

		public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime) => [];
	}

	private sealed record FakeAction(
		string ActorId,
		IReadOnlyList<string> PickedIds,
		FakeTargetedDef Def) : IAction<BattleWorld, ActorRuntime>
	{
		public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition => Def;
	}
}
