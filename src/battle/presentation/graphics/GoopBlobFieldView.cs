using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GoopBlobFieldView : Node3D
{
	public const float RevealSeconds = 0.65f;

	private const int MinBlobsPerCell = 22;
	private const int MaxBlobsPerCell = 28;
	private const int MediumBlobsPerCell = 2;
	private const int FirstEdgeBlobIndex = MediumBlobsPerCell;
	private const int EdgeBlobCount = 6;
	private const float CellMargin = 0.08f;
	private const float SmallDiameterMin = 0.26f;
	private const float SmallDiameterMax = 0.38f;
	private const float MediumDiameterMin = 0.44f;
	private const float MediumDiameterMax = 0.56f;
	private const float EdgeDiameterMin = 0.38f;
	private const float EdgeDiameterMax = 0.5f;
	private const float ClusteredSmallChance = 0.64f;
	private const ulong DiameterSalt = 0xA24BAED4963EE407UL;
	private const ulong ShapeSalt = 0x9FB21C651E98DF25UL;
	private const ulong BasisSalt = 0xC13FA9A902A6328FUL;
	private const ulong PositionSalt = 0x91E10DA5C79E7B1DUL;
	private const ulong ClusterSalt = 0xD1B54A32D192ED03UL;
	private const ulong CustomDataSalt = 0xABC98388FB8FAC03UL;
	private const ulong AnimationSalt = 0x8CB92BA72F3D8DD7UL;

	private static Mesh? _blobMesh;
	private static Aabb _blobBounds;
	private static ShaderMaterial? _blobMaterial;

	private readonly HashSet<Coord> _cells = [];
	private MultiMeshInstance3D? _blobs;
	private ulong _seed;
	private bool _hasBuild;
	private float _revealElapsed;
	private float _revealDuration;
	private int _revealBlobCount;

	public void Build(IReadOnlySet<Coord> cells, ulong seed)
		=> Build(cells, seed, null, 0f);

	public void Reveal(
		IReadOnlySet<Coord> cells,
		ulong seed,
		Coord center,
		float durationSeconds = RevealSeconds)
	{
		if (!float.IsFinite(durationSeconds) || durationSeconds <= 0f)
			throw new ArgumentOutOfRangeException(
				nameof(durationSeconds),
				"Reveal duration must be finite and greater than zero.");

		Build(cells, seed, center, durationSeconds);
	}

	public override void _Process(double delta)
	{
		if (_blobs?.Multimesh is not { } multiMesh || _revealBlobCount == 0)
		{
			SetProcess(false);
			return;
		}

		_revealElapsed += (float)delta;
		var progress = Mathf.Clamp(_revealElapsed / _revealDuration, 0f, 1f);
		var easedProgress = progress * progress * (3f - 2f * progress);
		multiMesh.VisibleInstanceCount = System.Math.Min(
			_revealBlobCount,
			(int)MathF.Ceiling(_revealBlobCount * easedProgress));
		if (progress < 1f)
			return;

		_revealBlobCount = 0;
		SetProcess(false);
	}

	private void Build(
		IReadOnlySet<Coord> cells,
		ulong seed,
		Coord? revealCenter,
		float revealDuration)
	{
		ArgumentNullException.ThrowIfNull(cells);
		if (revealCenter is null && _hasBuild && _seed == seed && _cells.SetEquals(cells))
			return;

		RemoveBlobs();
		_cells.Clear();
		_cells.UnionWith(cells);
		_seed = seed;
		_hasBuild = true;

		if (cells.Count == 0)
			return;

		EnsureResources();
		var orderedCells = cells
			.OrderBy(cell => revealCenter is { } center
				? DistanceSquared(cell, center)
				: 0L)
			.ThenBy(cell => cell.X)
			.ThenBy(cell => cell.Y)
			.ThenBy(cell => cell.Z)
			.Select(cell => new CellBlobs(cell, BlobCount(seed, cell)))
			.ToArray();
		var blobCount = orderedCells.Sum(entry => entry.Count);
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true,
			InstanceCount = blobCount,
			Mesh = _blobMesh,
		};
		if (revealCenter is not null)
			multiMesh.VisibleInstanceCount = 0;

		var instanceIndex = 0;
		foreach (var entry in orderedCells)
		{
			for (var localIndex = 0; localIndex < entry.Count; localIndex++)
			{
				var diameter = BlobDiameter(seed, entry.Cell, localIndex);
				var shapeRng = CreateRng(seed, entry.Cell, localIndex, ShapeSalt);
				var shape = IsEdgeBlob(localIndex)
					? shapeRng.RandfRange(0.2f, 0.8f)
					: shapeRng.Randf();
				var basis = CreateBlobBasis(
					CreateRng(seed, entry.Cell, localIndex, BasisSalt),
					diameter,
					shape);
				var position = CreateBlobPosition(seed, entry.Cell, localIndex, diameter);
				var customDataRng = CreateRng(seed, entry.Cell, localIndex, CustomDataSalt);
				var animationRng = CreateRng(
					seed,
					entry.Cell,
					localIndex % MediumBlobsPerCell,
					AnimationSalt);
				multiMesh.SetInstanceTransform(
					instanceIndex,
					new Transform3D(basis, position - basis * _blobBounds.GetCenter()));
				multiMesh.SetInstanceCustomData(
					instanceIndex,
					new Color(
						animationRng.Randf(),
						customDataRng.RandfRange(0.35f, 1f),
						customDataRng.Randf(),
						shape));
				instanceIndex++;
			}
		}

		_blobs = new MultiMeshInstance3D
		{
			Name = "GoopBlobs",
			Multimesh = multiMesh,
			MaterialOverride = _blobMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(_blobs);
		AddChild(_blobs);

		if (revealCenter is null)
			return;

		_revealElapsed = 0f;
		_revealDuration = revealDuration;
		_revealBlobCount = blobCount;
		SetProcess(true);
	}

	public void Clear()
	{
		RemoveBlobs();
		_cells.Clear();
		_seed = 0;
		_hasBuild = false;
	}

	private void RemoveBlobs()
	{
		SetProcess(false);
		_revealElapsed = 0f;
		_revealDuration = 0f;
		_revealBlobCount = 0;
		_blobs?.Free();
		_blobs = null;
	}

	private static long DistanceSquared(Coord cell, Coord center)
	{
		var x = (long)cell.X - center.X;
		var y = (long)cell.Y - center.Y;
		var z = (long)cell.Z - center.Z;
		return x * x + y * y + z * z;
	}

	private static int BlobCount(ulong seed, Coord cell) =>
		CreateRng(seed, cell, -1).RandiRange(MinBlobsPerCell, MaxBlobsPerCell);

	private static float BlobDiameter(ulong seed, Coord cell, int localIndex)
	{
		var rng = CreateRng(seed, cell, localIndex, DiameterSalt);
		var relativeDiameter = localIndex < MediumBlobsPerCell
			? rng.RandfRange(MediumDiameterMin, MediumDiameterMax)
			: IsEdgeBlob(localIndex)
				? rng.RandfRange(EdgeDiameterMin, EdgeDiameterMax)
				: rng.RandfRange(SmallDiameterMin, SmallDiameterMax);
		return WorldMapping.CellSize * relativeDiameter;
	}

	private static Basis CreateBlobBasis(RandomNumberGenerator rng, float diameter, float shape)
	{
		var normalizedScale = diameter / _blobBounds.Size.Length();
		var proportions = shape switch
		{
			< 0.34f => new Vector3(
				rng.RandfRange(0.72f, 1f),
				rng.RandfRange(0.72f, 1f),
				rng.RandfRange(0.72f, 1f)),
			< 0.68f => new Vector3(
				rng.RandfRange(0.34f, 0.5f),
				1f,
				rng.RandfRange(0.38f, 0.58f)),
			_ => new Vector3(
				1f,
				rng.RandfRange(0.42f, 0.62f),
				rng.RandfRange(0.65f, 0.9f)),
		};
		var rotation = Basis.FromEuler(new Vector3(
			rng.RandfRange(0f, Mathf.Tau),
			rng.RandfRange(0f, Mathf.Tau),
			rng.RandfRange(0f, Mathf.Tau)));
		return rotation.ScaledLocal(proportions * normalizedScale);
	}

	private static Vector3 CreateBlobPosition(
		ulong seed,
		Coord cell,
		int localIndex,
		float diameter)
	{
		var freePosition = FreePosition(
			CreateRng(seed, cell, localIndex, PositionSalt),
			cell,
			diameter);
		if (localIndex < MediumBlobsPerCell)
			return freePosition;
		if (IsEdgeBlob(localIndex))
			return EdgePosition(seed, cell, localIndex, diameter);

		var clusterRng = CreateRng(seed, cell, localIndex, ClusterSalt);
		if (clusterRng.Randf() > ClusteredSmallChance)
			return freePosition;

		var anchorIndex = localIndex % MediumBlobsPerCell;
		var anchorDiameter = BlobDiameter(seed, cell, anchorIndex);
		var anchor = FreePosition(
			CreateRng(seed, cell, anchorIndex, PositionSalt),
			cell,
			anchorDiameter);
		var clustered = anchor + RandomUnit(clusterRng)
			* clusterRng.RandfRange(anchorDiameter * 0.18f, anchorDiameter * 0.48f);
		return ClampToCell(clustered, cell, diameter);
	}

	private static bool IsEdgeBlob(int localIndex) =>
		localIndex >= FirstEdgeBlobIndex
		&& localIndex < FirstEdgeBlobIndex + EdgeBlobCount;

	private static Vector3 EdgePosition(
		ulong seed,
		Coord cell,
		int localIndex,
		float diameter)
	{
		var face = localIndex - FirstEdgeBlobIndex;
		var axis = face switch
		{
			0 => Vector3.Left,
			1 => Vector3.Right,
			2 => Vector3.Down,
			3 => Vector3.Up,
			4 => Vector3.Forward,
			_ => Vector3.Back,
		};
		var rng = CreateRng(seed, cell, localIndex, PositionSalt);
		var transverse = face switch
		{
			0 or 1 => new Vector3(
				0f,
				rng.RandfRange(-1f, 1f),
				rng.RandfRange(-1f, 1f)),
			2 or 3 => new Vector3(
				rng.RandfRange(-1f, 1f),
				0f,
				rng.RandfRange(-1f, 1f)),
			_ => new Vector3(
				rng.RandfRange(-1f, 1f),
				rng.RandfRange(-1f, 1f),
				0f),
		};
		return WorldMapping.ToWorld(cell)
			+ axis * (WorldMapping.CellSize * 0.5f - diameter * 0.08f)
			+ transverse * (WorldMapping.CellSize * 0.13f);
	}

	private static Vector3 FreePosition(RandomNumberGenerator rng, Coord cell, float diameter)
	{
		var availableOffset = Mathf.Max(
			0f,
			(WorldMapping.CellSize - diameter) * 0.5f - CellMargin);
		return WorldMapping.ToWorld(cell) + new Vector3(
			rng.RandfRange(-availableOffset, availableOffset),
			rng.RandfRange(-availableOffset, availableOffset),
			rng.RandfRange(-availableOffset, availableOffset));
	}

	private static Vector3 ClampToCell(Vector3 position, Coord cell, float diameter)
	{
		var center = WorldMapping.ToWorld(cell);
		var availableOffset = Mathf.Max(
			0f,
			(WorldMapping.CellSize - diameter) * 0.5f - CellMargin);
		return new Vector3(
			Mathf.Clamp(position.X, center.X - availableOffset, center.X + availableOffset),
			Mathf.Clamp(position.Y, center.Y - availableOffset, center.Y + availableOffset),
			Mathf.Clamp(position.Z, center.Z - availableOffset, center.Z + availableOffset));
	}

	private static Vector3 RandomUnit(RandomNumberGenerator rng)
	{
		var y = rng.RandfRange(-1f, 1f);
		var angle = rng.RandfRange(0f, Mathf.Tau);
		var radius = Mathf.Sqrt(1f - y * y);
		return new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));
	}

	private static RandomNumberGenerator CreateRng(
		ulong seed,
		Coord cell,
		int localIndex,
		ulong salt = 0)
	{
		var mixed = Mix(seed ^ 0x9E3779B97F4A7C15UL);
		mixed = Mix(mixed ^ unchecked((ulong)(uint)cell.X));
		mixed = Mix(mixed ^ unchecked((ulong)(uint)cell.Y));
		mixed = Mix(mixed ^ unchecked((ulong)(uint)cell.Z));
		mixed = Mix(mixed ^ unchecked((ulong)(uint)localIndex));
		mixed = Mix(mixed ^ salt);
		return new RandomNumberGenerator { Seed = mixed };
	}

	private static ulong Mix(ulong value)
	{
		value ^= value >> 30;
		value *= 0xBF58476D1CE4E5B9UL;
		value ^= value >> 27;
		value *= 0x94D049BB133111EBUL;
		return value ^ (value >> 31);
	}

	private static void EnsureResources()
	{
		if (_blobMesh is not null && _blobMaterial is not null)
			return;

		_blobMesh = new SphereMesh
		{
			Radius = 0.5f,
			Height = 1f,
			RadialSegments = 10,
			Rings = 6,
		};
		_blobBounds = _blobMesh.GetAabb();
		_blobMaterial = CreateBlobMaterial();
	}

	private static ShaderMaterial CreateBlobMaterial() =>
		new()
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;
					render_mode diffuse_burley, specular_schlick_ggx, shadows_disabled;

					varying float blob_variation;

					void vertex()
					{
						float phase = INSTANCE_CUSTOM.r * 6.283185;
						float shape = INSTANCE_CUSTOM.a;
						float pulse_strength = mix(0.012, 0.035, INSTANCE_CUSTOM.g);
						float pulse = sin(TIME * 1.15 + phase) * pulse_strength;
						vec3 float_offset = vec3(
							sin(TIME * 0.31 + phase),
							cos(TIME * 0.27 + phase * 1.37),
							sin(TIME * 0.29 + phase * 0.73)
						) * vec3(0.055, 0.07, 0.05);
						VERTEX += inverse(mat3(MODEL_MATRIX)) * float_offset;
						float organic = sin(VERTEX.x * 7.0 + phase)
							* sin(VERTEX.y * 6.0 - phase * 0.7)
							* sin(VERTEX.z * 7.0 + phase * 1.3)
							* 0.045;
						if (shape >= 0.34 && shape < 0.68)
						{
							float ends = smoothstep(0.08, 0.5, abs(VERTEX.y));
							VERTEX.xz *= mix(0.7, 1.12, ends);
						}
						else if (shape >= 0.68)
						{
							float taper = smoothstep(-0.5, 0.5, VERTEX.y);
							VERTEX.xz *= mix(1.18, 0.52, taper);
							VERTEX.y += (1.0 - taper) * 0.08;
						}
						float sway = sin(TIME * 0.58 + phase * 1.17)
							* (0.02 + 0.025 * INSTANCE_CUSTOM.g);
						VERTEX.xz += vec2(sway, -sway * 0.65) * VERTEX.y;
						VERTEX += NORMAL * (pulse + organic);
						blob_variation = INSTANCE_CUSTOM.b;
					}

					void fragment()
					{
						vec3 dark_green = vec3(0.10, 0.42, 0.08);
						vec3 goop_green = vec3(0.24, 0.78, 0.13);
						vec3 highlight_green = vec3(0.55, 1.00, 0.25);
						float fresnel = pow(
							1.0 - max(dot(normalize(NORMAL), normalize(VIEW)), 0.0),
							3.0
						);
						ALBEDO = mix(dark_green, goop_green, 0.55 + blob_variation * 0.4);
						ALBEDO = mix(ALBEDO, highlight_green, fresnel * 0.28);
						ROUGHNESS = 0.36;
						METALLIC = 0.0;
						SPECULAR = 0.48;
						EMISSION = goop_green * (0.018 + blob_variation * 0.012);
					}
					""",
			},
		};

	private readonly record struct CellBlobs(Coord Cell, int Count);
}
