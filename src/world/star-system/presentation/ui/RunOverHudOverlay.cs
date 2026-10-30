using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class RunOverHudOverlay : Control
{
	private readonly ModalShell _shell;

	public event Action? MainMenuRequested;

	public RunOverHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Technical);
		_shell.SetFooterButtonVisual(UiButtonVisual.Framed);
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(RunStatisticsSummary summary)
	{
		ArgumentNullException.ThrowIfNull(summary);

		_shell.Open("Run Over", "It is possible to commit no mistakes, yet still lose. However that's not what happened here...");
		_shell.SetRequiresDecision(true);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		body.AddChild(CreateStatsCard(summary));
		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction(
				"Main Menu",
				HudActionKind.Primary,
				() => MainMenuRequested?.Invoke()),
		]);
	}

	public void Close() => _shell.Close();

	private static UiCard CreateStatsCard(RunStatisticsSummary summary)
	{
		var card = new UiCard(
			"Run Stats",
			string.Empty,
			FormatStats(summary),
			string.Empty)
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeOverride = new UiSizeOverride(Height: 200f),
		};
		card.SetVisual(UiCardVisual.Angled);
		card.SetInteractive(false);
		return card;
	}

	internal static string FormatStats(RunStatisticsSummary summary) =>
		$"Contracts completed  {summary.ContractsCompleted}\n"
		+ $"Battles won  {summary.BattlesWon}\n"
		+ $"Battles lost  {summary.BattlesLost}\n"
		+ $"Credits earned  {summary.CreditsEarned}\n"
		+ $"Credits remaining  {summary.CreditsRemaining}";
}
