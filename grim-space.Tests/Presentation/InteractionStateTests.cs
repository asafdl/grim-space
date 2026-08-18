using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class InteractionStateTests
{
	[Fact]
	public void ClearHoversPreservesStagedSelection()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.StageMountedOn(ESpatialOrientation.Port);
		state.ClearHovers();

		Assert.Equal(ESpatialOrientation.Port, state.StagedMountedOn);
	}

	[Fact]
	public void SetModeClearsStagingAndActiveSpec()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.StageMountedOn(ESpatialOrientation.Port);
		state.SetMode(EPlayerMode.Move);

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.StagedMountedOn);
	}

	[Fact]
	public void StageMountedOnIgnoresDuplicateOrientation()
	{
		var state = new InteractionState();
		state.StageMountedOn(ESpatialOrientation.Port);
		state.StageMountedOn(ESpatialOrientation.Port);

		Assert.Equal(ESpatialOrientation.Port, state.StagedMountedOn);
	}

	[Fact]
	public void FocusChangeClearsStaging()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Railgun, spec);
		state.FocusUnit("enemy");

		Assert.Equal(EPlayerMode.Move, state.Mode);
		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.StagedMountedOn);
	}

	[Fact]
	public void ResetAfterTurnClearsStaging()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Torpedo, spec);
		state.StageMountedOn(ESpatialOrientation.Dorsal);
		state.ResetAfterTurn();

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.StagedMountedOn);
		Assert.Equal(EPlayerMode.Move, state.Mode);
	}
}
