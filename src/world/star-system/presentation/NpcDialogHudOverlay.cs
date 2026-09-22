using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Poi.Dialog;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class NpcDialogHudOverlay : CanvasLayer
{
	private Control _root = null!;
	private FramedActionBar _bar = null!;
	private RichTextLabel _body = null!;
	private HBoxContainer _choices = null!;
	private readonly TypewriterPager _pager = new();
	private NpcDialogDefinition? _dialog;

	public NpcDialogHudOverlay()
	{
		Layer = 25;
		_pager.VisibleCharacterCountChanged += count => _body.VisibleCharacters = count;
		_pager.PageBegan += OnPageBegan;
		_pager.NextPromptReady += _ => ShowChoices();
		Build();
		Visible = false;
	}

	public event Action<string>? ChoiceSelected;
	public event Action? Closed;

	public bool IsOpen => Visible;

	public void Open(NpcDialogDefinition dialog)
	{
		_dialog = dialog;
		Visible = true;
		ClearChoices();
		_bar.ActionVisible = false;
		_pager.ConfigureCharacterCounts(
			1,
			_ => _body.GetTotalCharacterCount());
	}

	public void Close()
	{
		_dialog = null;
		ClearChoices();
		Visible = false;
		Closed?.Invoke();
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

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			Close();
			GetViewport().SetInputAsHandled();
			return true;
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
			VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping,
		};
		_body.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				_pager.RevealCurrentPage();
		};
		bodyLayout.AddChild(_body);

		_choices = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End,
			Visible = false,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_choices.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		bodyLayout.AddChild(_choices);

		_bar = new FramedActionBar(bodyLayout);
		_root.AddChild(_bar);
	}

	private void OnPageBegan(int _)
	{
		var dialog = _dialog
			?? throw new InvalidOperationException("Cannot begin NPC dialog before opening a definition.");
		_body.Text = FormatLine(dialog);
		_body.VisibleCharacters = 0;
		ClearChoices();
		_bar.ActionVisible = false;
	}

	private static string FormatLine(NpcDialogDefinition dialog) =>
		$"[b]{dialog.SpeakerName}[/b]\n\n{dialog.Line}";

	private void ShowChoices()
	{
		var dialog = _dialog;
		if (dialog is null)
			return;

		ClearChoices();
		foreach (var choice in dialog.Choices)
		{
			var button = new Button
			{
				Text = choice.Label,
				FocusMode = Control.FocusModeEnum.All,
			};
			HudStyles.StyleButton(button, HudActionKind.Secondary);
			var choiceId = choice.Id;
			button.Pressed += () => ChoiceSelected?.Invoke(choiceId);
			_choices.AddChild(button);
		}

		_choices.Visible = dialog.Choices.Count > 0;
		if (dialog.Choices.Count > 0)
			((Button)_choices.GetChild(0)).GrabFocus();
	}

	private void ClearChoices()
	{
		foreach (var child in _choices.GetChildren())
			child.QueueFree();
		_choices.Visible = false;
	}

	private bool IsPointerOverPanel()
	{
		var pointer = _root.GetGlobalMousePosition();
		return _bar.ContainsGlobalPoint(pointer);
	}
}
