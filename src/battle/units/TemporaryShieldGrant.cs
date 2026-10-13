using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public sealed class TemporaryShieldGrant
{
	public required string GrantId { get; init; }
	public FaceShieldPoints Remaining { get; set; } = new();

	public TemporaryShieldGrant Clone() =>
		new()
		{
			GrantId = GrantId,
			Remaining = Remaining.Clone(),
		};
}
