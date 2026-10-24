namespace GrimSpace.Ui.Components;

public readonly record struct UiSize
{
	public float Width { get; }
	public float Height { get; }

	public UiSize(float width, float height)
	{
		ValidateDimension(width, nameof(width));
		ValidateDimension(height, nameof(height));
		Width = width;
		Height = height;
	}

	public UiSize WithOverride(UiSizeOverride? sizeOverride) =>
		new(sizeOverride?.Width ?? Width, sizeOverride?.Height ?? Height);

	internal static void ValidateDimension(float value, string name)
	{
		if (!float.IsFinite(value) || value < 0f)
			throw new ArgumentOutOfRangeException(name, value, "UI dimensions must be finite and non-negative.");
	}
}

public readonly record struct UiSizeOverride
{
	public float? Width { get; }
	public float? Height { get; }

	public UiSizeOverride(float? Width = null, float? Height = null)
	{
		if (Width is { } width)
			UiSize.ValidateDimension(width, nameof(Width));
		if (Height is { } height)
			UiSize.ValidateDimension(height, nameof(Height));
		this.Width = Width;
		this.Height = Height;
	}
}
