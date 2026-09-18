using GrimSpace.Education;

namespace GrimSpace.World.StarSystem.Presentation;

internal sealed class PendingWorldFocusHandle : IWorldFocusHandle
{
	private IWorldFocusHandle? _lease;

	public void AttachLease(IWorldFocusHandle lease) => _lease = lease;

	public void Cancel() => _lease = null;

	public void Dispose() => _lease?.Dispose();
}
