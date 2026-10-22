using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ResourceHud : MarginContainer
{
	private const string TransactionSfxPath = "res://assets/sfx/ui/resource_transaction.wav";
	private const int IconSize = 22;
	private const int AmountMinimumWidth = 48;
	private const int DeltaLabelHeight = 18;
	private const int EntrySeparation = 16;
	private const int ResourcePanelMinimumWidth = 408;
	private const int ResourcePanelMinimumHeight = 94;
	private const float CountUpSpeed = 140f;
	private const float DeltaHoldDuration = 4f;
	private const float DeltaFadeDuration = 1.5f;

	private readonly Dictionary<ResourceId, Label> _amountLabels = new();
	private readonly Dictionary<ResourceId, Label> _deltaLabels = new();
	private readonly Dictionary<ResourceId, float> _displayedAmounts = new();
	private readonly Dictionary<ResourceId, int> _targetAmounts = new();
	private readonly Dictionary<ResourceId, Tween> _deltaTweens = new();
	private AudioStreamPlayer _transactionSfx = null!;
	private Label _feedbackLabel = null!;
	private Tween? _feedbackTween;

	public override void _Ready()
	{
		_transactionSfx = new AudioStreamPlayer
		{
			Stream = GD.Load<AudioStream>(TransactionSfxPath)
				?? throw new InvalidOperationException($"Could not load SFX '{TransactionSfxPath}'."),
			Bus = AudioBuses.Sfx,
		};
		AddChild(_transactionSfx);
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
		if (transaction.Change.IsEmpty)
			return;

		foreach (var (id, delta) in transaction.Change)
		{
			_displayedAmounts[id] = _targetAmounts[id];
			_targetAmounts[id] += delta;
			ShowDelta(id, delta);
		}

		UpdateAmountLabels();
		_transactionSfx.Play();
	}

	public void PresentInsufficientFunds()
	{
		_feedbackTween?.Kill();
		_feedbackLabel.Text = "INSUFFICIENT FUNDS";
		_feedbackLabel.Modulate = HudStyles.TextColor(HudTextRole.Danger);

		var tween = CreateTween();
		_feedbackTween = tween;
		tween.TweenInterval(DeltaHoldDuration);
		tween.TweenProperty(_feedbackLabel, "modulate:a", 0f, DeltaFadeDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.Finished += () =>
		{
			if (!ReferenceEquals(_feedbackTween, tween))
				return;

			_feedbackTween = null;
			_feedbackLabel.Text = "";
		};
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
			? HudStyles.TextColor(HudTextRole.Success)
			: HudStyles.TextColor(HudTextRole.Danger);

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
		var content = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		content.AddThemeConstantOverride("separation", 2);
		AddChild(content);

		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", EntrySeparation);

		var panel = new UiNestedContainer(UiContainerVisual.SidebarLandscape)
		{
			CustomMinimumSize = new Vector2(
				ResourcePanelMinimumWidth,
				ResourcePanelMinimumHeight),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		panel.SetContent(row);
		content.AddChild(panel);

		foreach (var id in Enum.GetValues<ResourceId>())
			row.AddChild(CreateEntry(id));

		_feedbackLabel = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(0, DeltaLabelHeight),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = Colors.Transparent,
		};
		HudStyles.ApplyTextRole(_feedbackLabel, HudTextRole.Danger);
		content.AddChild(_feedbackLabel);
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
		};
		HudStyles.ApplyTextRole(amount, HudTextRole.Emphasis);
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
		};
		HudStyles.ApplyTextRole(delta, HudTextRole.Metadata);
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
