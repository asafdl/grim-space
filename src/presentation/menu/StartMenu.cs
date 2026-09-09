using Godot;
using GrimSpace.Core;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string IntroScenePath = "res://scenes/intro.tscn";

	[Export(PropertyHint.Range, "0.005,0.05,0.005")]
	private float _dustBandEndHalfThicknessRatio = 0.015f;

	private Control _menuColumn = null!;
	private Control _settingsOverlay = null!;
	private Control _art = null!;
	private Control _dustBandStart = null!;
	private Control _dustBandEnd = null!;
	private Control _dustBandWide = null!;
	private GpuParticles2D _dustParticles = null!;
	private ParticleProcessMaterial _dustMaterial = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;
	private Button _startButton = null!;

	public override void _Ready()
	{
		_menuColumn = GetNode<Control>("%MenuColumn");
		_settingsOverlay = GetNode<Control>("%SettingsOverlay");
		_art = GetNode<Control>("ArtFrame/Art");
		_dustBandStart = GetNode<Control>("%DustBandStart");
		_dustBandEnd = GetNode<Control>("%DustBandEnd");
		_dustBandWide = GetNode<Control>("%DustBandWide");
		_dustParticles = GetNode<GpuParticles2D>("%DustParticles");
		_dustMaterial = (ParticleProcessMaterial)_dustParticles.ProcessMaterial.Duplicate();
		_dustParticles.ProcessMaterial = _dustMaterial;
		_art.Resized += OnDustLayoutChanged;
		CallDeferred(MethodName.UpdateDustLayout);

		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");

		_displayMode.ItemSelected += _ => UpdateResolutionEnabled();

		_startButton = GetNode<Button>("%Start");
		GetNode<Button>("%PlayIntro").Pressed += OnPlayIntro;
		_startButton.Pressed += OnStart;
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
		GetNode<Button>("%Back").Pressed += ShowMainPanel;
		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();

		CallDeferred(MethodName.PrepareFirstScene);
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

	private void PrepareFirstScene() =>
		RunSession.Instance.PrepareFirstScene();

	private async void OnStart()
	{
		_startButton.Disabled = true;
		try
		{
			await RunSession.Instance.BeginMapFromMenuAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to start map: {ex}");
			_startButton.Disabled = false;
		}
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
