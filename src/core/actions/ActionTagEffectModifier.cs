using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public sealed class ActionTagEffectModifier<TWorld, TRuntime, TEffect>
	: IActionEffectModifier<TWorld, TRuntime, TEffect>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
	where TEffect : IEffect<TWorld, TRuntime>
{
	private readonly string[] _requiredTags;
	private readonly IActionEffectModifier<TWorld, TRuntime, TEffect> _inner;

	public ActionTagEffectModifier(
		IEnumerable<string> requiredTags,
		IActionEffectModifier<TWorld, TRuntime, TEffect> inner)
	{
		ArgumentNullException.ThrowIfNull(requiredTags);
		ArgumentNullException.ThrowIfNull(inner);
		_requiredTags = requiredTags.Distinct(StringComparer.Ordinal).ToArray();
		if (_requiredTags.Length == 0 || _requiredTags.Any(string.IsNullOrWhiteSpace))
			throw new ArgumentException(
				"At least one non-empty action tag is required.",
				nameof(requiredTags));

		_inner = inner;
	}

	public TEffect Modify(
		IAction action,
		TWorld world,
		TRuntime runtime,
		TEffect effect) =>
		_requiredTags.All(action.Tags.Contains)
			? _inner.Modify(action, world, runtime, effect)
			: effect;
}
