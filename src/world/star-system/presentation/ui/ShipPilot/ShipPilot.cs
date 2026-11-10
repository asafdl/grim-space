using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : Control
{
	private const int PortraitSize = 64;
	private const float FrameSize = PortraitSize + 8f;
	private const float PortraitHeight = FrameSize * 0.8f;
	private const float RosterReticleSize = 60f;
	private static readonly Vector2 BarSize = new(6, PortraitHeight);

	private Label _levelLabel = null!;
	private LevelUpButton _levelUpButton = null!;
	private TextureRect _portrait = null!;
	private ProgressBar _xpBar = null!;
	private readonly ShipPicker _shipPicker = new();
	private Control _shipAnchor = null!;
	private HudTooltipHost? _shipReticle;
	private IEnumerable<ShipInstance> _candidateShips = [];
	private (ShipInstance? Ship, int Hull, int BaseHull, int Shield, int BaseShield)? _shipStatus;
	private string? _portraitId;
	private string? _pilotId;

	public event Action<string>? PilotSheetRequested;
	public event Action<string, string>? ShipAssignmentRequested;

	public Control Portrait => _portrait;

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
		ShipInstance? ship,
		IEnumerable<ShipInstance> candidateShips)
	{
		_pilotId = pilotId;
		_candidateShips = candidateShips;
		if (!string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
		{
			_portrait.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
			_portraitId = portraitId;
		}
		_levelLabel.Text = $"LV {claimedLevel}";
		_levelUpButton.Visible = canLearnSkill;
		var shipStatus = (ship, ship?.CurrentHullPoints ?? 0, ship?.Loadout.BaseHullPoints ?? 0,
			ship?.TotalCurrentShieldPoints ?? 0, ship?.TotalBaseShieldPoints ?? 0);
		if (_shipStatus != shipStatus)
		{
			_shipStatus = shipStatus;
			if (_shipReticle is not null)
			{
				_shipAnchor.RemoveChild(_shipReticle);
				_shipReticle.QueueFree();
			}
			_shipReticle = _shipPicker.CreateReticle(
				ship,
				() => _candidateShips,
				candidate => ShipRecruitmentDisplay.CompareTooltip(candidate, ship),
				candidate => ShipAssignmentRequested?.Invoke(pilotId, candidate.Id));
			_shipAnchor.AddChild(_shipReticle);
			_shipReticle.Scale = Vector2.One * (RosterReticleSize / ShipPicker.ReticleSize);
		}
		_shipReticle!.SetTooltip(ship is null
			? HudTooltipContent.Plain("No ship - cannot deploy\nRight-click to assign a ship")
			: ShipRecruitmentDisplay.ShipTooltip(ship));
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		if (xpToNext > 0)
			ShipBox.SetBar(_xpBar, xpInLevel, xpToNext);
		else
		{
			_xpBar.MaxValue = 1;
			_xpBar.Value = 1;
		}
		var xpLine = xpToNext > 0
			? $"XP {xpInLevel}/{xpToNext}"
			: "XP max";
		HudTooltipCard.SetTooltip(
			_portrait,
			$"{pilotName}\n"
			+ $"Lv {claimedLevel} · {xpLine}\n"
			+ (canLearnSkill ? "Level up available" : ""));
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		var portraitPosition = new Vector2(FrameSize * 0.05f, FrameSize * 0.1f);
		var portraitSize = new Vector2(FrameSize * 0.9f, PortraitHeight);
		var reticlePosition = portraitPosition + portraitSize - Vector2.One * RosterReticleSize / 2f;
		CustomMinimumSize = reticlePosition + Vector2.One * RosterReticleSize;
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		AddChild(_shipPicker);

		AddChild(ShipBox.Frame(Vector2.Zero, FrameSize, HudPalettes.BlueCyan.Secondary));
		var portraitFrame = new Control
		{
			Position = portraitPosition,
			Size = portraitSize,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(portraitFrame);

		_portrait = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_portrait.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_portrait.GuiInput += OnPortraitGuiInput;
		portraitFrame.AddChild(_portrait);

		_xpBar = ShipBox.CreateBar(HudPalettes.PurpleRed.Secondary, BarSize);
		_xpBar.AddThemeStyleboxOverride("background", new StyleBoxEmpty());
		_xpBar.Size = BarSize;
		_xpBar.FillMode = (int)ProgressBar.FillModeEnum.BottomToTop;
		portraitFrame.AddChild(_xpBar);

		_levelLabel = new Label
		{
			OffsetLeft = BarSize.X + 1,
			OffsetTop = PortraitHeight - 17,
			OffsetRight = 29,
			OffsetBottom = PortraitHeight - 1,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_levelLabel.AddThemeFontSizeOverride("font_size", 10);
		_levelLabel.AddThemeColorOverride("font_color", Colors.White);
		portraitFrame.AddChild(_levelLabel);

		_levelUpButton = new LevelUpButton
		{
			OffsetLeft = PortraitSize - 29,
			OffsetTop = 1,
			OffsetRight = PortraitSize - 1,
			OffsetBottom = 31,
			Visible = false,
		};
		HudTooltipCard.SetTooltip(_levelUpButton, "Level up");
		_levelUpButton.Pressed += OnLevelUpPressed;
		portraitFrame.AddChild(_levelUpButton);

		_shipAnchor = new Control
		{
			Position = reticlePosition,
			Size = Vector2.One * RosterReticleSize,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_shipAnchor);
	}

	private void OnPortraitGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
			&& _pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private void OnLevelUpPressed()
	{
		if (_pilotId is not null)
			PilotSheetRequested?.Invoke(_pilotId);
	}

	private sealed partial class LevelUpButton : Button
	{
		public LevelUpButton()
		{
			FocusMode = FocusModeEnum.None;
			MouseDefaultCursorShape = CursorShape.PointingHand;
			foreach (var state in new[] { "normal", "hover", "pressed", "focus" })
				AddThemeStyleboxOverride(state, new StyleBoxEmpty());

			var chevron = new PilotLevelUpChevron();
			chevron.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			chevron.GrowHorizontal = GrowDirection.Both;
			chevron.GrowVertical = GrowDirection.Both;
			AddChild(chevron);
		}
	}
}
