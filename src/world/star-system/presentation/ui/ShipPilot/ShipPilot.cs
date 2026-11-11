using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.Ui.Portraits;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ShipPilot : Control
{
	private const int PortraitSize = 125;
	internal const float FrameSize = PortraitSize + 8f;
	private const float PortraitHeight = FrameSize * 0.8f;
	private const float RosterReticleSize = 60f;
	internal const float RosterWidth = FrameSize * .95f + RosterReticleSize / 2f;
	private static readonly Vector2 BarSize = new(6, PortraitHeight);

	private TextureRect _classIcon = null!;
	private Panel _classBadge = null!;
	private StyleBoxFlat _classBadgeStyle = null!;
	private EOperatorClassId? _classId;
	private LevelUpButton _levelUpButton = null!;
	private PilotPortrait _portrait = null!;
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
		EOperatorClassId classId,
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
			_portrait.SetIdentity(portraitId);
			_portraitId = portraitId;
		}
		if (_classId != classId)
		{
			var classColor = PilotIdentityIconRail.ClassColorFor(classId);
			_classIcon.Texture = SvgIconLoader.LoadCentered(
				PilotIdentityIconRail.ClassIconPathFor(classId), classColor, 20);
			_classBadgeStyle.BorderColor = classColor;
			HudTooltipCard.SetTooltip(_classBadge, classId.DisplayName());
			_classId = classId;
		}
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
				candidate => ShipAssignmentRequested?.Invoke(pilotId, candidate.Id),
				openToRight: true);
			_shipAnchor.AddChild(_shipReticle);
			_shipReticle.Scale = Vector2.One * (RosterReticleSize / ShipPicker.ReticleSize);
		}
		var shipTooltip = ship is null
			? HudTooltipContent.Plain("No ship - cannot deploy")
			: ShipRecruitmentDisplay.ShipTooltip(ship);
		_shipReticle!.SetTooltip(shipTooltip with { Body = $"{shipTooltip.Body}\n[RMB to assign]" });
		var (xpInLevel, xpToNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		if (xpToNext > 0)
			ShipBox.SetBar(_xpBar, xpInLevel, xpToNext);
		else
		{
			_xpBar.MaxValue = 1;
			_xpBar.Value = 1;
		}
		HudTooltipCard.SetTooltip(
			_portrait,
			$"{pilotName}\n"
			+ $"Lv {claimedLevel}"
			+ (canLearnSkill ? "\nLevel up available" : ""));
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

		_portrait = new PilotPortrait
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

		_classBadge = new Panel
		{
			Position = new Vector2(FrameSize - 29, 1),
			Size = new Vector2(28, 28),
			MouseFilter = MouseFilterEnum.Stop,
		};
		_classBadgeStyle = new StyleBoxFlat
		{
			BgColor = HudPalettes.WithAlpha(HudPalettes.BlueCyan.DeepSurface, 1f),
			BorderColor = HudPalettes.BlueCyan.Secondary,
			BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
		};
		_classBadge.AddThemeStyleboxOverride("panel", _classBadgeStyle);
		AddChild(_classBadge);
		_classIcon = new TextureRect
		{
			Position = new Vector2(4, 4),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Size = new Vector2(20, 20),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_classBadge.AddChild(_classIcon);

		_levelUpButton = new LevelUpButton
		{
			OffsetLeft = BarSize.X + 2,
			OffsetTop = PortraitHeight - 30,
			OffsetRight = BarSize.X + 30,
			OffsetBottom = PortraitHeight,
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
