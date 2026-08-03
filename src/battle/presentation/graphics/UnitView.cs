using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private Label3D? _momentumLabel;
	private MeshInstance3D? _hull;
	private Color _hullColor;
	private readonly int[] _shieldPoints = new int[Faces.Length];

	public void Bind(State state, Color color)
	{
		Name = state.Id;
		_hullColor = color;
		Array.Fill(_shieldPoints, -1);

		_hull = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var nose = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateNoseMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.35f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.5f),
				EmissionEnergyMultiplier = 0.6f,
				Roughness = 0.3f,
			},
		};
		AddChild(nose);

		_momentumLabel = new Label3D
		{
			Position = new Vector3(0f, 1.2f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 48,
			OutlineSize = 8,
			Modulate = Colors.White,
		};
		AddChild(_momentumLabel);

		Sync(state);
	}

	public void Sync(State state)
	{
		Position = WorldMapping.ToWorld(state.Position);
		ApplyOrientation(state);
		ApplyShieldColors(state);
		ApplyStatus(state);
	}

	private void ApplyShieldColors(State state)
	{
		if (_hull is null)
			return;

		var maxPoints = state.Stats.MaxShieldPointsPerFace;
		foreach (var face in Faces)
		{
			var index = ShipMesh.SurfaceIndex(face);
			var points = state.ShieldPoints[face];
			if (_shieldPoints[index] == points)
				continue;

			_shieldPoints[index] = points;
			_hull.SetSurfaceOverrideMaterial(
				index,
				ShieldFaceMaterials.For(_hullColor, points, maxPoints));
		}
	}

	private void ApplyStatus(State state)
	{
		if (_momentumLabel is null)
			return;

		var evasion = (int)(MomentumConfig.ForLevel(state.MomentumLevel).Evasion * 100);
		var text = $"H{state.HullPoints} M{state.MomentumLevel} ({evasion}%)";
		if (_momentumLabel.Text == text)
			return;

		_momentumLabel.Text = text;
	}

	private void ApplyOrientation(State state)
	{
		var fore = ToVector3(state.Fore);
		var dorsal = ToVector3(state.Dorsal);
		var starboard = ToVector3(state.Starboard);
		Basis = new Basis(starboard, dorsal, fore);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
