using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Tutorials;

public sealed class TutorialGhostPresenter(PosedUnitGhostView view) : IDisposable
{
	private PosedUnitGhostView? _view = view ?? throw new ArgumentNullException(nameof(view));
	private PosedUnitGhostSpec? _spec;
	private bool _suppressed;

	public void SetSpec(PosedUnitGhostSpec? spec)
	{
		_spec = spec;
		_suppressed = false;
		Apply();
	}

	public void Suppress()
	{
		_suppressed = true;
		Apply();
	}

	public void Restore()
	{
		_suppressed = false;
		Apply();
	}

	private void Apply()
	{
		if (_view is { } current)
			current.Apply(_suppressed ? null : _spec);
	}

	public void Dispose()
	{
		if (_view is not { } current)
			return;

		if (GodotObject.IsInstanceValid(current))
			current.QueueFree();
		_view = null;
	}
}
