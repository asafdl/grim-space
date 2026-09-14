using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ResourceHud : MarginContainer
{
	private const int IconSize = 22;
	private const int AmountMinimumWidth = 48;
	private const int DeltaLabelHeight = 18;
	private const int EntrySeparation = 16;
	private const float CountUpSpeed = 140f;
	private const float DeltaHoldDuration = 4f;
	private const float DeltaFadeDuration = 1.5f;

	private readonly Dictionary<ResourceId, Label> _amountLabels = new();
	private readonly Dictionary<ResourceId, Label> _deltaLabels = new();
	private readonly Dictionary<ResourceId, float> _displayedAmounts = new();
	private readonly Dictionary<ResourceId, int> _targetAmounts = new();
	private readonly Dictionary<ResourceId, Tween> _deltaTweens = new();

	public override void _Ready()
	{
		ConfigureChrome();
		Build();
		SetProcess(true);
	}

	public void SetBalances(IReadOnlyDictionary<ResourceId, int> balances)
	{
		foreach (var id in Enum.GetValues<ResourceId>())
		{
			var amount = balances.TryGetValue(id, out var balance) ? balance : 0;
			_targetAmounts[id] = amount;
			_displayedAmounts[id] = amount;
		}

		UpdateAmountLabels();
	}

	public void PresentTransaction(Transaction transaction)
	{
		foreach (var (id, delta) in transaction.Change)
		{
			if (delta == 0)
				continue;

			_displayedAmounts[id] = _targetAmounts[id];
			_targetAmounts[id] += delta;
			ShowDelta(id, delta);
		}

		UpdateAmountLabels();
	}

	public override void _Process(double delta)
	{
		var animating = false;
		foreach (var id in Enum.GetValues<ResourceId>())
		{
			var displayed = _displayedAmounts[id];
			var target = _targetAmounts[id];
			if (Mathf.IsEqualApprox(displayed, target))
				continue;

			_displayedAmounts[id] = Mathf.MoveToward(displayed, target, CountUpSpeed * (float)delta);
			animating = true;
		}

		if (animating)
			UpdateAmountLabels();
	}

	private void UpdateAmountLabels()
	{
		foreach (var (id, label) in _amountLabels)
			label.Text = Mathf.RoundToInt(_displayedAmounts[id]).ToString();
	}

	private void ShowDelta(ResourceId id, int delta)
	{
		var label = _deltaLabels[id];
		if (_deltaTweens.Remove(id, out var previous))
			previous.Kill();

		label.Text = delta > 0 ? $"+{delta}" : delta.ToString();
		label.Modulate = delta > 0
			? new Color(0.45f, 0.95f, 0.55f)
			: new Color(0.95f, 0.42f, 0.42f);

		var tween = CreateTween();
		_deltaTweens[id] = tween;
		tween.TweenInterval(DeltaHoldDuration);
		tween.TweenProperty(label, "modulate:a", 0f, DeltaFadeDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.Finished += () =>
		{
			if (!_deltaTweens.TryGetValue(id, out var active) || !ReferenceEquals(active, tween))
				return;
			_deltaTweens.Remove(id);
			label.Text = "";
		};
	}

	private void ConfigureChrome()
	{
		SetAnchorsPreset(LayoutPreset.TopRight);
		AnchorLeft = 1f;
		AnchorRight = 1f;
		GrowHorizontal = GrowDirection.Begin;
		MouseFilter = MouseFilterEnum.Ignore;

		AddThemeConstantOverride("margin_top", 48);
		AddThemeConstantOverride(
			"margin_right",
			HudStyles.Margin + HudStyles.ObjectivesHudPanelWidth + HudStyles.HudSiblingGap);
	}

	private void Build()
	{
		var shell = HudWidgets.CreateInformativePanel("RESOURCES");
		shell.Root.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		AddChild(shell.Root);

		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", EntrySeparation);
		shell.Body.AddChild(row);

		foreach (var id in Enum.GetValues<ResourceId>())
			row.AddChild(CreateEntry(id));
	}

	private Control CreateEntry(ResourceId id)
	{
		var root = new Control
		{
			CustomMinimumSize = new Vector2(IconSize + 6 + AmountMinimumWidth, IconSize),
			MouseFilter = MouseFilterEnum.Stop,
			TooltipText = DisplayName(id),
		};

		var entry = new HBoxContainer
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		entry.AddThemeConstantOverride("separation", 6);
		root.AddChild(entry);

		var icon = new TextureRect
		{
			Texture = GD.Load<Texture2D>(IconPath(id)),
			CustomMinimumSize = new Vector2(IconSize, IconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		entry.AddChild(icon);

		var amount = new Label
		{
			Text = "0",
			CustomMinimumSize = new Vector2(AmountMinimumWidth, IconSize),
			HorizontalAlignment = HorizontalAlignment.Right,
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		};
		entry.AddChild(amount);
		_amountLabels[id] = amount;
		_displayedAmounts[id] = 0f;
		_targetAmounts[id] = 0;

		var delta = new Label
		{
			Text = "",
			AnchorLeft = 0f,
			AnchorRight = 1f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = IconSize + 6,
			OffsetTop = 2f,
			OffsetBottom = 2f + DeltaLabelHeight,
			GrowHorizontal = GrowDirection.Both,
			HorizontalAlignment = HorizontalAlignment.Right,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = Colors.Transparent,
			ThemeTypeVariation = HudStyles.InformativeItemTitleLabelType,
		};
		root.AddChild(delta);
		_deltaLabels[id] = delta;

		return root;
	}

	private static string DisplayName(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "Credits",
			ResourceId.ScrapAlloy => "Scrap Alloy",
			ResourceId.IndustrialCore => "Industrial Core",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};

	private static string IconPath(ResourceId id) =>
		id switch
		{
			ResourceId.Credits => "res://assets/ui/resources/credits.svg",
			ResourceId.ScrapAlloy => "res://assets/ui/resources/scrap-alloy.svg",
			ResourceId.IndustrialCore => "res://assets/ui/resources/industrial-core.svg",
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
		};
}
