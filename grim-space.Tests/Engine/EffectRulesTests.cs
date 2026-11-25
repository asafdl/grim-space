using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Engine;

public sealed class EffectRulesTests
{
	private const string ActorId = "actor";
	private const string BonusTag = "bonus";

	[Fact]
	public void NoRegisteredRulesAppliesResolvedEffectsUnchanged()
	{
		var world = new LedgerWorld();
		var first = new Add(1);
		var second = new Add(2);

		var applied = ExecutionHelper.ApplyAndResolve(
			Act([], first, second),
			world,
			new EmptyRuntime(),
			EffectRules<LedgerWorld, EmptyRuntime>.Empty);

		Assert.Collection(
			applied,
			effect => Assert.Same(first, effect),
			effect => Assert.Same(second, effect));
		Assert.Equal(3, world.Value);
	}

	[Fact]
	public void NonMatchingRuleLeavesEffectUnchanged()
	{
		var effect = new Add(1);

		var prepared = Rules(new PlusOneWhenTagged()).Prepare(Act([], effect), new LedgerWorld(), new EmptyRuntime(), effect);

		Assert.Same(effect, prepared);
	}

	[Fact]
	public void TagMatcherAppliesOnlyToTaggedActions()
	{
		var rules = Rules(new PlusOneWhenTagged());

		Assert.Equal(2, Run(rules, Act([BonusTag], new Add(1))).Value);
		Assert.Equal(1, Run(rules, Act(["other"], new Add(1))).Value);
	}

	[Fact]
	public void PredicateMatcherObservesWorldStateLeftByEarlierEffects()
	{
		var world = Run(Rules(new TimesTenFromValue(10)), Act([], new Add(10), new Add(1)));

		Assert.Equal(20, world.Value);
	}

	[Theory]
	[InlineData(true, 11)]
	[InlineData(false, 12)]
	public void RulesRunOnceInRegistrationOrderAndSeeEarlierTransforms(bool doubleFirst, int expected)
	{
		var counter = new CountingRule();
		IEffectRule<LedgerWorld, EmptyRuntime>[] ordered = doubleFirst
			? [new Double(), new PlusOneWhenTagged(), counter]
			: [new PlusOneWhenTagged(), new Double(), counter];

		var world = Run(new EffectRules<LedgerWorld, EmptyRuntime>(ordered), Act([BonusTag], new Add(5)));

		Assert.Equal(expected, world.Value);
		Assert.Equal(1, counter.Visits);
	}

	[Fact]
	public void ReactionFiresOnlyWhenAppliedRecordsMatch()
	{
		var world = Run(Reactions(new HundredWhenTotalReaches(3)), Act([], new Add(1), new Add(2)));

		Assert.Equal(["+1", "+2", "+100"], world.Log);
	}

	[Fact]
	public void FollowUpEffectsPassThroughRules()
	{
		var rules = new EffectRules<LedgerWorld, EmptyRuntime>([new Double()], [new HundredWhenTotalReaches(2)]);

		var world = Run(rules, Act([], new Add(1)));

		Assert.Equal(["+2", "+200"], world.Log);
	}

	[Fact]
	public void NestedReactionsApplyDepthFirstAndUndoInReverse()
	{
		var world = new LedgerWorld();
		var action = Act([], new Add(2), new Add(3));
		var runtime = new EmptyRuntime();

		var applied = ExecutionHelper.ApplyAndResolve(action, world, runtime, Reactions(new Countdown()));
		Assert.Equal(["+2", "+1", "+3", "+2", "+1"], world.Log);
		Assert.Equal(5, applied.Count);

		ExecutionHelper.UndoEffects(applied, action, world, runtime);
		Assert.Equal(["-1", "-2", "-3", "-1", "-2"], world.Log.Skip(5));
		Assert.Equal(0, world.Value);
	}

