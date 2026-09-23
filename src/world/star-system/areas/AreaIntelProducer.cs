using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.World.StarSystem.Areas;

public static class AreaIntelProducer
{
	public static (string ClosestId, string SecondClosestId, string ThirdClosestId) OrderLandmarksByDistanceFromCenter(
		Coord center,
		string landmarkAId,
		string landmarkBId,
		string landmarkCId,
		Func<string, Coord> resolvePosition)
	{
		ArgumentNullException.ThrowIfNull(resolvePosition);

		var ordered = new[] { landmarkAId, landmarkBId, landmarkCId }
			.Select(id => (Id: id, Distance: RouteGeometry.Distance(center, resolvePosition(id))))
			.OrderBy(entry => entry.Distance)
			.ToArray();

		return (ordered[0].Id, ordered[1].Id, ordered[2].Id);
	}

	private sealed record IntelLine(EAreaIntelTone Tone, string Template);

	private static readonly IntelLine[] Lines =
	[
		new(EAreaIntelTone.Brief, "Somewhere in the area of {A}."),
		new(EAreaIntelTone.Brief, "Somewhere in the area between {A} and {B}."),
	];

	public static AreaIntel Produce(AreaIntelContext context) =>
		Produce(context, allowedTones: null);

	public static AreaIntel Produce(AreaIntelContext context, IReadOnlyCollection<EAreaIntelTone>? allowedTones)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkAId);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkBId);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkCId);

		var lines = Lines;
		if (allowedTones is not null)
		{
			if (allowedTones.Count == 0)
			{
				throw new ArgumentException(
					"allowedTones must not be empty when provided.",
					nameof(allowedTones));
			}

			lines = lines.Where(line => allowedTones.Contains(line.Tone)).ToArray();
		}

		if (lines.Length == 0)
		{
			throw new ArgumentException(
				"No intel lines match the requested tone filter.",
				nameof(allowedTones));
		}

		var line = lines[Random.Shared.Next(lines.Length)];
		return new AreaIntel(
			line.Template,
			context.LandmarkAId,
			context.LandmarkBId,
			context.LandmarkCId);
	}
}
