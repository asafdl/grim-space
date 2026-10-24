using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiToggle : HBoxContainer, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private const string OnTexturePath =
		"res://assets/ui/components/runtime/toggle_on_base.png";
	private const string OffTexturePath =
		"res://assets/ui/components/runtime/toggle_off_base.png";

	private readonly TextureButton _button;
	private HudPalette _palette;
	private bool _enabled = true;

	public event Action<bool>? Toggled;

	public bool Value
	{
		get => _button.ButtonPressed;
		set
		{
			_button.ButtonPressed = value;
			RefreshState();
		}
	}

	public UiToggle(bool value = false, HudPalette? palette = null)
	{
		_palette = palette ?? HudPalettes.BlueCyan;
		Alignment = AlignmentMode.Center;
		_sizing = new UiSizing(this, new UiSize(62f, 28f));

		var onTexture = LoadTexture(OnTexturePath);
		_button = new TextureButton
		{
			ToggleMode = true,
			IgnoreTextureSize = true,
			StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
			TextureNormal = LoadTexture(OffTexturePath),
			TexturePressed = onTexture,
			CustomMinimumSize = new Vector2(62f, 28f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			FocusMode = FocusModeEnum.All,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		_button.Toggled += OnToggled;
		AddChild(_button);

		Value = value;
		SetPalette(_palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		RefreshState();
	}

	public void SetEnabled(bool enabled)
	{
		_enabled = enabled;
		_button.Disabled = !enabled;
		RefreshState();
	}

	private void OnToggled(bool value)
	{
		RefreshState();
		Toggled?.Invoke(value);
	}

	private void RefreshState()
	{
		_button.Modulate = _enabled
			? PaletteTint()
			: HudPalettes.WithAlpha(PaletteTint(), 0.45f);
	}

	private Color PaletteTint() =>
		_palette == HudPalettes.BlueCyan ? Colors.White : _palette.Primary;

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load toggle texture '{path}'.");
}
