using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class ContractHudOverlay : CanvasLayer
{
	private enum ViewMode
	{
		List,
		Details,
		DeclineConfirm,
	}

	private Control _root = null!;
	private PanelContainer _panel = null!;
	private Label _title = null!;
	private Label _status = null!;
	private VBoxContainer _listContainer = null!;
	private VBoxContainer _detailsBody = null!;
	private VBoxContainer _detailsContainer = null!;
	private VBoxContainer _declineContainer = null!;
	private Button _backButton = null!;
	private Button _acceptButton = null!;
	private Button _declineButton = null!;
	private Button _confirmDeclineButton = null!;
	private Button _cancelDeclineButton = null!;

	private StarMap _map = null!;
	private string _activePoiId = "";
	private Contract? _selected;
	private ViewMode _mode = ViewMode.List;

	public event Action<string>? AcceptRequested;
	public event Action<string>? DeclineRequested;

	public ContractHudOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public void Open(StarMap map, string activePoiId)
	{
		_map = map;
		_activePoiId = activePoiId;
		_selected = null;
		_mode = ViewMode.List;
		_status.Text = "";
		ShowList();
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
		_selected = null;
		_status.Text = "";
	}

	public bool IsOpen => Visible;

	public void ShowConfirmation(string message)
	{
		_status.Text = message;
		ShowList();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_mode == ViewMode.DeclineConfirm)
			ShowDetails();
		else if (_mode == ViewMode.Details)
			ShowList();
		else
			Close();

		GetViewport().SetInputAsHandled();
	}

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(_root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = new Color(0f, 0f, 0f, 0.55f),
		};
		_root.AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		_root.AddChild(center);

		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", PanelStyle());
		center.AddChild(_panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		_panel.AddChild(margin);

		var content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 16);
		margin.AddChild(content);

		_title = new Label
		{
			Text = "Contracts",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_title.AddThemeFontSizeOverride("font_size", 32);
		content.AddChild(_title);

		_status = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		_status.AddThemeFontSizeOverride("font_size", 14);
		_status.AddThemeColorOverride("font_color", new Color(0.62f, 0.78f, 0.62f));
		content.AddChild(_status);

		_listContainer = new VBoxContainer();
		_listContainer.AddThemeConstantOverride("separation", 8);
		content.AddChild(_listContainer);

		_detailsBody = new VBoxContainer();
		_detailsBody.AddThemeConstantOverride("separation", 10);
		content.AddChild(_detailsBody);

		var detailsActions = new VBoxContainer();
		detailsActions.AddThemeConstantOverride("separation", 10);
		detailsActions.Visible = false;
		content.AddChild(detailsActions);
		_detailsContainer = detailsActions;

		_declineContainer = new VBoxContainer();
		_declineContainer.AddThemeConstantOverride("separation", 12);
		_declineContainer.Visible = false;
		content.AddChild(_declineContainer);

		_backButton = new Button { Text = "Back to list" };
		_backButton.Pressed += ShowList;
		_detailsContainer.AddChild(_backButton);

		_acceptButton = new Button { Text = "Accept" };
		_acceptButton.Pressed += OnAcceptPressed;
		_detailsContainer.AddChild(_acceptButton);

		_declineButton = new Button { Text = "Decline" };
		_declineButton.Pressed += ShowDeclineConfirm;
		_detailsContainer.AddChild(_declineButton);

		_confirmDeclineButton = new Button { Text = "Confirm decline" };
		_confirmDeclineButton.Pressed += OnConfirmDeclinePressed;
		_declineContainer.AddChild(_confirmDeclineButton);

		_cancelDeclineButton = new Button { Text = "Cancel" };
		_cancelDeclineButton.Pressed += ShowDetails;
		_declineContainer.AddChild(_cancelDeclineButton);

		Callable.From(ConnectViewportLayout).CallDeferred();
	}

	private void ConnectViewportLayout()
	{
		GetViewport().SizeChanged += OnViewportSizeChanged;
		LayoutPanel();
	}

	private void OnViewportSizeChanged() => LayoutPanel();

	private void LayoutPanel()
	{
		var viewport = GetViewport();
		_panel.CustomMinimumSize = new Vector2(
			UiScale.Px(560f, viewport),
			UiScale.Px(480f, viewport));
	}

	private void ShowList()
	{
		_mode = ViewMode.List;
		_title.Text = "Contracts";
		_listContainer.Visible = true;
		_detailsBody.Visible = false;
		_detailsContainer.Visible = false;
		_declineContainer.Visible = false;
		RebuildList();
	}

	private void ShowDetails()
	{
		if (_selected is null)
		{
			ShowList();
			return;
		}

		_mode = ViewMode.Details;
		_title.Text = ContractDisplay.Title(_selected);
		_listContainer.Visible = false;
		_detailsBody.Visible = true;
		_detailsContainer.Visible = true;
		_declineContainer.Visible = false;
		RebuildDetails();
	}

	private void ShowDeclineConfirm()
	{
		if (_selected is null)
			return;

		_mode = ViewMode.DeclineConfirm;
		_title.Text = "Decline contract?";
		_listContainer.Visible = false;
		_detailsBody.Visible = false;
		_detailsContainer.Visible = false;
		_declineContainer.Visible = true;
		_declineContainer.GetChildren().OfType<Label>().ToList().ForEach(child => child.QueueFree());

		var warning = new Label
		{
			Text = $"Permanently decline \"{ContractDisplay.Title(_selected)}\" for this run?",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		warning.AddThemeFontSizeOverride("font_size", 16);
		_declineContainer.AddChild(warning);
		_declineContainer.MoveChild(warning, 0);
	}

	private void RebuildList()
	{
		foreach (var child in _listContainer.GetChildren())
			child.QueueFree();

		var contracts = _map.ContractRegistry.AvailableForPoi(_activePoiId).ToArray();
		if (contracts.Length == 0)
		{
			var empty = new Label
			{
				Text = "No contracts available from this authority.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			empty.AddThemeFontSizeOverride("font_size", 16);
			_listContainer.AddChild(empty);
			return;
		}

		foreach (var contract in contracts)
		{
			var row = new Button
			{
				Text = $"{ContractDisplay.Title(contract)} · {ContractDisplay.Reward(contract)}",
				Alignment = HorizontalAlignment.Left,
			};
			row.Pressed += () =>
			{
				_selected = contract;
				ShowDetails();
			};
			_listContainer.AddChild(row);
		}
	}

	private void RebuildDetails()
	{
		if (_selected is null)
			return;

		foreach (var child in _detailsBody.GetChildren())
			child.QueueFree();

		AddDetailLine("Issuer", ContractDisplay.Issuer(_selected, _map));
		AddDetailLine("Objective", ContractDisplay.ObjectiveSummary(_selected));
		AddDetailLine("Search area", ContractDisplay.SearchArea(_selected));
		AddDetailLine("Reward", ContractDisplay.Reward(_selected));
		AddDetailLine("Danger", ContractDisplay.Danger(_selected));
		AddDetailLine("Briefing", ContractDisplay.Narrative(_selected));
	}

	private void AddDetailLine(string label, string value)
	{
		var row = new Label
		{
			Text = $"{label}: {value}",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		row.AddThemeFontSizeOverride("font_size", 15);
		_detailsBody.AddChild(row);
		_detailsBody.MoveChild(row, 0);
	}

	private void OnAcceptPressed()
	{
		if (_selected is null)
			return;

		AcceptRequested?.Invoke(_selected.Id);
	}

	private void OnConfirmDeclinePressed()
	{
		if (_selected is null)
			return;

		DeclineRequested?.Invoke(_selected.Id);
	}

	private static StyleBoxFlat PanelStyle() => new()
	{
		BgColor = new Color(0.02f, 0.04f, 0.07f, 0.95f),
		ContentMarginLeft = 0,
		ContentMarginRight = 0,
		ContentMarginTop = 0,
		ContentMarginBottom = 0,
		CornerRadiusTopLeft = 2,
		CornerRadiusTopRight = 2,
		CornerRadiusBottomLeft = 2,
		CornerRadiusBottomRight = 2,
	};
}
