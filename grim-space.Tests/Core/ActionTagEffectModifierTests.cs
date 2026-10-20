using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.ActionTagEffectModifier;

public sealed class ActionTagEffectModifierTests
{
	private sealed record StubAction(string ActorId, IReadOnlyList<string> Tags) : IAction;

	private sealed class CountingEffect(int id) : IEffect<BattleWorld, ActorRuntime>
	{
		public int Id { get; } = id;

		public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId) => [];

		public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) { }
	}

	private sealed class TrackingModifier : IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
	{
		public int ModifyCallCount { get; private set; }

		public IEffect<BattleWorld, ActorRuntime> Modify(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime,
			IEffect<BattleWorld, ActorRuntime> effect)
		{
			ModifyCallCount++;
			return effect;
		}
	}

	[Fact]
	public void Modify_AllRequiredTagsPresent_InvokesInnerModifier()
	{
		var inner = new TrackingModifier();
		var modifier = new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			["attack", "lightning"],
			inner);
		var action = new StubAction("actor", ["attack", "lightning"]);
		var effect = new CountingEffect(1);
		var runtime = ActorRuntime.From(null);

		var result = modifier.Modify(action, null!, runtime, effect);

		Assert.Equal(1, inner.ModifyCallCount);
		Assert.Same(effect, result);
	}

	[Fact]
	public void Modify_MissingRequiredTag_ReturnsOriginalEffect()
	{
		var inner = new TrackingModifier();
		var modifier = new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			["attack", "lightning"],
			inner);
		var action = new StubAction("actor", ["attack"]);
		var effect = new CountingEffect(2);
		var runtime = ActorRuntime.From(null);

		var result = modifier.Modify(action, null!, runtime, effect);

		Assert.Equal(0, inner.ModifyCallCount);
		Assert.Same(effect, result);
	}

	[Fact]
	public void Modify_ExtraUnrelatedTags_StillMatches()
	{
		var inner = new TrackingModifier();
		var modifier = new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			["attack"],
			inner);
		var action = new StubAction("actor", ["attack", "bonus", "lightning"]);
		var effect = new CountingEffect(3);
		var runtime = ActorRuntime.From(null);

		modifier.Modify(action, null!, runtime, effect);

		Assert.Equal(1, inner.ModifyCallCount);
	}

	[Fact]
	public void Constructor_RejectsEmptyTagSet()
	{
		var inner = new TrackingModifier();

		Assert.Throws<ArgumentException>(() =>
			new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				[],
				inner));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_RejectsEmptyOrWhitespaceTags(string invalidTag)
	{
		var inner = new TrackingModifier();

		Assert.Throws<ArgumentException>(() =>
			new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				["attack", invalidTag],
				inner));
	}
}
