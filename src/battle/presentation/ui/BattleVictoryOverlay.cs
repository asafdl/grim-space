using System.Text;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Objectives;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleVictoryOverlay : CanvasLayer
{
	public event Action? ContinueRequested;

	private const string DoorLatchSfxPath = "res://assets/sfx/ui/fire_door_closing.wav";

	private ModalShell _shell = null!;
	private AudioStreamPlayer _doorLatchPlayer = null!;
	private Tween? _revealTween;
	private string? _presentationKey;
	private IReadOnlyList<HudAction> _pendingFooterActions = [];
	private Control[]? _pendingRevealRows;
	private Control[]? _activeRevealRows;

	public BattleVictoryOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public void Sync(
		BattleVictorySummary summary,
		bool strategicBattle,
		EBattleResult result = EBattleResult.Win)
	{
		var key = PresentationKey(summary, strategicBattle, result);
		if (Visible && _presentationKey == key && _revealTween is not null)
			return;

		if (Visible && _presentationKey == key && _revealTween is null)
			return;

		CancelReveal();
		_presentationKey = key;

		_shell.Open(BattleHudCopy.ScorecardTitle(result), string.Empty);
		_shell.SetRequiresDecision(true);
		_shell.SetDismissVisible(false);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 10);

		var revealRows = new List<Control>();
		if (summary.Pilots.Count == 0)
		{
			var emptyState = HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				BattleHudCopy.VictoryNoPilotPerformance);
			body.AddChild(emptyState);
			revealRows.Add(emptyState);
		}
		else
		{
			foreach (var row in summary.Pilots)
			{
				var item = new BattleVictoryPilotRowView(row);
				body.AddChild(item);
				revealRows.Add(item);
			}
		}

		_shell.SetBody(body);
		_pendingFooterActions =
		[
			new HudAction(
				strategicBattle ? BattleHudCopy.ReturnToStarMap : BattleHudCopy.Reset,
				HudActionKind.Primary,
				() => ContinueRequested?.Invoke()),
		];
		_shell.SetFooter([]);
		Visible = true;
		_pendingRevealRows = revealRows.ToArray();
		CallDeferred(MethodName.StartPendingReveal);
	}

	public void HideOverlay()
	{
		CancelReveal();
		_presentationKey = null;
		_pendingRevealRows = null;
		_activeRevealRows = null;
		Visible = false;
	}

	private void StartPendingReveal()
	{
		if (_pendingRevealRows is null)
			return;

		var rows = _pendingRevealRows;
		_pendingRevealRows = null;
		_activeRevealRows = rows;

		_revealTween = BattleVictoryScorecardReveal.Begin(
			this,
			_doorLatchPlayer,
			rows,
			() =>
			{
				_revealTween = null;
				_activeRevealRows = null;
				_shell.SetFooter(_pendingFooterActions);
			});
	}

	private void CancelReveal()
	{
		if (_revealTween is not null)
		{
			_revealTween.Kill();
			_revealTween = null;
		}

		CancelExperienceFillOnRows(_pendingRevealRows);
		CancelExperienceFillOnRows(_activeRevealRows);
		_activeRevealRows = null;
	}

	private static void CancelExperienceFillOnRows(IReadOnlyList<Control>? rows)
	{
		if (rows is null)
			return;

		foreach (var row in rows)
		{
			if (row is BattleVictoryPilotRowView pilotRow)
				pilotRow.CancelExperienceFill();
		}
	}

	private static string PresentationKey(
		BattleVictorySummary summary,
		bool strategicBattle,
		EBattleResult result)
	{
		var builder = new StringBuilder(strategicBattle ? "1:" : "0:");
		builder.Append(result).Append(':');
		for (var index = 0; index < summary.Pilots.Count; index++)
		{
			if (index > 0)
				builder.Append('|');

			var row = summary.Pilots[index];
			builder.Append(row.DisplayName)
				.Append(':')
				.Append(row.DamageDealt)
				.Append(':')
				.Append(row.ExperiencePointsAfter)
				.Append(':')
				.Append(row.ScoreGrade)
				.Append(':')
				.Append(row.IsScrapped ? '1' : '0');
		}

		return builder.ToString();
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

		_doorLatchPlayer = new AudioStreamPlayer
		{
			Stream = GD.Load<AudioStream>(DoorLatchSfxPath)
				?? throw new InvalidOperationException($"Could not load SFX '{DoorLatchSfxPath}'."),
			Bus = AudioBuses.Sfx,
		};
		AddChild(_doorLatchPlayer);
	}
}
