using Godot;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class WreckageView : Node3D
{
	private const string DebrisPath = "res://assets/models/wreck_debris_pack/";
	private const float HoverScale = 1.12f;
	private static readonly Color RingColor = new(0.72f, 0.48f, 0.28f, 0.55f);
	private static readonly Color HoverRingColor = new(0.92f, 0.68f, 0.38f);
	private static readonly string[][] WreckLayouts =
	[
		["broken_fuselage", "engine_fragment", "hull_panel", "bent_armor"],
		["exposed_truss", "severed_prow", "bent_armor", "hull_panel"],
		["severed_prow", "broken_fuselage", "engine_fragment", "hull_panel"],
	];

	private readonly Dictionary<string, MarkerVisual> _markers = new(StringComparer.Ordinal);
	private int _width;
	private int _height;
	private string? _hoveredContractId;

	public void Sync(StarMap map, Func<string, ActorRuntime> runtimeFor, float tickFraction)
	{
		ArgumentNullException.ThrowIfNull(map);
		_width = map.Width;
		_height = map.Height;

		var visible = WreckageVisibilityQueries
			.VisibleForHolder(map, State.PlayerFleetUnitId, runtimeFor, tickFraction)
			.ToDictionary(site => site.ContractId, site => site, StringComparer.Ordinal);

		foreach (var contractId in _markers.Keys.Except(visible.Keys, StringComparer.Ordinal).ToArray())
		{
			_markers[contractId].Root.QueueFree();
			_markers.Remove(contractId);
		}

		foreach (var (contractId, site) in visible)
		{
			if (!_markers.TryGetValue(contractId, out var visual))
			{
				visual = BuildMarker(map.Seed, contractId);
				_markers[contractId] = visual;
				AddChild(visual.Root);
				ApplyStyle(visual, string.Equals(_hoveredContractId, contractId, StringComparison.Ordinal));
			}

			visual.Root.Position = MapMapping.ToWorld(site.Objective.Position, _width, _height);
		}

		if (_hoveredContractId is not null && !visible.ContainsKey(_hoveredContractId))
			_hoveredContractId = null;
	}

	public string? PickAtScreen(MapInteractivePick.Context context)
	{
		var map = context.Orchestrator.Map;
		string? bestContractId = null;
		var bestDirect = false;
		var bestDistance = float.MaxValue;

		foreach (var wreck in WreckageVisibilityQueries.VisibleForHolder(
			         map,
			         State.PlayerFleetUnitId,
			         context.Orchestrator.RuntimeFor,
			         context.TickFraction))
		{
			var position = wreck.Objective.Position;
			var direct = context.GridPoint is { } grid
				&& GridDistanceSquared(grid, position) <= WreckageVisibilityQueries.MapPickRadius
					* WreckageVisibilityQueries.MapPickRadius;
			var worldPosition = MapMapping.ToWorld(position, _width, _height);
			var screenDistance = MapScreenPick.DistancePixels(context.Camera, worldPosition, context.ScreenPos);
			if (!direct && screenDistance > MapScreenPick.SnapMarginPixels)
				continue;

			if (!MapScreenPick.IsBetterHit(direct, screenDistance, bestDirect, bestDistance))
				continue;

			bestDirect = direct;
			bestDistance = screenDistance;
			bestContractId = wreck.ContractId;
		}

		return bestContractId;
	}

	public string? WreckContractAt(
		Coord point,
		StarMap map,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction)
	{
		return WreckageVisibilityQueries.TryPickAt(
			map, State.PlayerFleetUnitId, point, runtimeFor, tickFraction, out var wreck)
			? wreck.ContractId
			: null;
	}

	public void SetHovered(string? contractId)
	{
		if (string.Equals(_hoveredContractId, contractId, StringComparison.Ordinal))
			return;

		if (_hoveredContractId is { } previous && _markers.TryGetValue(previous, out var previousVisual))
			ApplyStyle(previousVisual, hovered: false);

		_hoveredContractId = contractId;
		if (contractId is not null && _markers.TryGetValue(contractId, out var visual))
			ApplyStyle(visual, hovered: true);
	}

	public string TooltipFor(string contractId) => "Derelict wreck";

	private static MarkerVisual BuildMarker(int seed, string contractId)
	{
		var random = new RandomNumberGenerator
		{
			Seed = StableSeedMixer.From(seed).Add(contractId).Add("wreck-visual").Value,
		};
		var layout = WreckLayouts[(int)(random.Randi() % WreckLayouts.Length)];
		var root = new Node3D { Name = "WreckageMarker" };
		var heading = random.RandfRange(0f, Mathf.Tau);
		for (var i = 0; i < layout.Length; i++)
		{
			var path = $"{DebrisPath}{layout[i]}.glb";
			var scene = GD.Load<PackedScene>(path)
				?? throw new InvalidOperationException($"Could not load wreck debris '{path}'.");
			var part = scene.Instantiate<Node3D>();
			part.Name = $"Debris_{i}";
			part.Scale = Vector3.One * (i == 0 ? 0.055f : random.RandfRange(0.023f, 0.031f));
			var angle = heading + (i - 1) * Mathf.Tau / (layout.Length - 1)
				+ random.RandfRange(-0.35f, 0.35f);
			var radius = random.RandfRange(0.045f, 0.057f);
			part.Position = i == 0
				? new Vector3(0f, 0.02f, 0f)
				: new Vector3(Mathf.Cos(angle) * radius, random.RandfRange(0f, 0.025f),
					Mathf.Sin(angle) * radius);
			part.RotationDegrees = new Vector3(
				random.RandfRange(-15f, 15f),
				random.RandfRange(0f, 360f),
				random.RandfRange(-15f, 15f));
			root.AddChild(part);
		}

		var dust = new MapSmokeDustVisuals.SmokeDustCard[3];
		for (var i = 0; i < dust.Length; i++)
		{
			var angle = heading + i * Mathf.Tau / dust.Length + random.RandfRange(-0.4f, 0.4f);
			var radius = random.RandfRange(0.02f, 0.065f);
			dust[i] = new MapSmokeDustVisuals.SmokeDustCard(
				new Vector3(Mathf.Cos(angle) * radius, 0.01f, Mathf.Sin(angle) * radius),
				random.RandfRange(0.85f, 1.15f),
				new Color(0.45f, 0.47f, 0.48f, random.RandfRange(0.035f, 0.055f)));
		}
		root.AddChild(MapSmokeDustVisuals.CreateLayer(
			MapSmokeDustVisuals.LoadDefaultSmokeTexture(), 0.13f, dust, "WreckDust"));

		var ringMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = RingColor,
			EmissionEnabled = true,
			Emission = HoverRingColor,
			EmissionEnergyMultiplier = 0.25f,
		};
		root.AddChild(new MeshInstance3D
		{
			Name = "WreckRing",
			Mesh = new TorusMesh { InnerRadius = 0.100f, OuterRadius = 0.115f },
			Position = new Vector3(0f, 0.005f, 0f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = ringMaterial,
		});
		return new MarkerVisual(root, ringMaterial);
	}

	private static long GridDistanceSquared(Coord grid, Coord position)
	{
		var dx = (long)grid.X - position.X;
		var dz = (long)grid.Z - position.Z;
		return dx * dx + dz * dz;
	}

	private static void ApplyStyle(MarkerVisual visual, bool hovered)
	{
		visual.Root.Scale = hovered ? Vector3.One * HoverScale : Vector3.One;
		visual.RingMaterial.AlbedoColor = hovered ? HoverRingColor : RingColor;
		visual.RingMaterial.EmissionEnergyMultiplier = hovered ? 0.75f : 0.25f;
	}

	private sealed record MarkerVisual(Node3D Root, StandardMaterial3D RingMaterial);
}
