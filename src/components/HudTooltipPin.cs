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
	private HudTooltipCard? _child;
	private string? _childIcon;
	private double _closeCountdown = -1;

	public override void _Process(double delta)
	{
		if (_pinned)
		{
			if (!Input.IsKeyPressed(Key.Alt) || GetParent() is not Control source || !source.IsVisibleInTree())
			{
				QueueFree();
				return;
			}

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

		if (!Input.IsKeyPressed(Key.Alt) || GetTree().GetNodesInGroup(PinnedGroup).Count > 0)
			return;

		var original = (Control)GetParent();
		var popup = original.GetWindow();
		if (popup.GetParent() is not Control owner)
			return;

		var viewport = owner.GetViewport();
		var position = PinnedPosition(
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
		_parent = HudTooltipCard.Create(content, palette, pinned: true);
		_parent.Position = position;
		_parent.IconHoverStarted += OnIconHoverStarted;
		_parent.IconHoverEnded += OnIconHoverEnded;
		_layer.AddChild(_parent);
		popup.QueueFree();
	}

	private void OnIconHoverStarted(string icon)
	{
		var iconTooltip = HudTooltipIconCatalog.Resolve(icon).Tooltip(icon, content.IconTooltips);
		if (iconTooltip is null)
			return;

		_closeCountdown = -1;
		if (_child is not null && _childIcon == icon)
			return;
		if (_child is not null)
			CloseChild();

		_childIcon = icon;
		_child = HudTooltipCard.Create(iconTooltip, palette, pinned: true, headerIcon: icon);
		_child.MouseFilter = Control.MouseFilterEnum.Stop;
		_layer!.AddChild(_child);
		PositionChild();
	}

	private void OnIconHoverEnded(string icon)
	{
		if (icon == _childIcon && _child is not null)
			_closeCountdown = CloseDelay;
	}

	private void CloseChild()
	{
		_child!.QueueFree();
		_child = null;
		_childIcon = null;
		_closeCountdown = -1;
	}

	private void PositionChild()
	{
		var parent = new Rect2(_parent!.Position, _parent.Size);
		_child!.Position = ChildPosition(parent, _child.Size, GetViewport().GetVisibleRect());
	}

	internal static Vector2 PinnedPosition(
		Vector2 popupPosition,
		bool embedded,
		Vector2 windowPosition,
		Transform2D viewportTransform) =>
		embedded
			? popupPosition
			: viewportTransform.AffineInverse() * (popupPosition - windowPosition);

	internal static Vector2 ChildPosition(Rect2 parent, Vector2 size, Rect2 viewport)
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
