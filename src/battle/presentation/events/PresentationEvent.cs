using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Events;

public readonly record struct PresentationEvent(IAction Action);

public interface IPresentationEventSink
{
	void OnActionApplied(PresentationEvent presentationEvent);
}
