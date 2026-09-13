using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class InteractionStateTests
{
	[Fact]
	public void ClearHoversClearsAbilityHover()
	{
		var state = new InteractionState();

		state.SetAbilityHover(0, optionCount: 1);
		state.ClearHovers();

		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void AbilityHoverIsClampedAndClearedWithOtherHovers()
	{
		var state = new InteractionState();

		state.SetAbilityHover(1, optionCount: 2);
		Assert.Equal(1, state.AbilityHoveredIndex);

		state.SetAbilityHover(2, optionCount: 2);
		Assert.Null(state.AbilityHoveredIndex);

		state.SetAbilityHover(0, optionCount: 2);
		state.ClearHovers();
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void ChangingAbilityHoverClearsActionError()
	{
		var state = new InteractionState();
		state.ReportActionFailure();

		state.SetAbilityHover(0, optionCount: 1);

		Assert.Null(state.ActionError);
	}

	[Fact]
	public void SetModeClearsAbilityHoverAndActiveSpec()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.SetAbilityHover(0, optionCount: 1);
		state.SetMode(EPlayerMode.Move);

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void FocusChangeClearsAbilityTargeting()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Railgun, spec);
		state.FocusUnit("enemy");

		Assert.Equal(EPlayerMode.Move, state.Mode);
		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void ResetAfterTurnClearsAbilityTargeting()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.SetAbilityHover(0, optionCount: 1);
		state.ResetAfterTurn();

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
		Assert.Equal(EPlayerMode.Move, state.Mode);
	}
}
