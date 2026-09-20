using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Components;

/// <summary>Per-face shield capacity as stacked or inline blocks (battle HUD style).</summary>
public sealed partial class ShieldBlockBar : BoxContainer
{
	private static readonly ESpatialOrientation[] Faces =
	[
		ESpatialOrientation.Forward,
		ESpatialOrientation.Retro,
		ESpatialOrientation.Starboard,
		ESpatialOrientation.Port,
		ESpatialOrientation.Dorsal,
		ESpatialOrientation.Ventral,
	];

	private readonly ShieldBlockMetrics _metrics;
	private readonly List<(VBoxContainer Host, List<Panel> Blocks)> _faceHosts = [];

	public ShieldBlockBar(ShieldBlockBarSize size = ShieldBlockBarSize.Default)
	{
		_metrics = ShieldBlockMetrics.For(size);
		Vertical = true;
		AddThemeConstantOverride("separation", size == ShieldBlockBarSize.Compact ? 4 : 6);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void Set(FaceShieldPoints shieldPoints, FaceShieldPoints maxPoints)
	{
		Visible = maxPoints.MaxOnAnyFace > 0;
		if (maxPoints.MaxOnAnyFace <= 0)
			return;

		while (_faceHosts.Count < Faces.Length)
		{
			var host = new VBoxContainer
			{
				MouseFilter = MouseFilterEnum.Stop,
			};
			host.AddThemeConstantOverride("separation", _metrics.Separation);
			_faceHosts.Add((host, []));
			AddChild(host);
		}

		for (var faceIndex = 0; faceIndex < Faces.Length; faceIndex++)
		{
			var face = Faces[faceIndex];
			var (host, blocks) = _faceHosts[faceIndex];
			var maxPerFace = maxPoints[face];
			host.Visible = maxPerFace > 0;
			if (maxPerFace <= 0)
				continue;

			var current = System.Math.Clamp(shieldPoints[face], 0, maxPerFace);
			ShieldBlockVisuals.SyncBlocks(host, blocks, _metrics, maxPerFace, current);
		}
	}

	public void SetFaceTooltip(int faceIndex, string tooltip)
	{
		if (faceIndex < 0 || faceIndex >= _faceHosts.Count)
			return;

		_faceHosts[faceIndex].Host.TooltipText = tooltip;
	}
}
