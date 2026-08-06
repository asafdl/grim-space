using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private Label3D? _momentumLabel;
	private MeshInstance3D? _hull;
	private Color _hullColor;
	private EType _type;
	private readonly int[] _shieldPoints = new int[Faces.Length];

	public void Bind(State state, Color color)
	{
		Name = state.Id;
		_hullColor = color;
		_type = state.Type;
		Array.Fill(_shieldPoints, -1);

		if (state.Type == EType.Torpedo)
			BindTorpedo(color);
		else
			BindShip(color);

		_momentumLabel = new Label3D
		{
			Position = new Vector3(0f, state.Type == EType.Torpedo ? 0.55f : 1.2f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = state.Type == EType.Torpedo ? 36 : 48,
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
		if (_type != EType.Torpedo)
			ApplyShieldColors(state);
		ApplyStatus(state);
	}

	private void BindShip(Color color)
	{
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
	}

	private void BindTorpedo(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = TorpedoMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.08f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.25f),
				EmissionEnergyMultiplier = 0.4f,
				Roughness = 0.35f,
				Metallic = 0.35f,
			},
		};
		AddChild(_hull);
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

		var text = state.Type == EType.Torpedo
			? $"H{state.HullPoints} F{state.FuelRemaining} M{state.MomentumLevel}"
			: $"H{state.HullPoints} M{state.MomentumLevel}";
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
