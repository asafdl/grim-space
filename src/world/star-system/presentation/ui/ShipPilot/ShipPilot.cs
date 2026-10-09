using Godot;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 88;
	private const int BarWidth = 88;

	private static readonly Color DefaultBorderColor = new(0.3f, 0.42f, 0.6f, 0.8f);
	private static readonly Color SelectedBorderColor = new(0.55f, 0.78f, 1f, 1f);

	private TextureRect _portrait = null!;
	private ProgressBar _hullBar = null!;
	private ProgressBar _shieldBar = null!;
	private string? _portraitId;
	private string? _shipId;
	private bool _selected;

	public event Action<string>? Selected;

	public override void _Ready()
	{
		Build();
	}

	public void SetState(string shipId, string portraitId, ShipInstance ship, bool selected)
	{
		_shipId = shipId;
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		SetBar(_hullBar, ship.HullPoints, ship.Loadout.MaxHullPoints);
		SetBar(_shieldBar, ship.TotalCurrentShieldPoints, ship.TotalMaxShieldPoints);
		_portrait.TooltipText =
			$"{ship.Spec.Chassis} Pilot\n"
			+ $"HP {ship.HullPoints}/{ship.Loadout.MaxHullPoints}\n"
			+ $"SP {ship.TotalCurrentShieldPoints}/{ship.TotalMaxShieldPoints}";
		SetSelected(selected);
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ApplyPanelStyle(selected: false);

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		var column = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddThemeConstantOverride("separation", 5);
		margin.AddChild(column);

		_portrait = new TextureRect
		{
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_portrait.GuiInput += OnPortraitGuiInput;
		column.AddChild(_portrait);
		_hullBar = CreateBar(new Color(0.85f, 0.15f, 0.18f));
		_shieldBar = CreateBar(new Color(0.25f, 0.62f, 0.95f));
		column.AddChild(_hullBar);
		column.AddChild(_shieldBar);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			return;
		if (_shipId is null)
			return;

		Selected?.Invoke(_shipId);
	}

	private void SetSelected(bool selected)
	{
		if (_selected == selected)
			return;

		_selected = selected;
		ApplyPanelStyle(selected);
	}

	private void ApplyPanelStyle(bool selected)
	{
		var borderColor = selected ? SelectedBorderColor : DefaultBorderColor;
		var borderWidth = selected ? 2 : 1;
		AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor = new Color(0.035f, 0.05f, 0.08f, 0.9f),
				BorderColor = borderColor,
				BorderWidthLeft = borderWidth,
				BorderWidthTop = borderWidth,
				BorderWidthRight = borderWidth,
				BorderWidthBottom = borderWidth,
				CornerRadiusTopLeft = 4,
				CornerRadiusTopRight = 4,
				CornerRadiusBottomRight = 4,
				CornerRadiusBottomLeft = 4,
			});
	}

	private static ProgressBar CreateBar(Color color)
	{
		var bar = new ProgressBar
		{
			CustomMinimumSize = new Vector2(BarWidth, 6),
			ShowPercentage = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		bar.AddThemeStyleboxOverride("fill", MakeBarStyle(color));
		bar.AddThemeStyleboxOverride("background", MakeBarStyle(new Color(0.08f, 0.1f, 0.14f, 0.95f)));
		return bar;
	}

	private static void SetBar(ProgressBar bar, int current, int max)
	{
		max = Mathf.Max(0, max);
		current = Mathf.Clamp(current, 0, max);
		bar.MaxValue = max;
		bar.Value = current;
	}

	private static StyleBoxFlat MakeBarStyle(Color color) =>
		new()
		{
			BgColor = color,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};
}
