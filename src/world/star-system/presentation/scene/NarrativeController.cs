using Godot;
using GrimSpace.Education;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class NarrativeController : IDisposable
{
	private readonly NarrativeHudOverlay _hud;
	private readonly WorldLinkNavigator _worldLinks;
	private readonly Func<string, NarrativeDefinition?> _resolveNarrative;
	private readonly Func<string, bool> _tryComplete;
	private readonly Action<string, string> _presentSystemError;
	private readonly IDisposable _beginSubscription;
	private NarrativeDefinition? _currentNarrative;

	public NarrativeController(
		NarrativeHudOverlay hud,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator,
		NarrativeDefinition? initialNarrative,
		Func<string, NarrativeDefinition?> resolveNarrative,
		Func<string, bool> tryComplete,
		Func<Action<string>, IDisposable> subscribeBegin,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_hud = hud;
		_worldLinks = new WorldLinkNavigator(worldFocus, worldIndicator);
		_resolveNarrative = resolveNarrative;
		_tryComplete = tryComplete;
		_presentSystemError = presentSystemError;
		_hud.Completed += OnCompleted;
		_hud.PageBegan += OnPageBegan;
		_hud.Body.MetaClicked += OnMetaClicked;
		_beginSubscription = subscribeBegin(OnNarrativeBegan);
		if (initialNarrative is not null)
			Show(initialNarrative);
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void OnNarrativeBegan(string narrativeId)
	{
		var narrative = _resolveNarrative(narrativeId);
		if (narrative is null)
		{
			GD.PushError($"Narrative '{narrativeId}' is not defined.");
			Close();
			return;
		}

		Show(narrative);
	}

	private void OnCompleted()
	{
		ClearIndicator();
		var narrative = _currentNarrative;
		if (narrative is null)
			return;

		_hud.SetBusy(true);
		if (_tryComplete(narrative.Id))
		{
			Close();
			return;
		}

		Close();
		_presentSystemError(
			"Could not continue the event.",
			$"Complete narrative action was rejected for narrative '{narrative.Id}'.");
	}

	private void Show(NarrativeDefinition narrative)
	{
		_currentNarrative = narrative;
		_hud.Open(narrative);
	}

	private void Close()
	{
		_currentNarrative = null;
		ClearIndicator();
		if (_hud.IsOpen)
			_hud.Close();
	}

	private void OnMetaClicked(Variant metadata)
	{
		ClearIndicator();
		if (metadata.VariantType != Variant.Type.String)
		{
			_presentSystemError(
				"Event target is unavailable.",
				$"Narrative received unsupported link metadata type '{metadata.VariantType}'.");
			return;
		}

		var objectId = metadata.AsString();
		var result = _worldLinks.Follow(objectId);
		if (result is not WorldLinkNavigationResult.Followed)
		{
			ShowWorldLinkFailure(objectId, result);
			return;
		}

	}

	private void ShowWorldLinkFailure(string objectId, WorldLinkNavigationResult result)
	{
		_presentSystemError(
			"Event target is unavailable.",
			$"Narrative world link '{objectId}' failed: {result.GetType().Name}.");
	}

	private void OnPageBegan(int _) => ClearIndicator();

	private void ClearIndicator() => _worldLinks.Clear();

	public void ClearWorldLinkIndicator() => _worldLinks.ClearIndicator();

	public void Dispose()
	{
		ClearIndicator();
		_beginSubscription.Dispose();
		_hud.Completed -= OnCompleted;
		_hud.PageBegan -= OnPageBegan;
		_hud.Body.MetaClicked -= OnMetaClicked;
		_worldLinks.Dispose();
	}
}
