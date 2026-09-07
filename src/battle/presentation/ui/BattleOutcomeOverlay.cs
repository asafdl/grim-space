using Godot;
using GrimSpace.Battle.Objectives;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleOutcomeOverlay : Node
{
	public event Action? ResetRequested;

	private readonly ModalHudShell _shell;
	private ActionLogPanel _actionLog = null!;

	public bool Visible
	{
		get => _shell.Visible;
		set => _shell.Visible = value;
	}

	public BattleOutcomeOverlay()
	{
		_shell = new ModalHudShell();
		AddChild(_shell);
		_shell.SetHeaderVisible(false);
	}

	public void SetOutcome(EBattleResult result, IReadOnlyList<string> actionLogLines)
	{
		_shell.Open(BattleHudCopy.OutcomeTitle(result));
		_shell.SetSubtitle("");

		_actionLog = new ActionLogPanel
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(440, 280),
		};
		_actionLog.SetLines(actionLogLines);

		var body = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		body.AddChild(_actionLog);

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction(BattleHudCopy.Reset, HudActionKind.Primary, () => ResetRequested?.Invoke()),
		]);
	}
}
