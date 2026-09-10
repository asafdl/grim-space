using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Intro;

public partial class IntroController : Control
{
	private const string MainScenePath = "res://scenes/main.tscn";
	private const double AutoAdvanceSeconds = 10.0;

	[Export] public IntroStory? Story { get; set; }

	private IntroSceneView _scene = null!;
	private readonly TypewriterPager _pager = new();
	private bool _advancing;

	public override void _Ready()
	{
		_scene = GetNode<IntroSceneView>("Scene");
		_scene.NextPressed += AdvancePage;
		_pager.VisibleTextChanged += text => _scene.SetBodyText(text);
		_pager.PageBegan += pageIndex =>
		{
			_scene.SetBackground(PageBackground(pageIndex));
			_scene.SetNextVisible(false);
		};
		_pager.NextPromptReady += prompt =>
		{
			_scene.SetNextVisible(true);
			_scene.SetNextText(prompt.ButtonText);
			_scene.FocusNextButton();
		};
		_pager.AutoAdvanceDue += AdvancePage;
		_pager.Completed += OnPagerCompleted;

		if (Story is null || Story.Pages.Length == 0)
		{
			GetTree().ChangeSceneToFile(MainScenePath);
			return;
		}

		_pager.Configure(
			Story.Pages,
			(pageIndex, pageCount) => pageIndex >= pageCount - 1 ? "Begin" : "Next",
			autoAdvanceSeconds: AutoAdvanceSeconds);
		_scene.SetNextVisible(false);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		GetViewport().SetInputAsHandled();
		GetTree().ChangeSceneToFile(MainScenePath);
	}

	public override void _Process(double delta) => _pager.Tick(delta);

	private void AdvancePage()
	{
		if (_advancing)
			return;

		_advancing = true;
		_pager.Advance();
		_advancing = false;
	}

	private void OnPagerCompleted() => GetTree().ChangeSceneToFile(MainScenePath);

	private Texture2D? PageBackground(int pageIndex)
	{
		var backgrounds = Story!.Backgrounds;
		if (pageIndex >= backgrounds.Length)
			return null;

		return backgrounds[pageIndex];
	}
}
