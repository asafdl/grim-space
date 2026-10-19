using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class FleetManagementOverlay : Control
{
	private static readonly Color HullColor = new(0.94f, 0.34f, 0.32f);
	private static readonly Color ShieldColor = new(0.3f, 0.66f, 1f);
	private static readonly Color ActionPointColor = HudPalettes.PurpleRed.Primary;
	private static readonly Color ManeuverPointColor = HudPalettes.PurpleRed.Secondary;
	private static readonly Color InitiativeColor = HudPalettes.Gold.Secondary;
	private const int PilotPortraitSize = CenteredCarousel.SlotSize;
	private const int PilotGridPortraitSize = 36;
	private const int PilotPopupColumns = 4;
	private const int PilotPopupWidth = 184;
	private const int AbilityIconSize = 30;
	private const int AbilitySelectorRows = 4;
	private readonly ModalShell _shell;
	private readonly CenteredCarousel _carousel;
	private readonly PopupPanel _pilotPopup;
	private readonly VBoxContainer _pilotPopupContent;
	private State? _run;
	private FleetManagementView _view = new([], []);
	private string? _selectedShipId;
	private EAbilityKind? _selectedAbilityKind;

	public event Action<string>? PilotSheetRequested;
	public event Action<string, string>? PilotAssignmentRequested;

	public FleetManagementOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		_shell.SetDismissVisible(true);
		_shell.Closed += HidePilotPopup;
		AddChild(_shell);

		_carousel = new CenteredCarousel();
		_carousel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		_carousel.ItemClicked += OnShipClicked;
		_carousel.IndexChanged += OnShipIndexChanged;
		_carousel.SetIconResolver(ShipIconFor);

		_pilotPopup = new PopupPanel
		{
			Exclusive = false,
			Unresizable = true,
		};
		AddChild(_pilotPopup);
		var popupMargin = CreateMargin();
		_pilotPopup.AddChild(popupMargin);
		_pilotPopupContent = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_pilotPopupContent.AddThemeConstantOverride("separation", 8);
		popupMargin.AddChild(_pilotPopupContent);
	}

	public bool IsOpen => _shell.IsOpen;
	public string? SelectedShipId => _selectedShipId;

	public void Close()
	{
		HidePilotPopup();
		_shell.Close();
	}

	public void Open(State run, string? selectedShipId = null)
	{
		ArgumentNullException.ThrowIfNull(run);
		_run = run;
		_selectedShipId = selectedShipId;
		_selectedAbilityKind = null;
		HidePilotPopup();
		_shell.Open("FLEET MANAGEMENT");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);
		Refresh();
	}

	public void Refresh()
	{
		if (_run is null || !IsOpen)
			return;

		_view = FleetManagementProjection.Collect(_run);
		if (_view.Ships.Count == 0)
		{
			_selectedShipId = null;
			DetachCarousel();
			_shell.SetBody(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No player-owned ships."));
			return;
		}

		var selectedIndex = ResolveSelectedIndex();
		_selectedShipId = _view.Ships[selectedIndex].Id;
		_carousel.SetItems(_view.Ships
			.Select(ship => (CenteredCarouselEntry?)new CenteredCarouselEntry(ship.Id, ship.Chassis))
			.ToArray());
		_carousel.SetIndex(selectedIndex);
		ShowShipDetails();
	}

	public bool TryHandleInput(InputEvent @event)
	{
		if (_pilotPopup.Visible
			&& @event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			HidePilotPopup();
			return true;
		}
		if (_shell.TryHandleInput(@event))
			return true;
		if (!IsOpen)
			return false;

		var direction = @event is InputEventKey { Pressed: true, Echo: false } key
			? key.Keycode switch
			{
				Key.Left => -1,
				Key.Right => 1,
				_ => 0,
			}
			: 0;
		if (direction == 0 && @event.IsActionPressed("ui_left"))
			direction = -1;
		else if (direction == 0 && @event.IsActionPressed("ui_right"))
			direction = 1;
		if (direction == 0)
			return false;

		_carousel.SetIndex(ResolveSelectedIndex() + direction);
		return true;
	}

	private int ResolveSelectedIndex()
	{
		for (var index = 0; index < _view.Ships.Count; index++)
		{
			if (string.Equals(_view.Ships[index].Id, _selectedShipId, StringComparison.Ordinal))
				return index;
		}

		return 0;
	}

	private void OnShipClicked(string shipId)
	{
		for (var index = 0; index < _view.Ships.Count; index++)
		{
			if (!string.Equals(_view.Ships[index].Id, shipId, StringComparison.Ordinal))
				continue;

			_carousel.SetIndex(index);
			return;
		}
	}

	private void OnShipIndexChanged(int index)
	{
		if (index < 0 || index >= _view.Ships.Count)
			return;

		_carousel.SetIndex(index);
		_selectedShipId = _view.Ships[index].Id;
		_selectedAbilityKind = null;
		HidePilotPopup();
		ShowShipDetails();
	}

	private void ShowShipDetails()
	{
		_shell.SetTitle("FLEET MANAGEMENT");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		var ship = _view.Ships[ResolveSelectedIndex()];
		_shell.SetSubtitle("");

		var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 14);
		DetachCarousel();
		body.AddChild(BuildCarouselRow(ship));
		var selectedAbility = ResolveSelectedAbility(ship);
		var statsRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var stats = BuildStatsPanel(ship);
		stats.CustomMinimumSize = new Vector2(230, 0);
		stats.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		statsRow.AddChild(stats);
		statsRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		body.AddChild(statsRow);
		if (selectedAbility is not null)
			body.AddChild(BuildAbilityWorkspace(ship, selectedAbility));
		_shell.SetBody(body);
	}

	private Control BuildCarouselRow(FleetShipView ship)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);
		var pilotCenter = new CenterContainer
		{
			CustomMinimumSize = new Vector2(
				PilotPortraitSize,
				CenteredCarousel.ViewportHeight + 8),
		};
		pilotCenter.AddChild(BuildPilotControl(ship));
		row.AddChild(pilotCenter);
		var carouselCenter = new CenterContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		carouselCenter.AddChild(_carousel);
		row.AddChild(carouselCenter);
		row.AddChild(new Control
		{
			CustomMinimumSize = new Vector2(
				PilotPortraitSize,
				CenteredCarousel.ViewportHeight + 8),
		});
		return row;
	}

	private Control BuildPilotControl(FleetShipView ship)
	{
		if (ship.Pilot is { } pilot)
		{
			Control portrait = null!;
			portrait = BuildPilotPortrait(
				pilot,
				PilotPortraitSize,
				"Left-click to change pilot\nRight-click to open character sheet",
				() => ShowPilotPopup(portrait, ship),
				() =>
				{
					HidePilotPopup();
					PilotSheetRequested?.Invoke(pilot.Id);
				});
			return portrait;
		}

		var unassigned = new Button
		{
			Text = "Assign Pilot",
			CustomMinimumSize = new Vector2(PilotPortraitSize, PilotPortraitSize),
			TooltipText = "Choose a pilot",
		};
		HudStyles.StyleButton(unassigned, HudActionKind.Secondary);
		unassigned.Pressed += () => ShowPilotPopup(unassigned, ship);
		return unassigned;
	}

	private static Control BuildStatsPanel(FleetShipView ship)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, HudStyles.FleetSectionPanelType);
		var margin = CreateMargin();
		panel.AddChild(margin);
		var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rows.AddThemeConstantOverride("separation", 8);
		margin.AddChild(rows);
		rows.AddChild(SummaryRow("CHASSIS", ship.Chassis));
		rows.AddChild(SummaryRow("HULL", $"{ship.Hull} / {ship.MaxHull}", HullColor));
		rows.AddChild(SummaryRow("SHIELDS", $"{ship.Shields} / {ship.MaxShields}", ShieldColor));
		rows.AddChild(SummaryRow("ACTION POINTS", ship.ActionPoints.ToString(), ActionPointColor));
		rows.AddChild(SummaryRow("MANEUVER POINTS", ship.ManeuverPoints.ToString(), ManeuverPointColor));
		rows.AddChild(SummaryRow("INITIATIVE", ship.Initiative.ToString(), InitiativeColor));
		return panel;
	}

	private Control BuildAbilityWorkspace(FleetShipView ship, FleetAbilityView selectedAbility)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, HudStyles.FleetSectionPanelType);
		var margin = CreateMargin();
		panel.AddChild(margin);
		var workspace = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		workspace.AddThemeConstantOverride("separation", 12);
		margin.AddChild(workspace);

		var selector = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(76, 0),
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
		};
		selector.AddThemeConstantOverride("separation", 8);
		selector.AddChild(SectionHeading("ABILITIES"));
		var icons = new HBoxContainer();
		icons.AddThemeConstantOverride("separation", 6);
		selector.AddChild(icons);
		for (var columnStart = 0; columnStart < ship.Abilities.Count; columnStart += AbilitySelectorRows)
		{
			var iconColumn = new VBoxContainer();
			iconColumn.AddThemeConstantOverride("separation", 6);
			icons.AddChild(iconColumn);
			foreach (var ability in ship.Abilities
				.Skip(columnStart)
				.Take(AbilitySelectorRows))
			{
				var visual = AbilityHudCatalog.VisualFor(ability.Kind);
				var selected = ability.Kind == _selectedAbilityKind;
				var button = new Button
				{
					CustomMinimumSize = new Vector2(AbilityIconSize, AbilityIconSize),
					Icon = SvgIconLoader.Load(visual.IconPath, visual.Tint, 18),
					ExpandIcon = false,
					IconAlignment = HorizontalAlignment.Center,
					VerticalIconAlignment = VerticalAlignment.Center,
					Alignment = HorizontalAlignment.Center,
					TooltipText = ability.Name,
				};
				button.AddThemeConstantOverride("h_separation", 0);
				HudStyles.StyleButton(button, selected ? HudActionKind.Primary : HudActionKind.Secondary);
				var kind = ability.Kind;
				button.Pressed += () =>
				{
					_selectedAbilityKind = kind;
					ShowShipDetails();
				};
				iconColumn.AddChild(button);
			}
		}
		workspace.AddChild(selector);
		workspace.AddChild(new VSeparator());
		var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rows.AddThemeConstantOverride("separation", 8);
		workspace.AddChild(rows);
		var selectedVisual = AbilityHudCatalog.VisualFor(selectedAbility.Kind);
		var title = SectionHeading(selectedAbility.Name);
		title.AddThemeColorOverride("font_color", selectedVisual.Tint);
		rows.AddChild(title);
		foreach (var stat in selectedAbility.Stats)
			rows.AddChild(SummaryRow(stat.Label, stat.Value));
		rows.AddChild(SummaryRow("MOUNTS", selectedAbility.Facets));
		return panel;
	}

	private FleetAbilityView? ResolveSelectedAbility(FleetShipView ship)
	{
		var selected = ship.Abilities.FirstOrDefault(ability => ability.Kind == _selectedAbilityKind);
		if (selected is not null)
			return selected;

		selected = ship.Abilities.FirstOrDefault();
		_selectedAbilityKind = selected?.Kind;
		return selected;
	}

	private void ShowPilotPopup(Control anchor, FleetShipView ship)
	{
		foreach (var child in _pilotPopupContent.GetChildren())
		{
			_pilotPopupContent.RemoveChild(child);
			child.QueueFree();
		}
		var grid = new GridContainer
		{
			Columns = PilotPopupColumns,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		grid.AddThemeConstantOverride("h_separation", 6);
		grid.AddThemeConstantOverride("v_separation", 6);
		_pilotPopupContent.AddChild(grid);
		foreach (var pilot in _view.Pilots)
		{
			var candidate = pilot;
			var status = string.Equals(pilot.AssignedShipId, ship.Id, StringComparison.Ordinal)
				? "CURRENT"
				: pilot.AssignedShipId is null
					? "RESERVE"
					: pilot.IsActive
						? "ACTIVE"
						: "ASSIGNED";
			grid.AddChild(BuildPilotPortrait(
				pilot,
				PilotGridPortraitSize,
				$"{pilot.Name} · {status}",
				() => AssignPilot(candidate.Id, ship.Id)));
		}

		var anchorRect = anchor.GetGlobalRect();
		var rowCount = Mathf.CeilToInt(_view.Pilots.Count / (float)PilotPopupColumns);
		var popupSize = new Vector2I(PilotPopupWidth, 16 + rowCount * 48);
		var popupPosition = new Vector2I(
			Mathf.RoundToInt(anchorRect.End.X - popupSize.X),
			Mathf.RoundToInt(anchorRect.End.Y + 6));
		_pilotPopup.Popup(new Rect2I(popupPosition, popupSize));
	}

	private static Control BuildPilotPortrait(
		FleetPilotView pilot,
		int size,
		string tooltip,
		Action onLeftClick,
		Action? onRightClick = null)
	{
		var portrait = new TextureRect
		{
			CustomMinimumSize = new Vector2(size, size),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			TooltipText = tooltip,
			Texture = ShipPilotPortraitCatalog.TextureFor(pilot.PortraitId),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
		};
		portrait.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true } mouse)
				return;
			if (mouse.ButtonIndex == MouseButton.Left)
				onLeftClick();
			else if (mouse.ButtonIndex == MouseButton.Right)
				onRightClick?.Invoke();
			else
				return;

			portrait.AcceptEvent();
		};
		return portrait;
	}

	private void AssignPilot(string pilotId, string shipId)
	{
		HidePilotPopup();
		PilotAssignmentRequested?.Invoke(pilotId, shipId);
		Refresh();
	}

	private void HidePilotPopup() => _pilotPopup.Hide();

	private static Control SummaryRow(string label, string value, Color? color = null)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var caption = new Label
		{
			Text = label,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(caption, HudTextRole.Metadata);
		if (color is { } rowColor)
			caption.AddThemeColorOverride("font_color", rowColor);
		row.AddChild(caption);
		var data = new Label { Text = value };
		HudStyles.ApplyTextRole(data, HudTextRole.Emphasis);
		if (color is { } valueColor)
			data.AddThemeColorOverride("font_color", valueColor);
		row.AddChild(data);
		return row;
	}

	private static Label SectionHeading(string text) =>
		new()
		{
			Text = text,
			ThemeTypeVariation = "SectionHeading",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

	private static MarginContainer CreateMargin()
	{
		var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		return margin;
	}

	private Texture2D? ShipIconFor(string shipId)
	{
		var ship = _view.Ships.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, shipId, StringComparison.Ordinal));
		if (ship is null)
			return null;

		var chassis = _run?.ItemInventory.TryGet(shipId, out var instance) == true
			? instance.Spec.Chassis
			: (EType?)null;
		return chassis is { } type ? UnitPortraitCatalog.For(type) : null;
	}

	private void DetachCarousel()
	{
		_carousel.GetParent()?.RemoveChild(_carousel);
	}
}
