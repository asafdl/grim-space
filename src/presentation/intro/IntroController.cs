using Godot;

namespace GrimSpace.Presentation.Intro;

public partial class IntroController : Control
{
	private const string MainScenePath = "res://scenes/main.tscn";
	private const double CharIntervalSeconds = 0.048;
	private const double ShowNextButtonSeconds = 0.45;
	private const double AutoAdvanceSeconds = 10.0;

	[Export] public IntroStory? Story { get; set; }

	private IntroSceneView _scene = null!;

	private int _pageIndex;
	private int _visibleChars;
	private double _elapsed;
	private bool _typingComplete;
	private bool _nextVisible;
	private bool _advancing;

	public override void _Ready()
	{
		_scene = GetNode<IntroSceneView>("Scene");

		if (Story is null || Story.Pages.Length == 0)
		{
			GetTree().ChangeSceneToFile(MainScenePath);
			return;
		}

		BeginPage();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		GetViewport().SetInputAsHandled();
		GetTree().ChangeSceneToFile(MainScenePath);
	}

	public override void _Process(double delta)
	{
		if (Story is null || _pageIndex >= Story.Pages.Length)
			return;

		var pageText = Story.Pages[_pageIndex];
		if (!_typingComplete)
		{
			_elapsed += delta;
			while (_elapsed >= CharIntervalSeconds && _visibleChars < pageText.Length)
			{
				_elapsed -= CharIntervalSeconds;
				_visibleChars++;
				_scene.SetBodyText(pageText[.._visibleChars]);
			}

			if (_visibleChars >= pageText.Length)
			{
				_typingComplete = true;
				_elapsed = 0;
			}

			return;
		}

		_elapsed += delta;
		if (!_nextVisible && _elapsed >= ShowNextButtonSeconds)
			ShowNextButton();

		if (_elapsed >= AutoAdvanceSeconds)
			AdvancePage();
	}

	private void BeginPage()
	{
		_advancing = false;
		_visibleChars = 0;
		_elapsed = 0;
		_typingComplete = false;
		_nextVisible = false;
		_scene.SetBodyText("");
		_scene.SetNextVisible(false);
		_scene.SetBackground(PageBackground(_pageIndex));
	}

	private Texture2D? PageBackground(int pageIndex)
	{
		var backgrounds = Story!.Backgrounds;
		if (pageIndex >= backgrounds.Length)
			return null;

		return backgrounds[pageIndex];
	}

	private void ShowNextButton()
	{
		_nextVisible = true;
		_scene.SetNextVisible(true);
		_scene.SetNextText(_pageIndex >= Story!.Pages.Length - 1 ? "Begin" : "Next");
		_scene.FocusNextButton();
	}

	public void OnNextPressed() => AdvancePage();

	private void AdvancePage()
	{
		if (_advancing)
			return;

		_advancing = true;
		_pageIndex++;
		if (_pageIndex >= Story!.Pages.Length)
		{
			GetTree().ChangeSceneToFile(MainScenePath);
			return;
		}

		BeginPage();
	}
}
