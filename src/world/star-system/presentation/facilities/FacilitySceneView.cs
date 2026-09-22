using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class FacilitySceneView : Control
{
	private static readonly Vector2 DesignViewportSize = new(1920f, 1080f);

	private TextureRect _background = null!;
	private readonly List<(FacilityOperatorButtonView Button, Rect2 DesignRect)> _operatorButtons = [];

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		foreach (var child in GetChildren())
		{
			if (child is not FacilityOperatorButtonView button)
				continue;

			_operatorButtons.Add((button, new Rect2(button.Position, button.Size)));
		}

		Resized += LayoutOperatorButtons;
		Callable.From(LayoutOperatorButtons).CallDeferred();
	}

	public override void _ExitTree() => Resized -= LayoutOperatorButtons;

	private void LayoutOperatorButtons()
	{
		if (_background.Texture is not { } texture)
			return;

		var textureSize = texture.GetSize();
		var designBackdrop = AspectFitRect(DesignViewportSize, textureSize);
		var runtimeBackdrop = AspectFitRect(Size, textureSize);

		foreach (var (button, designRect) in _operatorButtons)
			LayoutOperatorButton(button, designRect, designBackdrop, runtimeBackdrop);
	}

	private static void LayoutOperatorButton(
		FacilityOperatorButtonView button,
		Rect2 designRect,
		Rect2 designBackdrop,
		Rect2 runtimeBackdrop)
	{
		var relativePosition = (designRect.Position - designBackdrop.Position) / designBackdrop.Size;
		var relativeSize = designRect.Size / designBackdrop.Size;
		button.Position = runtimeBackdrop.Position + runtimeBackdrop.Size * relativePosition;
		button.Size = runtimeBackdrop.Size * relativeSize;
	}

	private static Rect2 AspectFitRect(Vector2 containerSize, Vector2 contentSize)
	{
		var scale = Mathf.Min(
			containerSize.X / contentSize.X,
			containerSize.Y / contentSize.Y);
		var size = contentSize * scale;
		return new Rect2((containerSize - size) * 0.5f, size);
	}
}
