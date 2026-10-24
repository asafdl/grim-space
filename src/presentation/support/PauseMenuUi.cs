using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Presentation.Support;

internal static class PauseMenuUi
{
	internal static ModalShell CreateShell(Action onContinue)
	{
		var shell = new ModalShell(
			UiContainerVisual.Modal,
			HudThemeFamily.Informative,
			HudPalettes.PurpleRed);
		shell.SetHeader(HudHeaderMode.TitleOnly);
		shell.SetDismissVisible(true);
		shell.SetDismissOnBackdropClick(false);
		shell.SetEscapeHandlingEnabled(true);
		shell.SetCloseHandler(onContinue);
		shell.SetBackHandler(onContinue);
		shell.SetFooter([]);
		shell.SetCompactBody(true);
		shell.SetBodyScrollingEnabled(false);
		return shell;
	}

	internal static UiButton AddButton(VBoxContainer content, string text, Action onPressed)
	{
		var button = HudWidgets.CreateModalMenuButton(
			text, onPressed, palette: HudPalettes.PurpleRed);
		content.AddChild(button);
		return button;
	}

	internal static void AddReportIssueButton(VBoxContainer content, Action onPressed)
	{
		content.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 12f) });
		var button = HudWidgets.CreateModalMenuButton(
			ReportIssueCopy.ReportIssue, onPressed, palette: HudPalettes.Gold);
		button.SetVisual(UiButtonVisual.Technical);
		content.AddChild(button);
	}

	internal static void OpenShell(ModalShell shell)
	{
		var viewport = shell.GetViewportRect().Size;
		shell.SetPanelMinimumSize(new Vector2(
			Mathf.Min(640f, viewport.X - 40f),
			Mathf.Min(660f, viewport.Y - 40f)));
		shell.Open("Paused");
	}
}
