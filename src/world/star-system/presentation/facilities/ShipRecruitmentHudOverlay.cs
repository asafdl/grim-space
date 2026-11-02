using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class ShipRecruitmentHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";

	public event Action<RecruitmentOffer>? RecruitmentRequested;
	public event Action? Closed;

	public ShipRecruitmentHudOverlay()
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

	public void Open(State run, string facilityTitle)
	{
		_run = run;
		_facilityTitle = facilityTitle;
		_shell.Open("Ship broker", _facilityTitle);
		ShowMain();
	}

	public void Sync(State run)
	{
		_run = run;
		if (IsOpen)
			ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle("Ship broker");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		_shell.SetBody(ShipOfferCardList.Build(_run, offer => RecruitmentRequested?.Invoke(offer)));
	}
}
