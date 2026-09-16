using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class MapCameraFocusLeasesTests
{
	[Fact]
	public void Supersede_InvalidatesEarlierLease()
	{
		var leases = new MapCameraFocusLeases();
		var generation = leases.Begin();

		leases.Supersede();

		Assert.False(leases.IsCurrent(generation));
	}

	[Fact]
	public void StaleLeaseDispose_DoesNotRestoreCapturedPose()
	{
		var simulator = new FocusLeaseSimulator();

		var lease = simulator.BeginFocusLease(focusPivot: 10f);
		simulator.ApplyJourneyFraming(journeyPivot: 25f);
		lease.Dispose();

		Assert.Equal(25f, simulator.Pivot);
	}

	[Fact]
	public void CurrentLeaseDispose_RestoresCapturedPose()
	{
		var simulator = new FocusLeaseSimulator();
		simulator.Pivot = 5f;

		var lease = simulator.BeginFocusLease(focusPivot: 10f);
		lease.Dispose();

		Assert.Equal(5f, simulator.Pivot);
	}

	private sealed class FocusLeaseSimulator
	{
		private readonly MapCameraFocusLeases _leases = new();
		private float _pivot;

		public float Pivot
		{
			get => _pivot;
			set => _pivot = value;
		}

		public IDisposable BeginFocusLease(float focusPivot)
		{
			var capturedPivot = _pivot;
			var generation = _leases.Begin();
			_pivot = focusPivot;
			return new Lease(this, generation, capturedPivot);
		}

		public void ApplyJourneyFraming(float journeyPivot)
		{
			_leases.Supersede();
			_pivot = journeyPivot;
		}

		private sealed class Lease(FocusLeaseSimulator owner, int generation, float capturedPivot)
			: IDisposable
		{
			public void Dispose()
			{
				if (!owner._leases.IsCurrent(generation))
					return;

				owner.Pivot = capturedPivot;
			}
		}
	}
}
