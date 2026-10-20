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

	[Export(PropertyHint.Range, "0.005,0.05,0.005")]
	private float _dustBandEndHalfThicknessRatio = 0.015f;

	private Control _menuColumn = null!;
	private GameSettingsMenu _settingsMenu = null!;
	private Control _loadingOverlay = null!;
	private Control _loadingGear = null!;
	private Label _loadingLabel = null!;
	private int _loadingMessageIndex = -1;
	private double _loadingMessageNextChangeAt;
	private Control _art = null!;
	private Control _dustBandStart = null!;
	private Control _dustBandEnd = null!;
	private Control _dustBandWide = null!;
	private GpuParticles2D _dustParticles = null!;
	private ParticleProcessMaterial _dustMaterial = null!;
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
		_settingsMenu.CloseRequested += ShowMainPanel;
		_loadingOverlay = GetNode<Control>("%LoadingOverlay");
		_loadingGear = GetNode<Control>("%LoadingGear");
		_loadingLabel = GetNode<Label>("%LoadingLabel");
		_art = GetNode<Control>("ArtFrame/Art");
		_dustBandStart = GetNode<Control>("%DustBandStart");
		_dustBandEnd = GetNode<Control>("%DustBandEnd");
		_dustBandWide = GetNode<Control>("%DustBandWide");
		_dustParticles = GetNode<GpuParticles2D>("%DustParticles");
		_dustMaterial = (ParticleProcessMaterial)_dustParticles.ProcessMaterial.Duplicate();
		_dustParticles.ProcessMaterial = _dustMaterial;
		_art.Resized += OnDustLayoutChanged;
		CallDeferred(MethodName.UpdateDustLayout);

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
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
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

	private void ShowSettingsPanel()
	{
		_menuColumn.Hide();
		_settingsMenu.Open();
	}

	private void ShowMainPanel()
	{
		_settingsMenu.Hide();
		_menuColumn.Show();
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

	private void OnContinue()
	{
		_continueButton.Disabled = true;
		_newGameButton.Disabled = true;
		_menuColumn.Hide();
		_loadingOverlay.Show();
		_loadingLabel.Text = "Restoring run...";

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

	private void OnDustLayoutChanged() => UpdateDustLayout();

	private void UpdateDustLayout()
	{
		var artSize = _art.Size;
		if (artSize.X < 1f || artSize.Y < 1f)
			return;

		var start = _dustBandStart.Position;
		var end = _dustBandEnd.Position;
		var wide = _dustBandWide.Position;
		var delta = end - start;
		var length = delta.Length();
		if (length < 1f)
			return;

		var tangent = delta / length;
		var normal = new Vector2(-tangent.Y, tangent.X);
		var startHalf = Mathf.Abs((wide - start).Dot(normal));
		var endHalf = artSize.Y * _dustBandEndHalfThicknessRatio;
		var bandHalfThickness = (startHalf + endHalf) * 0.5f;

		var center = (start + end) * 0.5f;
		var visibilityPad = length * 0.5f + bandHalfThickness + 64f;

		_dustParticles.Position = center;
		_dustParticles.Rotation = delta.Angle();
		_dustMaterial.EmissionBoxExtents = new Vector3(length * 0.5f, bandHalfThickness, 1f);
		_dustParticles.VisibilityRect = new Rect2(
			-visibilityPad,
			-visibilityPad,
			visibilityPad * 2f,
			visibilityPad * 2f);
	}
}
