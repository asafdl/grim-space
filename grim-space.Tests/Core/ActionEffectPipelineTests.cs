using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.ActionEffectPipeline;

public sealed class ActionEffectPipelineTests
{
	private sealed record StubAction(string ActorId) : IAction;

	private sealed class CountingEffect(int id) : IEffect<BattleWorld, ActorRuntime>
	{
		public int Id { get; } = id;

		public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId) => [];

		public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) { }
	}

	private sealed class AppendEffectModifier(string suffix)
		: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
	{
		public IEffect<BattleWorld, ActorRuntime> Modify(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime,
			IEffect<BattleWorld, ActorRuntime> effect)
		{
			var (id, label) = effect switch
			{
				TaggedEffect tagged => (tagged.Id, tagged.Tag),
				CountingEffect counting => (counting.Id, string.Empty),
				_ => throw new InvalidOperationException("Unexpected effect type."),
			};
			return new TaggedEffect(id, label + suffix);
		}
	}

	private sealed class NullReturningModifier
		: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
	{
		public IEffect<BattleWorld, ActorRuntime> Modify(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime,
			IEffect<BattleWorld, ActorRuntime> effect) => null!;
	}

	private sealed class TaggedEffect(int id, string tag) : IEffect<BattleWorld, ActorRuntime>
	{
		public int Id { get; } = id;
		public string Tag { get; } = tag;

		public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId) => [];

		public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) { }
	}

	[Fact]
	public void Empty_ReturnsInputListUnchanged()
	{
		var runtime = ActorRuntime.From(null);
		IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects =
			[new CountingEffect(1), new CountingEffect(2)];

		var result = ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty
			.Apply(new StubAction("actor"), null!, runtime, effects);

		Assert.Same(effects, result);
	}

	[Fact]
	public void Apply_FoldsModifiersInConstructorOrder()
	{
		var runtime = ActorRuntime.From(null);
		var pipeline = new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
		[
			new AppendEffectModifier("a"),
			new AppendEffectModifier("b"),
		]);
		var effects = new IEffect<BattleWorld, ActorRuntime>[] { new CountingEffect(7) };

		var result = pipeline.Apply(new StubAction("actor"), null!, runtime, effects);

		var tagged = Assert.IsType<TaggedEffect>(Assert.Single(result));
		Assert.Equal("ab", tagged.Tag);
	}

	[Fact]
	public void Apply_ThrowsWhenModifierReturnsNull()
	{
		var runtime = ActorRuntime.From(null);
		var pipeline = new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			[new NullReturningModifier()]);
		var effects = new IEffect<BattleWorld, ActorRuntime>[] { new CountingEffect(1) };

		Assert.Throws<InvalidOperationException>(() =>
			pipeline.Apply(new StubAction("actor"), null!, runtime, effects));
	}
}
