using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Narrative;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class NarrativeHudOverlay : CanvasLayer
{
	private Control _root = null!;
	private FramedActionBar _bar = null!;
	private RichTextLabel _body = null!;
	private Label _feedback = null!;
	private readonly TypewriterPager _pager = new();
	private NarrativeDefinition? _narrative;

	private bool _busy;

	public event Action? Completed;
	public event Action<int>? PageBegan;

	public NarrativeHudOverlay()
	{
		Layer = 25;
		_pager.VisibleCharacterCountChanged += count => _body.VisibleCharacters = count;
		_pager.PageBegan += OnPageBegan;
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
	public RichTextLabel Body => _body;

	public void Open(NarrativeDefinition narrative)
	{
		_busy = false;
		_narrative = narrative;
		Visible = true;
		_pager.ConfigureCharacterCounts(
			narrative.Pages.Count,
			_ => _body.GetTotalCharacterCount());
		_bar.ActionVisible = false;
	}

	public void Close()
	{
		_narrative = null;
		_feedback.Visible = false;
		Visible = false;
	}

	public void SetBusy(bool busy) => _busy = busy;

	public void ShowError(string message)
	{
		_busy = false;
		ShowWorldLinkError(message);
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

		var bodyLayout = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		_body = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			ThemeTypeVariation = "NarrativeRichTextLabel",
			MetaUnderlined = true,
			VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping,
		};
		bodyLayout.AddChild(_body);
		_feedback = new Label
		{
			Visible = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ThemeTypeVariation = "MetadataLabel",
		};
		bodyLayout.AddChild(_feedback);

		_bar = new FramedActionBar(bodyLayout);
		_bar.ActionPressed += OnNextPressed;
		_root.AddChild(_bar);
	}

	public void ClearWorldLinkError() => _feedback.Visible = false;

	public void ShowWorldLinkError(string message)
	{
		_feedback.Text = message;
		_feedback.Visible = true;
	}

	private void OnPageBegan(int pageIndex)
	{
		var narrative = _narrative
			?? throw new InvalidOperationException("Cannot begin a narrative page before opening a narrative.");
		_body.Text = narrative.Pages[pageIndex];
		_body.VisibleCharacters = 0;
		_feedback.Visible = false;
		_bar.ActionVisible = false;
		PageBegan?.Invoke(pageIndex);
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
