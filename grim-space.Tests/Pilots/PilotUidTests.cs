using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

public sealed class PilotUidTests
{
	[Fact]
	public void FromId_IsStableFourUpperHex()
	{
		var uid = PilotUid.FromId("pilot-swift-fox");
		Assert.Equal(4, uid.Length);
		Assert.Matches("^[0-9A-F]{4}$", uid);
		Assert.Equal(uid, PilotUid.FromId("pilot-swift-fox"));
		Assert.NotEqual(uid, PilotUid.FromId("pilot-other-slug"));
	}
}
