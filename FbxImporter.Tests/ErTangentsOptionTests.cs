using System.Reactive.Linq;
using FbxImporter.ViewModels;
using SoulsAssetPipeline.FLVERImporting;
using FlverVersion = FbxImporter.ViewModels.FlverViewModel.FlverVersion;

namespace FbxImporter.Tests;

public class ErTangentsOptionTests
{
    private static FLVER2MaterialInfoBank Bank()
    {
        var bank = new FLVER2MaterialInfoBank();
        bank.MaterialDefs["test.mtd"] = new FLVER2MaterialInfoBank.MaterialDef();
        return bank;
    }

    [Fact]
    public void OptionsDefaultToErTangents()
    {
        Assert.True(new MeshImportOptions().ErTangents);
    }

    [Fact]
    public void DialogStartsTickedForEldenRing()
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), null, FlverVersion.ER);
        Assert.True(vm.ErTangents);
    }

    [Theory]
    [InlineData(FlverVersion.DS3)]
    [InlineData(FlverVersion.SDT)]
    [InlineData(FlverVersion.AC6)]
    [InlineData(FlverVersion.NR)]
    public void DialogStartsUntickedForOtherGames(FlverVersion game)
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), null, game);
        Assert.False(vm.ErTangents);
    }

    [Theory]
    [InlineData(FlverVersion.ER, false)]
    [InlineData(FlverVersion.DS3, true)]
    public void DialogKeepsTheCachedChoiceForTheSameGame(FlverVersion game, bool cached)
    {
        var cache = new MeshImportOptions { ErTangents = cached, Game = game };
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), cache, game);
        Assert.Equal(cached, vm.ErTangents);
    }

    [Theory]
    [InlineData(FlverVersion.DS3, FlverVersion.ER, true)]
    [InlineData(FlverVersion.ER, FlverVersion.DS3, false)]
    public void DialogIgnoresTheCachedChoiceOfAnotherGame(FlverVersion cachedGame, FlverVersion game, bool expected)
    {
        var cache = new MeshImportOptions { ErTangents = !expected, Game = cachedGame };
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), cache, game);
        Assert.Equal(expected, vm.ErTangents);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmCarriesTheTickBoxAndTheGame(bool ticked)
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), null, FlverVersion.SDT) { ErTangents = ticked };
        MeshImportOptions options = await vm.ConfirmCommand.Execute();
        Assert.Equal(ticked, options.ErTangents);
        Assert.Equal(FlverVersion.SDT, options.Game);
    }
}
