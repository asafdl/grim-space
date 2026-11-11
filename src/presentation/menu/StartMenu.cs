using Godot;
using GrimSpace.Application;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string IntroScenePath = "res://scenes/intro.tscn";
	private const string DiscordInviteUrl = "https://discord.gg/dMPX9SH3Dd";
	private const string MenuHoverSfxPath = "res://assets/sfx/ui/menu_hover_click.wav";
	private static readonly string[] LoadingMessages =
	[
		"Lighting galaxy on fire...",
		"Scrapping all rust boxes...",
		"Cooking neuro-drugs...",
		"Counting suspicious oxygen tanks...",
		"Polishing the escape pods...",
		"Hunting down any remaining humans...",
		"Grooming Syndi's beard..."
	];

	private Control _menuColumn = null!;
	private GameSettingsMenu _settingsMenu = null!;
	private Control _loadingOverlay = null!;
	private Control _loadingGear = null!;
	private Label _loadingLabel = null!;
	private int _loadingMessageIndex = -1;
	private double _loadingMessageNextChangeAt;
	private Button _newGameButton = null!;
	private Button _continueButton = null!;
	private AudioStreamPlayer _menuHoverSfx = null!;

	public override void _Ready()
	{
		MenuUi.ApplyTheme(this);
		GetNode<Label>("%VersionLabel").Text = $"v{GameVersion.Display}";

		_menuColumn = GetNode<Control>("%MenuColumn");
		_settingsMenu = GetNode<GameSettingsMenu>("%SettingsOverlay");
		_settingsMenu.Host = GameSettingsApplicator.Host.MainMenu;
		_settingsMenu.CloseRequested += _settingsMenu.Hide;
		_loadingOverlay = GetNode<Control>("%LoadingOverlay");
		_loadingGear = GetNode<Control>("%LoadingGear");
		_loadingLabel = GetNode<Label>("%LoadingLabel");

		_newGameButton = GetNode<Button>("%NewGame");
		_continueButton = GetNode<Button>("%Continue");
		_continueButton.Disabled = !Session.Instance.HasSaveGame;
		_menuHoverSfx = new AudioStreamPlayer
		{
			Stream = GD.Load<AudioStream>(MenuHoverSfxPath)
				?? throw new InvalidOperationException($"Could not load SFX '{MenuHoverSfxPath}'."),
			Bus = AudioBuses.Sfx,
		};
		AddChild(_menuHoverSfx);
		foreach (var child in _menuColumn.GetChildren())
		{
			if (child is Button button)
				button.MouseEntered += () => PlayMenuHoverSfx(button);
		}

		GetNode<Button>("%PlayIntro").Pressed += OnPlayIntro;
		_newGameButton.Pressed += OnNewGame;
		_continueButton.Pressed += OnContinue;
		GetNode<Button>("%Settings").Pressed += _settingsMenu.Open;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();
		var discord = GetNode<TextureButton>("%Discord");
		discord.MouseDefaultCursorShape = CursorShape.PointingHand;
		discord.Pressed += OnDiscordPressed;

		CallDeferred(MethodName.PrepareFirstScene);
	}

	private void PlayMenuHoverSfx(Button button)
	{
		if (!button.Disabled)
			_menuHoverSfx.Play();
	}

	private void OnPlayIntro() =>
		GetTree().ChangeSceneToFile(IntroScenePath);

	private static void OnDiscordPressed() =>
		OS.ShellOpen(DiscordInviteUrl);

	private void PrepareFirstScene() =>
		Session.Instance.PrepareFirstScene();

	private async void OnNewGame()
	{
		_newGameButton.Disabled = true;
		_menuColumn.Hide();
		_loadingOverlay.Show();
		SetLoadingMessage();
		SetProcess(true);
		try
		{
			await Session.Instance.BeginMapFromMenuAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to start map: {ex}");
			_newGameButton.Disabled = false;
			_loadingOverlay.Hide();
			_menuColumn.Show();
		}
	}

	private async void OnContinue()
	{
		_continueButton.Disabled = true;
		_newGameButton.Disabled = true;
		_menuColumn.Hide();
		_loadingOverlay.Show();
		SetLoadingMessage();
		SetProcess(true);
		// Let the loading overlay paint and spin before the synchronous restore.
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var result = Session.Instance.TryLoadGame();
		if (result == GrimSpace.Run.Persistence.LoadResult.Success)
			return;

		_newGameButton.Disabled = false;
		_continueButton.Disabled = !Session.Instance.HasSaveGame;
		_loadingOverlay.Hide();
		_menuColumn.Show();
		GD.PrintErr($"Failed to load save game: {result}");
	}

	public override void _Process(double _delta)
	{
		if (!_loadingOverlay.Visible)
			return;

		_loadingGear.Rotation += Mathf.DegToRad(90f) * (float)_delta;
		if (Time.GetTicksMsec() / 1000.0 >= _loadingMessageNextChangeAt)
			SetLoadingMessage();
	}

	private void SetLoadingMessage()
	{
		var nextIndex = Random.Shared.Next(LoadingMessages.Length);
		while (LoadingMessages.Length > 1 && nextIndex == _loadingMessageIndex)
			nextIndex = Random.Shared.Next(LoadingMessages.Length);

		_loadingMessageIndex = nextIndex;
		_loadingLabel.Text = LoadingMessages[nextIndex];
		_loadingMessageNextChangeAt = Time.GetTicksMsec() / 1000.0 + 2.5;
	}
}
