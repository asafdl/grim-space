using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class ResourceHud : MarginContainer
{
	private const string TransactionSfxPath = "res://assets/sfx/ui/resource_transaction.wav";
	private const int DeltaLabelHeight = 18;
	private const int DeltaLabelWidth = 56;
	private const int RailSeparation = 1;
	private const int BottomMargin = 12;
	private const float CountUpSpeed = 140f;
	private const float DeltaHoldDuration = 4f;
	private const float DeltaFadeDuration = 1.5f;

	private readonly Dictionary<ResourceId, ResourceRail> _rails = new();
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

		var isTravelFuel = transaction.Source == TransactionSource.TravelFuel;
		var isHazardPay = transaction.Source == TransactionSource.HazardPay;
		foreach (var (id, delta) in transaction.Change)
		{
			_displayedAmounts[id] = _targetAmounts[id];
			_targetAmounts[id] += delta;
			if (!isTravelFuel || id != ResourceId.Fuel)
				ShowDelta(id, delta);
		}

		UpdateAmountLabels();
		if (isTravelFuel)
			return;

		_transactionSfx.Play();
		if (isHazardPay)
			PresentFeedback("HAZARD PAY", hold: true, HudTextRole.Warning);
	}

	public void PresentInsufficientFunds() =>
		PresentFeedback("INSUFFICIENT FUNDS", hold: true);

	public void PresentTravelFuelStatus(int fuelBalance, int routeFuelEstimate, bool starving)
	{
		if (starving)
		{
			PresentFeedback("Not enough fuel. Can't manuever properly.", hold: false);
			return;
		}

		if (routeFuelEstimate <= 0)
		{
			ClearPersistentFeedback();
			return;
		}

		ClearPersistentFeedback();
	}

	private void PresentFeedback(string text, bool hold, HudTextRole role = HudTextRole.Danger)
	{
		_feedbackTween?.Kill();
		_feedbackLabel.Text = text;
		HudStyles.ApplyTextRole(_feedbackLabel, role);
		_feedbackLabel.Modulate = HudStyles.TextColor(role);

		if (!hold)
		{
			_feedbackTween = null;
			return;
		}

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

	private void ClearPersistentFeedback()
	{
		if (_feedbackTween is not null)
			return;

		_feedbackLabel.Text = "";
		_feedbackLabel.Modulate = Colors.Transparent;
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
		foreach (var (id, rail) in _rails)
			rail.SetValueText(Mathf.RoundToInt(_displayedAmounts[id]).ToString());
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
		SetAnchorsPreset(LayoutPreset.BottomRight);
		GrowHorizontal = GrowDirection.Begin;
		GrowVertical = GrowDirection.Begin;
		MouseFilter = MouseFilterEnum.Ignore;

		AddThemeConstantOverride("margin_right", HudStyles.Margin);
		AddThemeConstantOverride("margin_bottom", BottomMargin);
	}

	private void Build()
	{
		var content = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		content.AddThemeConstantOverride("separation", RailSeparation);
		AddChild(content);

		_feedbackLabel = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(0, DeltaLabelHeight),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = Colors.Transparent,
		};
		HudStyles.ApplyTextRole(_feedbackLabel, HudTextRole.Danger);
		content.AddChild(_feedbackLabel);

		foreach (var id in Enum.GetValues<ResourceId>())
			content.AddChild(CreateEntry(id));
	}

	private Control CreateEntry(ResourceId id)
	{
		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", 4);

		var tooltipHost = new HudTooltipHost
		{
			MouseFilter = MouseFilterEnum.Stop,
		};
		tooltipHost.SetTooltip(ResourceIconCatalog.Tooltip(id));
		row.AddChild(tooltipHost);

		var rail = new ResourceRail(ResourceIconCatalog.RailColor(id), ResourceIconCatalog.IconPath(id));
		tooltipHost.AddChild(rail);
		_rails[id] = rail;
		_displayedAmounts[id] = 0f;
		_targetAmounts[id] = 0;

		var delta = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(DeltaLabelWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = Colors.Transparent,
		};
		HudStyles.ApplyTextRole(delta, HudTextRole.Metadata);
		row.AddChild(delta);
		row.MoveChild(delta, 0);
		_deltaLabels[id] = delta;

		return row;
	}
}
