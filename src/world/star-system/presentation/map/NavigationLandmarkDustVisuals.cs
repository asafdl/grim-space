using Godot;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal static class NavigationLandmarkDustVisuals
{
	public static void AddCloud(
		Node3D root,
		float worldRadius,
		RandomNumberGenerator random,
		int visualSeed,
		float opacityScale = 1f)
	{
		var texture = MapSmokeDustVisuals.LoadDefaultSmokeTexture();
		var cardCount = 5 + (int)(random.Randi() % 5);
		var lobeCount = 2 + (int)(random.Randi() % 2);
		var lobeCenters = new Vector3[lobeCount];
		for (var l = 0; l < lobeCount; l++)
		{
			var angle = random.Randf() * Mathf.Tau;
			var distance = worldRadius * (0.12f + random.Randf() * 0.22f);
			lobeCenters[l] = new Vector3(
				Mathf.Cos(angle) * distance,
				(random.Randf() - 0.5f) * worldRadius * 0.06f,
				Mathf.Sin(angle) * distance);
		}

		var quadSize = worldRadius * (0.55f + random.Randf() * 0.25f);
		var cards = new List<MapSmokeDustVisuals.SmokeDustCard>(cardCount);
		for (var i = 0; i < cardCount; i++)
		{
			var lobe = lobeCenters[i % lobeCount];
			var spread = worldRadius * (0.18f + random.Randf() * 0.2f);
			var offsetAngle = random.Randf() * Mathf.Tau;
			var position = lobe + new Vector3(
				Mathf.Cos(offsetAngle) * spread * random.Randf(),
				(random.Randf() - 0.5f) * worldRadius * 0.08f,
				Mathf.Sin(offsetAngle) * spread * random.Randf());

			var scale = 0.85f + random.Randf() * 0.55f;
			var alpha = Mathf.Clamp((0.06f + random.Randf() * 0.08f) * opacityScale, 0.04f, 0.18f);
			var vertexColor = NavigationLandmarkPalette.DustCardColor(visualSeed, i, random, alpha);
			cards.Add(new MapSmokeDustVisuals.SmokeDustCard(position, scale, vertexColor));
		}

		root.AddChild(MapSmokeDustVisuals.CreateLayer(texture, quadSize, cards, "LandmarkDust"));
	}
}
