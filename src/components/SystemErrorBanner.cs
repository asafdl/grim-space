using Godot;

namespace GrimSpace.Components;

public sealed partial class SystemErrorBanner : Control
{
	private Label _message = null!;
	private string _summary = "";
	private string _technicalDetails = "";

	public event Action<string, string>? ReportRequested;

	public SystemErrorBanner()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;
		ZIndex = 10;
		Visible = false;
		Build();
	}

	public void ShowError(string summary, string technicalDetails)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(summary);
		ArgumentException.ThrowIfNullOrWhiteSpace(technicalDetails);
		_summary = summary;
		_technicalDetails = technicalDetails;
		_message.Text = $"ERROR · {summary}";
		Visible = true;
	}

	public void Dismiss()
	{
		Visible = false;
		_summary = "";
		_technicalDetails = "";
	}

	private void Build()
	{
		var position = new CenterContainer
		{
			AnchorRight = 1f,
			OffsetTop = 20,
			OffsetBottom = 84,
			GrowHorizontal = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(position);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(520, 0),
			MouseFilter = MouseFilterEnum.Stop,
		};
		HudStyles.SetPanelVariation(panel, HudStyles.StatusPanelVariation(HudStatusKind.Error));
		position.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		panel.AddChild(margin);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		margin.AddChild(row);

		_message = new Label
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		HudStyles.ApplyTextRole(_message, HudTextRole.Danger);
		row.AddChild(_message);

		var report = new Button { Text = "Report" };
		HudStyles.StyleButton(report, HudActionKind.Secondary);
		report.Pressed += () =>
		{
			if (_summary.Length > 0 && _technicalDetails.Length > 0)
				ReportRequested?.Invoke(_summary, _technicalDetails);
		};
		row.AddChild(report);

		var dismiss = new Button { Text = "Dismiss" };
		HudStyles.StyleButton(dismiss, HudActionKind.Secondary);
		dismiss.Pressed += Dismiss;
		row.AddChild(dismiss);
	}
}
