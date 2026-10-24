using Godot;

namespace GrimSpace.Ui.Components;

public interface IUiSized
{
	UiSize DefaultSize { get; }
	UiSizeOverride? SizeOverride { get; set; }
}

internal sealed class UiSizing
{
	private readonly Control _control;
	private UiSizeOverride? _sizeOverride;

	public UiSize DefaultSize { get; private set; }

	public UiSizeOverride? SizeOverride
	{
		get => _sizeOverride;
		set
		{
			_sizeOverride = value;
			Apply();
		}
	}

	public UiSizing(Control control, UiSize defaultSize)
	{
		_control = control;
		SetDefaultSize(defaultSize);
	}

	public void SetDefaultSize(UiSize defaultSize)
	{
		DefaultSize = defaultSize;
		Apply();
	}

	private void Apply()
	{
		var size = DefaultSize.WithOverride(_sizeOverride);
		_control.CustomMinimumSize = new Vector2(size.Width, size.Height);
	}
}
