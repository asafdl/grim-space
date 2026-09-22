using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

/// <summary>
/// Godot-free check that facility .tscn files define nodes matching modeled <see cref="FacilityOperator.SceneSlotId"/> values.
/// </summary>
public sealed class FacilitySceneWiringTests
{
	private static string RepoPath(string relativePath) =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", relativePath));

	[Fact]
	public void DockyardScene_DefinesModeledOperatorSlots()
	{
		var scene = File.ReadAllText(RepoPath("scenes/dockyard.tscn"));
		Assert.Contains(NodeDeclaration(TradeHub.ShopOperatorSceneSlotId), scene);
		Assert.Contains(NodeDeclaration(TradeHub.ShieldOperatorSceneSlotId), scene);
		Assert.Contains("FacilityOperatorButtonView.cs", scene);
	}

	[Fact]
	public void CommandAuthorityScene_DefinesModeledOperatorSlots()
	{
		var scene = File.ReadAllText(RepoPath("scenes/command_authority.tscn"));
		Assert.Contains(NodeDeclaration(AdministrativeCore.ContractOperatorSceneSlotId), scene);
		Assert.Contains("FacilityOperatorButtonView.cs", scene);
	}

	[Fact]
	public void WarehouseScene_DefinesModeledOperatorSlots()
	{
		var scene = File.ReadAllText(RepoPath("scenes/warehouse.tscn"));
		Assert.Contains(NodeDeclaration(StorageFacility.WarehouseManagerOperatorSceneSlotId), scene);
		Assert.Contains("FacilityOperatorButtonView.cs", scene);
	}

	[Fact]
	public void RefineryScene_DefinesModeledOperatorSlots()
	{
		var scene = File.ReadAllText(RepoPath("scenes/refinery.tscn"));
		Assert.Contains(NodeDeclaration(Refinery.RefineryOperatorSceneSlotId), scene);
		Assert.Contains("FacilityOperatorButtonView.cs", scene);
	}

	[Fact]
	public void TravelScene_DefinesModeledOperatorSlots()
	{
		var scene = File.ReadAllText(RepoPath("scenes/travel.tscn"));
		Assert.Contains(NodeDeclaration(Wormhole.TravelOperatorSceneSlotId), scene);
		Assert.Contains("FacilityOperatorButtonView.cs", scene);
	}

	private static string NodeDeclaration(string nodeName) => $"[node name=\"{nodeName}\"";
}
