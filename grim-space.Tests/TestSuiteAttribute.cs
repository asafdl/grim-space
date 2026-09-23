namespace GrimSpace.Tests;

public sealed class BattleTestSuiteAttribute() : TraitAttribute(TestSuites.TraitName, TestSuites.Battle);

public sealed class StarSystemTestSuiteAttribute() : TraitAttribute(TestSuites.TraitName, TestSuites.StarSystem);

public sealed class IntegrationTestSuiteAttribute() : TraitAttribute(TestSuites.TraitName, TestSuites.Integration);
