using Godot;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Owns unit views and applies backend <see cref="State"/> snapshots via in-place <see cref="UnitView.Sync"/>.
/// Shared by planning preview and turn playback.
/// </summary>
public partial class BattleView : Node3D
{
	private readonly Dictionary<string, UnitView> _unitViews = new();

	public IReadOnlyDictionary<string, UnitView> UnitViews => _unitViews;

	public void BindInitial(IEnumerable<(string id, State state, Color color)> units)
	{
		foreach (var (id, state, color) in units)
			Ensure(state, color);
	}

	public void Ensure(State state, Color color)
	{
		if (_unitViews.ContainsKey(state.Id))
			return;

		var view = new UnitView();
		view.Bind(state, color);
		AddChild(view);
		_unitViews[state.Id] = view;
	}

	public void ApplyUnitStates(
		IReadOnlyDictionary<string, State> states,
		Func<string, Color>? colorFor = null)
	{
		foreach (var (unitId, state) in states)
		{
			if (!_unitViews.TryGetValue(unitId, out var view))
			{
				Ensure(state, colorFor?.Invoke(unitId) ?? Colors.White);
				view = _unitViews[unitId];
			}

			view.Sync(state);
		}
	}
}
