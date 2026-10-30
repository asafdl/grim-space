using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;

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
		body.AddThemeConstantOverride("separation", 10);

		if (summary.Pilots.Count == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				BattleHudCopy.VictoryNoPilotPerformance));
		}
		else
		{
			foreach (var row in summary.Pilots)
				body.AddChild(CreatePilotRow(row));
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

	private static UiImageListItem CreatePilotRow(BattleVictoryPilotRow row)
	{
		var item = new UiImageListItem(
			row.DisplayName,
			BattleHudCopy.VictoryPilotPerformanceMetadata(
				row.DamageDealt,
				row.DamageTaken,
				row.HealingDone,
				row.ExperienceEarned),
			string.Empty,
			ShipPilotPortraitCatalog.TextureFor(row.PortraitId))
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			MouseDefaultCursorShape = Control.CursorShape.Arrow,
		};
		item.SetTrailingContent(BattlePerformanceGradeDisplay.CreateTrailing(row.ScoreGrade));
		return item;
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
