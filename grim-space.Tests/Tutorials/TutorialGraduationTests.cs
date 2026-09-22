using GrimSpace.Tutorials;

namespace GrimSpace.Tests.Tutorials;

public sealed class TutorialGraduationTests
{
	[Fact]
	public void TutorialGraduation_Flow_HasAcceptStep()
	{
		var flow = TutorialGraduation.Create();
		Assert.Equal(TutorialGraduation.Id, flow.Id);
		var step = Assert.Single(flow.Steps);
		Assert.Null(step.TargetId);
		Assert.True(step.AdvanceOnAccept);
		Assert.Equal("Accept", step.Dialog.AcceptText);
		Assert.Contains("settings", step.Dialog.Message, StringComparison.OrdinalIgnoreCase);
	}
}
