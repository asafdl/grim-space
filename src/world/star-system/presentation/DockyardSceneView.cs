using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class DockyardSceneView : Control
{
	private static readonly Vector2 DesignViewportSize = new(1920f, 1080f);

	private TextureRect _background = null!;
	private ServiceButtonView _salesman = null!;
	private Rect2 _designSalesmanRect;

	public event Action? SalesmanClicked;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_salesman = GetNode<ServiceButtonView>("Salesman");
		_designSalesmanRect = new Rect2(_salesman.Position, _salesman.Size);
		_salesman.Pressed += () => SalesmanClicked?.Invoke();

		Resized += LayoutSalesman;
		Callable.From(LayoutSalesman).CallDeferred();
	}

	public override void _ExitTree() => Resized -= LayoutSalesman;

	private void LayoutSalesman()
	{
		if (_background.Texture is not { } texture)
			return;

		var textureSize = texture.GetSize();
		var designBackdrop = AspectFitRect(DesignViewportSize, textureSize);
		var runtimeBackdrop = AspectFitRect(Size, textureSize);
		var relativePosition =
			(_designSalesmanRect.Position - designBackdrop.Position) / designBackdrop.Size;
		var relativeSize = _designSalesmanRect.Size / designBackdrop.Size;

		_salesman.Position =
			runtimeBackdrop.Position + runtimeBackdrop.Size * relativePosition;
		_salesman.Size = runtimeBackdrop.Size * relativeSize;
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
