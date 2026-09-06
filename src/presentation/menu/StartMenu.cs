using Godot;
using GrimSpace.Core;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string IntroScenePath = "res://scenes/intro.tscn";

	private const float DustRegionAnchorLeft = 0.48f;
	private const float VisibilityRectPadding = 128f;
	private int _dustLayoutAttempts;

	private Control _menuColumn = null!;
	private Control _settingsOverlay = null!;
	private Control _art = null!;
	private Control _dustRegion = null!;
	private GpuParticles2D _dustParticles = null!;
	private ParticleProcessMaterial _dustMaterial = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;

	public override void _Ready()
	{
		_menuColumn = GetNode<Control>("%MenuColumn");
		_settingsOverlay = GetNode<Control>("%SettingsOverlay");
		_art = GetNode<Control>("ArtFrame/Art");
		_dustRegion = GetNode<Control>("%DustRegion");
		_dustParticles = _dustRegion.GetNode<GpuParticles2D>("%DustParticles");
		_dustMaterial = (ParticleProcessMaterial)_dustParticles.ProcessMaterial.Duplicate();
		_dustParticles.ProcessMaterial = _dustMaterial;
		_dustRegion.Resized += OnDustRegionResized;
		_art.Resized += OnDustRegionResized;
		CallDeferred(MethodName.UpdateDustLayout);
		CallDeferred(MethodName.FinalizeDustLayout);
		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");

		_displayMode.ItemSelected += _ => UpdateResolutionEnabled();

		GetNode<Button>("%PlayIntro").Pressed += OnPlayIntro;
		GetNode<Button>("%Start").Pressed += OnStart;
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
		GetNode<Button>("%Back").Pressed += ShowMainPanel;
		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();
	}

	private void LoadSettingsToUi()
	{
		var (mode, width, height) = GameSettings.ReadVideoConfig();
		_displayMode.Selected = mode == "windowed" ? 1 : 0;
		SelectResolution(width, height);
		UpdateResolutionEnabled();
	}

	private void SelectResolution(int width, int height)
	{
		if (GameSettings.TryFindResolutionIndex(width, height, out var index))
			_resolution.Selected = index;
		else
			_resolution.Selected = 0;
	}

	private Vector2I SelectedResolution() => GameSettings.SupportedResolutions[_resolution.Selected];

	private void ApplyVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		GameSettings.ApplyVideoConfig(
			windowed ? "windowed" : "fullscreen",
			SelectedResolution());
	}

	private void SaveVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		var size = windowed
			? SelectedResolution()
			: GameSettings.SupportedResolutions[0];

		GameSettings.SaveVideoConfig(
			windowed ? "windowed" : "fullscreen",
			size.X,
			size.Y);
	}

	private void UpdateResolutionEnabled() =>
		_resolution.Disabled = _displayMode.Selected != 1;

	private void OnApply()
	{
		ApplyVideoSettings();
		SaveVideoSettings();
	}

	private void ShowSettingsPanel()
	{
		_menuColumn.Visible = false;
		_settingsOverlay.Visible = true;
		LoadSettingsToUi();
	}

	private void ShowMainPanel()
	{
		_settingsOverlay.Visible = false;
		_menuColumn.Visible = true;
	}

	private void OnPlayIntro() =>
		GetTree().ChangeSceneToFile(IntroScenePath);

	private void OnStart()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ChangeSceneToFile("res://scenes/battle.tscn");
	}

	private void OnDustRegionResized()
	{
		if (UpdateDustLayout())
			_dustParticles.Restart();
	}

	private void FinalizeDustLayout()
	{
		if (UpdateDustLayout())
		{
			_dustParticles.Restart();
			_dustParticles.Emitting = true;
			return;
		}

		if (++_dustLayoutAttempts < 8)
			CallDeferred(MethodName.FinalizeDustLayout);
	}

	private bool UpdateDustLayout()
	{
		var size = ResolveDustRegionSize();
		if (size.X < 1f || size.Y < 1f)
			return false;

		var half = size * 0.5f;
		var pad = VisibilityRectPadding;
		_dustParticles.Position = half;
		_dustMaterial.EmissionBoxExtents = new Vector3(half.X, half.Y, 1f);
		_dustParticles.VisibilityRect = new Rect2(
			-half.X - pad,
			-half.Y - pad,
			size.X + pad * 2f,
			size.Y + pad * 2f);
		return true;
	}

	private Vector2 ResolveDustRegionSize()
	{
		var size = _dustRegion.Size;
		if (size.X >= 1f && size.Y >= 1f)
			return size;

		var artSize = _art.Size;
		if (artSize.X < 1f || artSize.Y < 1f)
			return Vector2.Zero;

		return new Vector2(artSize.X * (1f - DustRegionAnchorLeft), artSize.Y);
	}
}
