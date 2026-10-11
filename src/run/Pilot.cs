using GrimSpace.Core.Ids;
using GrimSpace.Run.Persistence;
using UniqueNameGenerator;

namespace GrimSpace.Run;

public sealed class Pilot
{
	internal static readonly string[] PortraitPool =
	[
		"pilot_01",
		"pilot_02",
		"pilot_03",
		"pilot_04",
		"pilot_05",
		"pilot_06",
		"pilot_07",
		"pilot_08",
	];

	private static readonly UniqueName DisplayNames = new UniqueName(
		Adjectives.WordList,
		[Animals.WordList, Colors.WordList])
		.Separator(" ")
		.Format(Style.TitleCase);

	public string Id { get; }
	public string Name { get; }
	public string PortraitId { get; }
	public string AssignedShipId { get; }

	private Pilot(string id, string name, string portraitId, string assignedShipId)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		Id = id;
		Name = name;
		PortraitId = portraitId;
		AssignedShipId = assignedShipId;
	}

	public static Pilot CreateForShip(string assignedShipId, Random? rng = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(assignedShipId);
		rng ??= Random.Shared;
		return new Pilot(
			TypedIdGenerator.NextId("pilot"),
			DisplayNames.Generate(),
			PortraitPool[rng.Next(PortraitPool.Length)],
			assignedShipId);
	}

	internal Pilot Clone() => new(Id, Name, PortraitId, AssignedShipId);

	internal static Pilot FromPersistence(PilotPersistenceDto dto) =>
		new(dto.Id, dto.Name, dto.PortraitId, dto.AssignedShipId);

	internal PilotPersistenceDto ToPersistence() => new(Id, Name, PortraitId, AssignedShipId);
}
