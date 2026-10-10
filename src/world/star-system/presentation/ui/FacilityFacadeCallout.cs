using Godot;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>
/// Screen-space facility marker: pin from a model anchor to a floating map icon button.
/// </summary>
public partial class FacilityFacadeCallout : Control
{
	public static readonly Color IconBorderColor = new(0.94f, 0.91f, 0.84f, 0.85f);
	public static readonly Vector2 IconSize = new(48f, 48f);
	public const float IconFloatPixels = 180f;

	private static readonly Color AnchorDiamondColor = new(0.92f, 0.28f, 0.24f, 0.95f);

	private const string ContractIndicatorPath = "res://assets/ui/map/contract-offer-icon.svg";
	private const float ContractIndicatorSize = 16f;
	private const float ContractIndicatorGap = 3f;
	private const float PinWidth = 2f;
	private const float AnchorDiamondRadius = 4.5f;
	private const float ViewportMargin = 8f;

	private readonly Button _button;
	private readonly TextureRect _contractIndicator;
	private bool _contractIndicatorEnabled;
	private Vector2 _pinStart;
	private Vector2 _pinEnd;
	private bool _pinVisible;

	public Button Button => _button;

	public FacilityFacadeCallout()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		_button = new Button
		{
			MouseFilter = MouseFilterEnum.Stop,
			ThemeTypeVariation = "MapIcon",
			ExpandIcon = true,
			CustomMinimumSize = IconSize,
		};
		AddChild(_button);
		_contractIndicator = new TextureRect
		{
			Texture = SvgIconLoader.LoadRaw(ContractIndicatorPath, (int)ContractIndicatorSize),
			CustomMinimumSize = new Vector2(ContractIndicatorSize, ContractIndicatorSize),
			Size = new Vector2(ContractIndicatorSize, ContractIndicatorSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		AddChild(_contractIndicator);
	}

	public void SetContractIndicatorVisible(bool visible) =>
		_contractIndicatorEnabled = visible;

	public void UpdateLayout(
		Vector2 anchorScreen,
		Vector2 iconCenterScreen,
		float viewportWidth,
		float viewportHeight,
		bool visible)
	{
		if (!visible)
		{
			_pinVisible = false;
			_button.Visible = false;
			_contractIndicator.Visible = false;
			QueueRedraw();
			return;
		}

		_button.ResetSize();
		var size = _button.Size;
		var position = FacilityCalloutScreenLayout.ClampIconTopLeft(
			iconCenterScreen,
			size,
			ViewportMargin,
			viewportWidth,
			viewportHeight);
		_button.Position = position;
		_button.Visible = true;
		_contractIndicator.Position = position + new Vector2(
			(size.X - ContractIndicatorSize) * 0.5f,
			-ContractIndicatorSize - ContractIndicatorGap);
		_contractIndicator.Visible = _contractIndicatorEnabled;

		_pinStart = anchorScreen;
		_pinEnd = position + new Vector2(size.X * 0.5f, size.Y);
		_pinVisible = true;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_pinVisible)
			return;

		DrawLine(_pinStart, _pinEnd, IconBorderColor, PinWidth);
		DrawColoredPolygon(DiamondPoints(_pinStart), AnchorDiamondColor);
	}

	private static Vector2[] DiamondPoints(Vector2 center)
	{
		var r = AnchorDiamondRadius;
		return
		[
			center + new Vector2(0f, -r),
			center + new Vector2(r, 0f),
			center + new Vector2(0f, r),
			center + new Vector2(-r, 0f),
		];
	}
}
