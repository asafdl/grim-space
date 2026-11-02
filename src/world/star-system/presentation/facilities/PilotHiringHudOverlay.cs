using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class PilotHiringHudOverlay : Control
{
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

		_shell = new ModalShell(UiContainerVisual.Modal, HudThemeFamily.Informative);
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
		_shell.SetTitle("Pilot broker");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		_shell.SetBody(PilotHireCardList.Build(_run, _contractId, slot => HireRequested?.Invoke(slot)));
	}
}
