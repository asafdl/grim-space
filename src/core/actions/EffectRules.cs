using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public sealed class EffectRules<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	public static EffectRules<TWorld, TRuntime> Empty { get; } = new([]);

	private readonly IEffectRule<TWorld, TRuntime>[] _rules;
	private readonly IEffectReaction<TWorld, TRuntime>[] _reactions;

	public EffectRules(
		IEnumerable<IEffectRule<TWorld, TRuntime>> rules,
		IEnumerable<IEffectReaction<TWorld, TRuntime>>? reactions = null)
	{
		ArgumentNullException.ThrowIfNull(rules);
		_rules = rules.ToArray();
		if (_rules.Any(rule => rule is null))
			throw new ArgumentException("Effect rules cannot contain null entries.", nameof(rules));

		_reactions = reactions?.ToArray() ?? [];
		if (_reactions.Any(reaction => reaction is null))
			throw new ArgumentException("Effect reactions cannot contain null entries.", nameof(reactions));
	}

	public IEffect<TWorld, TRuntime> Prepare(
		IAction action,
		TWorld world,
		TRuntime runtime,
		IEffect<TWorld, TRuntime> effect)
	{
		ArgumentNullException.ThrowIfNull(effect);
		var context = new EffectContext<TWorld, TRuntime>(action, world, runtime);
		var current = effect;
		foreach (var rule in _rules)
		{
			if (!rule.Matches(context, current))
				continue;

			current = rule.Transform(context, current)
				?? throw new InvalidOperationException($"Effect rule '{rule.GetType().Name}' returned null.");
		}

		return current;
	}

	public IReadOnlyList<IEffect<TWorld, TRuntime>> React(
		IAction action,
		TWorld world,
		TRuntime runtime,
		IEffect<TWorld, TRuntime> effect,
		IReadOnlyList<IRecord> records)
	{
		if (_reactions.Length == 0)
			return [];

		var context = new EffectContext<TWorld, TRuntime>(action, world, runtime);
		List<IEffect<TWorld, TRuntime>>? followUps = null;
		foreach (var reaction in _reactions)
		{
			var produced = reaction.React(context, effect, records)
				?? throw new InvalidOperationException($"Effect reaction '{reaction.GetType().Name}' returned null.");
			if (produced.Count == 0)
				continue;
			if (produced.Any(followUp => followUp is null))
				throw new InvalidOperationException($"Effect reaction '{reaction.GetType().Name}' returned a null effect.");
			(followUps ??= []).AddRange(produced);
		}

		return followUps ?? [];
	}
}
