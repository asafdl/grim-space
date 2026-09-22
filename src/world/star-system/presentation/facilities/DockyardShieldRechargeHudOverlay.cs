using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardShieldRechargeHudOverlay : Control
{
	private static readonly ESpatialOrientation[] Faces =
	[
		ESpatialOrientation.Forward,
		ESpatialOrientation.Retro,
		ESpatialOrientation.Starboard,
		ESpatialOrientation.Port,
		ESpatialOrientation.Dorsal,
		ESpatialOrientation.Ventral,
	];

	private static readonly ShieldBlockMetrics CompactMetrics = ShieldBlockMetrics.For(ShieldBlockBarSize.Compact);

	private readonly ModalShell _shell;
	private State _run = null!;
	private StarMap _map = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<string, ESpatialOrientation>? FaceRechargeRequested;
	public event Action<string>? FillAllRechargeRequested;
	public event Action? Closed;

	public DockyardShieldRechargeHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(State run, StarMap map, string facilityTitle)
	{
		_run = run;
		_map = map;
		_facilityTitle = facilityTitle;
		_statusKind = null;
		_statusMessage = "";
		_shell.Open(_facilityTitle, string.Empty);
		ShowMain();
	}

	public void Close()
	{
		_statusKind = null;
		_statusMessage = "";
		_shell.Close();
	}

	public void Sync(State run, StarMap map)
	{
		_run = run;
		_map = map;
	}

	public void ShowError(string message)
	{
		_statusKind = HudStatusKind.Error;
		_statusMessage = message;
		ShowMain();
	}

	public void ShowConfirmation(string message, HudStatusKind kind)
	{
		_statusKind = kind;
		_statusMessage = message;
		ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle("Shield recharge");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));

		if (!TryGetActiveShip(out var ship))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships in your party."));
			_shell.SetBody(body);
			return;
		}

		body.AddChild(CreateShieldRechargePanel(ship));

		_shell.SetBody(body);
	}

	private bool TryGetActiveShip(out ShipInstance ship)
	{
		ship = null!;
		var shipId = _run.PlayerParty.ShipIds.FirstOrDefault();
		if (shipId is null)
			return false;

		ship = _run.ShipRegistry.Get(shipId);
		return true;
	}

	private Control CreateShieldRechargePanel(ShipInstance ship)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Status");

		var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);

		if (ship.Spec.MaxShieldPoints.MaxOnAnyFace <= 0)
		{
			var label = new Label
			{
				Text = "This ship has no shield facings.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};
			HudStyles.ApplyTextRole(label, HudTextRole.Metadata);
			column.AddChild(label);
			return panel;
		}

		var creditsOnHand = _map.PlayerResources.GetBalance(ResourceId.Credits);
		var shipId = ship.Id;
		var addedRow = false;

		foreach (var face in Faces)
		{
			if (ship.Spec.MaxShieldPoints[face] <= 0)
				continue;

			if (addedRow)
				column.AddChild(CreateRowDivider());

			column.AddChild(CreateFaceRow(ship, face, creditsOnHand, shipId));
			addedRow = true;
		}

		if (addedRow)
			column.AddChild(CreateRowDivider());

		var fillAllCost = DockyardShieldRecharge.TryQuote(ship, out var allCost)
			? CreditAmount(allCost)
			: 0;
		var fillAllMissing = DockyardShieldRecharge.MissingPoints(ship);
		column.AddChild(ResourceCostDisplay.CreateLabeledCostButton(
			"Fill all",
			ResourceId.Credits,
			fillAllCost,
			fillAllMissing > 0 && creditsOnHand >= fillAllCost,
			() => FillAllRechargeRequested?.Invoke(shipId)));

		return panel;
	}

	private Control CreateFaceRow(
		ShipInstance ship,
		ESpatialOrientation face,
		int creditsOnHand,
		string shipId)
	{
		var max = ship.Spec.MaxShieldPoints[face];
		var current = System.Math.Clamp(ship.ShieldPoints[face], 0, max);
		var missing = DockyardShieldRecharge.MissingPointsOnFace(ship, face);
		var creditCost = missing * DockyardShieldRecharge.CreditsPerPoint;

		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);

		var name = new Label
		{
			Text = ShortFaceName(face),
			CustomMinimumSize = new Vector2(72f, 0f),
		};
		HudStyles.ApplyTextRole(name, HudTextRole.Metadata);
		row.AddChild(name);

		var barHost = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		barHost.AddThemeConstantOverride("separation", CompactMetrics.Separation);
		row.AddChild(barHost);
		var blocks = new List<Panel>();
		ShieldBlockVisuals.SyncBlocks(barHost, blocks, CompactMetrics, max, current);

		row.AddChild(ResourceCostDisplay.CreateLabeledCostButton(
			"Fill",
			ResourceId.Credits,
			creditCost,
			missing > 0 && creditsOnHand >= creditCost,
			() => FaceRechargeRequested?.Invoke(shipId, face)));

		return row;
	}

	private static Control CreateRowDivider()
	{
		var padding = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		padding.AddThemeConstantOverride("margin_top", 2);
		padding.AddThemeConstantOverride("margin_bottom", 2);
		padding.AddChild(HudWidgets.CreateInformativeHairline());
		return padding;
	}

	private static int CreditAmount(ResourceBundle cost)
	{
		foreach (var (id, amount) in cost)
		{
			if (id == ResourceId.Credits)
				return amount;
		}

		return 0;
	}

	private static string ShortFaceName(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "fore",
			ESpatialOrientation.Retro => "aft",
			ESpatialOrientation.Starboard => "stbd",
			ESpatialOrientation.Port => "port",
			ESpatialOrientation.Dorsal => "dorsal",
			ESpatialOrientation.Ventral => "ventral",
			_ => face.ToString().ToLowerInvariant(),
		};
}
