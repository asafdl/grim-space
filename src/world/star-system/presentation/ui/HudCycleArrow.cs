using Godot;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Cursor arrow used to cycle pilots, pages, or scroll a roster.</summary>
public static class HudCycleArrow
{
	private const string CursorPath = "res://assets/ui/turn_flow/active_cursor.svg";
	private const int Width = 20;
	private const int Height = 16;

	public static Control Create(
		int direction,
		float hostHeight,
		string tooltip,
		Action pressed,
		bool vertical = false)
	{
		var host = new Control
		{
			CustomMinimumSize = vertical ? new Vector2(hostHeight, Width) : new Vector2(Width, hostHeight),
			MouseFilter = Control.MouseFilterEnum.Stop,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		HudTooltipCard.SetTooltip(host, tooltip);
		var iconHost = new Control
		{
			CustomMinimumSize = new Vector2(Width, Height),
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -Width * 0.5f,
			OffsetTop = -Height * 0.5f,
			OffsetRight = Width * 0.5f,
			OffsetBottom = Height * 0.5f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(Width * 0.5f, Height * 0.5f),
			RotationDegrees = vertical
				? (direction < 0 ? 0f : 180f)
				: (direction < 0 ? -90f : 90f),
		};
		iconHost.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>(CursorPath),
			CustomMinimumSize = new Vector2(Width, Height),
			Size = new Vector2(Width, Height),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		host.AddChild(iconHost);
		host.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			{
				host.AcceptEvent();
				pressed();
			}
		};
		return host;
	}
}
