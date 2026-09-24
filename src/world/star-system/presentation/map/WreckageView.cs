using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public partial class WreckageView : Node3D
{
	private const float MarkerRadius = 0.045f;
	private const float HoverScale = 1.12f;
	private static readonly Color MarkerColor = new(0.72f, 0.48f, 0.28f, 0.92f);
	private static readonly Color HoverColor = new(0.92f, 0.68f, 0.38f, 1f);

	private readonly Dictionary<string, MarkerVisual> _markers = new(StringComparer.Ordinal);
	private int _width;
	private int _height;
	private string? _hoveredContractId;

	public void Sync(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);
		_width = map.Width;
		_height = map.Height;

		var visible = WreckageVisibilityQueries
			.VisibleForHolder(map, State.PlayerFleetUnitId)
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
				visual = new MarkerVisual(BuildMarkerRoot());
				_markers[contractId] = visual;
				AddChild(visual.Root);
			}

			visual.Root.Position = MapMapping.ToWorld(site.Objective.Position, _width, _height);
		}

		if (_hoveredContractId is not null && !visible.ContainsKey(_hoveredContractId))
			_hoveredContractId = null;
	}

	public string? WreckContractAt(Coord point, StarMap map)
	{
		return WreckageVisibilityQueries.TryPickAt(map, State.PlayerFleetUnitId, point, out var wreck)
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

	private static Node3D BuildMarkerRoot()
	{
		var mesh = new SphereMesh { Radius = MarkerRadius, Height = MarkerRadius * 2f };
		var material = new StandardMaterial3D
		{
			AlbedoColor = MarkerColor,
			EmissionEnabled = true,
			Emission = MarkerColor * 0.35f,
		};
		var instance = new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		var root = new Node3D { Name = "WreckageMarker" };
		root.AddChild(instance);
		return root;
	}

	private static void ApplyStyle(MarkerVisual visual, bool hovered)
	{
		visual.Root.Scale = hovered ? Vector3.One * HoverScale : Vector3.One;
		if (visual.Root.GetChild(0) is MeshInstance3D mesh
			&& mesh.MaterialOverride is StandardMaterial3D material)
		{
			material.AlbedoColor = hovered ? HoverColor : MarkerColor;
			material.Emission = (hovered ? HoverColor : MarkerColor) * 0.35f;
		}
	}

	private sealed record MarkerVisual(Node3D Root);
}
