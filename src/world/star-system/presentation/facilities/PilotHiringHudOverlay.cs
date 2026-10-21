using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class PilotHiringHudOverlay : Control
{
	private const int BackgroundIconSize = 48;
	private const int PortraitSize = 72;

	private static readonly Color BackgroundBorderColor = new(0.32f, 0.38f, 0.46f);
	private static readonly Color IdentityIconColor = HudStyles.TextColor(HudTextRole.Body);

	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private string _contractId = "";

	public event Action<int>? HireRequested;
	public event Action? Closed;

	public PilotHiringHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(State run, string facilityTitle, string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		_run = run;
		_facilityTitle = facilityTitle;
		_contractId = contractId;
		_shell.Open("Pilot broker", _facilityTitle);
		ShowMain();
	}

	public void Sync(State run, string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		_run = run;
		_contractId = contractId;
		if (IsOpen)
			ShowMain();
	}

	private void ShowMain()
	{
		var map = _run.StarSystem.Map;
		var tier = MerchantCatalog.Tier(map);
		_shell.SetTitle("Pilot broker");
		_shell.SetSubtitle($"{_facilityTitle} · {MerchantOfferDisplay.BandLabel(tier)}");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);

		IReadOnlyList<PilotRecruitment.PilotHireCandidate> candidates;
		try
		{
			var purchased = PurchasedSlots(map, _contractId);
			candidates = PilotRecruitment.RollAvailableCandidates(map, _contractId, purchased);
		}
		catch (InvalidOperationException error)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, error.Message));
			_shell.SetBody(body);
			return;
		}

		if (candidates.Count == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"All pilot hire slots on this lead have been filled."));
			_shell.SetBody(body);
			return;
		}

		foreach (var candidate in candidates)
		{
			var captured = candidate;
			var cost = PilotRecruitment.Price(map, captured);
			var canAfford = map.PlayerResources.CanApply(cost.Negate());
			body.AddChild(BuildCandidateCard(captured, cost, canAfford));
		}

		_shell.SetBody(body);
	}

	private Control BuildCandidateCard(
		PilotRecruitment.PilotHireCandidate candidate,
		ResourceBundle cost,
		bool canAfford)
	{
		var profile = candidate.Profile;
		var background = PilotBackgroundCatalog.Get(profile.BackgroundId);
		var rows = new List<Control>
		{
			BodyLabel(PilotHireDisplay.CardSubtitle(profile)),
			BodyLabel(PilotHireDisplay.ShipPackageLine(candidate.Ship.Chassis, candidate.Ship.GearTier)),
			ResourceCostDisplay.CreateOfferDetails(
				canAfford ? "" : "You do not have the required resources.",
				cost),
		};

		var panel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			FocusMode = FocusModeEnum.All,
		};
		HudStyles.SetPanelVariation(panel, "CardPanelContainer");

		var margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		margin.AddThemeConstantOverride("margin_left", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 12);
		margin.AddChild(row);

		var identityColumn = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
			CustomMinimumSize = new Vector2(PortraitSize, 0),
		};
		identityColumn.AddThemeConstantOverride("separation", 6);
		AddCenteredIdentityChild(
			identityColumn,
			new TextureRect
			{
				Texture = ShipPilotPortraitCatalog.TextureFor(profile.PortraitId),
				CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				MouseFilter = MouseFilterEnum.Ignore,
				TooltipText = profile.Name,
			},
			PortraitSize);
		AddCenteredIdentityChild(identityColumn, BuildBackgroundIcon(background.Id), BackgroundIconSize);
		AddQuirkIcons(identityColumn, profile);
		row.AddChild(identityColumn);

		var textColumn = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		textColumn.AddThemeConstantOverride("separation", 6);
		row.AddChild(textColumn);

		var titleLabel = new Label
		{
			Text = PilotHireDisplay.CardTitle(background.Id),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "CardTitleLabel",
			TooltipText = background.Description,
		};
		textColumn.AddChild(titleLabel);
		foreach (var contentRow in rows)
			textColumn.AddChild(contentRow);

		panel.MouseEntered += () => HudStyles.SetPanelVariation(panel, "CardHoverPanelContainer");
		panel.MouseExited += () => HudStyles.SetPanelVariation(panel, "CardPanelContainer");
		panel.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				return;

			HudStyles.SetPanelVariation(panel, "CardPressedPanelContainer");
			HireRequested?.Invoke(candidate.Slot);
		};
		panel.FocusEntered += () => HudStyles.SetPanelVariation(panel, "CardHoverPanelContainer");

		return panel;
	}

	private static void AddQuirkIcons(VBoxContainer column, PilotRecruitProfile profile)
	{
		var quirks = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		quirks.AddThemeConstantOverride("separation", 6);
		var added = false;
		if (profile.PositiveQuirkId is { } positiveQuirkId
			&& PilotHireDisplay.ShouldShowRecruitedQuirkIcon(
				positiveQuirkId,
				EPilotQuirkPolarity.Positive))
		{
			var quirk = PilotQuirkCatalog.Get(positiveQuirkId);
			quirks.AddChild(BuildQuirkIcon(quirk.Id, HudPalettes.Gold.Primary, quirk.Name, quirk.Description));
			added = true;
		}

		if (profile.NegativeQuirkId is { } negativeQuirkId
			&& PilotHireDisplay.ShouldShowRecruitedQuirkIcon(
				negativeQuirkId,
				EPilotQuirkPolarity.Negative))
		{
			var quirk = PilotQuirkCatalog.Get(negativeQuirkId);
			quirks.AddChild(BuildQuirkIcon(
				quirk.Id,
				HudStyles.TextColor(HudTextRole.Danger),
				quirk.Name,
				quirk.Description));
			added = true;
		}

		if (added)
			AddCenteredIdentityChild(column, quirks, BackgroundIconSize);
	}

	private static void AddCenteredIdentityChild(VBoxContainer column, Control child, float rowHeight)
	{
		child.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		var row = new CenterContainer
		{
			CustomMinimumSize = new Vector2(PortraitSize, rowHeight),
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddChild(child);
		column.AddChild(row);
	}

	private static Control BuildBackgroundIcon(EPilotBackgroundId backgroundId)
	{
		var background = PilotBackgroundCatalog.Get(backgroundId);
		return BuildIdentityIcon(
			BackgroundIconPathFor(backgroundId),
			BackgroundBorderColor,
			$"Background: {background.Name}\n{background.Description}");
	}

	private static Control BuildQuirkIcon(
		EPilotQuirkId quirkId,
		Color borderColor,
		string name,
		string description) =>
		BuildIdentityIcon(
			QuirkIconPathFor(quirkId),
			borderColor,
			$"Quirk: {name}\n{description}");

	private static Control BuildIdentityIcon(string iconPath, Color borderColor, string tooltip)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(BackgroundIconSize, BackgroundIconSize),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			Icon = SvgIconLoader.Load(iconPath, IdentityIconColor, 28),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.Arrow,
			TooltipText = tooltip,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		button.AddThemeStyleboxOverride("normal", IdentityIconStyle(borderColor, 0.08f, 2));
		button.AddThemeStyleboxOverride("hover", IdentityIconStyle(borderColor, 0.16f, 3));
		button.AddThemeStyleboxOverride("pressed", IdentityIconStyle(borderColor, 0.08f, 2));
		return button;
	}

	private static StyleBoxFlat IdentityIconStyle(Color color, float backgroundAlpha, int borderWidth) =>
		new()
		{
			BgColor = new Color(color.R, color.G, color.B, backgroundAlpha),
			BorderColor = color,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = BackgroundIconSize / 2,
			CornerRadiusTopRight = BackgroundIconSize / 2,
			CornerRadiusBottomLeft = BackgroundIconSize / 2,
			CornerRadiusBottomRight = BackgroundIconSize / 2,
		};

	private static Label BodyLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, HudTextRole.Body);
		return label;
	}

	private static IReadOnlyCollection<int> PurchasedSlots(StarMap map, string contractId)
	{
		if (!map.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead)
			return [];

		return lead.PurchasedPilotHireSlots;
	}

	private static string BackgroundIconPathFor(EPilotBackgroundId backgroundId) =>
		backgroundId switch
		{
			EPilotBackgroundId.ScrapDrone => "res://assets/ui/pilots/scrap-drone.png",
			EPilotBackgroundId.OptimalityEnforcer => "res://assets/ui/pilots/optimality-enforcer.png",
			EPilotBackgroundId.CorporateSlave => "res://assets/ui/pilots/corporate-slave.png",
			EPilotBackgroundId.PodRacer => "res://assets/ui/pilots/pod-racer.png",
			EPilotBackgroundId.AsteroidSurfer => "res://assets/ui/pilots/asteroid-surfer.png",
			EPilotBackgroundId.SynthSinger => "res://assets/ui/pilots/synth-singer.png",
			_ => throw new ArgumentOutOfRangeException(nameof(backgroundId), backgroundId, null),
		};

	private static string QuirkIconPathFor(EPilotQuirkId quirkId) =>
		quirkId switch
		{
			EPilotQuirkId.Charismatic => "res://assets/ui/pilots/charismatic.png",
			EPilotQuirkId.NeuroDrugAddict => "res://assets/ui/pilots/neuro-drug-addict.png",
			_ => throw new ArgumentOutOfRangeException(nameof(quirkId), quirkId, null),
		};
}