	[Fact]
	public void ReactionsPastMaximumDepthThrow()
	{
		var world = new LedgerWorld();

		Assert.Throws<InvalidOperationException>(() => Run(Reactions(new Endless()), Act([], new Add(1)), world));

		Assert.Equal(ExecutionHelper.MaxReactionDepth + 1, world.Log.Count);
	}

	[Fact]
	public void FailedFollowUpPropagatesWithoutApplyingLaterPrimaryEffects()
	{
		var world = new LedgerWorld();

		Assert.Throws<InvalidOperationException>(() => Run(
			Reactions(new FailAfterFirst()),
			Act([], new Add(1), new Add(2)),
			world));

		Assert.Equal(["+1"], world.Log);
	}

	[Fact]
	public void FailedEffectPropagatesWithoutRollingBackEarlierEffects()
	{
		var world = new LedgerWorld();

		Assert.Throws<InvalidOperationException>(() => Run(
			EffectRules<LedgerWorld, EmptyRuntime>.Empty,
			Act([], new Add(1), new Fail(), new Add(2)),
			world));

		Assert.Equal(["+1"], world.Log);
	}

	[Fact]
	public void InvalidRulesFailExplicitly()
	{
		var effect = new Add(1);

		Assert.Throws<ArgumentException>(() => new EffectRules<LedgerWorld, EmptyRuntime>([null!]));
		Assert.Throws<ArgumentException>(() => new EffectRules<LedgerWorld, EmptyRuntime>([], [null!]));
		Assert.Throws<InvalidOperationException>(() =>
			Rules(new ReturnsNull()).Prepare(Act([], effect), new LedgerWorld(), new EmptyRuntime(), effect));
		Assert.Throws<InvalidOperationException>(() =>
			Reactions(new ReturnsNullFollowUp()).React(Act([], effect), new LedgerWorld(), new EmptyRuntime(), effect, []));
	}

	[Fact]
	public void PrepareTransformsWithoutApplying()
	{
		var world = new LedgerWorld();
		var effect = new Add(3);

		var prepared = Assert.IsType<Add>(Rules(new Double()).Prepare(Act([], effect), world, new EmptyRuntime(), effect));

		Assert.Equal(6, prepared.Amount);
		Assert.Equal(0, world.Value);
		Assert.Empty(world.Log);
	}

	[Fact]
	public void EngineSimulationsReplaysAndForksKeepRegisteredRules()
	{
		using var engine = new Engine<LedgerWorld, EmptyRuntime>(
			new LedgerWorld(),
			new ActorRuntimes<EmptyRuntime>(),
			TimelineGcOptions.Disabled,
			Rules(new Double()));

		engine.Commit(Act([], new Add(1)));
		Assert.Equal(2, engine.World.Value);

		var sim = engine.CreateSimulation();
		Assert.Same(engine.Rules, sim.Rules);
		Assert.True(sim.TryEnqueue(Act([], new Add(3))));
		Assert.Equal(8, sim.World.Value);
		Assert.Equal(18, sim.Peek(Act([], new Add(5)))!.Value.World.Value);
		Assert.Equal(8, sim.ReplayWorld(1).Value);
		sim.Reevaluate();
		Assert.Equal(8, sim.World.Value);

		var fork = sim.Fork();
		Assert.True(fork.TryEnqueue(Act([], new Add(1))));
		Assert.Equal(10, fork.World.Value);
		Assert.Equal(8, sim.World.Value);

		var fresh = sim.ForkFromAnchor();
		Assert.True(fresh.TryEnqueue(Act([], new Add(2))));
		Assert.Equal(6, fresh.World.Value);
		Assert.Equal(2, engine.World.Value);
	}

	[Fact]
	public void DirectlyConstructedSimulationUsesSuppliedRules()
	{
		var sim = new Simulation<LedgerWorld, EmptyRuntime>(
			new LedgerWorld(),
			new ActorRuntimes<EmptyRuntime>(),
			Rules(new Double()));
		sim.Begin(1, 0);

		Assert.True(sim.TryEnqueue(Act([], new Add(4))));
		Assert.Equal(8, sim.World.Value);
		sim.Dequeue();
		Assert.Equal(0, sim.World.Value);
	}

