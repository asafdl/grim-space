using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public sealed class ActionEffectPipeline<TWorld, TRuntime, TEffect>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
	where TEffect : IEffect<TWorld, TRuntime>
{
	public static ActionEffectPipeline<TWorld, TRuntime, TEffect> Empty { get; } =
		new([]);

	private readonly IReadOnlyList<IActionEffectModifier<TWorld, TRuntime, TEffect>> _modifiers;

	public ActionEffectPipeline(IEnumerable<IActionEffectModifier<TWorld, TRuntime, TEffect>> modifiers)
	{
		ArgumentNullException.ThrowIfNull(modifiers);
		_modifiers = modifiers.ToArray();
	}

	public IReadOnlyList<IActionEffectModifier<TWorld, TRuntime, TEffect>> Modifiers => _modifiers;

	public bool IsEmpty => _modifiers.Count == 0;

	public IReadOnlyList<TEffect> Apply(
		IAction action,
		TWorld world,
		TRuntime runtime,
		IReadOnlyList<TEffect> effects)
	{
		ArgumentNullException.ThrowIfNull(effects);
		if (_modifiers.Count == 0)
			return effects;

		var modified = new List<TEffect>(effects.Count);
		foreach (var effect in effects)
		{
			var current = effect;
			foreach (var modifier in _modifiers)
			{
				current = modifier.Modify(action, world, runtime, current)
					?? throw new InvalidOperationException(
						$"Action effect modifier '{modifier.GetType().Name}' returned null.");
			}

			modified.Add(current);
		}

		return modified;
	}
}
