using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

/// <summary>
/// Presents pilot barks one at a time at dynamically resolved screen anchors.
/// Owns no gameplay logic: callers decide when and what a pilot says; scenes supply portrait or projected ship anchors.
/// </summary>
public sealed partial class PilotSpeechHost : CanvasLayer
{
	private const float AnchorGap = 6f;
	private const float ViewportPadding = 8f;

	public readonly record struct Anchor(Rect2 Rect, bool Above = false);

	private readonly Func<string, Anchor?> _resolveAnchor;
	private readonly Control _root;
	private readonly Queue<(string PilotId, string Text)> _pending = new();
	private PilotSpeechBubble? _bubble;
	private string? _speakerId;
	private Action? _whenIdle;

	public void WhenIdle(Action completed)
	{
		if (_bubble is null && _pending.Count == 0)
			completed();
		else
			_whenIdle += completed;
	}

	public override void _ExitTree() => Clear();

	public PilotSpeechHost(Func<string, Control?> resolveAnchor, int layer)
		: this(pilotId => PortraitAnchor(resolveAnchor(pilotId)), layer)
	{
		ArgumentNullException.ThrowIfNull(resolveAnchor);
	}

	public PilotSpeechHost(Func<string, Anchor?> resolveAnchor, int layer)
	{
		ArgumentNullException.ThrowIfNull(resolveAnchor);
		_resolveAnchor = resolveAnchor;
		Layer = layer;
		_root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		HudThemes.Apply(_root, HudThemeFamily.Theatrical);
		AddChild(_root);
	}

	/// <summary>Queues behind the current line. Returns false when the pilot has no available anchor.</summary>
	public bool Say(string pilotId, string text)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		if (ResolveAnchorRect(pilotId) is null)
			return false;

		_pending.Enqueue((pilotId, text));
		if (_bubble is null)
			ShowNext();
		return true;
	}

	public void Clear()
	{
		_pending.Clear();
		DropCurrent();
		NotifyIdle();
	}

	public override void _Process(double delta)
	{
		if (_bubble is null || _speakerId is null)
			return;

		if (ResolveAnchorRect(_speakerId) is { } anchor)
		{
			Place(_bubble, anchor);
			return;
		}

		DropCurrent();
		ShowNext();
	}

	private void ShowNext()
	{
		while (_pending.TryDequeue(out var line))
		{
			if (ResolveAnchorRect(line.PilotId) is not { } anchor)
				continue;

			var bubble = new PilotSpeechBubble(line.Text);
			bubble.Finished += () => OnFinished(bubble);
			_bubble = bubble;
			_speakerId = line.PilotId;
			_root.AddChild(bubble);
			Place(bubble, anchor);
			return;
		}
		NotifyIdle();
	}

	private void NotifyIdle()
	{
		var completed = _whenIdle;
		_whenIdle = null;
		completed?.Invoke();
	}

	private void OnFinished(PilotSpeechBubble bubble)
	{
		if (bubble != _bubble)
			return;

		DropCurrent();
		ShowNext();
	}

	private void DropCurrent()
	{
		_bubble?.QueueFree();
		_bubble = null;
		_speakerId = null;
	}

	public static Anchor? PortraitAnchor(Control? anchor)
	{
		if (anchor is null || !anchor.IsVisibleInTree())
			return null;

		var transform = anchor.GetGlobalTransformWithCanvas();
		return new Anchor(new Rect2(transform.Origin, anchor.Size * transform.Scale));
	}

	private Anchor? ResolveAnchorRect(string pilotId) => _resolveAnchor(pilotId);

	private void Place(PilotSpeechBubble bubble, Anchor placement)
	{
		var anchor = placement.Rect;
		var size = bubble.GetCombinedMinimumSize();
		var viewport = _root.GetViewportRect().Size;
		bubble.ShowTail = !placement.Above;
		if (placement.Above)
		{
			bubble.Position = new Vector2(
				Mathf.Clamp(anchor.GetCenter().X - size.X / 2f, ViewportPadding,
					Mathf.Max(ViewportPadding, viewport.X - size.X - ViewportPadding)),
				Mathf.Clamp(anchor.Position.Y - size.Y - AnchorGap, ViewportPadding,
					Mathf.Max(ViewportPadding, viewport.Y - size.Y - ViewportPadding)));
			bubble.Size = size;
			return;
		}
		var x = anchor.End.X + AnchorGap + PilotSpeechBubble.TailWidth;
		var y = Mathf.Clamp(
			anchor.GetCenter().Y - size.Y / 2f,
			ViewportPadding,
			Mathf.Max(ViewportPadding, viewport.Y - size.Y - ViewportPadding));
		bubble.Position = new Vector2(x, y);
		bubble.Size = size;
		bubble.TailY = anchor.GetCenter().Y - y;
	}
}
