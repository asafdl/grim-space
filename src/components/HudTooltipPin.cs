using Godot;

namespace GrimSpace.Components;

internal sealed partial class HudTooltipPin(HudTooltipContent content, HudPalette palette) : Node
{
	private const string PinnedGroup = "hud_pinned_tooltip";
	private const float TooltipGap = 4f;
	private const double CloseDelay = 0.3;
	private bool _pinned;
	private CanvasLayer? _layer;
	private HudTooltipCard? _parent;
	private HudTooltipCard? _comparison;
	private HudTooltipCard? _child;
	private HudTooltipCard? _childOrigin;
	private string? _childIcon;
	private bool _comparing;
	private Vector2 _anchor;
	private double _closeCountdown = -1;

	public override void _Process(double delta)
	{
		var comparing = content.Comparison is not null && Input.IsKeyPressed(Key.Shift);
		var pinHeld = comparing || Input.IsKeyPressed(Key.Alt);
		if (_pinned)
		{
			if (!pinHeld || GetParent() is not Control source || !source.IsVisibleInTree())
			{
				QueueFree();
				return;
			}

			if (_comparing != comparing)
				ShowParent(comparing);
			PositionComparison();
			if (_child is not null)
			{
				PositionChild();
				if (_closeCountdown >= 0 && !_child.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
				{
					_closeCountdown -= delta;
					if (_closeCountdown <= 0)
						CloseChild();
				}
			}
			return;
		}

		if (!pinHeld || GetTree().GetNodesInGroup(PinnedGroup).Count > 0)
			return;

		var original = (Control)GetParent();
		var popup = original.GetWindow();
		if (popup.GetParent() is not Control owner)
			return;

		var viewport = owner.GetViewport();
		_anchor = PinnedPosition(
			popup.Position,
			popup.IsEmbedded(),
			owner.GetWindow().Position,
			viewport.GetFinalTransform());

		Reparent(owner, false);
		AddToGroup(PinnedGroup);
		_pinned = true;

		_layer = new CanvasLayer { Layer = 100 };
		AddChild(_layer);
		var backdrop = new Control
		{
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		_layer.AddChild(backdrop);
		backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ShowParent(comparing);
		popup.QueueFree();
	}

	private void ShowParent(bool comparing)
	{
		if (_child is not null)
			CloseChild();
		if (_parent is not null)
		{
			_layer!.RemoveChild(_parent);
			_parent.QueueFree();
		}
		if (_comparison is not null)
		{
			_layer!.RemoveChild(_comparison);
			_comparison.QueueFree();
			_comparison = null;
		}

		_comparing = comparing;
		var shown = comparing ? content.Comparison!.Candidate : content;
		_parent = CreatePinnedCard(shown);
		_parent.Position = _anchor;
		if (comparing)
			_comparison = CreatePinnedCard(content.Comparison!.Current);
		PositionComparison();
	}

	private HudTooltipCard CreatePinnedCard(HudTooltipContent tooltip)
	{
		var card = HudTooltipCard.Create(tooltip, palette, pinned: true);
		card.IconHoverStarted += icon => OnIconHoverStarted(card, tooltip, icon);
		card.IconHoverEnded += icon => OnIconHoverEnded(card, icon);
		_layer!.AddChild(card);
		return card;
	}

	private void OnIconHoverStarted(HudTooltipCard origin, HudTooltipContent tooltip, string icon)
	{
		var iconTooltip = HudTooltipIconCatalog.Resolve(icon).Tooltip(icon, tooltip.IconTooltips);
		if (iconTooltip is null)
			return;

		_closeCountdown = -1;
		if (_child is not null && _childIcon == icon && _childOrigin == origin)
			return;
		if (_child is not null)
			CloseChild();

		_childIcon = icon;
		_childOrigin = origin;
		_child = HudTooltipCard.Create(iconTooltip, palette, pinned: true, headerIcon: icon);
		_child.MouseFilter = Control.MouseFilterEnum.Stop;
		_layer!.AddChild(_child);
		PositionChild();
	}

	private void OnIconHoverEnded(HudTooltipCard origin, string icon)
	{
		if (origin == _childOrigin && icon == _childIcon && _child is not null)
			_closeCountdown = CloseDelay;
	}

	private void CloseChild()
	{
		_child!.QueueFree();
		_child = null;
		_childOrigin = null;
		_childIcon = null;
		_closeCountdown = -1;
	}

	private void PositionComparison()
	{
		if (_comparison is not null)
			_comparison.Position = ChildPosition(
				new Rect2(_parent!.Position, _parent.Size),
				_comparison.Size,
				GetViewport().GetVisibleRect());
	}

	private void PositionChild()
	{
		var origin = _childOrigin!;
		var other = origin == _parent ? _comparison : _parent;
		var avoid = other is null ? (Rect2?)null : new Rect2(other.Position, other.Size);
		_child!.Position = ChildPosition(
			new Rect2(origin.Position, origin.Size),
			_child.Size,
			GetViewport().GetVisibleRect(),
			avoid);
	}

	internal static Vector2 PinnedPosition(
		Vector2 popupPosition,
		bool embedded,
		Vector2 windowPosition,
		Transform2D viewportTransform) =>
		embedded
			? popupPosition
			: viewportTransform.AffineInverse() * (popupPosition - windowPosition);

	internal static Vector2 ChildPosition(Rect2 parent, Vector2 size, Rect2 viewport, Rect2? avoid = null)
	{
		var candidates = new[]
		{
			new Vector2(parent.End.X + TooltipGap, parent.Position.Y),
			new Vector2(parent.Position.X - size.X - TooltipGap, parent.Position.Y),
			new Vector2(parent.Position.X, parent.End.Y + TooltipGap),
			new Vector2(parent.Position.X, parent.Position.Y - size.Y - TooltipGap),
		};
		var maximum = (viewport.End - size).Max(viewport.Position);
		var best = viewport.Position;
		var leastOverlap = float.PositiveInfinity;
		foreach (var candidate in candidates)
		{
			var position = candidate.Clamp(viewport.Position, maximum);
			var intersection = new Rect2(position, size).Intersection(parent);
			var overlap = intersection.Size.X * intersection.Size.Y;
			if (avoid is { } other)
			{
				var otherIntersection = new Rect2(position, size).Intersection(other);
				overlap += otherIntersection.Size.X * otherIntersection.Size.Y;
			}
			if (overlap < leastOverlap)
			{
				best = position;
				leastOverlap = overlap;
			}
		}
		return best;
	}

	public override void _Notification(int what)
	{
		if (_pinned && what == NotificationWMWindowFocusOut)
			QueueFree();
	}
}