	[Fact]
	public void EngineSimulationsReplaysAndForksKeepRegisteredReactions()
	{
		using var engine = new Engine<LedgerWorld, EmptyRuntime>(
			new LedgerWorld(),
			new ActorRuntimes<EmptyRuntime>(),
			TimelineGcOptions.Disabled,
			Reactions(new Countdown()));

		engine.Commit(Act([], new Add(2)));
		Assert.Equal(3, engine.World.Value);

		var sim = engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(Act([], new Add(2))));
		Assert.Equal(6, sim.World.Value);
		Assert.Equal(12, sim.Peek(Act([], new Add(3)))!.Value.World.Value);
		Assert.Equal(6, sim.ReplayWorld(1).Value);
		sim.Reevaluate();
		Assert.Equal(6, sim.World.Value);

		var fork = sim.Fork();
		Assert.True(fork.TryEnqueue(Act([], new Add(3))));
		Assert.Equal(12, fork.World.Value);
		Assert.Equal(6, sim.World.Value);

		var fresh = sim.ForkFromAnchor();
		Assert.True(fresh.TryEnqueue(Act([], new Add(2))));
		Assert.Equal(6, fresh.World.Value);

		sim.Dequeue();
		Assert.Equal(3, sim.World.Value);
		Assert.Equal(3, engine.World.Value);
	}

	private static EffectRules<LedgerWorld, EmptyRuntime> Rules(params IEffectRule<LedgerWorld, EmptyRuntime>[] rules) =>
		new(rules);

	private static EffectRules<LedgerWorld, EmptyRuntime> Reactions(
		params IEffectReaction<LedgerWorld, EmptyRuntime>[] reactions) =>
		new([], reactions);

	private static LedgerAction Act(IReadOnlyList<string> tags, params IEffect<LedgerWorld, EmptyRuntime>[] effects) =>
		new(ActorId, tags, effects);

	private static LedgerWorld Run(
		EffectRules<LedgerWorld, EmptyRuntime> rules,
		LedgerAction action,
		LedgerWorld? world = null)
	{
		world ??= new LedgerWorld();
		ExecutionHelper.Apply(action, world, new EmptyRuntime(), rules);
		return world;
	}

	private sealed class LedgerWorld : IWorld<LedgerWorld>
	{
		public int Value { get; set; }

		public List<string> Log { get; private init; } = [];

		public Timeline Timeline { get; private init; } = new();

		public LedgerWorld Fork() => new() { Value = Value, Log = [..Log], Timeline = Timeline.Clone() };
	}

	private sealed record LedgerAction(
		string ActorId,
		IReadOnlyList<string> Tags,
		IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> Effects) : IAction<LedgerWorld, EmptyRuntime>
	{
		public IActionDef<IAction, LedgerWorld, EmptyRuntime, IEffect<LedgerWorld, EmptyRuntime>> Definition =>
			LedgerActionDef.Instance;
	}

	private sealed class LedgerActionDef
		: IActionDef<IAction, LedgerWorld, EmptyRuntime, IEffect<LedgerWorld, EmptyRuntime>>
	{
		public static LedgerActionDef Instance { get; } = new();

		public IEnumerable<IAction> Discover(LedgerWorld world, EmptyRuntime runtime, string actorId) => [];

		public bool IsPossible(IAction action, LedgerWorld world, EmptyRuntime runtime) => true;

		public bool IsLegal(IAction action, LedgerWorld world, EmptyRuntime runtime) => true;

		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> Resolve(
			IAction action,
			LedgerWorld world,
			EmptyRuntime runtime) =>
			((LedgerAction)action).Effects;
	}

	private sealed record Add(int Amount) : IEffect<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IRecord> Apply(LedgerWorld world, EmptyRuntime runtime, string actorId)
		{
			world.Value += Amount;
			world.Log.Add($"+{Amount}");
			return [new Record<int>(world.Value)];
		}

		public void Undo(LedgerWorld world, EmptyRuntime runtime, string actorId)
		{
			world.Value -= Amount;
			world.Log.Add($"-{Amount}");
		}
	}

	private sealed class Fail : IEffect<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IRecord> Apply(LedgerWorld world, EmptyRuntime runtime, string actorId) =>
			throw new InvalidOperationException("Effect failed.");

		public void Undo(LedgerWorld world, EmptyRuntime runtime, string actorId) { }
	}

	private sealed class Double : IEffectRule<LedgerWorld, EmptyRuntime>
	{
		public bool Matches(EffectContext<LedgerWorld, EmptyRuntime> context, IEffect<LedgerWorld, EmptyRuntime> effect) =>
			effect is Add;

		public IEffect<LedgerWorld, EmptyRuntime> Transform(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect) =>
			new Add(((Add)effect).Amount * 2);
	}

	private sealed class PlusOneWhenTagged : IEffectRule<LedgerWorld, EmptyRuntime>
	{
		public bool Matches(EffectContext<LedgerWorld, EmptyRuntime> context, IEffect<LedgerWorld, EmptyRuntime> effect) =>
			effect is Add && context.Action.Tags.Contains(BonusTag);

		public IEffect<LedgerWorld, EmptyRuntime> Transform(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect) =>
			new Add(((Add)effect).Amount + 1);
	}

	private sealed class TimesTenFromValue(int threshold) : IEffectRule<LedgerWorld, EmptyRuntime>
	{
		public bool Matches(EffectContext<LedgerWorld, EmptyRuntime> context, IEffect<LedgerWorld, EmptyRuntime> effect) =>
			effect is Add && context.World.Value >= threshold;

		public IEffect<LedgerWorld, EmptyRuntime> Transform(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect) =>
			new Add(((Add)effect).Amount * 10);
	}

	private sealed class HundredWhenTotalReaches(int threshold) : IEffectReaction<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> React(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect,
			IReadOnlyList<IRecord> records) =>
			effect is Add { Amount: < 100 } && records.OfType<Record<int>>().Any(r => r.Value >= threshold)
				? [new Add(100)]
				: [];
	}

	private sealed class Countdown : IEffectReaction<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> React(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect,
			IReadOnlyList<IRecord> records) =>
			effect is Add { Amount: > 1 } add ? [new Add(add.Amount - 1)] : [];
	}

	private sealed class Endless : IEffectReaction<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> React(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect,
			IReadOnlyList<IRecord> records) =>
			[new Add(1)];
	}

	private sealed class FailAfterFirst : IEffectReaction<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> React(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect,
			IReadOnlyList<IRecord> records) =>
			effect is Add { Amount: 1 } ? [new Fail()] : [];
	}

	private sealed class ReturnsNullFollowUp : IEffectReaction<LedgerWorld, EmptyRuntime>
	{
		public IReadOnlyList<IEffect<LedgerWorld, EmptyRuntime>> React(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect,
			IReadOnlyList<IRecord> records) =>
			[null!];
	}

	private sealed class CountingRule : IEffectRule<LedgerWorld, EmptyRuntime>
	{
		public int Visits { get; private set; }

		public bool Matches(EffectContext<LedgerWorld, EmptyRuntime> context, IEffect<LedgerWorld, EmptyRuntime> effect)
		{
			Visits++;
			return false;
		}

		public IEffect<LedgerWorld, EmptyRuntime> Transform(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect) =>
			effect;
	}

	private sealed class ReturnsNull : IEffectRule<LedgerWorld, EmptyRuntime>
	{
		public bool Matches(EffectContext<LedgerWorld, EmptyRuntime> context, IEffect<LedgerWorld, EmptyRuntime> effect) =>
			true;

		public IEffect<LedgerWorld, EmptyRuntime> Transform(
			EffectContext<LedgerWorld, EmptyRuntime> context,
			IEffect<LedgerWorld, EmptyRuntime> effect) =>
			null!;
	}
}
