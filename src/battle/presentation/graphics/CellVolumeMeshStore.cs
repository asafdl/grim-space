using Godot;
using GrimSpace.Core.Cache;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal sealed class CellVolumeMeshStore : IDisposable
{
	private const int Capacity = 96;
	private const int MaxPending = 8;
	private const int RetentionTicks = 1;

	private readonly int _ownerThreadId = System.Environment.CurrentManagedThreadId;
	private readonly TickGenerationCache<
		string,
		BuildRequest,
		CellVolumeMesh.Prepared,
		ArrayMesh> _exactMeshes;

	public CellVolumeMeshStore()
	{
		_exactMeshes = new TickGenerationCache<
			string,
			BuildRequest,
			CellVolumeMesh.Prepared,
			ArrayMesh>(
			Capacity,
			MaxPending,
			RetentionTicks,
			Prepare,
			CellVolumeMesh.Create);
	}

	internal int ResourceCount => _exactMeshes.ResourceCount;
	internal int PendingCount => _exactMeshes.PendingCount;
	internal sealed record GenerationFailure(string Key, Exception Error);
	internal sealed record PumpResult(
		int ProcessedCount,
		int FinalizedCount,
		IReadOnlyList<GenerationFailure> Failures);

	public bool Request(
		CellVolumePreview volume,
		CellVolumeGeometry.Settings settings,
		ECellVolumeMeshPrimitive primitive,
		int tick,
		out ArrayMesh mesh)
	{
		var key = Key(volume, settings, primitive);
		return _exactMeshes.Request(
			key,
			() => new BuildRequest(
				volume.Origin,
				volume.Cells.ToArray(),
				settings,
				primitive),
			tick,
			out mesh);
	}

	public PumpResult Pump(int tick)
	{
		if (System.Environment.CurrentManagedThreadId != _ownerThreadId)
			throw new InvalidOperationException(
				"Cell-volume meshes must be uploaded on their owning thread.");

		var result = _exactMeshes.Pump(tick);
		return new PumpResult(
			result.ProcessedCount,
			result.FinalizedCount,
			result.Failures
				.Select(failure => new GenerationFailure(
					failure.Key,
					failure.Error))
				.ToArray());
	}

	public void Dispose() => _exactMeshes.Dispose();

	internal static string Key(
		CellVolumePreview volume,
		CellVolumeGeometry.Settings settings,
		ECellVolumeMeshPrimitive primitive) =>
		$"{primitive}:{CellVolumeGeometry.RelativeCellKey(volume.Origin, volume.Cells, settings)}";

	private static CellVolumeMesh.Prepared Prepare(
		BuildRequest request,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var surface = CellVolumeGeometry.Build(
			request.Origin,
			request.Cells,
			request.Settings);
		cancellationToken.ThrowIfCancellationRequested();
		return request.Primitive switch
		{
			ECellVolumeMeshPrimitive.Triangles => CellVolumeMesh.PrepareTriangles(surface),
			ECellVolumeMeshPrimitive.Wireframe => CellVolumeMesh.PrepareWireframe(surface),
			_ => throw new ArgumentOutOfRangeException(
				nameof(request),
				request.Primitive,
				null),
		};
	}

	private sealed record BuildRequest(
		Coord Origin,
		Coord[] Cells,
		CellVolumeGeometry.Settings Settings,
		ECellVolumeMeshPrimitive Primitive);
}
