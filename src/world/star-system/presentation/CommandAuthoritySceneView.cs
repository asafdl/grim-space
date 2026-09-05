using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class CommandAuthoritySceneView : Control
{
	private static readonly Vector2 DesignViewportSize = new(1920f, 1080f);

	private TextureRect _background = null!;
	private ContractGiverView _giver = null!;
	private Rect2 _designGiverRect;

	public event Action? GiverClicked;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_giver = GetNode<ContractGiverView>("Manager");
		_designGiverRect = new Rect2(_giver.Position, _giver.Size);
		_giver.GiverClicked += () => GiverClicked?.Invoke();

		Resized += LayoutGiver;
		Callable.From(LayoutGiver).CallDeferred();
	}

	public override void _ExitTree() => Resized -= LayoutGiver;

	private void LayoutGiver()
	{
		if (_background.Texture is not { } texture)
			return;

		var textureSize = texture.GetSize();
		var designBackdrop = AspectFitRect(DesignViewportSize, textureSize);
		var runtimeBackdrop = AspectFitRect(Size, textureSize);
		var relativePosition =
			(_designGiverRect.Position - designBackdrop.Position) / designBackdrop.Size;
		var relativeSize = _designGiverRect.Size / designBackdrop.Size;

		_giver.Position =
			runtimeBackdrop.Position + runtimeBackdrop.Size * relativePosition;
		_giver.Size = runtimeBackdrop.Size * relativeSize;
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
