using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class WorldMapDirector
{
	private readonly MapPresentationContext _ctx;
	private readonly Dictionary<string, IPresentationMode> _modes = new(StringComparer.Ordinal);
	private readonly List<Action> _pendingFocusCallbacks = [];
	private IPresentationMode? _currentMode;
	private bool _isTransitioning;
	private int _transitionToken;

	public WorldMapDirector(MapPresentationContext context) => _ctx = context;

	public string? CurrentModeId => _currentMode?.Id;
	public IPresentationMode? CurrentMode => _currentMode;
	public bool IsTransitioning => _isTransitioning;

	public PresentationInputPolicy EffectiveInputPolicy =>
		_isTransitioning || (_currentMode?.IsBusy ?? false)
			? PresentationInputPolicy.Locked
			: _currentMode?.InputPolicy ?? PresentationInputPolicy.Locked;

	public void RegisterMode(IPresentationMode mode) => _modes[mode.Id] = mode;

	public void SetInitialMode(string id, object? payload = null)
	{
		if (!_modes.TryGetValue(id, out var target))
			throw new InvalidOperationException($"Unknown presentation mode '{id}'.");

		var validation = target.ValidateEnterPayload(payload);
		if (!validation.Succeeded)
			throw new InvalidOperationException(
				$"Invalid bootstrap payload for mode '{id}': {validation.Failure}.");

		_ctx.SetOcclusionEnabled(UsesCameraOcclusion(target));
		_ctx.ApplyLimits(target.Limits);
		var pose = target.ResolveEnterPose(string.Empty, _ctx, payload);
		target.OnEntering(_ctx, string.Empty, payload);
		_ctx.SnapToPose(pose, target.Limits);
		target.OnSettled(_ctx);
		_currentMode = target;
		_isTransitioning = false;
	}

	public PresentationTransitionResult TryEnter(string modeId, object? payload = null)
	{
		if (_isTransitioning)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.AlreadyTransitioning);

		if (_currentMode is null)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.WrongCurrentMode);

		if (_currentMode.IsBusy)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.ModeBusy);

		if (!_modes.TryGetValue(modeId, out var target))
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.UnknownMode);

		if (target.IsBusy)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.ModeBusy);

		var source = _currentMode;
		if (ReferenceEquals(source, target))
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.NotAllowed);

		var payloadValidation = target.ValidateEnterPayload(payload);
		if (!payloadValidation.Succeeded)
			return payloadValidation;

		if (!target.AllowedFrom.Contains(source.Id))
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.NotAllowed);

		if (!target.CanEnter(_ctx, source.Id, payload))
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.CanEnterRejected);

		BeginTransition(source, target, payload);
		return PresentationTransitionResult.Ok();
	}

	public PresentationTransitionResult TryExit(string modeId)
	{
		if (_currentMode is null || _currentMode.Id != modeId)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.WrongCurrentMode);

		var exitTargetId = _currentMode.ExitTargetId;
		if (exitTargetId is null)
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.ExitTargetMissing);

		return TryEnter(exitTargetId);
	}

	public void Update(double delta) => _currentMode?.Update(_ctx, delta);

	public PresentationTransitionResult PrepareForFocus(Action onReady)
	{
		ArgumentNullException.ThrowIfNull(onReady);
		if (_isTransitioning || (_currentMode?.IsBusy ?? false))
			return PresentationTransitionResult.Fail(PresentationTransitionFailure.ModeBusy);

		var modeId = _currentMode?.Id;
		if (modeId is null or CinematicPresentationMode.ModeId)
		{
			onReady();
			return PresentationTransitionResult.Ok();
		}

		if (modeId is OverviewPresentationMode.ModeId or FacadePresentationMode.ModeId)
		{
			var result = TryExit(modeId);
			if (result.Succeeded)
				_pendingFocusCallbacks.Add(onReady);
			return result;
		}

		onReady();
		return PresentationTransitionResult.Ok();
	}

	public bool FilterInput(InputEvent @event)
	{
		if (_currentMode is FacadePresentationMode facade && facade.FilterInput(@event))
		{
			if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
			    && !IsTransitioning
			    && !facade.IsBusy)
				TryExit(FacadePresentationMode.ModeId);
			return true;
		}

		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right })
			return !EffectiveInputPolicy.AllowsRmbMovement;

		return false;
	}

	private void BeginTransition(IPresentationMode source, IPresentationMode target, object? payload)
	{
		var token = ++_transitionToken;
		_isTransitioning = true;

		source.OnExiting(_ctx, target.Id);
		_ctx.SetOcclusionEnabled(UsesCameraOcclusion(target));
		_ctx.ApplyLimits(target.Limits);
		var targetPose = target.ResolveEnterPose(source.Id, _ctx, payload);
		target.OnEntering(_ctx, source.Id, payload);

		_ctx.TweenToPose(targetPose, target.Limits, () =>
		{
			if (token != _transitionToken)
				return;

			_isTransitioning = false;
			target.OnSettled(_ctx);
			_currentMode = target;
			InvokePendingFocusCallbacks();
		});
	}

	private void InvokePendingFocusCallbacks()
	{
		foreach (var callback in _pendingFocusCallbacks.ToArray())
			callback();
		_pendingFocusCallbacks.Clear();
	}

	private static bool UsesCameraOcclusion(IPresentationMode mode) =>
		mode.Id == CinematicPresentationMode.ModeId;
}
