using Godot;
using GrimSpace.Components;
using GrimSpace.Presentation.Support;
using GrimSpace.Ui.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePauseMenuOverlay : CanvasLayer
{
	public event Action? ContinueRequested;
	public event Action? SaveRequested;
	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	private Control _root = null!;
	private ModalShell _shell = null!;
	private UiButton _saveButton = null!;
	private UiButton _restartButton = null!;
	private ReportIssueDialog _reportDialog = null!;
	private readonly PauseMenuSettingsHost _settings = new();

	public bool IsSettingsOpen => _settings.IsOpen;

	public BattlePauseMenuOverlay()
	{
		Layer = 15;
		Build();
		Visible = false;
	}

	public bool IsReportDialogOpen => _reportDialog.IsOpen;

	public void ApplyTheme(Theme theme) => _reportDialog.ApplyTheme(theme);

	public void SetRestartEnabled(bool enabled) => _restartButton.Disabled = !enabled;

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_root);

		_shell = PauseMenuUi.CreateShell(() => ContinueRequested?.Invoke());
		_root.AddChild(_shell);

		var content = HudWidgets.CreateModalButtonColumn();
		_saveButton = PauseMenuUi.AddButton(
			content,
			"Save",
			() => SaveRequested?.Invoke());
		PauseMenuUi.AddButton(
			content,
			"Settings",
			() => _settings.Open());
		PauseMenuUi.AddButton(
			content,
			BattleHudCopy.Retire,
			() => RetireRequested?.Invoke());
		_restartButton = PauseMenuUi.AddButton(
			content,
			BattleHudCopy.Restart,
			() => RestartRequested?.Invoke());
		PauseMenuUi.AddButton(
			content,
			"Save and Quit",
			() => MainMenuRequested?.Invoke());
		PauseMenuUi.AddReportIssueButton(content, () => _reportDialog.Open("battle"));
		_shell.SetBody(content);

		_reportDialog = new ReportIssueDialog();
		_root.AddChild(_reportDialog);
		_settings.Attach(_root);

		VisibilityChanged += OnVisibilityChanged;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (_reportDialog.IsOpen)
			return;

		if (_settings.TryCloseOnEscape(@event))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_settings.IsOpen)
			return;

		if (_shell.TryHandleInput(@event))
			GetViewport().SetInputAsHandled();
	}

	public void ShowSaveConfirmation() => _saveButton.Text = "✓ Saved";

	private void OnVisibilityChanged()
	{
		if (!Visible)
		{
			if (_shell.IsOpen)
				_shell.Close();
			return;
		}

		CallDeferred(MethodName.OpenShellDeferred);
	}

	private void OpenShellDeferred() =>
		PauseMenuUi.OpenShell(_shell);
}
