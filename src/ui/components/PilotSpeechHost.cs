using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

/// <summary>
/// Presents pilot barks as speech bubbles beside the pilot's portrait, one at a time.
/// Owns no gameplay logic: callers decide when and what a pilot says; the scene supplies where that pilot's portrait is.
/// </summary>
public sealed partial class PilotSpeechHost : CanvasLayer
{
	private const float AnchorGap = 6f;
	private const float ViewportPadding = 8f;

	private readonly Func<string, Control?> _resolveAnchor;
	private readonly Control _root;
	private readonly Queue<(string PilotId, string Text)> _pending = new();
	private PilotSpeechBubble? _bubble;
	private string? _speakerId;

	public PilotSpeechHost(Func<string, Control?> resolveAnchor, int layer)
	{
		ArgumentNullException.ThrowIfNull(resolveAnchor);
		_resolveAnchor = resolveAnchor;
		Layer = layer;
		_root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		HudThemes.Apply(_root, HudThemeFamily.Theatrical);
		AddChild(_root);
	}

	/// <summary>Queues behind the current line. Returns false when the pilot has no visible portrait.</summary>
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

	private Rect2? ResolveAnchorRect(string pilotId)
	{
		var anchor = _resolveAnchor(pilotId);
		if (anchor is null || !anchor.IsVisibleInTree())
			return null;

		var transform = anchor.GetGlobalTransformWithCanvas();
		return new Rect2(transform.Origin, anchor.Size * transform.Scale);
	}

	private void Place(PilotSpeechBubble bubble, Rect2 anchor)
	{
		var size = bubble.GetCombinedMinimumSize();
		var viewport = _root.GetViewportRect().Size;
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
