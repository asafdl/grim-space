using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class DockyardSceneView : Control
{
	private static readonly Vector2 DesignViewportSize = new(1920f, 1080f);

	private TextureRect _background = null!;
	private ServiceButtonView _salesman = null!;
	private ServiceButtonView _shieldRecharge = null!;
	private Rect2 _designSalesmanRect;
	private Rect2 _designShieldRechargeRect;

	public event Action? SalesmanClicked;
	public event Action? ShieldRechargeClicked;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_salesman = GetNode<ServiceButtonView>("Salesman");
		_shieldRecharge = GetNode<ServiceButtonView>("ShieldRecharge");
		_designSalesmanRect = new Rect2(_salesman.Position, _salesman.Size);
		_designShieldRechargeRect = new Rect2(_shieldRecharge.Position, _shieldRecharge.Size);
		_salesman.Pressed += () => SalesmanClicked?.Invoke();
		_shieldRecharge.Pressed += () => ShieldRechargeClicked?.Invoke();

		Resized += LayoutServiceButtons;
		Callable.From(LayoutServiceButtons).CallDeferred();
	}

	public override void _ExitTree() => Resized -= LayoutServiceButtons;

	private void LayoutServiceButtons()
	{
		if (_background.Texture is not { } texture)
			return;

		var textureSize = texture.GetSize();
		var designBackdrop = AspectFitRect(DesignViewportSize, textureSize);
		var runtimeBackdrop = AspectFitRect(Size, textureSize);
		LayoutServiceButton(_salesman, _designSalesmanRect, designBackdrop, runtimeBackdrop);
		LayoutServiceButton(_shieldRecharge, _designShieldRechargeRect, designBackdrop, runtimeBackdrop);
	}

	private static void LayoutServiceButton(
		ServiceButtonView button,
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
