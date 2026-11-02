using Godot;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>
/// Screen-space facility marker: pin from a model anchor to a floating map icon button.
/// </summary>
public partial class FacilityFacadeCallout : Control
{
	public static readonly Color IconBorderColor =
		HudPalettes.WithAlpha(HudPalettes.Gold.Highlight, 0.85f);
	public static readonly Vector2 IconSize = new(48f, 48f);
	public const float IconFloatPixels = 180f;

	private static readonly Color AnchorDiamondColor =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Highlight, 0.95f);

	private const string ContractIndicatorPath = "res://assets/ui/map/contract-offer-icon.svg";
	private const string LeadIndicatorPath = "res://assets/ui/map/lead-contact-icon.svg";
	private const string DeliveryIndicatorPath = "res://assets/ui/map/delivery-target-icon.png";
	private static readonly Color DeliveryIndicatorTint = new(0x4a / 255f, 0xd5 / 255f, 0xf0 / 255f);
	private const float RoleIndicatorSize = 32f;
	private const float RoleIndicatorGap = 3f;
	private const float RoleIndicatorRowGap = 4f;
	private const float PinWidth = 2f;
	private const float AnchorDiamondRadius = 4.5f;
	private const float ViewportMargin = 8f;

	private readonly Button _button;
	private readonly TextureRect _contractIndicator;
	private readonly TextureRect _leadIndicator;
	private readonly TextureRect _deliveryIndicator;
	private bool _contractIndicatorEnabled;
	private bool _leadIndicatorEnabled;
	private bool _deliveryIndicatorEnabled;
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
		_contractIndicator = CreateRoleIndicator(ContractIndicatorPath);
		_leadIndicator = CreateRoleIndicator(LeadIndicatorPath);
		_deliveryIndicator = CreateRoleIndicator(DeliveryIndicatorPath, DeliveryIndicatorTint);
		AddChild(_contractIndicator);
		AddChild(_leadIndicator);
		AddChild(_deliveryIndicator);
	}

	public void SetContractIndicatorVisible(bool visible) =>
		_contractIndicatorEnabled = visible;

	public void SetLeadIndicatorVisible(bool visible) =>
		_leadIndicatorEnabled = visible;

	public void SetDeliveryIndicatorVisible(bool visible) =>
		_deliveryIndicatorEnabled = visible;

	private static TextureRect CreateRoleIndicator(string path, Color? tint = null) =>
		new()
		{
			Texture = tint is { } color
				? SvgIconLoader.Load(path, color, (int)RoleIndicatorSize)
				: SvgIconLoader.LoadRaw(path, (int)RoleIndicatorSize),
			CustomMinimumSize = new Vector2(RoleIndicatorSize, RoleIndicatorSize),
			Size = new Vector2(RoleIndicatorSize, RoleIndicatorSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};

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
			_leadIndicator.Visible = false;
			_deliveryIndicator.Visible = false;
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
		LayoutRoleIndicators(position, size);
		_contractIndicator.Visible = _contractIndicatorEnabled;
		_leadIndicator.Visible = _leadIndicatorEnabled;
		_deliveryIndicator.Visible = _deliveryIndicatorEnabled;

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

	private void LayoutRoleIndicators(Vector2 buttonPosition, Vector2 buttonSize)
	{
		var shown = new List<TextureRect>(3);
		if (_contractIndicatorEnabled)
			shown.Add(_contractIndicator);
		if (_leadIndicatorEnabled)
			shown.Add(_leadIndicator);
		if (_deliveryIndicatorEnabled)
			shown.Add(_deliveryIndicator);

		var top = buttonPosition.Y - RoleIndicatorSize - RoleIndicatorGap;
		var gapCount = shown.Count > 0 ? shown.Count - 1 : 0;
		var rowWidth = shown.Count * RoleIndicatorSize + gapCount * RoleIndicatorRowGap;
		var left = buttonPosition.X + buttonSize.X * 0.5f - rowWidth * 0.5f;
		for (var i = 0; i < shown.Count; i++)
		{
			shown[i].Position = new Vector2(
				left + i * (RoleIndicatorSize + RoleIndicatorRowGap),
				top);
		}
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
