using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : PanelContainer
{
	private const int PortraitSize = 64;
	private const int BarWidth = 64;

	private static readonly Color DefaultBorderColor =
		HudPalettes.WithAlpha(HudPalettes.BlueCyan.Border, 0.8f);
	private static readonly Color SelectedBorderColor = HudPalettes.BlueCyan.Primary;

	private Label _levelLabel = null!;
	private LevelUpButton _levelUpButton = null!;
	private TextureRect _portrait = null!;
	private ProgressBar _hullBar = null!;
	private string? _portraitId;
	private string? _pilotId;
	private string? _shipId;
	private bool _selected;

	public event Action<string>? Selected;
	public event Action<string>? PilotSheetRequested;

	public override void _Ready()
	{
		Build();
	}

	public void SetState(
		string pilotId,
		string pilotName,
		string portraitId,
		int experiencePoints,
		int claimedLevel,
		bool canLearnSkill,
		ShipInstance ship,
		bool selected)
	{
		_pilotId = pilotId;
		_shipId = ship.Id;
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		_levelLabel.Text = $"LV {claimedLevel}";
		_levelUpButton.Visible = canLearnSkill;
		SetBar(_hullBar, ship.HullPoints, ship.Loadout.MaxHullPoints);
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		var xpLine = xpToNext > 0
			? $"XP {xpInLevel}/{xpToNext}"
			: "XP max";
		HudTooltipCard.SetTooltip(
			_portrait,
			$"{pilotName}\n"
			+ $"Lv {claimedLevel} · {xpLine}\n"
			+ (canLearnSkill ? "Level up available\n" : "")
			+ $"{ship.Spec.Chassis}\n"
			+ $"HP {ship.HullPoints}/{ship.Loadout.MaxHullPoints}\n"
			+ $"SP {ship.TotalCurrentShieldPoints}/{ship.TotalMaxShieldPoints}");
		SetSelected(selected);
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(PortraitSize + 8, 0);
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		ApplyPanelStyle(selected: false);

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 4);
		AddChild(margin);

		var column = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddThemeConstantOverride("separation", 3);
		margin.AddChild(column);

		var portraitFrame = new Control
		{
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddChild(portraitFrame);

		_portrait = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_portrait.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_portrait.GuiInput += OnPortraitGuiInput;
		portraitFrame.AddChild(_portrait);

		_levelLabel = new Label
		{
			OffsetLeft = 1,
			OffsetTop = PortraitSize - 17,
			OffsetRight = 29,
			OffsetBottom = PortraitSize - 1,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_levelLabel.AddThemeFontSizeOverride("font_size", 10);
		_levelLabel.AddThemeColorOverride("font_color", Colors.White);
		portraitFrame.AddChild(_levelLabel);

		_levelUpButton = new LevelUpButton
		{
			TooltipText = "Choose a pilot skill",
			OffsetLeft = PortraitSize - 29,
			OffsetTop = PortraitSize - 31,
			OffsetRight = PortraitSize - 1,
			OffsetBottom = PortraitSize - 1,
			Visible = false,
		};
		_levelUpButton.Pressed += OnLevelUpPressed;
		portraitFrame.AddChild(_levelUpButton);

		_hullBar = CreateBar(new Color(0.85f, 0.15f, 0.18f));
		column.AddChild(_hullBar);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouseButton)
			return;

		if (mouseButton.ButtonIndex == MouseButton.Left && _shipId is not null)
			Selected?.Invoke(_shipId);
		else if (mouseButton.ButtonIndex == MouseButton.Right && _pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private void OnLevelUpPressed()
	{
		if (_pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
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
				BgColor = HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.9f),
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
		bar.AddThemeStyleboxOverride(
			"background",
			MakeBarStyle(HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.95f)));
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

	private sealed partial class LevelUpButton : Button
	{
		private static readonly Color Shadow =
			HudPalettes.WithAlpha(HudPalettes.PurpleRed.DeepSurface, 0.9f);
		private static readonly Color Lavender = HudPalettes.PurpleRed.Primary;
		private static readonly Color Purple = HudPalettes.PurpleRed.Secondary;
		private static readonly Color Crimson = HudPalettes.PurpleRed.Highlight;

		private float _pulseTime;

		public LevelUpButton()
		{
			FocusMode = FocusModeEnum.None;
			MouseDefaultCursorShape = CursorShape.PointingHand;
			foreach (var state in new[] { "normal", "hover", "pressed", "focus" })
				AddThemeStyleboxOverride(state, new StyleBoxEmpty());
		}

		public override void _Process(double delta)
		{
			_pulseTime += (float)delta;
			var pulse = (Mathf.Sin(_pulseTime * Mathf.Tau / 1.35f) + 1f) * 0.5f;
			var scale = Mathf.Lerp(0.94f, 1.08f, pulse);
			PivotOffset = Size * 0.5f;
			Scale = Vector2.One * scale;
			Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(0.78f, 1f, pulse));
		}

		public override void _Draw()
		{
			var top = new[] { new Vector2(4f, 9f), new Vector2(14f, 2f), new Vector2(24f, 9f) };
			var middle = new[] { new Vector2(4f, 16f), new Vector2(14f, 9f), new Vector2(24f, 16f) };
			var arrow = new[]
			{
				new Vector2(14f, 13f),
				new Vector2(26f, 22f),
				new Vector2(20f, 22f),
				new Vector2(20f, 29f),
				new Vector2(8f, 29f),
				new Vector2(8f, 22f),
				new Vector2(2f, 22f),
			};

			DrawChevron(top, Shadow, 7f);
			DrawChevron(top, Lavender, 4f);
			DrawChevron(middle, Shadow, 7f);
			DrawChevron(middle, Purple, 4f);
			DrawPolygon(arrow, Enumerable.Repeat(Shadow, arrow.Length).ToArray());
			DrawPolyline([.. arrow, arrow[0]], Shadow, 3f, true);

			var insetArrow = new[]
			{
				new Vector2(14f, 15.5f),
				new Vector2(23f, 21.5f),
				new Vector2(18f, 21.5f),
				new Vector2(18f, 27f),
				new Vector2(10f, 27f),
				new Vector2(10f, 21.5f),
				new Vector2(5f, 21.5f),
			};
			DrawPolygon(insetArrow, Enumerable.Repeat(Crimson, insetArrow.Length).ToArray());
		}

		private void DrawChevron(Vector2[] points, Color color, float width)
		{
			DrawLine(points[0], points[1], color, width, true);
			DrawLine(points[1], points[2], color, width, true);
		}
	}
}
