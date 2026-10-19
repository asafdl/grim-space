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
	private const float RoleIndicatorSize = 32f;
	private const float RoleIndicatorGap = 3f;
	private const float RoleIndicatorRowGap = 4f;
	private const float PinWidth = 2f;
	private const float AnchorDiamondRadius = 4.5f;
	private const float ViewportMargin = 8f;

	private readonly Button _button;
	private readonly TextureRect _contractIndicator;
	private readonly TextureRect _leadIndicator;
	private bool _contractIndicatorEnabled;
	private bool _leadIndicatorEnabled;
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
		AddChild(_contractIndicator);
		AddChild(_leadIndicator);
	}

	public void SetContractIndicatorVisible(bool visible) =>
		_contractIndicatorEnabled = visible;

	public void SetLeadIndicatorVisible(bool visible) =>
		_leadIndicatorEnabled = visible;

	private static TextureRect CreateRoleIndicator(string path) =>
		new()
		{
			Texture = SvgIconLoader.LoadRaw(path, (int)RoleIndicatorSize),
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
		var top = buttonPosition.Y - RoleIndicatorSize - RoleIndicatorGap;
		var centerX = buttonPosition.X + buttonSize.X * 0.5f;
		if (_contractIndicatorEnabled && _leadIndicatorEnabled)
		{
			var rowLeft = centerX - (RoleIndicatorSize * 2f + RoleIndicatorRowGap) * 0.5f;
			_contractIndicator.Position = new Vector2(rowLeft, top);
			_leadIndicator.Position = new Vector2(
				rowLeft + RoleIndicatorSize + RoleIndicatorRowGap,
				top);
			return;
		}

		var single = new Vector2(centerX - RoleIndicatorSize * 0.5f, top);
		_contractIndicator.Position = single;
		_leadIndicator.Position = single;
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
