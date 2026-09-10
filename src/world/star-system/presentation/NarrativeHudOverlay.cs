using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Narrative;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class NarrativeHudOverlay : CanvasLayer
{
	private Control _root = null!;
	private FramedActionBar _bar = null!;
	private readonly TypewriterPager _pager = new();

	private bool _busy;

	public event Action? Completed;

	public NarrativeHudOverlay()
	{
		Layer = 25;
		_pager.VisibleTextChanged += OnVisibleTextChanged;
		_pager.PageBegan += _ => _bar.ActionVisible = false;
		_pager.NextPromptReady += prompt =>
		{
			_bar.ActionVisible = true;
			_bar.ActionDisabled = _busy;
			_bar.ActionText = prompt.ButtonText;
			_bar.FocusAction();
		};
		_pager.Completed += () => Completed?.Invoke();
		Build();
		Visible = false;
	}

	public bool IsOpen => Visible;

	public void Open(NarrativeDefinition narrative)
	{
		_busy = false;
		Visible = true;
		_pager.Configure(narrative.Pages);
		_bar.ActionVisible = false;
	}

	public void Close()
	{
		Visible = false;
	}

	public void SetBusy(bool busy) => _busy = busy;

	public void ShowError(string message)
	{
		_busy = false;
		_bar.Text = message;
		_bar.ActionVisible = true;
		_bar.ActionDisabled = false;
		_bar.ActionText = "Next";
		_bar.FocusAction();
	}

	public bool TryHandleInput(InputEvent @event)
	{
		if (!IsOpen)
			return false;

		if (@event is InputEventMouseButton { Pressed: true })
		{
			if (!IsPointerOverPanel())
			{
				GetViewport().SetInputAsHandled();
				return true;
			}

			return false;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false })
		{
			GetViewport().SetInputAsHandled();
			return true;
		}

		return true;
	}

	public override void _Process(double delta)
	{
		if (!IsOpen)
			return;

		_pager.Tick(delta);
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
		HudThemes.Apply(_root, HudThemeFamily.Theatrical);

		_bar = new FramedActionBar();
		_bar.ActionPressed += OnNextPressed;
		_root.AddChild(_bar);
	}

	private void OnVisibleTextChanged(string text)
	{
		_bar.Text = text;
	}

	private void OnNextPressed()
	{
		if (_busy)
			return;

		_pager.Advance();
	}

	private bool IsPointerOverPanel()
	{
		var pointer = _root.GetGlobalMousePosition();
		return _bar.ContainsGlobalPoint(pointer);
	}
}
