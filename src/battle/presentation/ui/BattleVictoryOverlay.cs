using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleVictoryOverlay : CanvasLayer
{
	public event Action? ContinueRequested;

	private ModalShell _shell = null!;

	public BattleVictoryOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public void Sync(BattleVictorySummary summary, bool strategicBattle)
	{
		_shell.Open(BattleHudCopy.VictoryTitle, string.Empty);
		_shell.SetRequiresDecision(true);
		_shell.SetDismissVisible(false);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		if (summary.DamageRows.Count == 0 && summary.CompletionRow is null)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				BattleHudCopy.VictoryNoDamageReport));
		}
		else
		{
			foreach (var row in summary.DamageRows)
				body.AddChild(CreateDamageRow(row));
			if (summary.CompletionRow is { } completion)
				body.AddChild(CreateDamageRow(completion));
		}

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction(
				strategicBattle ? BattleHudCopy.ReturnToStarMap : BattleHudCopy.Reset,
				HudActionKind.Primary,
				() => ContinueRequested?.Invoke()),
		]);
		Visible = true;
	}

	public void HideOverlay() => Visible = false;

	private static Label CreateDamageRow(BattleDamageReportRow row)
	{
		var label = new Label
		{
			Text = BattleHudCopy.VictoryDamageReportLine(row.TargetLabel, row.ExperienceGained),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, HudTextRole.Body);
		return label;
	}

	private void Build()
	{
		var root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = HudStyles.ModalBackdrop,
		};
		root.AddChild(backdrop);

		_shell = new ModalShell(
			UiContainerVisual.Technical,
			HudThemeFamily.Informative,
			HudPalettes.BlueCyan);
		_shell.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
		_shell.AnchorRight = 1f;
		_shell.AnchorBottom = 1f;
		_shell.GrowHorizontal = Control.GrowDirection.Both;
		_shell.GrowVertical = Control.GrowDirection.Both;
		root.AddChild(_shell);
	}
}
