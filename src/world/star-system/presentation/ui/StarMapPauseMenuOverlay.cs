using Godot;
using GrimSpace.Components;
using GrimSpace.Presentation.Support;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class StarMapPauseMenuOverlay : Control
{
	public event Action? ContinueRequested;
	public event Action? SaveRequested;
	public event Action? MainMenuRequested;

	private ModalShell _shell = null!;
	private UiButton _saveButton = null!;
	private ReportIssueDialog _reportDialog = null!;
	private readonly PauseMenuSettingsHost _settings = new();

	public bool IsSettingsOpen => _settings.IsOpen;

	public StarMapPauseMenuOverlay()
	{
		ZIndex = 15;
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;
		Build();
		Visible = false;
	}

	public bool IsReportDialogOpen => _reportDialog.IsOpen;

	public void ApplyTheme(Theme theme) => _reportDialog.ApplyTheme(theme);

	private void Build()
	{
		_shell = PauseMenuUi.CreateShell(() => ContinueRequested?.Invoke());
		AddChild(_shell);

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
			"Save and Quit",
			() => MainMenuRequested?.Invoke());
		PauseMenuUi.AddReportIssueButton(content, () => _reportDialog.Open("star-map"));
		_shell.SetBody(content);

		_reportDialog = new ReportIssueDialog();
		AddChild(_reportDialog);
		_settings.Attach(this);

		VisibilityChanged += OnVisibilityChanged;
	}

	public void ShowSaveConfirmation() => _saveButton.Text = "✓ Saved";

	public bool TryHandleInput(InputEvent @event)
	{
		if (!Visible && !_reportDialog.IsOpen && !_settings.IsOpen)
			return false;

		if (_reportDialog.IsOpen)
			return true;

		if (_settings.TryCloseOnEscape(@event))
			return true;

		if (_settings.IsOpen)
			return true;

		if (_shell.TryHandleInput(@event))
			return true;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			ContinueRequested?.Invoke();
			GetViewport().SetInputAsHandled();
			return true;
		}

		GetViewport().SetInputAsHandled();
		return true;
	}

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

	private void OpenShellDeferred() => PauseMenuUi.OpenShell(_shell);
}
