using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Support;

internal static class PauseMenuReportFooter
{
	private const int ExtraTopSpacing = 10;

	internal static void AppendMenuItem(VBoxContainer menu, Action onPressed)
	{
		menu.AddChild(new Control { CustomMinimumSize = new Vector2(0, ExtraTopSpacing) });

		var reportButton = new Button
		{
			Text = ReportIssueCopy.ReportIssue,
			TooltipText = "Describe a bug and send it to the team",
			CustomMinimumSize = new Vector2(220, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.StyleButton(reportButton, HudActionKind.Secondary);
		reportButton.Pressed += onPressed;
		menu.AddChild(reportButton);
	}
}
