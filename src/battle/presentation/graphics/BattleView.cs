using Godot;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Owns authoritative unit views. Replay-only visual states are driven by the replay player.
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

	public void Remove(string unitId)
	{
		if (!_unitViews.Remove(unitId, out var view))
			return;

		view.QueueFree();
	}

	/// <summary>
	/// Authoritative sync from live or post-replay world: dead units are removed; survivors use <see cref="UnitView.Sync"/>.
	/// </summary>
	public void ApplyUnitStates(
		IReadOnlyDictionary<string, State> states,
		Func<string, Color>? colorFor = null)
	{
		var keep = new HashSet<string>(states.Count);
		foreach (var (unitId, state) in states)
		{
			if (!state.IsAlive)
			{
				Remove(unitId);
				continue;
			}

			keep.Add(unitId);
			if (!_unitViews.TryGetValue(unitId, out var view))
			{
				Ensure(state, colorFor?.Invoke(unitId) ?? Colors.White);
				view = _unitViews[unitId];
			}

			view.Sync(state);
			view.SetHitMarked(false);
		}

		if (keep.Count == _unitViews.Count)
			return;

		foreach (var id in _unitViews.Keys.Where(id => !keep.Contains(id)).ToList())
			Remove(id);
	}

	/// <summary>
	/// Planning / resolving preview: existing views stay live even when preview marks them dead;
	/// new views are created only for preview spawns; views missing from the preview set are removed.
	/// </summary>
	public void ApplyPlanningPreview(
		IReadOnlyDictionary<string, State> states,
		Func<string, Color>? colorFor = null)
	{
		var keep = new HashSet<string>(states.Count);
		foreach (var (unitId, state) in states)
		{
			keep.Add(unitId);
			if (_unitViews.TryGetValue(unitId, out var view))
			{
				view.Present(state, UnitVisualState.Live);
				view.SetHitMarked(false);
				continue;
			}

			if (!state.IsAlive)
				continue;

			Ensure(state, colorFor?.Invoke(unitId) ?? Colors.White);
			view = _unitViews[unitId];
			view.Present(state, UnitVisualState.Live);
			view.SetHitMarked(false);
		}

		foreach (var id in _unitViews.Keys.Where(id => !keep.Contains(id)).ToList())
			Remove(id);
	}

	public void ApplyHitMarks(IReadOnlySet<string> threatenedUnitIds)
	{
		foreach (var (unitId, view) in _unitViews)
			view.SetHitMarked(threatenedUnitIds.Contains(unitId));
	}
}
