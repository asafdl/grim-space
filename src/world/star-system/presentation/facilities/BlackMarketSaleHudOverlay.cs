using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

/// <summary>One black-market sale modal with a pilot section and a ship section.</summary>
public sealed partial class BlackMarketSaleHudOverlay : Control
{
	private static readonly Vector2 PanelSize = new(1240f, 760f);

	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private string _pilotContractId = "";

	public event Action<int>? HireRequested;
	public event Action<RecruitmentOffer>? RecruitmentRequested;
	public event Action? Closed;

	public BlackMarketSaleHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Modal, HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetPanelMinimumSize(PanelSize);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(State run, string facilityTitle, string pilotContractId)
	{
		_facilityTitle = facilityTitle;
		_shell.Open("Black market", facilityTitle);
		Sync(run, pilotContractId);
	}

	public void Sync(State run, string pilotContractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotContractId);
		_run = run;
		_pilotContractId = pilotContractId;
		if (IsOpen)
			ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle("Black market");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		columns.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		columns.AddChild(CreateShopSection(
			"Pilots",
			PilotHireCardList.Build(_run, _pilotContractId, slot => HireRequested?.Invoke(slot))));
		columns.AddChild(CreateShopSection(
			"Ships",
			ShipOfferCardList.Build(_run, offer => RecruitmentRequested?.Invoke(offer))));
		_shell.SetBody(columns);
	}

	private static Control CreateShopSection(string heading, Control list)
	{
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 8);
		column.AddChild(new Label
		{
			Text = heading.ToUpperInvariant(),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "SectionHeading",
		});
		column.AddChild(list);

		var card = new UiNestedContainer(UiContainerVisual.Simple)
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		card.SetContentPadding(0, 0, 0, 0);
		card.SetContent(column);
		return card;
	}
}
