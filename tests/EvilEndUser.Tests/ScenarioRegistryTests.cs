using EvilEndUser.Core.Models;
using EvilEndUser.MitreAttack;
using Xunit;

namespace EvilEndUser.Tests;

public class ScenarioRegistryTests
{
    private readonly ScenarioRegistry _registry = new();

    [Fact]
    public void Has_scenarios()
    {
        Assert.NotEmpty(_registry.All);
    }

    [Fact]
    public void All_scenario_ids_are_unique()
    {
        var ids = _registry.All.Select(s => s.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData(MitreTactic.InitialAccess)]
    [InlineData(MitreTactic.Execution)]
    [InlineData(MitreTactic.Persistence)]
    [InlineData(MitreTactic.DefenseImpairment)]
    [InlineData(MitreTactic.CredentialAccess)]
    [InlineData(MitreTactic.Discovery)]
    [InlineData(MitreTactic.LateralMovement)]
    [InlineData(MitreTactic.Collection)]
    [InlineData(MitreTactic.Exfiltration)]
    [InlineData(MitreTactic.Impact)]
    public void Covers_expected_tactics(MitreTactic tactic)
    {
        Assert.NotEmpty(_registry.ForTactic(tactic));
    }

    [Fact]
    public void FullKillChain_resolves_all_scenarios()
    {
        var chain = _registry.FullKillChain();
        Assert.NotEmpty(chain);
        Assert.All(chain, s => Assert.NotNull(_registry.FindById(s.Id)));
    }

    [Fact]
    public void Every_scenario_declares_at_least_one_technique()
    {
        Assert.All(_registry.All, s => Assert.NotEmpty(s.Techniques));
    }

    [Fact]
    public void FindById_is_case_insensitive()
    {
        var first = _registry.All[0];
        Assert.NotNull(_registry.FindById(first.Id.ToUpperInvariant()));
    }
}

public class TechniqueCatalogTests
{
    [Fact]
    public void FindById_locates_known_technique()
    {
        var t = TechniqueCatalog.FindById("T1566.002");
        Assert.NotNull(t);
        Assert.Equal(MitreTactic.InitialAccess, t!.Tactic);
    }

    [Fact]
    public void Subtechnique_url_is_well_formed()
    {
        var t = TechniqueCatalog.SpearphishingLink; // T1566.002
        Assert.Equal("https://attack.mitre.org/techniques/T1566/002/", t.Url);
    }

    [Fact]
    public void Base_technique_url_is_well_formed()
    {
        var t = TechniqueCatalog.DriveByCompromise; // T1189
        Assert.Equal("https://attack.mitre.org/techniques/T1189/", t.Url);
    }

    [Fact]
    public void All_technique_ids_are_unique()
    {
        var ids = TechniqueCatalog.All.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
